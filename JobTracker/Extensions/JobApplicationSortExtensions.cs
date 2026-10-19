using JobTracker.Models;

namespace JobTracker.Extensions;

public static class JobApplicationSortExtensions
{
    public static IOrderedQueryable<JobApplication> SortByDate(this IQueryable<JobApplication> applications, bool newestFirst = true)
    {
        ArgumentNullException.ThrowIfNull(applications);

        // Id as a tie-breaker so pages stay stable when dates are equal
        return newestFirst
            ? applications.OrderByDescending(a => a.ApplicationDate).ThenByDescending(a => a.Id)
            : applications.OrderBy(a => a.ApplicationDate).ThenBy(a => a.Id);
    }

    public static IEnumerable<JobApplication> SortByDate(this IEnumerable<JobApplication> applications, bool newestFirst = true)
    {
        ArgumentNullException.ThrowIfNull(applications);

        return applications.AsQueryable().SortByDate(newestFirst);
    }
}
