using System.Reflection;
using System.Text.Json.Serialization;
using CadeAEscalacao.Application;
using CadeAEscalacao.Infrastructure;
using CadeAEscalacao.Server.Authentication;
using CadeAEscalacao.Server.Endpoints;
using CadeAEscalacao.Server.ErrorHandling;

var builder = WebApplication.CreateBuilder(args);

// The build runs this file only to write the OpenAPI document (ADR-019). The database and the
// signing key are never used then, so placeholders avoid needing the secrets (or the CI) to build.
if (Assembly.GetEntryAssembly()?.GetName().Name == "GetDocument.Insider")
{
    builder.Configuration["ConnectionStrings:Default"] ??= "Host=unused";
    builder.Configuration["Jwt:SigningKey"] ??= Convert.ToBase64String(new byte[JwtOptions.MinimumKeyBytes]);
}

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Login with the API's own tokens (ADR-014, ADR-015).
builder.Services.AddApiAuthentication();

// Errors as Problem Details: known exceptions (ADR-011/012/013), validation and empty error responses.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ProblemDetailsExceptionHandler>();
builder.Services.AddValidation();

// Enums as text ("Regular", "Midfielder") instead of numbers, in requests, responses and OpenAPI.
// Numbers only as numbers (not "3"), so the OpenAPI types are plain numbers for the React client (ADR-019).
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict;
});

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseDefaultFiles();
app.MapStaticAssets();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapApiEndpoints();

app.MapFallbackToFile("/index.html");

app.Run();
