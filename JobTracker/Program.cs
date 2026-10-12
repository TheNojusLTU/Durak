using JobTracker.Client.Pages;
using JobTracker.Components;
using JobTracker.Data;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentityCore<ApplicationUser>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager();
builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddIdentityCookies();
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login";
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapPost("/account/login", async (
    HttpContext httpContext,
    IAntiforgery antiforgery,
    SignInManager<ApplicationUser> signInManager) =>
{
    if (!await antiforgery.IsRequestValidAsync(httpContext))
    {
        return Results.BadRequest();
    }

    var form = await httpContext.Request.ReadFormAsync();
    var userName = form["Username"].ToString().Trim();
    var password = form["Password"].ToString();
    var returnUrl = form["ReturnUrl"].ToString();

    var result = await signInManager.PasswordSignInAsync(
        userName,
        password,
        isPersistent: false,
        lockoutOnFailure: true);

    if (result.Succeeded)
    {
        var isLocalReturnUrl = returnUrl.StartsWith("/", StringComparison.Ordinal)
            && !returnUrl.StartsWith("//", StringComparison.Ordinal)
            && !returnUrl.StartsWith("/\\", StringComparison.Ordinal)
            && !returnUrl.Contains('\\');
        var redirectUrl = isLocalReturnUrl ? returnUrl : "/profile";

        return Results.LocalRedirect(redirectUrl);
    }

    return Results.Redirect($"/login?error={Uri.EscapeDataString("Invalid username or password.")}");
}).AllowAnonymous();

app.MapPost("/account/logout", async (
    HttpContext httpContext,
    IAntiforgery antiforgery,
    SignInManager<ApplicationUser> signInManager) =>
{
    if (!await antiforgery.IsRequestValidAsync(httpContext))
    {
        return Results.BadRequest();
    }

    await signInManager.SignOutAsync();
    return Results.LocalRedirect("/");
}).RequireAuthorization();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(JobTracker.Client._Imports).Assembly);

app.Run();
