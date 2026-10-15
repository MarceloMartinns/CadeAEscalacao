using System.Text.Json.Serialization;
using CadeAEscalacao.Application;
using CadeAEscalacao.Infrastructure;
using CadeAEscalacao.Server.Endpoints;
using CadeAEscalacao.Server.ErrorHandling;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Errors as Problem Details: known exceptions (ADR-011/012/013), validation and empty error responses.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ProblemDetailsExceptionHandler>();
builder.Services.AddValidation();

// Enums as text ("Regular", "Midfielder") instead of numbers, in requests, responses and OpenAPI.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

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

app.MapApiEndpoints();

app.MapFallbackToFile("/index.html");

app.Run();
