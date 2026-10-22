using System.Text.Json.Serialization;
using JobTracker.Api;
using JobTracker.Auth;
using JobTracker.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
// Let invalid JSON/body binding return 400/415, then format it through status-code pages.
builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = false);
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Instance = context.HttpContext.Request.Path;
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
    };
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = false;
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager();
builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddIdentityCookies();
builder.Services.AddAuthorization();
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "JobTracker.Antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
});
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "JobTracker.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    // API clients need status codes rather than redirects to the former Blazor pages.
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});

var app = builder.Build();

// Use Problem Details for unhandled exceptions and empty error responses in every environment.
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
        options.SwaggerEndpoint("/openapi/v1.json", "JobTracker API v1"));
}
else
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();

app.MapGet("/api/health", () => TypedResults.Ok(new { status = "healthy" }))
    .WithName("GetHealth")
    .WithSummary("Checks that the API host is running.")
    .WithDescription("Liveness only; does not connect to PostgreSQL or check other dependencies.")
    .AllowAnonymous();

app.MapPost("/account/applications/delete", async (
    HttpContext httpContext,
    IAntiforgery antiforgery,
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext dbContext) =>
{

    if (!await antiforgery.IsRequestValidAsync(httpContext))
    {
        return Results.BadRequest("Anti-forgery token validation failed.");
    }

    var user = await userManager.GetUserAsync(httpContext.User);
    if (user is null)
    {
        return Results.Challenge();
    }

    var form = await httpContext.Request.ReadFormAsync();
    var idInput = form["Id"].ToString();
    
    if (!int.TryParse(idInput, out var applicationId) || applicationId <= 0)
    {
        return Results.BadRequest("Invalid Application ID.");
    }

    var application = await dbContext.JobApplications
        .SingleOrDefaultAsync(item => item.Id == applicationId && item.UserId == user.Id);

    if (application is null)
    {
        return Results.NotFound();
    }

    dbContext.JobApplications.Remove(application);
    await dbContext.SaveChangesAsync();
    return Results.LocalRedirect("/applications?deleted=true");
}).RequireAuthorization();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(JobTracker.Client._Imports).Assembly);

app.Run();
