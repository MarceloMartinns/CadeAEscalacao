using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace CadeAEscalacao.Server.Tests;

/// <summary>A new API (and new mocks) for each test.</summary>
public abstract class ApiTestBase
{
    protected static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    protected ApiFactory Api { get; private set; } = null!;

    protected HttpClient Client { get; private set; } = null!;

    [SetUp]
    public void CreateApi()
    {
        Api = new ApiFactory();
        Client = Api.CreateClient();
    }

    [TearDown]
    public void DisposeApi()
    {
        Client.Dispose();
        Api.Dispose();
    }

    protected Task<HttpResponseMessage> PostAsync(string url, object? body = null) =>
        Client.PostAsJsonAsync(url, body ?? new { }, Json);

    protected Task<HttpResponseMessage> PutAsync(string url, object body) =>
        Client.PutAsJsonAsync(url, body, Json);

    /// <summary>Checks the status code and reads the body.</summary>
    protected static async Task<T> ReadAsync<T>(HttpResponseMessage response, HttpStatusCode expected)
    {
        response.StatusCode.ShouldBe(expected, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<T>(Json))!;
    }

    /// <summary>Checks the status code and that the body is Problem Details.</summary>
    protected static async Task<ProblemDetails> ReadProblemAsync(HttpResponseMessage response, HttpStatusCode expected)
    {
        response.StatusCode.ShouldBe(expected, await response.Content.ReadAsStringAsync());
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        return (await response.Content.ReadFromJsonAsync<ProblemDetails>(Json))!;
    }
}
