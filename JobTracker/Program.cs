using JobTracker.Client.Pages;
using JobTracker.Components;
using JobTracker.Data;
using JobTracker.Models;
using System.Globalization;
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

app.MapPost("/account/applications/save", async (
    HttpContext httpContext,
    IAntiforgery antiforgery,
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext dbContext) =>
{
    if (!await antiforgery.IsRequestValidAsync(httpContext))
    {
        return Results.BadRequest();
    }

    var user = await userManager.GetUserAsync(httpContext.User);
    if (user is null)
    {
        return Results.Challenge();
    }

    var form = await httpContext.Request.ReadFormAsync();
    var idInput = form["Id"].ToString();
    var applicationId = 0;
    if (idInput.Length > 0 && (!int.TryParse(idInput, out applicationId) || applicationId < 0))
    {
        return Results.BadRequest();
    }

    var formUrl = applicationId > 0
        ? $"/applications/{applicationId}/edit"
        : "/create-application";
    IResult Invalid(string message) =>
        Results.Redirect($"{formUrl}?error={Uri.EscapeDataString(message)}");

    JobApplication? application = null;
    if (applicationId > 0)
    {
        application = await dbContext.JobApplications
            .SingleOrDefaultAsync(item => item.Id == applicationId && item.UserId == user.Id);
        if (application is null)
        {
            return Results.NotFound();
        }
    }

    var companyName = form["CompanyName"].ToString().Trim();
    var jobTitle = form["JobTitle"].ToString().Trim();
    var location = form["Location"].ToString().Trim();
    var salaryInput = form["Salary"].ToString();
    var jobUrl = form["JobUrl"].ToString().Trim();
    var applicationDateInput = form["ApplicationDate"].ToString();
    var statusInput = form["Status"].ToString();
    var notes = form["Notes"].ToString().Trim();

    if (companyName.Length is < 1 or > 200)
    {
        return Invalid("Company name is required and must be 200 characters or fewer.");
    }
    if (jobTitle.Length is < 1 or > 200)
    {
        return Invalid("Job title is required and must be 200 characters or fewer.");
    }
    if (location.Length > 200)
    {
        return Invalid("Location must be 200 characters or fewer.");
    }
    if (jobUrl.Length > 2048 ||
        (jobUrl.Length > 0 &&
         (!Uri.TryCreate(jobUrl, UriKind.Absolute, out var parsedJobUrl) ||
          (parsedJobUrl.Scheme != Uri.UriSchemeHttp && parsedJobUrl.Scheme != Uri.UriSchemeHttps))))
    {
        return Invalid("Enter a valid http or https job posting URL.");
    }
    if (notes.Length > 5000)
    {
        return Invalid("Notes must be 5,000 characters or fewer.");
    }

    decimal? salary = null;
    if (salaryInput.Length > 0)
    {
        if (!decimal.TryParse(
                salaryInput,
                NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out var parsedSalary) ||
            parsedSalary < 0)
        {
            return Invalid("Salary must be a non-negative number.");
        }
        salary = parsedSalary;
    }

    if (!DateTime.TryParseExact(
            applicationDateInput,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var parsedDate))
    {
        return Invalid("Enter a valid application date.");
    }

    if (!Enum.TryParse<ApplicationStatus>(statusInput, ignoreCase: false, out var status) ||
        !Enum.IsDefined(status))
    {
        return Invalid("Choose a valid application status.");
    }

    application ??= new JobApplication { UserId = user.Id };
    application.CompanyName = companyName;
    application.JobTitle = jobTitle;
    application.Location = string.IsNullOrWhiteSpace(location) ? null : location;
    application.Salary = salary;
    application.JobUrl = string.IsNullOrWhiteSpace(jobUrl) ? null : jobUrl;
    application.ApplicationDate = DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc);
    application.Status = status;
    application.Notes = string.IsNullOrWhiteSpace(notes) ? null : notes;

    if (applicationId == 0)
    {
        dbContext.JobApplications.Add(application);
    }

    await dbContext.SaveChangesAsync();
    return Results.LocalRedirect("/applications?saved=true");
}).RequireAuthorization();

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
    return Results.LocalRedirect("/profile");
}).AllowAnonymous();

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
