namespace BpfBuilder.Application.Tests;

public sealed class ActivityWorkbookParserTests
{
    private static readonly DateOnly YearStart = new(2025, 1, 1);
    private static readonly DateOnly YearEnd = new(2025, 12, 31);

    private static readonly Dictionary<string, TemplateSheet> SheetsByName =
        ActivityTemplate.Sheets.ToDictionary(s => s.Name);

    [Fact]
    public void Reads_the_sample_without_issues()
    {
        var parsed = Parse(WorkbookBuilder.Sample());

        Assert.Empty(parsed.Issues);
        Assert.False(parsed.HasErrors);
        Assert.Equal([5, 10, 6, 7, 3], [parsed.Sessions.Count, parsed.Learners.Count, parsed.Trainers.Count, parsed.Revenue.Count, parsed.Costs.Count]);
        Assert.Equal(
            new ImportedSession(
                "S-2025-02", "Web developer certificate", new DateOnly(2025, 3, 3), new DateOnly(2025, 6, 27), 399m, "Direct", "Partial", "RNCP", "5", "326"),
            parsed.Sessions[1]);
        Assert.Equal(new ImportedLearner("Student 05", "S-2025-02", "Job seeker", 385.5m), parsed.Learners[4]);
        Assert.Equal(new ImportedTrainer("Trainer 02", "S-2025-02", "External", 189m), parsed.Trainers[2]);
        Assert.Equal(new ImportedRevenue("11", 600m, new DateOnly(2025, 12, 15), null, "Course material sold"), parsed.Revenue[6]);
        Assert.Equal(new ImportedCost("Other costs", 2310m, "Room hire and course material"), parsed.Costs[2]);
    }

    [Fact]
    public void Rejects_a_missing_workbook()
    {
        Assert.Throws<ArgumentNullException>(() => ActivityWorkbookParser.Parse(null!, YearStart, YearEnd));
    }

    [Fact]
    public void Reports_a_missing_sheet()
    {
        var parsed = Parse(WorkbookBuilder.Sample().Remove(ActivityTemplate.Costs));

        Assert.Equal(
            new ImportIssue("Costs", null, null, "The Costs sheet is missing. Start from the latest template.", IssueSeverity.Error),
            Assert.Single(parsed.Issues));
        Assert.Empty(parsed.Costs);
        Assert.Equal(7, parsed.Revenue.Count);
    }

    [Fact]
    public void Reports_each_wrong_or_missing_header_and_skips_the_sheet()
    {
        var parsed = Parse(WorkbookBuilder.Sample().Headers(ActivityTemplate.Learners, "learner CODE", "Session", "Category"));

        var headerIssues = parsed.Issues.Where(i => i.Sheet == "Learners").ToList();
        Assert.Equal(
            [
                new ImportIssue("Learners", 1, "Session code", "Column B must be headed \"Session code\". Start from the latest template.", IssueSeverity.Error),
                new ImportIssue("Learners", 1, "Hours attended", "Column D must be headed \"Hours attended\". Start from the latest template.", IssueSeverity.Error),
            ],
            headerIssues);
        Assert.Empty(parsed.Learners);
    }

    [Fact]
    public void Warns_about_sessions_without_learners()
    {
        var parsed = Parse(WorkbookBuilder.Sample().Clear(ActivityTemplate.Learners));

        Assert.False(parsed.HasErrors);
        Assert.Equal(5, parsed.Sessions.Count);
        Assert.Equal(
            new ImportIssue("Sessions", 2, null, "Session S-2025-01 has no learner on the Learners sheet.", IssueSeverity.Warning),
            parsed.Issues[0]);
        Assert.Equal(5, parsed.Issues.Count);
    }

    [Theory]
    [InlineData("Sessions", 0, 0, null, "Session code", "Session code is required.")]
    [InlineData("Sessions", 1, 0, "s-2025-01", "Session code", "Session code s-2025-01 is used on more than one row.")]
    [InlineData("Sessions", 0, 2, "03/02/2025", "Start date", "Enter a date, for example 2025-02-03.")]
    [InlineData("Sessions", 0, 3, 2.5, "End date", "Enter a date, for example 2025-02-03.")]
    [InlineData("Sessions", 0, 3, "2025-02-01", "End date", "End date must be on or after the start date.")]
    [InlineData("Sessions", 0, 4, null, "Planned hours per learner", "Planned hours per learner is required.")]
    [InlineData("Sessions", 0, 4, "twenty", "Planned hours per learner", "Enter a number.")]
    [InlineData("Sessions", 0, 4, -1.0, "Planned hours per learner", "Enter zero or a positive number.")]
    [InlineData("Sessions", 0, 4, 1e13, "Planned hours per learner", "Enter a number.")]
    [InlineData("Sessions", 0, 4, "10000000000000", "Planned hours per learner", "Enter a number.")]
    [InlineData("Sessions", 0, 5, "Remote", "Delivery mode", "\"Remote\" is not in the Delivery modes list.")]
    [InlineData("Sessions", 0, 9, null, "NSF code", "NSF code is required.")]
    [InlineData("Sessions", 0, 8, "5", "RNCP level", "Leave the RNCP level empty unless the training goal is RNCP.")]
    [InlineData("Sessions", 1, 8, null, "RNCP level", "Enter the RNCP level of an RNCP training goal.")]
    [InlineData("Learners", 0, 1, "S-9", "Session code", "Session S-9 is not on the Sessions sheet.")]
    [InlineData("Learners", 0, 2, 5.0, "Category", "\"5\" is not in the Learner categories list.")]
    [InlineData("Learners", 0, 3, 21.5, "Hours attended", "Hours attended are above the 21 hours planned for the session.")]
    [InlineData("Trainers", 0, 1, null, "Session code", "Session code is required.")]
    [InlineData("Trainers", 0, 2, "Volunteer", "Type", "\"Volunteer\" is not in the Trainer types list.")]
    [InlineData("Revenue", 0, 2, null, "Date", "Date is required.")]
    [InlineData("Revenue", 0, 2, "2024-12-31", "Date", "The date is outside the fiscal year.")]
    [InlineData("Revenue", 0, 2, "2026-01-01", "Date", "The date is outside the fiscal year.")]
    [InlineData("Revenue", 0, 3, "S-9", "Session code", "Session S-9 is not on the Sessions sheet.")]
    [InlineData("Costs", 0, 0, "Rent", "Category", "\"Rent\" is not in the Cost categories list.")]
    [InlineData("Costs", 0, 1, null, "Amount excl. VAT", "Amount excl. VAT is required.")]
    public void Reports_a_wrong_cell_and_drops_its_row(string sheetName, int row, int column, object? value, string header, string message)
    {
        var sheet = SheetsByName[sheetName];
        var sample = SampleActivity.Rows[sheetName].Count;

        var parsed = Parse(WorkbookBuilder.Sample().Set(sheet, row, column, value));

        Assert.Contains(new ImportIssue(sheetName, row + 2, header, message, IssueSeverity.Error), parsed.Issues);
        Assert.True(parsed.HasErrors);
        Assert.Equal(sample - 1, Rows(parsed, sheetName));
    }

    [Fact]
    public void Rejects_text_longer_than_the_limit()
    {
        var parsed = Parse(WorkbookBuilder.Sample().Set(ActivityTemplate.Costs, 0, 2, new string('x', ActivityWorkbookParser.TextMaxLength + 1)));

        Assert.Equal(
            new ImportIssue("Costs", 2, "Label", "Use at most 200 characters.", IssueSeverity.Error),
            Assert.Single(parsed.Issues));
    }

    [Fact]
    public void Rejects_sessions_entirely_outside_the_fiscal_year()
    {
        var parsed = Parse(WorkbookBuilder.Sample()
            .Set(ActivityTemplate.Sessions, 0, 2, new DateOnly(2024, 2, 3))
            .Set(ActivityTemplate.Sessions, 0, 3, new DateOnly(2024, 2, 5))
            .Set(ActivityTemplate.Sessions, 4, 2, new DateOnly(2026, 1, 5))
            .Set(ActivityTemplate.Sessions, 4, 3, new DateOnly(2026, 1, 9)));

        const string Message = "The session is entirely outside the fiscal year.";
        Assert.Contains(new ImportIssue("Sessions", 2, "Start date", Message, IssueSeverity.Error), parsed.Issues);
        Assert.Contains(new ImportIssue("Sessions", 6, "Start date", Message, IssueSeverity.Error), parsed.Issues);
        Assert.Equal(3, parsed.Sessions.Count);
    }

    [Fact]
    public void Warns_about_sessions_over_the_edge_of_the_fiscal_year()
    {
        var parsed = Parse(WorkbookBuilder.Sample()
            .Set(ActivityTemplate.Sessions, 0, 2, new DateOnly(2024, 12, 30))
            .Set(ActivityTemplate.Sessions, 4, 3, new DateOnly(2026, 1, 9)));

        const string Message = "The session runs over the edge of the fiscal year; check its hours belong to this year.";
        Assert.Equal(
            [
                new ImportIssue("Sessions", 2, "Start date", Message, IssueSeverity.Warning),
                new ImportIssue("Sessions", 6, "Start date", Message, IssueSeverity.Warning),
            ],
            parsed.Issues);
        Assert.False(parsed.HasErrors);
        Assert.Equal(5, parsed.Sessions.Count);
    }

    [Fact]
    public void Reads_text_dates_and_numbers_and_numeric_or_date_codes()
    {
        var parsed = Parse(WorkbookBuilder.Sample()
            .Set(ActivityTemplate.Sessions, 0, 2, "2025-02-03")
            .Set(ActivityTemplate.Sessions, 0, 4, "21")
            .Set(ActivityTemplate.Revenue, 0, 0, 3)
            .Set(ActivityTemplate.Learners, 0, 0, new DateOnly(2025, 1, 1)));

        Assert.Empty(parsed.Issues);
        Assert.Equal(new DateOnly(2025, 2, 3), parsed.Sessions[0].StartDate);
        Assert.Equal(21m, parsed.Sessions[0].PlannedHours);
        Assert.Equal("3", parsed.Revenue[0].FormLine);
        Assert.Equal("2025-01-01", parsed.Learners[0].LearnerCode);
    }

    [Fact]
    public void Keeps_learners_of_a_session_with_errors_without_comparing_hours()
    {
        var parsed = Parse(WorkbookBuilder.Sample()
            .Set(ActivityTemplate.Sessions, 0, 5, "Remote")
            .Set(ActivityTemplate.Learners, 0, 3, 99));

        Assert.Single(parsed.Issues);
        Assert.Equal(10, parsed.Learners.Count);
        Assert.Equal(4, parsed.Sessions.Count);
    }

    [Fact]
    public void Treats_missing_trailing_cells_as_empty()
    {
        var parsed = Parse(WorkbookBuilder.Sample().Add(ActivityTemplate.Costs, "Other costs"));

        Assert.Equal(
            new ImportIssue("Costs", 5, "Amount excl. VAT", "Amount excl. VAT is required.", IssueSeverity.Error),
            Assert.Single(parsed.Issues));
    }

    private static ParsedActivity Parse(WorkbookBuilder builder) =>
        ActivityWorkbookParser.Parse(builder.Build(), YearStart, YearEnd);

    private static int Rows(ParsedActivity parsed, string sheetName) => sheetName switch
    {
        "Sessions" => parsed.Sessions.Count,
        "Learners" => parsed.Learners.Count,
        "Trainers" => parsed.Trainers.Count,
        "Revenue" => parsed.Revenue.Count,
        _ => parsed.Costs.Count,
    };
}
