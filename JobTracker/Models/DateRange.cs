namespace JobTracker.Models;

public readonly struct DateRange
{
    public DateRange(DateTime? from, DateTime? to)
    {
        if (from is not null && to is not null && from.Value.Date > to.Value.Date)
        {
            throw new ArgumentException("From date can't be after To date.", nameof(from));
        }

        From = from;
        To = to;
    }

    public DateTime? From { get; }

    public DateTime? To { get; }

    public bool Contains(DateTime date)
    {
        // compare whole days so the To day is included
        return (From is null || date.Date >= From.Value.Date) &&
               (To is null || date.Date <= To.Value.Date);
    }
}
