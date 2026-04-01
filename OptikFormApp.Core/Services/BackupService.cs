using OptikFormApp.Core.Models;
using OptikFormApp.Core.Interfaces;

namespace OptikFormApp.Core.Services;

public class BackupService
{
    private readonly ILoggingService _logger;
    private readonly IAppSettingsService _settingsService;

    public BackupService(ILoggingService logger, IAppSettingsService settingsService)
    {
        _logger = logger;
        _settingsService = settingsService;
    }

    public async Task<bool> CreateBackupAsync()
    {
        try
        {
            var settings = _settingsService.LoadSettings();
            var sourcePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, settings.DatabasePath);
            var backupDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, settings.BackupPath);
            
            Directory.CreateDirectory(backupDir);

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var backupFileName = $"optik_backup_{timestamp}.db";
            var backupPath = Path.Combine(backupDir, backupFileName);

            if (!File.Exists(sourcePath))
            {
                _logger.LogWarning("Source database file not found: {SourcePath}", sourcePath);
                return false;
            }

            // Copy database file
            File.Copy(sourcePath, backupPath, true);
            
            _logger.LogInformation("Backup created successfully: {BackupPath}", backupPath);
            
            // Clean old backups
            await CleanOldBackupsAsync(backupDir);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create backup");
            return false;
        }
    }

    private async Task CleanOldBackupsAsync(string backupDir)
    {
        try
        {
            var settings = _settingsService.LoadSettings();
            var retentionDays = 30; // Keep backups for 30 days
            
            var backupFiles = Directory.GetFiles(backupDir, "optik_backup_*.db")
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.CreationTime)
                .ToList();

            var filesToDelete = backupFiles
                .Where(f => f.CreationTime < DateTime.Now.AddDays(-retentionDays))
                .ToList();

            foreach (var file in filesToDelete)
            {
                try
                {
                    File.Delete(file.FullName);
                    _logger.LogInformation("Deleted old backup: {FileName}", file.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Failed to delete old backup: {FileName}", file.Name);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to clean old backups");
        }
    }

    public List<string> GetAvailableBackups()
    {
        try
        {
            var settings = _settingsService.LoadSettings();
            var backupDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, settings.BackupPath);
            
            if (!Directory.Exists(backupDir))
                return new List<string>();

            return Directory.GetFiles(backupDir, "optik_backup_*.db")
                .Select(Path.GetFileName)
                .Where(name => name != null)
                .OrderByDescending(name => name)
                .ToList()!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get available backups");
            return new List<string>();
        }
    }

    public async Task<bool> RestoreBackupAsync(string backupFileName)
    {
        try
        {
            var settings = _settingsService.LoadSettings();
            var backupDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, settings.BackupPath);
            var backupPath = Path.Combine(backupDir, backupFileName);
            var targetPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, settings.DatabasePath);

            if (!File.Exists(backupPath))
            {
                _logger.LogWarning("Backup file not found: {BackupPath}", backupPath);
                return false;
            }

            // Create backup of current database before restore
            var currentBackupSuccess = await CreateBackupAsync();
            if (!currentBackupSuccess)
            {
                _logger.LogWarning("Failed to create backup before restore");
            }

            // Restore from backup
            File.Copy(backupPath, targetPath, true);
            
            _logger.LogInformation("Database restored successfully from: {BackupFileName}", backupFileName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore backup: {BackupFileName}", backupFileName);
            return false;
        }
    }
}
