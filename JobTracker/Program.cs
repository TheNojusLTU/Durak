using JobTracker.Client.Pages;
using JobTracker.Components;
using JobTracker.Data;
using System.Net.Mail;
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

app.MapPost("/account/register", async (
    HttpContext httpContext,
    IAntiforgery antiforgery,
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager) =>
{
    if (!await antiforgery.IsRequestValidAsync(httpContext))
    {
        return Results.BadRequest();
    }

    var form = await httpContext.Request.ReadFormAsync();
    var userName = form["Username"].ToString().Trim();
    var displayName = form["DisplayName"].ToString().Trim();
    var emailInput = form["Email"].ToString().Trim();
    var password = form["Password"].ToString();
    var avatarKey = form["AvatarKey"].ToString();

    string? validationError = null;
    if (userName.Length is < 3 or > 20)
    {
        validationError = "Username must be between 3 and 20 characters.";
    }
    else if (displayName.Length is < 3 or > 20)
    {
        validationError = "Display name must be between 3 and 20 characters.";
    }
    else if (emailInput.Length > 0 && !MailAddress.TryCreate(emailInput, out _))
    {
        validationError = "Enter a valid email address, or leave it blank.";
    }
    else if (password.Length == 0)
    {
        validationError = "Password is required.";
    }
    else if (avatarKey.Length > 0 && !AvatarCatalog.Contains(avatarKey))
    {
        validationError = "Choose an available avatar.";
    }

    if (validationError is not null)
    {
        return Results.Redirect($"/register?error={Uri.EscapeDataString(validationError)}");
    }

    var user = new ApplicationUser
    {
        UserName = userName,
        Email = string.IsNullOrWhiteSpace(emailInput) ? null : emailInput,
        DisplayName = displayName,
        AvatarKey = string.IsNullOrWhiteSpace(avatarKey) ? AvatarCatalog.Default.Key : avatarKey
    };

    var createResult = await userManager.CreateAsync(user, password);
    if (!createResult.Succeeded)
    {
        var error = string.Join(" ", createResult.Errors.Select(identityError => identityError.Description));
        return Results.Redirect($"/register?error={Uri.EscapeDataString(error)}");
    }

    await signInManager.SignInAsync(user, isPersistent: false);
    return Results.LocalRedirect("/");
}).AllowAnonymous();

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

    var result = await signInManager.PasswordSignInAsync(
        userName,
        password,
        isPersistent: false,
        lockoutOnFailure: true);

    if (result.Succeeded)
    {
        return Results.LocalRedirect("/");
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
