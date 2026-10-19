using System.Net;
using CadeAEscalacao.Server.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;

namespace CadeAEscalacao.Server.Tests.Auth;

/// <summary>Passwordless login (ADR-014) and session tokens (ADR-015).</summary>
public sealed class AuthEndpointsTests : ApiTestBase
{
    private const string Email = "ze@example.com";

    [Test]
    public async Task Start_NewEmail_Returns202AndSendsCode()
    {
        var response = await PostAsync("/api/auth/email/start", new { email = Email });

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        Api.Emails.LastCodeSentTo(Email).Length.ShouldBe(6);
    }

    [Test]
    public async Task Start_InvalidEmail_Returns400()
    {
        var response = await PostAsync("/api/auth/email/start", new { email = "not-an-email" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        Api.Emails.Sent.ShouldBeEmpty();
    }

    [Test]
    public async Task Verify_RightCode_ReturnsTokensAndConfirmsEmail()
    {
        var tokens = await LoginAsync(Email);

        tokens.ExpiresIn.ShouldBe(30 * 60);
        tokens.RefreshToken.ShouldNotBeNullOrWhiteSpace();
        var accessToken = new JsonWebToken(tokens.AccessToken);
        accessToken.GetClaim(AppClaims.Email).Value.ShouldBe(Email);
        accessToken.TryGetClaim(AppClaims.PlayerId, out _).ShouldBeFalse();
        var user = await Api.WithAuthDbAsync(db => db.Users.SingleAsync());
        user.Id.ToString().ShouldBe(accessToken.Subject);
        user.EmailConfirmed.ShouldBeTrue();
    }

    [Test]
    public async Task Verify_WrongCode_Returns400()
    {
        await PostAsync("/api/auth/email/start", new { email = Email });

        var response = await PostAsync("/api/auth/email/verify", new { email = Email, code = WrongCode() });

        var problem = await ReadProblemAsync(response, HttpStatusCode.BadRequest);
        problem.Title.ShouldBe("Invalid code");
    }

    [Test]
    public async Task Verify_UnknownEmail_Returns400()
    {
        var response = await PostAsync("/api/auth/email/verify", new { email = Email, code = "123456" });

        var problem = await ReadProblemAsync(response, HttpStatusCode.BadRequest);
        problem.Title.ShouldBe("Invalid code");
    }

    [Test]
    public async Task Verify_CodeUsedTwice_Returns400()
    {
        await LoginAsync(Email);

        var response = await PostAsync(
            "/api/auth/email/verify", new { email = Email, code = Api.Emails.LastCodeSentTo(Email) });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Verify_FiveWrongCodes_LocksTheAccount()
    {
        await PostAsync("/api/auth/email/start", new { email = Email });
        var code = Api.Emails.LastCodeSentTo(Email);
        for (var attempt = 0; attempt < 5; attempt++)
        {
            await PostAsync("/api/auth/email/verify", new { email = Email, code = WrongCode() });
        }

        var response = await PostAsync("/api/auth/email/verify", new { email = Email, code });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Refresh_ValidToken_ReturnsNewTokens()
    {
        var tokens = await LoginAsync(Email);

        var response = await PostAsync("/api/auth/refresh", new { refreshToken = tokens.RefreshToken });

        var refreshed = await ReadAsync<TokenResponse>(response, HttpStatusCode.OK);
        refreshed.RefreshToken.ShouldNotBe(tokens.RefreshToken);
        new JsonWebToken(refreshed.AccessToken).Subject.ShouldBe(new JsonWebToken(tokens.AccessToken).Subject);
    }

    [Test]
    public async Task Refresh_TokenUsedTwice_Returns401AndRevokesEveryToken()
    {
        var tokens = await LoginAsync(Email);
        var refreshed = await ReadAsync<TokenResponse>(
            await PostAsync("/api/auth/refresh", new { refreshToken = tokens.RefreshToken }), HttpStatusCode.OK);

        var reused = await PostAsync("/api/auth/refresh", new { refreshToken = tokens.RefreshToken });
        var afterReuse = await PostAsync("/api/auth/refresh", new { refreshToken = refreshed.RefreshToken });

        await ReadProblemAsync(reused, HttpStatusCode.Unauthorized);
        afterReuse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Refresh_UnknownToken_Returns401()
    {
        var response = await PostAsync("/api/auth/refresh", new { refreshToken = "unknown" });

        await ReadProblemAsync(response, HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Logout_ThenRefresh_Returns401()
    {
        var tokens = await LoginAsync(Email);

        var logout = await PostAsync("/api/auth/logout", new { refreshToken = tokens.RefreshToken });
        var refresh = await PostAsync("/api/auth/refresh", new { refreshToken = tokens.RefreshToken });

        logout.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        refresh.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Start_MoreThanTenRequestsInAMinute_Returns429()
    {
        for (var request = 0; request < 10; request++)
        {
            (await PostAsync("/api/auth/email/start", new { email = Email })).StatusCode
                .ShouldBe(HttpStatusCode.Accepted);
        }

        var response = await PostAsync("/api/auth/email/start", new { email = Email });

        response.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }

    private string WrongCode() => Api.Emails.LastCodeSentTo(Email) == "000000" ? "111111" : "000000";
}
