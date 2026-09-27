using CafPortal.Application;
using CafPortal.Infrastructure;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// --- Services -------------------------------------------------------------
builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
    c.SwaggerDoc("v1", new() { Title = "CAF Operations Portal API", Version = "v1" }));

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Phase 1: no authentication. CORS opens the SPA dev origin; Program is structured so
// Entra ID (AddAuthentication().AddMicrosoftIdentityWebApi(...)) can be added here later.
const string SpaCors = "SpaCors";
builder.Services.AddCors(options => options.AddPolicy(SpaCors, policy =>
    policy.WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:5173"])
          .AllowAnyHeader()
          .AllowAnyMethod()));

builder.Services.AddProblemDetails();

// Custom-login auth: HttpOnly cookie, same-origin SPA. API returns 401/403 instead of redirecting.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "caf.auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Events.OnRedirectToLogin = ctx => { ctx.Response.StatusCode = StatusCodes.Status401Unauthorized; return Task.CompletedTask; };
        options.Events.OnRedirectToAccessDenied = ctx => { ctx.Response.StatusCode = StatusCodes.Status403Forbidden; return Task.CompletedTask; };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

// --- Database bootstrap ---------------------------------------------------
await DbInitializer.InitializeAsync(app.Services);

// --- Pipeline -------------------------------------------------------------
app.UseExceptionHandler(handler => handler.Run(async context =>
{
    var feature = context.Features.Get<IExceptionHandlerFeature>();
    var problem = new ProblemDetails
    {
        Title = "An unexpected error occurred.",
        Status = StatusCodes.Status500InternalServerError,
        Detail = app.Environment.IsDevelopment() ? feature?.Error.Message : null
    };
    context.Response.StatusCode = problem.Status!.Value;
    await context.Response.WriteAsJsonAsync(problem);
}));

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "CAF Operations Portal API v1"));
}

app.UseCors(SpaCors);

app.UseAuthentication();
app.UseAuthorization();

// Serve the built React SPA (wwwroot) as a single deployment package.
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "healthy", utc = DateTimeOffset.UtcNow }));

// SPA fallback: non-API routes return index.html for client-side routing.
app.MapFallbackToFile("index.html");

app.Run();
