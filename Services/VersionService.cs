using System;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace OptikFormApp.Services
{
    public class VersionService
    {
        private readonly HttpClient _httpClient;
        private const string GITHUB_OWNER = "eekilinc";
        private const string GITHUB_REPO = "optikdegerlendirme";
        
        public VersionService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "OptikDegerlendirme-App");
        }
        
        public string CurrentVersion
        {
            get
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                return $"v{version?.Major}.{version?.Minor}.{version?.Build}";
            }
        }
        
        public async Task<VersionCheckResult> CheckForUpdateAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync(
                    $"https://api.github.com/repos/{GITHUB_OWNER}/{GITHUB_REPO}/releases/latest");
                
                if (!response.IsSuccessStatusCode)
                    return new VersionCheckResult { HasUpdate = false };
                
                var json = await response.Content.ReadAsStringAsync();
                var release = JObject.Parse(json);
                
                var latestVersion = release["tag_name"]?.ToString() ?? "";
                var current = CurrentVersion.TrimStart('v');
                var latest = latestVersion.TrimStart('v');
                
                var hasUpdate = Version.Parse(latest) > Version.Parse(current);
                
                return new VersionCheckResult
                {
                    HasUpdate = hasUpdate,
                    LatestVersion = latestVersion,
                    ReleaseUrl = release["html_url"]?.ToString() ?? "",
                    ReleaseNotes = release["body"]?.ToString() ?? "",
                    DownloadUrl = release["assets"]?.First?["browser_download_url"]?.ToString() ?? ""
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Version check failed: {ex.Message}");
                return new VersionCheckResult { HasUpdate = false };
            }
        }
    }
}
