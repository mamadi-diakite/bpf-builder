namespace BpfBuilder.Domain;

/// <summary>The accounting period a BPF covers.</summary>
public sealed class FiscalYear
{
    private FiscalYear()
    {
    }

    public FiscalYear(Guid organizationId, DateOnly startDate, DateOnly endDate, decimal totalTurnover, DateTime createdAtUtc)
    {
        Id = Guid.CreateVersion7();
        OrganizationId = Guard.NotEmpty(organizationId, nameof(organizationId));
        CreatedAtUtc = Guard.Utc(createdAtUtc, nameof(createdAtUtc));
        Apply(startDate, endDate, totalTurnover);
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public DateOnly StartDate { get; private set; }

    public DateOnly EndDate { get; private set; }

    /// <summary>Total turnover of the organization excluding VAT, training or not.</summary>
    public decimal TotalTurnover { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public static FiscalYear CalendarYear(Guid organizationId, int year, decimal totalTurnover, DateTime createdAtUtc) =>
        new(organizationId, new DateOnly(year, 1, 1), new DateOnly(year, 12, 31), totalTurnover, createdAtUtc);

    public bool Contains(DateOnly date) => date >= StartDate && date <= EndDate;

    public void Update(DateOnly startDate, DateOnly endDate, decimal totalTurnover, DateTime nowUtc)
    {
        var updatedAt = Guard.Utc(nowUtc, nameof(nowUtc));
        Apply(startDate, endDate, totalTurnover);
        UpdatedAtUtc = updatedAt;
    }

    private void Apply(DateOnly startDate, DateOnly endDate, decimal totalTurnover)
    {
        if (endDate <= startDate)
        {
            throw new ArgumentException("End date must be after start date.", nameof(endDate));
        }

        TotalTurnover = Guard.NotNegative(totalTurnover, nameof(totalTurnover));
        StartDate = startDate;
        EndDate = endDate;
    }
}
