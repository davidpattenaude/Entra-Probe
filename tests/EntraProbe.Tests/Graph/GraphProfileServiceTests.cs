using EntraProbe.Graph;
using System.Net;
using System.Net.Http;
using System.Text;

namespace EntraProbe.Tests.Graph;

[TestClass]
public sealed class GraphProfileServiceTests
{
    [TestMethod]
    public async Task GetProfileAsync_ReturnsFriendlyErrorWhenResponseBodyIsEmpty()
    {
        var httpClient = new HttpClient(new StubHttpMessageHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(string.Empty, Encoding.UTF8, "application/json")
            })))
        {
            BaseAddress = new Uri("https://graph.microsoft.com/v1.0/")
        };

        var service = new GraphProfileService(httpClient);

        var exception = await TestAssert.ThrowsAsync<GraphQueryException>(() => service.GetProfileAsync("token", CancellationToken.None));

        Assert.AreEqual("Microsoft Graph returned an empty response.", exception.Message);
    }

    [TestMethod]
    public async Task GetProfileAsync_ReturnsFriendlyErrorWhenNetworkIsUnavailable()
    {
        var httpClient = new HttpClient(new StubHttpMessageHandler(_ => throw new HttpRequestException("No network")))
        {
            BaseAddress = new Uri("https://graph.microsoft.com/v1.0/")
        };

        var service = new GraphProfileService(httpClient);

        var exception = await TestAssert.ThrowsAsync<GraphQueryException>(() => service.GetProfileAsync("token", CancellationToken.None));

        Assert.AreEqual("Microsoft Graph query failed: network unavailable.", exception.Message);
    }

    [TestMethod]
    public async Task GetProfileAsync_ReturnsFriendlyErrorWhenRequestTimesOut()
    {
        var httpClient = new HttpClient(new StubHttpMessageHandler(_ => throw new TaskCanceledException("Timed out")))
        {
            BaseAddress = new Uri("https://graph.microsoft.com/v1.0/")
        };

        var service = new GraphProfileService(httpClient);

        var exception = await TestAssert.ThrowsAsync<GraphQueryException>(() => service.GetProfileAsync("token", CancellationToken.None));

        Assert.AreEqual("Microsoft Graph query failed: request timed out.", exception.Message);
    }

    [TestMethod]
    public async Task GetProfileAsync_PropagatesCallerCancellation()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        var httpClient = new HttpClient(new StubHttpMessageHandler(_ => throw new OperationCanceledException(cancellationTokenSource.Token)))
        {
            BaseAddress = new Uri("https://graph.microsoft.com/v1.0/")
        };

        var service = new GraphProfileService(httpClient);

        await TestAssert.ThrowsAsync<OperationCanceledException>(() => service.GetProfileAsync("token", cancellationTokenSource.Token));
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _sendAsync;

        public StubHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> sendAsync)
        {
            _sendAsync = sendAsync;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return _sendAsync(request);
        }
    }
}
