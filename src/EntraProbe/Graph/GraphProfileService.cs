using System.Net.Http.Headers;
using System.Text.Json;

namespace EntraProbe.Graph;

public sealed class GraphProfileService : IGraphProfileService
{
    private readonly HttpClient _httpClient;

    public GraphProfileService(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? GraphHttpClientFactory.Create();
    }

    public async Task<SignedInUserProfile> GetProfileAsync(string accessToken, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "me?$select=department,onPremisesDistinguishedName");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                throw new GraphQueryException($"Microsoft Graph query failed with HTTP {(int)response.StatusCode}.");
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                throw new GraphQueryException("Microsoft Graph returned an empty response.");
            }

            using var document = JsonDocument.Parse(content);
            var root = document.RootElement;
            var department = root.TryGetProperty("department", out var departmentElement)
                ? departmentElement.GetString()
                : null;
            var distinguishedName = root.TryGetProperty("onPremisesDistinguishedName", out var distinguishedNameElement)
                ? distinguishedNameElement.GetString()
                : null;

            return new SignedInUserProfile(department, distinguishedName);
        }
        catch (GraphQueryException)
        {
            throw;
        }
        catch (JsonException ex)
        {
            throw new GraphQueryException($"Microsoft Graph returned invalid JSON: {ex.Message}", ex);
        }
        catch (HttpRequestException ex)
        {
            throw new GraphQueryException("Microsoft Graph query failed: network unavailable.", ex);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new GraphQueryException("Microsoft Graph query failed: request timed out.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new GraphQueryException($"Microsoft Graph query failed: {ex.Message}", ex);
        }
    }
}
