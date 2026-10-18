using JobTracker.Models;

namespace JobTracker.Extensions;

public static class JobApplicationSearchExtensions
{
    public static IEnumerable<JobApplication> Search(this IEnumerable<JobApplication> applications, string? text)
    {
        ArgumentNullException.ThrowIfNull(applications);

        if (string.IsNullOrWhiteSpace(text))
        {
            return applications;
        }

        var term = text.Trim();

        return applications.Where(a =>
            a.CompanyName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
            a.JobTitle.Contains(term, StringComparison.OrdinalIgnoreCase));
    }
}
