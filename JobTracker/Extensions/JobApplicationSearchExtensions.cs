using JobTracker.Models;

namespace JobTracker.Extensions;

public static class JobApplicationSearchExtensions
{
    public static IQueryable<JobApplication> Search(this IQueryable<JobApplication> applications, string? text)
    {
        ArgumentNullException.ThrowIfNull(applications);

        if (string.IsNullOrWhiteSpace(text))
        {
            return applications;
        }

        var term = text.Trim().ToLower();

        // ToLower + Contains instead of StringComparison so EF can turn it into SQL
        return applications.Where(a =>
            a.CompanyName.ToLower().Contains(term) ||
            a.JobTitle.ToLower().Contains(term));
    }

    public static IEnumerable<JobApplication> Search(this IEnumerable<JobApplication> applications, string? text)
    {
        ArgumentNullException.ThrowIfNull(applications);

        return applications.AsQueryable().Search(text);
    }
}
