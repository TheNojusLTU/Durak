using System.Security.Claims;
using JobTracker.Data;
using JobTracker.Extensions;
using JobTracker.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Api;

public static class ApplicationEndpoints
{
    public static IEndpointRouteBuilder MapApplicationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/applications")
            .RequireAuthorization()
            .WithTags("Applications");

        group.MapGet("/", GetApplications)
            .WithName("GetApplications")
            .WithSummary("Lists the current user's applications, with optional search, filters and sorting.");
        group.MapGet("/{id:int}", GetApplication)
            .WithName("GetApplication");
        group.MapPost("/", CreateApplication)
            .WithName("CreateApplication");
        group.MapPut("/{id:int}", UpdateApplication)
            .WithName("UpdateApplication");
        group.MapDelete("/{id:int}", DeleteApplication)
            .WithName("DeleteApplication");

        return app;
    }

    public static async Task<Results<Ok<List<ApplicationResponse>>, ValidationProblem>> GetApplications(
        ClaimsPrincipal user,
        ApplicationDbContext dbContext,
        string? search = null,
        ApplicationStatus? status = null,
        string? location = null,
        DateTime? from = null,
        DateTime? to = null,
        bool newestFirst = true)
    {
        if (from is not null && to is not null && from.Value.Date > to.Value.Date)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["from"] = ["From date can't be after To date."]
            });
        }

        var userId = GetUserId(user);
        var applications = await dbContext.JobApplications
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .ToListAsync();

        DateRange? dates = from is null && to is null ? null : new DateRange(from, to);

        var result = applications
            .Search(search)
            .Filter(status: status, location: location, dates: dates)
            .SortByDate(newestFirst)
            .Select(ApplicationResponse.From)
            .ToList();

        return TypedResults.Ok(result);
    }

    public static async Task<Results<Ok<ApplicationResponse>, NotFound>> GetApplication(
        int id,
        ClaimsPrincipal user,
        ApplicationDbContext dbContext)
    {
        var application = await FindOwnedAsync(dbContext.JobApplications.AsNoTracking(), id, user);

        return application is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(ApplicationResponse.From(application));
    }

    public static async Task<Results<Created<ApplicationResponse>, ValidationProblem>> CreateApplication(
        ApplicationRequest request,
        ClaimsPrincipal user,
        ApplicationDbContext dbContext)
    {
        var errors = request.Validate();
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var application = new JobApplication { UserId = GetUserId(user) };
        request.ApplyTo(application);

        dbContext.JobApplications.Add(application);
        await dbContext.SaveChangesAsync();

        return TypedResults.Created($"/api/applications/{application.Id}", ApplicationResponse.From(application));
    }

    public static async Task<Results<Ok<ApplicationResponse>, NotFound, ValidationProblem>> UpdateApplication(
        int id,
        ApplicationRequest request,
        ClaimsPrincipal user,
        ApplicationDbContext dbContext)
    {
        var errors = request.Validate();
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var application = await FindOwnedAsync(dbContext.JobApplications, id, user);
        if (application is null)
        {
            return TypedResults.NotFound();
        }

        request.ApplyTo(application);
        await dbContext.SaveChangesAsync();

        return TypedResults.Ok(ApplicationResponse.From(application));
    }

    public static async Task<Results<NoContent, NotFound>> DeleteApplication(
        int id,
        ClaimsPrincipal user,
        ApplicationDbContext dbContext)
    {
        var application = await FindOwnedAsync(dbContext.JobApplications, id, user);
        if (application is null)
        {
            return TypedResults.NotFound();
        }

        dbContext.JobApplications.Remove(application);
        await dbContext.SaveChangesAsync();

        return TypedResults.NoContent();
    }

    // other users' applications return 404 too, so ids of other users can't be probed
    private static Task<JobApplication?> FindOwnedAsync(IQueryable<JobApplication> applications, int id, ClaimsPrincipal user)
    {
        var userId = GetUserId(user);
        return applications.SingleOrDefaultAsync(a => a.Id == id && a.UserId == userId);
    }

    private static string GetUserId(ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("Authenticated user has no id claim.");
}
