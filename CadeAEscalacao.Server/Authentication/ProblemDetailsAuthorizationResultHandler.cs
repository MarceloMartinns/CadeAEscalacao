using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace CadeAEscalacao.Server.Authentication;

/// <summary>
/// Explains the 403 of a signed-in person who did not create the profile yet (ADR-016).
/// Any other result keeps the default behavior (empty 401/403, turned into Problem Details).
/// </summary>
internal sealed class ProblemDetailsAuthorizationResultHandler(IProblemDetailsService problemDetailsService)
    : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _default = new();

    public async Task HandleAsync(
        RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Forbidden
            && context.User.Identity?.IsAuthenticated == true
            && !context.User.HasClaim(claim => claim.Type == AppClaims.PlayerId))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await problemDetailsService.WriteAsync(new ProblemDetailsContext
            {
                HttpContext = context,
                ProblemDetails =
                {
                    Status = StatusCodes.Status403Forbidden,
                    Title = "Profile required",
                    Detail = "Create your player first (POST /api/me), then refresh the access token.",
                },
            });
            return;
        }

        await _default.HandleAsync(next, context, policy, authorizeResult);
    }
}
