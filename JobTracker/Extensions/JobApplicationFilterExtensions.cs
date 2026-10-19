using JobTracker.Models;

namespace JobTracker.Extensions;

public static class JobApplicationFilterExtensions
{
    public static IEnumerable<JobApplication> Filter(
        this IEnumerable<JobApplication> applications,
        ApplicationStatus? status = null,
        string? location = null,
        DateRange? dates = null)
    {
        ArgumentNullException.ThrowIfNull(applications);

        var result = applications;

        if (status is not null)
        {
            result = result.Where(a => a.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(location))
        {
            var place = location.Trim();
            result = result.Where(a => a.Location != null && a.Location.Contains(place, StringComparison.OrdinalIgnoreCase));
        }

        if (dates is { } range)
        {
            result = result.Where(a => range.Contains(a.ApplicationDate));
        }

        return result;
    }
}
