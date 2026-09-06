using System.Net.Http.Json;
using System.Text.Json;

namespace Orbshacker;

public sealed class ApiClient(HttpClient? client = null)
{
    private readonly HttpClient _client = client ?? CreateClient();
    private static HttpClient CreateClient()
    {
        var result = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
        result.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        result.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        result.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.9");
        result.DefaultRequestHeaders.Referrer = new Uri("https://discord.com/");
        result.DefaultRequestHeaders.TryAddWithoutValidation("Origin", "https://discord.com");
        return result;
    }

    public async Task<T> GetJsonAsync<T>(string url, CancellationToken cancellationToken = default, TimeSpan? timeout = null)
    {
        try
        {
            using var timeoutSource = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(10));
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutSource.Token);
            using var response = await _client.GetAsync(url, linked.Token);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<T>(AppConfig.JsonOptions, linked.Token)
                ?? throw new JsonException("The server returned an empty response.");
        }
        catch (Exception error) when (error is HttpRequestException or TaskCanceledException or JsonException)
        { throw new NetworkException($"Request to {url} failed: {error.Message}", error); }
    }

    public async Task DownloadAsync(string url, string destination, CancellationToken cancellationToken = default)
    {
        using var timeoutSource = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutSource.Token);
        using var response = await _client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, linked.Token);
        response.EnsureSuccessStatusCode();
        await using var input = await response.Content.ReadAsStreamAsync(linked.Token);
        await using var output = File.Create(destination);
        await input.CopyToAsync(output, linked.Token);
    }
}
