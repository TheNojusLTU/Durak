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

    // start of the From day, in UTC because postgres timestamptz only compares with UTC values
    public DateTime? StartUtc => From is null ? null : DateTime.SpecifyKind(From.Value.Date, DateTimeKind.Utc);

    // start of the day after To, so the whole To day is included
    public DateTime? EndUtc => To is null ? null : DateTime.SpecifyKind(To.Value.Date.AddDays(1), DateTimeKind.Utc);

    public bool Contains(DateTime date)
    {
        return (StartUtc is null || date >= StartUtc) &&
               (EndUtc is null || date < EndUtc);
    }
}
