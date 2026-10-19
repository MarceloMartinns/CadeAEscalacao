using System.Net;
using CadeAEscalacao.Domain.Players;
using CadeAEscalacao.Server.Authentication;
using CadeAEscalacao.Server.Endpoints.Me;
using Microsoft.IdentityModel.JsonWebTokens;

namespace CadeAEscalacao.Server.Tests.Me;

/// <summary>Profile of the signed-in person and the JWT validation (ADR-014, ADR-015).</summary>
public sealed class MeEndpointsTests : ApiTestBase
{
    private const string Email = "ze@example.com";

    [Test]
    public async Task Get_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync("/api/me");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Get_TokenSignedWithAnotherKey_Returns401()
    {
        UseAccessToken(TestTokens.Create(signingKey: Convert.ToBase64String(new byte[32])));

        var response = await Client.GetAsync("/api/me");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Get_ExpiredToken_Returns401()
    {
        UseAccessToken(TestTokens.Create(expires: DateTimeOffset.UtcNow.AddMinutes(-5)));

        var response = await Client.GetAsync("/api/me");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Get_TokenFromAnotherIssuer_Returns401()
    {
        UseAccessToken(TestTokens.Create(issuer: "someone-else"));

        var response = await Client.GetAsync("/api/me");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Get_AfterLogin_ReturnsAccountWithoutPlayer()
    {
        UseAccessToken((await LoginAsync(Email)).AccessToken);

        var response = await Client.GetAsync("/api/me");

        var me = await ReadAsync<MeResponse>(response, HttpStatusCode.OK);
        me.Email.ShouldBe(Email);
        me.Player.ShouldBeNull();
    }

    [Test]
    public async Task Create_AfterLogin_CreatesPlayerAndReturnsTokenWithPlayer()
    {
        var tokens = await LoginAsync(Email);
        UseAccessToken(tokens.AccessToken);
        Player? added = null;
        Api.Players.When(players => players.Add(Arg.Any<Player>())).Do(call => added = call.Arg<Player>());

        var response = await PostAsync("/api/me", new { name = "  Zé  " });

        var created = await ReadAsync<CreateProfileResponse>(response, HttpStatusCode.Created);
        created.Player.Name.ShouldBe("Zé");
        added.ShouldNotBeNull().Id.Value.ShouldBe(created.Player.Id);
        await Api.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        new JsonWebToken(created.Token.AccessToken).GetClaim(AppClaims.PlayerId).Value
            .ShouldBe(created.Player.Id.ToString());

        // Later sessions also carry the player: it is read from the account on every refresh.
        var refreshed = await ReadAsync<TokenResponse>(
            await PostAsync("/api/auth/refresh", new { refreshToken = tokens.RefreshToken }), HttpStatusCode.OK);
        new JsonWebToken(refreshed.AccessToken).GetClaim(AppClaims.PlayerId).Value
            .ShouldBe(created.Player.Id.ToString());

        Api.Players.GetByIdAsync(added.Id, Arg.Any<CancellationToken>()).Returns(added);
        var me = await ReadAsync<MeResponse>(await Client.GetAsync("/api/me"), HttpStatusCode.OK);
        me.Player.ShouldNotBeNull().Name.ShouldBe("Zé");
    }

    [Test]
    public async Task Create_Twice_Returns409()
    {
        UseAccessToken((await LoginAsync(Email)).AccessToken);
        await PostAsync("/api/me", new { name = "Zé" });

        var response = await PostAsync("/api/me", new { name = "Zé de novo" });

        await ReadProblemAsync(response, HttpStatusCode.Conflict);
        Api.Players.Received(1).Add(Arg.Any<Player>());
    }

    [Test]
    public async Task Create_EmptyName_Returns400()
    {
        UseAccessToken((await LoginAsync(Email)).AccessToken);

        var response = await PostAsync("/api/me", new { name = "" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        Api.Players.DidNotReceive().Add(Arg.Any<Player>());
    }
}
