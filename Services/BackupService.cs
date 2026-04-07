using System;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace OptikFormApp.Services
{
    /// <summary>
    /// Tam yedekleme ve geri yükleme servisi
    /// Veritabanı + ayarlar + şablonlar tek zip dosyasında
    /// </summary>
    public class BackupService
    {
        private readonly DatabaseService _dbService;
        private readonly AppSettingsService _settingsService;
        private readonly NotificationService _notificationService;
        private readonly JsonDataService _jsonDataService;

        private static readonly string AppDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "OptikDegerlendirme");

        private static readonly string SettingsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "OptikFormApp");

        public BackupService(
            DatabaseService dbService,
            AppSettingsService settingsService,
            NotificationService notificationService,
            JsonDataService jsonDataService)
        {
            _dbService = dbService;
            _settingsService = settingsService;
            _notificationService = notificationService;
            _jsonDataService = jsonDataService;
        }

        /// <summary>
        /// Tam yedek oluşturur (DB + Settings + Metadata)
        /// </summary>
        public async Task<string> CreateFullBackupAsync(string? customPath = null)
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var backupName = $"OptikBackup_{timestamp}.zip";
            var backupPath = customPath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), backupName);

            var tempDir = Path.Combine(Path.GetTempPath(), $"OptikBackup_{timestamp}");
            Directory.CreateDirectory(tempDir);

            try
            {
                // 1. Veritabanı dosyasını kopyala
                var dbSource = Path.Combine(AppDataPath, "optik.db");
                if (File.Exists(dbSource))
                {
                    File.Copy(dbSource, Path.Combine(tempDir, "optik.db"), true);
                }

                // 2. Ayarları JSON olarak kaydet
                var settings = _settingsService.Load();
                var settingsJson = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(Path.Combine(tempDir, "settings.json"), settingsJson);

                // 3. Metadata oluştur
                var metadata = new BackupMetadata
                {
                    BackupDate = DateTime.Now,
                    AppVersion = GetAppVersion(),
                    DatabaseVersion = await GetDatabaseVersionAsync(),
                    Description = "Tam Yedek",
                    BackupType = "Full"
                };
                var metaJson = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(Path.Combine(tempDir, "metadata.json"), metaJson);

                // 4. Tüm sınav verilerini JSON olarak dışa aktar
                var exportData = await _jsonDataService.ExportAllDataAsync();
                await File.WriteAllTextAsync(Path.Combine(tempDir, "data_export.json"), exportData);

                // 5. Zip dosyası oluştur
                if (File.Exists(backupPath))
                    File.Delete(backupPath);

                ZipFile.CreateFromDirectory(tempDir, backupPath, CompressionLevel.Optimal, false);

                _notificationService.ShowSuccess($"Yedekleme tamamlandı: {backupPath}");
                return backupPath;
            }
            finally
            {
                // Temizlik
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        /// <summary>
        /// Sadece veritabanı yedeği
        /// </summary>
        public async Task<string> CreateDatabaseBackupAsync(string? customPath = null)
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var backupName = $"OptikDB_{timestamp}.db";
            var backupPath = customPath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), backupName);

            var dbSource = Path.Combine(AppDataPath, "optik.db");
            if (!File.Exists(dbSource))
                throw new FileNotFoundException("Veritabanı dosyası bulunamadı");

            // SQLite backup: checkpoint + copy
            await _dbService.BackupDatabaseAsync(backupPath);

            _notificationService.ShowSuccess($"Veritabanı yedeği oluşturuldu: {backupPath}");
            return backupPath;
        }

        /// <summary>
        /// Sadece ayarları dışa aktar
        /// </summary>
        public async Task<string> ExportSettingsAsync(string? customPath = null)
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var fileName = $"OptikSettings_{timestamp}.json";
            var exportPath = customPath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);

            var settings = _settingsService.Load();
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(exportPath, json);

            _notificationService.ShowSuccess($"Ayarlar dışa aktarıldı: {exportPath}");
            return exportPath;
        }

        /// <summary>
        /// Tam yedeği geri yükle
        /// </summary>
        public async Task RestoreFullBackupAsync(string zipPath, bool overwriteExisting = false)
        {
            if (!File.Exists(zipPath))
                throw new FileNotFoundException("Yedek dosyası bulunamadı", zipPath);

            var tempDir = Path.Combine(Path.GetTempPath(), $"OptikRestore_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);

            try
            {
                // Zip'i aç
                ZipFile.ExtractToDirectory(zipPath, tempDir, true);

                // Metadata kontrolü
                var metaPath = Path.Combine(tempDir, "metadata.json");
                if (File.Exists(metaPath))
                {
                    var metaJson = await File.ReadAllTextAsync(metaPath);
                    var metadata = JsonSerializer.Deserialize<BackupMetadata>(metaJson);
                    // İleride versiyon kontrolü eklenebilir
                }

                // Mevcut veriyi yedekle (güvenlik için)
                var safetyBackup = Path.Combine(AppDataPath, $"pre_restore_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db");
                var dbCurrent = Path.Combine(AppDataPath, "optik.db");
                if (File.Exists(dbCurrent))
                {
                    File.Copy(dbCurrent, safetyBackup, true);
                }

                // Veritabanını geri yükle
                var dbBackup = Path.Combine(tempDir, "optik.db");
                if (File.Exists(dbBackup))
                {
                    if (overwriteExisting || !File.Exists(dbCurrent))
                    {
                        // DB'yi kapat ve değiştir
                        await _dbService.CloseConnectionAsync();
                        File.Copy(dbBackup, dbCurrent, true);
                        await _dbService.ReconnectAsync();
                    }
                }

                // Ayarları geri yükle
                var settingsPath = Path.Combine(tempDir, "settings.json");
                if (File.Exists(settingsPath))
                {
                    var settingsJson = await File.ReadAllTextAsync(settingsPath);
                    var settings = JsonSerializer.Deserialize<Models.AppSettings>(settingsJson);
                    if (settings != null)
                    {
                        _settingsService.Save(settings);
                    }
                }

                _notificationService.ShowSuccess("Yedek başarıyla geri yüklendi. Uygulama yeniden başlatılmalı.");
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        /// <summary>
        /// Veritabanını geri yükle
        /// </summary>
        public async Task RestoreDatabaseAsync(string dbPath)
        {
            if (!File.Exists(dbPath))
                throw new FileNotFoundException("Veritabanı dosyası bulunamadı", dbPath);

            // Güvenlik yedeği
            var safetyBackup = Path.Combine(AppDataPath, $"pre_restore_{DateTime.Now:yyyyMMdd_HHmmss}.db");
            var dbCurrent = Path.Combine(AppDataPath, "optik.db");
            if (File.Exists(dbCurrent))
            {
                File.Copy(dbCurrent, safetyBackup, true);
            }

            // DB'yi kapat ve değiştir
            await _dbService.CloseConnectionAsync();
            File.Copy(dbPath, dbCurrent, true);
            await _dbService.ReconnectAsync();

            _notificationService.ShowSuccess("Veritabanı geri yüklendi.");
        }

        /// <summary>
        /// Ayarları içe aktar
        /// </summary>
        public async Task ImportSettingsAsync(string jsonPath)
        {
            if (!File.Exists(jsonPath))
                throw new FileNotFoundException("Ayar dosyası bulunamadı", jsonPath);

            var json = await File.ReadAllTextAsync(jsonPath);
            var settings = JsonSerializer.Deserialize<Models.AppSettings>(json);
            if (settings == null)
                throw new InvalidDataException("Ayar dosyası geçersiz");

            _settingsService.Save(settings);
            _notificationService.ShowSuccess("Ayarlar içe aktarıldı.");
        }

        /// <summary>
        /// Yedek dosyası bilgilerini oku
        /// </summary>
        public async Task<BackupInfo?> GetBackupInfoAsync(string zipPath)
        {
            if (!File.Exists(zipPath))
                return null;

            var tempDir = Path.Combine(Path.GetTempPath(), $"OptikInfo_{Guid.NewGuid():N}");
            try
            {
                ZipFile.ExtractToDirectory(zipPath, tempDir);
                var metaPath = Path.Combine(tempDir, "metadata.json");
                if (File.Exists(metaPath))
                {
                    var json = await File.ReadAllTextAsync(metaPath);
                    var metadata = JsonSerializer.Deserialize<BackupMetadata>(json);
                    if (metadata != null)
                    {
                        var fileInfo = new FileInfo(zipPath);
                        return new BackupInfo
                        {
                            FilePath = zipPath,
                            FileSize = fileInfo.Length,
                            CreatedDate = metadata.BackupDate,
                            AppVersion = metadata.AppVersion,
                            DatabaseVersion = metadata.DatabaseVersion,
                            Description = metadata.Description,
                            BackupType = metadata.BackupType
                        };
                    }
                }
                return null;
            }
            catch
            {
                return null;
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        /// <summary>
        /// Mevcut yedekleri listele
        /// </summary>
        public List<BackupInfo> ListBackups(string directory)
        {
            var backups = new List<BackupInfo>();
            if (!Directory.Exists(directory))
                return backups;

            var files = Directory.GetFiles(directory, "OptikBackup_*.zip");
            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                backups.Add(new BackupInfo
                {
                    FilePath = file,
                    FileSize = fileInfo.Length,
                    CreatedDate = fileInfo.CreationTime,
                    AppVersion = "Bilinmiyor",
                    BackupType = "Full"
                });
            }

            return backups.OrderByDescending(b => b.CreatedDate).ToList();
        }

        private string GetAppVersion()
        {
            return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0.0";
        }

        private async Task<string> GetDatabaseVersionAsync()
        {
            try
            {
                var version = await _dbService.GetUserVersionAsync();
                return version.ToString();
            }
            catch
            {
                return "0";
            }
        }
    }

    public class BackupMetadata
    {
        public DateTime BackupDate { get; set; }
        public string AppVersion { get; set; } = "";
        public string DatabaseVersion { get; set; } = "";
        public string Description { get; set; } = "";
        public string BackupType { get; set; } = "Full";
    }

    public class BackupInfo
    {
        public string FilePath { get; set; } = "";
        public long FileSize { get; set; }
        public DateTime CreatedDate { get; set; }
        public string AppVersion { get; set; } = "";
        public string DatabaseVersion { get; set; } = "";
        public string Description { get; set; } = "";
        public string BackupType { get; set; } = "";

        public string FileSizeFormatted => FormatBytes(FileSize);
        public string CreatedDateFormatted => CreatedDate.ToString("dd.MM.yyyy HH:mm");

        private static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}
