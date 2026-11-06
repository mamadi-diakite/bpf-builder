namespace BpfBuilder.Application.Tests;

public sealed class ActivityImportServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 6, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid OwnerId = Guid.NewGuid();

    private readonly InMemoryOrganizationRepository _organizations = new();
    private readonly InMemoryActivityImportRepository _imports = new();
    private readonly FiscalYear _fiscalYear;

    public ActivityImportServiceTests()
    {
        var organization = new Organization(OwnerId, "Northwind Training", Now.UtcDateTime);
        _organizations.Add(organization);
        _fiscalYear = FiscalYear.CalendarYear(organization.Id, 2025, 0m, Now.UtcDateTime);
        _organizations.AddFiscalYear(_fiscalYear);
    }

    [Fact]
    public async Task Imports_a_valid_workbook_and_reports_its_counts()
    {
        var service = Service(WorkbookBuilder.Sample().Clear(ActivityTemplate.Costs).Build());

        var result = await Import(service, "uploads/ activity 2025.XLSX ");

        Assert.Equal(ImportStatus.Imported, result.Status);
        Assert.Equal(new ImportCountsResponse(5, 10, 6, 7, 0), result.Report!.Counts);
        Assert.True(result.Report.Imported);
        Assert.Empty(result.Report.Issues);
        var import = Assert.Single(_imports.Imports);
        Assert.Equal(_fiscalYear.Id, import.FiscalYearId);
        Assert.Equal("activity 2025.XLSX", import.FileName);
        Assert.Equal(Now.UtcDateTime, import.ImportedAtUtc);
        Assert.Equal(1, _imports.SaveCount);

        Assert.Equal(
            new ActivityImportResponse("activity 2025.XLSX", Now.UtcDateTime, new(5, 10, 6, 7, 0)),
            await service.GetAsync(OwnerId, _fiscalYear.Id));
    }

    [Fact]
    public async Task Saves_nothing_while_the_report_has_errors()
    {
        var service = Service(WorkbookBuilder.Sample().Set(ActivityTemplate.Costs, 0, 0, "Rent").Clear(ActivityTemplate.Learners).Build());

        var result = await Import(service);

        Assert.Equal(ImportStatus.HasErrors, result.Status);
        Assert.False(result.Report!.Imported);
        Assert.Equal(new ImportCountsResponse(5, 0, 6, 7, 2), result.Report.Counts);
        Assert.Contains(new ImportIssueResponse("Costs", 2, "Category", "\"Rent\" is not in the Cost categories list.", "Error"), result.Report.Issues);
        Assert.Contains(result.Report.Issues, i => i.Severity == "Warning");
        Assert.Empty(_imports.Imports);
        Assert.Equal(0, _imports.SaveCount);
    }

    [Fact]
    public async Task Imports_a_workbook_with_warnings_only()
    {
        var service = Service(WorkbookBuilder.Sample().Clear(ActivityTemplate.Learners).Build());

        var result = await Import(service);

        Assert.Equal(ImportStatus.Imported, result.Status);
        Assert.Equal(5, result.Report!.Issues.Count);
        Assert.Single(_imports.Imports);
    }

    [Fact]
    public async Task Refuses_a_second_import_of_the_same_year()
    {
        var reader = new FakeWorkbookReader(WorkbookBuilder.Sample().Build());
        var service = Service(reader);
        await Import(service);

        var result = await Import(service);

        Assert.Equal(new ImportResult(ImportStatus.AlreadyImported), result);
        Assert.Single(_imports.Imports);
        Assert.Equal(1, reader.ReadCount);
    }

    [Fact]
    public async Task Finds_nothing_for_another_users_year_or_a_user_without_organization()
    {
        var service = Service(WorkbookBuilder.Sample().Build());

        Assert.Equal(ImportStatus.NotFound, (await Import(service, fiscalYearId: Guid.NewGuid())).Status);
        Assert.Equal(ImportStatus.NotFound, (await Import(service, ownerId: Guid.NewGuid())).Status);
        Assert.Null(await service.GetAsync(OwnerId, Guid.NewGuid()));
        Assert.Null(await service.GetAsync(Guid.NewGuid(), _fiscalYear.Id));
        Assert.Null(await service.GetAsync(OwnerId, _fiscalYear.Id));
    }

    [Theory]
    [InlineData("activity.xlsx", false, 1L, "Choose a filled activity template.")]
    [InlineData("activity.xlsx", true, 0L, "Choose a filled activity template.")]
    [InlineData("activity.xlsx", true, ActivityImportService.MaxFileBytes + 1, "The file is larger than 10 MB.")]
    [InlineData("activity.xls", true, 1L, "Only Excel .xlsx files can be imported.")]
    [InlineData(null, true, 1L, "Only Excel .xlsx files can be imported.")]
    public async Task Rejects_a_missing_large_or_non_xlsx_file(string? fileName, bool withContent, long length, string message)
    {
        var reader = new FakeWorkbookReader(WorkbookBuilder.Sample().Build());
        var service = Service(reader);
        using var content = new MemoryStream([1]);

        var result = await service.ImportAsync(OwnerId, _fiscalYear.Id, fileName, withContent ? content : null, length);

        Assert.Equal(new ImportResult(ImportStatus.InvalidFile, FileError: message), result);
        Assert.Equal(0, reader.ReadCount);
    }

    [Fact]
    public async Task Rejects_a_file_name_longer_than_the_limit()
    {
        var service = Service(WorkbookBuilder.Sample().Build());

        var result = await Import(service, new string('a', ActivityImport.FileNameMaxLength - 4) + ".xlsx");

        Assert.Equal("The file name is longer than 255 characters.", result.FileError);
    }

    [Fact]
    public async Task Rejects_an_unreadable_workbook()
    {
        var service = Service((ActivityWorkbook?)null);

        var result = await Import(service);

        Assert.Equal(new ImportResult(ImportStatus.InvalidFile, FileError: "This file is not a readable Excel workbook."), result);
        Assert.Empty(_imports.Imports);
    }

    private ActivityImportService Service(ActivityWorkbook? workbook) => Service(new FakeWorkbookReader(workbook));

    private ActivityImportService Service(FakeWorkbookReader reader) =>
        new(_organizations, _imports, reader, new FixedTimeProvider(Now));

    private async Task<ImportResult> Import(
        ActivityImportService service,
        string fileName = "activity.xlsx",
        Guid? fiscalYearId = null,
        Guid? ownerId = null)
    {
        using var content = new MemoryStream([1, 2, 3]);
        return await service.ImportAsync(ownerId ?? OwnerId, fiscalYearId ?? _fiscalYear.Id, fileName, content, content.Length);
    }
}
