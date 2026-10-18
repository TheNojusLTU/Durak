using JobTracker.Models;

namespace JobTracker.Extensions;

public static class JobApplicationSortExtensions
{
    public static IEnumerable<JobApplication> SortByDate(this IEnumerable<JobApplication> applications, bool newestFirst = true)
    {
        ArgumentNullException.ThrowIfNull(applications);

        return newestFirst
            ? applications.OrderByDescending(a => a.ApplicationDate)
            : applications.OrderBy(a => a.ApplicationDate);
    }
}
