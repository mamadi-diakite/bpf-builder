namespace BpfBuilder.Domain.Tests;

public sealed class ActivityImportTests
{
    private static readonly Guid FiscalYearId = Guid.CreateVersion7();
    private static readonly ImportedSession Session = new(
        "S-1", "Spreadsheet basics", new DateOnly(2025, 2, 3), new DateOnly(2025, 2, 5), 21m, "Direct", "None", "Other", null, "324");

    [Fact]
    public void Keeps_the_rows_of_the_import()
    {
        var import = new ActivityImport(
            FiscalYearId,
            " activity.xlsx ",
            TestDates.Now,
            [Session],
            [new("Student 01", "S-1", "Employee", 21m)],
            [new("Trainer 01", "S-1", "Internal", 21m)],
            [new("1", 4200m, new DateOnly(2025, 2, 28), "S-1", "Invoice 1")],
            [new("Trainer salaries", 1800m, null)]);

        Assert.NotEqual(Guid.Empty, import.Id);
        Assert.Equal(FiscalYearId, import.FiscalYearId);
        Assert.Equal("activity.xlsx", import.FileName);
        Assert.Equal(TestDates.Now, import.ImportedAtUtc);
        Assert.Equal(Session, Assert.Single(import.Sessions));
        Assert.Equal("Student 01", Assert.Single(import.Learners).LearnerCode);
        Assert.Equal("Internal", Assert.Single(import.Trainers).Type);
        Assert.Equal(4200m, Assert.Single(import.Revenue).Amount);
        Assert.Null(Assert.Single(import.Costs).Label);
    }

    [Fact]
    public void Rejects_missing_fiscal_year_file_name_or_local_date()
    {
        Assert.Equal("fiscalYearId", Assert.Throws<ArgumentException>(() => Create(Guid.Empty, "a.xlsx", TestDates.Now)).ParamName);
        Assert.Equal("fileName", Assert.Throws<ArgumentException>(() => Create(FiscalYearId, " ", TestDates.Now)).ParamName);
        Assert.Equal("importedAtUtc", Assert.Throws<ArgumentException>(
            () => Create(FiscalYearId, "a.xlsx", DateTime.SpecifyKind(TestDates.Now, DateTimeKind.Local))).ParamName);
    }

    private static ActivityImport Create(Guid fiscalYearId, string fileName, DateTime importedAtUtc) =>
        new(fiscalYearId, fileName, importedAtUtc, [], [], [], [], []);
}
