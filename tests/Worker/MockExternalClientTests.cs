using System.Net;
using System.Text;
using System.Text.Json;
using Niuro.Core.Infrastructure.Messaging;

namespace Niuro.Tests.Worker;

public class MockExternalClientTests
{
    private static MockExternalClient CreateClient(StubHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://mock.test")
        };
        return new MockExternalClient(httpClient);
    }

    [Fact]
    public async Task CreateCustomerAsync_IssuesPostToCustomers_WithSnakeCaseJson()
    {
        var handler = new StubHandler();
        var client = CreateClient(handler);

        var payload = new { Operation = "Create", CustomerName = "Acme" };
        await client.CreateCustomerAsync(payload);

        Assert.Equal(HttpMethod.Post, handler.Request.Method);
        Assert.Equal("/api/customers", handler.Request.RequestUri?.AbsolutePath);
        Assert.Equal("application/json", handler.Request.Content?.Headers.ContentType?.MediaType);

        var body = await handler.Request.Content!.ReadAsStringAsync();
        Assert.Contains("\"customer_name\":\"Acme\"", body); // snake_case
    }

    [Fact]
    public async Task UpdateCustomerAsync_IssuesPutToCustomersBySsn_WithSnakeCaseJson()
    {
        var handler = new StubHandler();
        var client = CreateClient(handler);

        var payload = new { Operation = "Update", CustomerName = "Acme" };
        await client.UpdateCustomerAsync("123-45-6789", payload);

        Assert.Equal(HttpMethod.Put, handler.Request.Method);
        Assert.Equal("/api/customers/123-45-6789", handler.Request.RequestUri?.PathAndQuery);
        Assert.Equal("application/json", handler.Request.Content?.Headers.ContentType?.MediaType);

        var body = await handler.Request.Content!.ReadAsStringAsync();
        Assert.Contains("\"customer_name\":\"Acme\"", body); // snake_case
    }

    /// <summary>
    /// HttpMessageHandler stub that captures the request and responds 201 (Create) / 200 (Update).
    /// </summary>
    private sealed class StubHandler : HttpMessageHandler
    {
        public HttpRequestMessage Request { get; private set; } = null!;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            var statusCode = request.Method == HttpMethod.Post
                ? HttpStatusCode.Created
                : HttpStatusCode.OK;

            var response = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }
    }
}
