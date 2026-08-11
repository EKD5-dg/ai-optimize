using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AiOptimize.Services;

/// <summary>GitHub Releases 上的最新版本信息。</summary>
public sealed record UpdateInfo(Version LatestVersion, string DownloadUrl, string ReleaseNotes);

/// <summary>
/// 更新检查：从 GitHub Releases 获取最新版本并下载安装包。
/// 发布流程：打 tag（如 v1.3.6）→ 把 Inno Setup 生成的安装包上传到 Releases。
/// 检查失败（无网络 / 限流 / 仓库还没发布过）一律静默返回 null，不打扰用户。
/// </summary>
public static class UpdateService
{
    // 发布渠道：仓库 EKD5-dg/ai-optimize 的 GitHub Releases
    private const string ApiUrl = "https://api.github.com/repos/EKD5-dg/ai-optimize/releases/latest";

    // 不设全局超时：下载安装包可能要几分钟；检查接口的超时用 CancelAfter 单独控制
    private static readonly HttpClient HttpClient = CreateHttpClient();

    /// <summary>查询最新发布版本；无新版本或查询失败返回 null。</summary>
    public static async Task<UpdateInfo?> CheckLatestAsync(Version currentVersion, CancellationToken cancellationToken = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(15)); // 检查接口 15 秒内必须响应

            using var response = await HttpClient.GetAsync(ApiUrl, cts.Token);
            if (!response.IsSuccessStatusCode) return null; // 404=还没发布过，403=限流，一律视为无更新

            var release = JsonSerializer.Deserialize<GitHubRelease>(
                await response.Content.ReadAsStringAsync(cts.Token));
            if (release is null
                || !Version.TryParse(release.TagName?.TrimStart('v'), out var latestVersion))
            {
                return null;
            }

            var downloadUrl = release.Assets?
                .FirstOrDefault(a => a.Name?.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) == true)
                ?.BrowserDownloadUrl;
            if (downloadUrl is null) return null;

            return latestVersion > currentVersion
                ? new UpdateInfo(latestVersion, downloadUrl, release.Body ?? "")
                : null;
        }
        catch (Exception)
        {
            return null; // 网络异常 / 取消 一律静默
        }
    }

    /// <summary>下载安装包到本地并报告进度（0-100），返回本地文件路径。</summary>
    public static async Task<string> DownloadInstallerAsync(string url, string targetPath,
        IProgress<int>? progress = null, CancellationToken cancellationToken = default)
    {
        using var response = await HttpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        long total = response.Content.Headers.ContentLength ?? -1;
        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);

        await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var target = File.Create(targetPath);
        var buffer = new byte[81920];
        long downloaded = 0;
        int bytes;
        while ((bytes = await source.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await target.WriteAsync(buffer.AsMemory(0, bytes), cancellationToken);
            downloaded += bytes;
            if (total > 0) progress?.Report((int)(downloaded * 100 / total));
        }
        return targetPath;
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient { Timeout = System.Threading.Timeout.InfiniteTimeSpan };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("AiOptimize"); // GitHub API 必须带 User-Agent，否则 403
        return client;
    }

    private sealed class GitHubRelease
    {
        [JsonPropertyName("tag_name")] public string? TagName { get; set; }
        [JsonPropertyName("body")] public string? Body { get; set; }
        [JsonPropertyName("assets")] public List<GitHubAsset>? Assets { get; set; }
    }

    private sealed class GitHubAsset
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("browser_download_url")] public string? BrowserDownloadUrl { get; set; }
    }
}
