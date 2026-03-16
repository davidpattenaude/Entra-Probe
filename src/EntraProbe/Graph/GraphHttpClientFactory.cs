using System.Net.Http.Headers;

namespace EntraProbe.Graph;

public static class GraphHttpClientFactory
{
    public static HttpClient Create()
    {
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://graph.microsoft.com/v1.0/"),
            // Endpoint tools should fail quickly enough for detection/remediation workflows instead of hanging on network calls.
            Timeout = TimeSpan.FromSeconds(15)
        };

        var version = typeof(GraphHttpClientFactory).Assembly.GetName().Version?.ToString(3) ?? "1.0.0";
        // A stable User-Agent helps trace calls in proxies and Graph diagnostics without changing stdout/stderr behavior.
        httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("EntraProbe", version));

        return httpClient;
    }
}
