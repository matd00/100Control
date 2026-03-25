using System.Net.Http.Json;
using Polly;
using Polly.Extensions.Http;
using Polly.Retry;

namespace Integrations.Common;

public abstract class BaseApiClient
{
    protected readonly HttpClient HttpClient;
    private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;

    protected BaseApiClient(HttpClient httpClient)
    {
        HttpClient = httpClient;
        
        _retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }

    protected async Task<T?> GetAsync<T>(string url, CancellationToken ct = default)
    {
        var response = await _retryPolicy.ExecuteAsync(() => HttpClient.GetAsync(url, ct));
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
    }

    protected async Task PostAsync<T>(string url, T data, CancellationToken ct = default)
    {
        var response = await _retryPolicy.ExecuteAsync(() => HttpClient.PostAsJsonAsync(url, data, ct));
        response.EnsureSuccessStatusCode();
    }

    protected async Task PutAsync<T>(string url, T data, CancellationToken ct = default)
    {
        var response = await _retryPolicy.ExecuteAsync(() => HttpClient.PutAsJsonAsync(url, data, ct));
        response.EnsureSuccessStatusCode();
    }
}
