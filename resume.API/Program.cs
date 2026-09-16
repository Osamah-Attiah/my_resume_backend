using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Resume.Api;
using Resume.Application;
using Resume.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
{
    context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
    context.ProblemDetails.Extensions.TryAdd("code", "UNEXPECTED_ERROR");
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Port=54324;Database=my_resume;Username=resume_local;Password=resume_local_password";
builder.Services.AddDbContext<ResumeDbContext>(options => options.UseNpgsql(connectionString, postgres => postgres.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<SnapshotService>();
builder.Services.AddSingleton<IResumePdfRenderer, ResumePdfRenderer>();
builder.Services.AddScoped<BootstrapService>();
builder.Services.AddHttpClient<PublicationDispatcher>();
builder.Services.AddHttpClient<PublicationReconciler>();
builder.Services.AddHttpClient<PrivateArtifactDownloader>();
builder.Services.AddHttpClient<IMediaStorage, CloudinaryMediaStorage>();
builder.Services.Configure<FormOptions>(options => { options.MultipartBodyLengthLimit = 5_500_000; options.MemoryBufferThreshold = 5_500_000; });

var signingKey = builder.Configuration["Auth:SigningKey"];
if (string.IsNullOrWhiteSpace(signingKey))
{
    if (!builder.Environment.IsDevelopment()) throw new InvalidOperationException("Auth:SigningKey is required outside Development.");
    signingKey = "development-signing-key-change-me-32-chars";
}
if (Encoding.UTF8.GetByteCount(signingKey) < 32) throw new InvalidOperationException("Auth:SigningKey must contain at least 32 UTF-8 bytes.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Auth:Issuer"] ?? "resume-api",
        ValidAudience = builder.Configuration["Auth:Audience"] ?? "resume-admin",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
        ClockSkew = TimeSpan.FromSeconds(30)
    };
    options.Events = SessionValidation.CreateEvents();
});
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(options => options.AddFixedWindowLimiter("login", limiter =>
{
    limiter.PermitLimit = 8;
    limiter.Window = TimeSpan.FromMinutes(5);
    limiter.QueueLimit = 0;
}));
builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);
builder.Services.AddCors(options => options.AddPolicy("admin", policy => policy
    .WithOrigins(builder.Configuration["Cors:AdminOrigin"] ?? "http://localhost:5173")
    .AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
app.UseExceptionHandler(handler => handler.Run(async context =>
{
    var error = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    app.Logger.LogError(error, "Unhandled API exception for {Path}", context.Request.Path);
    if (error is DbUpdateConcurrencyException)
    {
        context.Response.StatusCode = StatusCodes.Status409Conflict;
        await Results.Problem(statusCode: 409, title: "The record changed", extensions: new Dictionary<string, object?> { ["code"] = "VERSION_CONFLICT", ["traceId"] = context.TraceIdentifier }).ExecuteAsync(context);
        return;
    }
    await Results.Problem(statusCode: 500, title: "An unexpected error occurred", extensions: new Dictionary<string, object?> { ["code"] = "UNEXPECTED_ERROR", ["traceId"] = context.TraceIdentifier }).ExecuteAsync(context);
}));
app.UseForwardedHeaders(new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto });
app.UseCors("admin");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi();
app.MapHealthChecks("/health/live", new() { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new() { Predicate = check => check.Tags.Contains("ready") });
app.MapResumeApi();

if (args.Contains("--bootstrap-owner", StringComparer.Ordinal))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<BootstrapService>().BootstrapOwnerAsync();
    return;
}

if (args.Contains("--seed-demo", StringComparer.Ordinal))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<BootstrapService>().SeedDemoAsync();
    return;
}

app.Run();

public partial class Program;
