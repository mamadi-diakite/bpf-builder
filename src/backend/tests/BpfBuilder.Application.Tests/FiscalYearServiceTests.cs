namespace BpfBuilder.Application.Tests;

public sealed class FiscalYearServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 6, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid OwnerId = Guid.NewGuid();

    private readonly InMemoryOrganizationRepository _organizations = new();
    private readonly InMemoryActivityImportRepository _imports = new();
    private readonly FiscalYearService _service;
    private readonly Organization _organization = new(OwnerId, "Northwind Training", Now.UtcDateTime);

    public FiscalYearServiceTests()
    {
        _service = new FiscalYearService(_organizations, _imports, new FixedTimeProvider(Now));
    }

    [Fact]
    public async Task Without_organization_there_is_nothing_to_list_and_nothing_to_create()
    {
        Assert.Empty(await _service.ListAsync(OwnerId));
        Assert.Null(await _service.GetAsync(OwnerId, Guid.NewGuid()));
        Assert.Equal(ServiceResultStatus.NotFound, (await _service.CreateAsync(OwnerId, new(null, null, 0m))).Status);
    }

    [Fact]
    public async Task Empty_dates_create_the_last_calendar_year()
    {
        _organizations.Add(_organization);

        var result = await _service.CreateAsync(OwnerId, new CreateFiscalYearRequest(null, null, 250_000m));

        Assert.Equal(ServiceResultStatus.Ok, result.Status);
        Assert.Equal(
            new FiscalYearResponse(result.Value!.Id, new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31), 250_000m, Now.UtcDateTime, null),
            result.Value);
        Assert.Equal(_organization.Id, Assert.Single(_organizations.FiscalYears).OrganizationId);
        Assert.Equal(1, _organizations.SaveCount);
    }

    [Fact]
    public async Task Lists_and_gets_fiscal_years_latest_first()
    {
        _organizations.Add(_organization);
        await _service.CreateAsync(OwnerId, new CreateFiscalYearRequest(new DateOnly(2023, 7, 1), new DateOnly(2024, 6, 30), 1m));
        await _service.CreateAsync(OwnerId, new CreateFiscalYearRequest(new DateOnly(2024, 7, 1), new DateOnly(2025, 6, 30), 2m));

        var list = await _service.ListAsync(OwnerId);

        Assert.Equal([2m, 1m], list.Select(y => y.TotalTurnover));
        Assert.Equal(list[1], await _service.GetAsync(OwnerId, list[1].Id));
        Assert.Null(await _service.GetAsync(OwnerId, Guid.NewGuid()));
    }

    [Fact]
    public async Task Shows_when_the_activity_of_a_year_was_imported()
    {
        _organizations.Add(_organization);
        var imported = FiscalYear.CalendarYear(_organization.Id, 2025, 0m, Now.UtcDateTime);
        var notImported = FiscalYear.CalendarYear(_organization.Id, 2024, 0m, Now.UtcDateTime);
        _organizations.AddFiscalYear(imported);
        _organizations.AddFiscalYear(notImported);
        var importedAt = Now.UtcDateTime.AddHours(1);
        _imports.Add(new ActivityImport(imported.Id, "activity.xlsx", importedAt, [], [], [], [], []));

        var list = await _service.ListAsync(OwnerId);

        Assert.Equal<DateTime?>([importedAt, null], list.Select(y => y.ImportedAtUtc));
        Assert.Equal(importedAt, (await _service.GetAsync(OwnerId, imported.Id))!.ImportedAtUtc);
        Assert.Null((await _service.GetAsync(OwnerId, notImported.Id))!.ImportedAtUtc);
    }

    [Theory]
    [InlineData("2025-01-01", null, "endDate", "Enter both dates, or none for the last calendar year.")]
    [InlineData(null, "2025-12-31", "startDate", "Enter both dates, or none for the last calendar year.")]
    [InlineData("2025-01-01", "2025-01-01", "endDate", "End date must be after the start date.")]
    [InlineData("2025-06-01", "2026-05-31", "startDate", "This period overlaps another fiscal year.")]
    [InlineData("2024-02-01", "2025-01-01", "startDate", "This period overlaps another fiscal year.")]
    public async Task Rejects_invalid_periods(string? start, string? end, string field, string message)
    {
        _organizations.Add(_organization);
        _organizations.AddFiscalYear(FiscalYear.CalendarYear(_organization.Id, 2025, 0m, Now.UtcDateTime));

        var result = await _service.CreateAsync(OwnerId, new CreateFiscalYearRequest(ParseDate(start), ParseDate(end), 0m));

        Assert.Equal(ServiceResultStatus.Invalid, result.Status);
        Assert.Equal([message], result.Errors[field]);
        Assert.Single(_organizations.FiscalYears);
    }

    [Fact]
    public async Task Accepts_periods_right_before_and_after_an_existing_year()
    {
        _organizations.Add(_organization);
        _organizations.AddFiscalYear(FiscalYear.CalendarYear(_organization.Id, 2025, 0m, Now.UtcDateTime));

        var before = await _service.CreateAsync(OwnerId, new CreateFiscalYearRequest(new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31), 0m));
        var after = await _service.CreateAsync(OwnerId, new CreateFiscalYearRequest(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), 0m));

        Assert.Equal(ServiceResultStatus.Ok, before.Status);
        Assert.Equal(ServiceResultStatus.Ok, after.Status);
    }

    [Fact]
    public async Task Reports_negative_turnover_with_or_without_a_valid_period()
    {
        _organizations.Add(_organization);

        var valid = await _service.CreateAsync(OwnerId, new CreateFiscalYearRequest(null, null, -1m));
        var invalid = await _service.CreateAsync(OwnerId, new CreateFiscalYearRequest(new DateOnly(2025, 1, 1), null, -1m));

        Assert.Equal(["Turnover cannot be negative."], valid.Errors["totalTurnover"]);
        Assert.Equal(["endDate", "totalTurnover"], invalid.Errors.Keys.Order());
        Assert.Empty(_organizations.FiscalYears);
    }

    [Fact]
    public async Task Rejects_a_missing_request()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.CreateAsync(OwnerId, null!));
    }

    private static DateOnly? ParseDate(string? value) => value is null ? null : DateOnly.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
}
