using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CadeAEscalacao.Domain.Players;
using CadeAEscalacao.Server.Authentication;
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

    /// <summary>Simulates a signed-in person with a token made by the tests (see <see cref="TestTokens"/>).</summary>
    protected void SignInAs(Guid? playerId = null, Guid? userId = null) =>
        UseAccessToken(TestTokens.Create(playerId, userId));

    protected void SignInAs(PlayerId playerId) => SignInAs(playerId.Value);

    protected void UseAccessToken(string? accessToken) =>
        Client.DefaultRequestHeaders.Authorization =
            accessToken is null ? null : new AuthenticationHeaderValue("Bearer", accessToken);

    /// <summary>The real login: asks for the code, reads it from the fake e-mail and checks it.</summary>
    protected async Task<TokenResponse> LoginAsync(string email)
    {
        (await PostAsync("/api/auth/email/start", new { email })).StatusCode.ShouldBe(HttpStatusCode.Accepted);
        var response = await PostAsync("/api/auth/email/verify", new { email, code = Api.Emails.LastCodeSentTo(email) });
        return await ReadAsync<TokenResponse>(response, HttpStatusCode.OK);
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
