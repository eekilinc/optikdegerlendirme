using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Diagnostics;

namespace OptikFormApp.Services
{
    /// <summary>
    /// GitHub releases üzerinden güncelleme kontrolü yapan servis
    /// </summary>
    public class VersionCheckService
    {
        private readonly HttpClient _httpClient;
        private readonly string _githubApiUrl = "https://api.github.com/repos/eekilinc/optikdegerlendirme/releases/latest";
        
        public VersionCheckService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "OptikDegerlendirme-App");
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
        }

        /// <summary>
        /// Mevcut versiyon ile GitHub'daki son versiyonu karşılaştırır
        /// </summary>
        public async Task<VersionCheckResult> CheckForUpdateAsync(string currentVersion)
        {
            try
            {
                // TLS 1.2 ve üzeri kullan
                System.Net.ServicePointManager.SecurityProtocol = 
                    System.Net.SecurityProtocolType.Tls12 | 
                    System.Net.SecurityProtocolType.Tls13;
                
                var response = await _httpClient.GetAsync(_githubApiUrl);
                
                if (!response.IsSuccessStatusCode)
                {
                    string errorMessage = response.StatusCode switch
                    {
                        System.Net.HttpStatusCode.NotFound => "GitHub'da proje bulunamadı. Repo henüz oluşturulmamış olabilir.",
                        System.Net.HttpStatusCode.Forbidden => "GitHub API erişim izni reddedildi. API limiti aşılmış olabilir.",
                        System.Net.HttpStatusCode.Unauthorized => "GitHub API yetkilendirme hatası.",
                        _ => $"GitHub API hatası (HTTP {(int)response.StatusCode}). İnternet bağlantınızı kontrol edin."
                    };
                    
                    return new VersionCheckResult
                    {
                        HasUpdate = false,
                        ErrorMessage = errorMessage
                    };
                }

                var json = await response.Content.ReadAsStringAsync();
                var release = JsonSerializer.Deserialize<GitHubRelease>(json);

                if (release == null || string.IsNullOrEmpty(release.TagName))
                {
                    return new VersionCheckResult
                    {
                        HasUpdate = false,
                        ErrorMessage = "Versiyon bilgisi alınamadı"
                    };
                }

                // v1.2.3 formatından 1.2.3 formatına çevir
                string latestVersion = release.TagName.TrimStart('v', 'V');
                
                bool hasUpdate = IsNewerVersion(latestVersion, currentVersion);

                return new VersionCheckResult
                {
                    HasUpdate = hasUpdate,
                    CurrentVersion = currentVersion,
                    LatestVersion = latestVersion,
                    DownloadUrl = release.Assets?.FirstOrDefault()?.BrowserDownloadUrl ?? "",
                    ReleaseUrl = release.HtmlUrl,
                    ReleaseNotes = release.Body,
                    PublishedAt = release.PublishedAt
                };
            }
            catch (HttpRequestException ex)
            {
                return new VersionCheckResult
                {
                    HasUpdate = false,
                    ErrorMessage = "İnternet bağlantısı hatası. Lütfen internet bağlantınızı kontrol edip tekrar deneyin."
                };
            }
            catch (TaskCanceledException)
            {
                return new VersionCheckResult
                {
                    HasUpdate = false,
                    ErrorMessage = "Bağlantı zaman aşımına uğradı. Lütfen daha sonra tekrar deneyin."
                };
            }
            catch (Exception ex)
            {
                return new VersionCheckResult
                {
                    HasUpdate = false,
                    ErrorMessage = $"Kontrol hatası: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Versiyon karşılaştırması yapar (1.2.3 > 1.2.2 = true)
        /// </summary>
        private bool IsNewerVersion(string latest, string current)
        {
            try
            {
                var latestParts = latest.Split('.');
                var currentParts = current.Split('.');

                int maxLength = Math.Max(latestParts.Length, currentParts.Length);

                for (int i = 0; i < maxLength; i++)
                {
                    int latestPart = i < latestParts.Length ? int.Parse(latestParts[i]) : 0;
                    int currentPart = i < currentParts.Length ? int.Parse(currentParts[i]) : 0;

                    if (latestPart > currentPart) return true;
                    if (latestPart < currentPart) return false;
                }

                return false; // Eşit versiyonlar
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// GitHub releases sayfasını tarayıcıda açar
        /// </summary>
        public void OpenReleasesPage()
        {
            var psi = new ProcessStartInfo
            {
                FileName = "https://github.com/eekilinc/optikdegerlendirme/releases",
                UseShellExecute = true
            };
            Process.Start(psi);
        }
    }

    /// <summary>
    /// GitHub API'den gelen release bilgisi
    /// </summary>
    public class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; } = string.Empty;

        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; set; } = string.Empty;

        [JsonPropertyName("body")]
        public string Body { get; set; } = string.Empty;

        [JsonPropertyName("published_at")]
        public DateTime PublishedAt { get; set; }

        [JsonPropertyName("assets")]
        public List<GitHubAsset> Assets { get; set; } = new();
    }

    /// <summary>
    /// GitHub release asset bilgisi
    /// </summary>
    public class GitHubAsset
    {
        [JsonPropertyName("browser_download_url")]
        public string BrowserDownloadUrl { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// Güncelleme kontrolü sonucu
    /// </summary>
    public class VersionCheckResult
    {
        public bool HasUpdate { get; set; }
        public string CurrentVersion { get; set; } = string.Empty;
        public string LatestVersion { get; set; } = string.Empty;
        public string ReleaseUrl { get; set; } = string.Empty;
        public string ReleaseNotes { get; set; } = string.Empty;
        public DateTime PublishedAt { get; set; }
        public string DownloadUrl { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;

        public string StatusText => HasUpdate 
            ? $"Yeni sürüm mevcut: {LatestVersion}" 
            : "Uygulama güncel";
    }
}
