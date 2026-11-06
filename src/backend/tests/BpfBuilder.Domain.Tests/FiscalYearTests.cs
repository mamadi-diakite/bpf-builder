namespace BpfBuilder.Domain.Tests;

public sealed class FiscalYearTests
{
    private static readonly Guid OrganizationId = Guid.NewGuid();

    [Fact]
    public void Creates_a_fiscal_year()
    {
        var year = new FiscalYear(OrganizationId, new DateOnly(2025, 7, 1), new DateOnly(2026, 6, 30), 250_000m, TestDates.Now);

        Assert.NotEqual(Guid.Empty, year.Id);
        Assert.Equal(OrganizationId, year.OrganizationId);
        Assert.Equal(new DateOnly(2025, 7, 1), year.StartDate);
        Assert.Equal(new DateOnly(2026, 6, 30), year.EndDate);
        Assert.Equal(250_000m, year.TotalTurnover);
        Assert.Equal(TestDates.Now, year.CreatedAtUtc);
        Assert.Equal(TestDates.Now, year.UpdatedAtUtc);
    }

    [Fact]
    public void Calendar_year_runs_from_january_to_december()
    {
        var year = FiscalYear.CalendarYear(OrganizationId, 2025, 0m, TestDates.Now);

        Assert.Equal(new DateOnly(2025, 1, 1), year.StartDate);
        Assert.Equal(new DateOnly(2025, 12, 31), year.EndDate);
    }

    [Theory]
    [InlineData(2025, 1, 1, true)]
    [InlineData(2025, 12, 31, true)]
    [InlineData(2024, 12, 31, false)]
    [InlineData(2026, 1, 1, false)]
    public void Contains_includes_both_ends(int year, int month, int day, bool expected)
    {
        var fiscalYear = FiscalYear.CalendarYear(OrganizationId, 2025, 0m, TestDates.Now);

        Assert.Equal(expected, fiscalYear.Contains(new DateOnly(year, month, day)));
    }

    [Theory]
    [InlineData(2025, 12, 31)]
    [InlineData(2025, 1, 1)]
    public void End_date_must_be_after_start_date(int year, int month, int day)
    {
        var error = Assert.Throws<ArgumentException>(
            () => new FiscalYear(OrganizationId, new DateOnly(2025, 12, 31), new DateOnly(year, month, day), 0m, TestDates.Now));

        Assert.Equal("endDate", error.ParamName);
    }

    [Fact]
    public void Rejects_negative_turnover_empty_organization_and_local_date()
    {
        Assert.Equal("totalTurnover", Assert.Throws<ArgumentOutOfRangeException>(
            () => FiscalYear.CalendarYear(OrganizationId, 2025, -1m, TestDates.Now)).ParamName);
        Assert.Equal("organizationId", Assert.Throws<ArgumentException>(
            () => FiscalYear.CalendarYear(Guid.Empty, 2025, 0m, TestDates.Now)).ParamName);
        Assert.Equal("createdAtUtc", Assert.Throws<ArgumentException>(
            () => FiscalYear.CalendarYear(OrganizationId, 2025, 0m, DateTime.SpecifyKind(TestDates.Now, DateTimeKind.Local))).ParamName);
    }

    [Fact]
    public void Update_changes_dates_and_turnover()
    {
        var year = FiscalYear.CalendarYear(OrganizationId, 2025, 0m, TestDates.Now);
        var later = TestDates.Now.AddDays(1);

        year.Update(new DateOnly(2025, 4, 1), new DateOnly(2026, 3, 31), 120_000m, later);

        Assert.Equal(new DateOnly(2025, 4, 1), year.StartDate);
        Assert.Equal(new DateOnly(2026, 3, 31), year.EndDate);
        Assert.Equal(120_000m, year.TotalTurnover);
        Assert.Equal(later, year.UpdatedAtUtc);
    }

    [Fact]
    public void Failed_update_changes_nothing()
    {
        var year = FiscalYear.CalendarYear(OrganizationId, 2025, 10m, TestDates.Now);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => year.Update(new DateOnly(2025, 4, 1), new DateOnly(2026, 3, 31), -5m, TestDates.Now.AddDays(1)));
        Assert.Equal("nowUtc", Assert.Throws<ArgumentException>(
            () => year.Update(new DateOnly(2025, 4, 1), new DateOnly(2026, 3, 31), 5m,
                DateTime.SpecifyKind(TestDates.Now, DateTimeKind.Local))).ParamName);

        Assert.Equal(new DateOnly(2025, 1, 1), year.StartDate);
        Assert.Equal(10m, year.TotalTurnover);
        Assert.Equal(TestDates.Now, year.UpdatedAtUtc);
    }
}
