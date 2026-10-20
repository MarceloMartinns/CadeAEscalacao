using System.Net;
using System.Net.Http.Json;
using CadeAEscalacao.Server.Authentication;
using CadeAEscalacao.Server.Endpoints.Auth;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CadeAEscalacao.Server.Tests.Auth;

/// <summary>
/// The browser session (ADR-020): refresh token in an HttpOnly cookie, never in the body, and the
/// login link in the e-mail (ADR-022). The client uses HTTPS and keeps cookies, like a browser.
/// </summary>
public sealed class BrowserSessionTests : ApiTestBase
{
    private const string Email = "ze@example.com";

    private HttpClient _browser = null!;

    [SetUp]
    public void CreateBrowser() =>
        _browser = Api.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });

    [TearDown]
    public void DisposeBrowser() => _browser.Dispose();

    [Test]
    public async Task Verify_UseCookie_PutsTheRefreshTokenOnlyInAnHttpOnlyCookie()
    {
        var response = await LoginWithCookieAsync();

        var tokens = await ReadAsync<TokenResponse>(response, HttpStatusCode.OK);
        tokens.AccessToken.ShouldNotBeNullOrWhiteSpace();
        tokens.RefreshToken.ShouldBeNull();
        var cookie = response.Headers.GetValues("Set-Cookie").Single().ToLowerInvariant();
        cookie.ShouldStartWith($"{RefreshTokenCookie.Name}=");
        cookie.ShouldContain("httponly");
        cookie.ShouldContain("secure");
        cookie.ShouldContain("samesite=strict");
        cookie.ShouldContain("path=/api/auth");
    }

    [Test]
    public async Task Refresh_WithTheCookie_ReturnsNewAccessTokenAndRotatesTheCookie()
    {
        var login = await LoginWithCookieAsync();
        var firstCookie = login.Headers.GetValues("Set-Cookie").Single();

        var response = await _browser.PostAsJsonAsync("/api/auth/refresh", new { useCookie = true }, Json);

        var tokens = await ReadAsync<TokenResponse>(response, HttpStatusCode.OK);
        tokens.AccessToken.ShouldNotBeNullOrWhiteSpace();
        tokens.RefreshToken.ShouldBeNull();
        response.Headers.GetValues("Set-Cookie").Single().ShouldNotBe(firstCookie);
    }

    [Test]
    public async Task Refresh_NoCookieAndNoToken_Returns401()
    {
        var response = await _browser.PostAsJsonAsync("/api/auth/refresh", new { useCookie = true }, Json);

        await ReadProblemAsync(response, HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Logout_WithTheCookie_RevokesItAndDeletesTheCookie()
    {
        await LoginWithCookieAsync();

        var logout = await _browser.PostAsJsonAsync("/api/auth/logout", new { }, Json);
        var refresh = await _browser.PostAsJsonAsync("/api/auth/refresh", new { useCookie = true }, Json);

        logout.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        logout.Headers.GetValues("Set-Cookie").Single().ShouldContain("expires=Thu, 01 Jan 1970");
        refresh.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Start_WithPublicUrl_SendsALinkWithTheCodeInTheFragment()
    {
        using var api = Api.WithWebHostBuilder(builder =>
            builder.UseSetting(AuthEndpoints.PublicUrlSetting, "https://app.example.com/"));
        using var client = api.CreateClient();

        (await client.PostAsJsonAsync("/api/auth/email/start", new { email = Email }, Json))
            .StatusCode.ShouldBe(HttpStatusCode.Accepted);

        var code = Api.Emails.LastCodeSentTo(Email);
        Api.Emails.Sent.Last().Body
            .ShouldContain($"https://app.example.com/login#email=ze%40example.com&code={code}");
    }

    private async Task<HttpResponseMessage> LoginWithCookieAsync()
    {
        (await _browser.PostAsJsonAsync("/api/auth/email/start", new { email = Email }, Json))
            .StatusCode.ShouldBe(HttpStatusCode.Accepted);
        return await _browser.PostAsJsonAsync(
            "/api/auth/email/verify", new { email = Email, code = Api.Emails.LastCodeSentTo(Email), useCookie = true }, Json);
    }
}
