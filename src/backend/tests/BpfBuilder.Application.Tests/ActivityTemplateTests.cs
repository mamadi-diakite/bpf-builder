namespace BpfBuilder.Application.Tests;

public sealed class ActivityTemplateTests
{
    private static IEnumerable<TemplateColumn> AllColumns => ActivityTemplate.Sheets.SelectMany(sheet => sheet.Columns);

    [Fact]
    public void Has_the_five_activity_sheets_in_order()
    {
        Assert.Equal(["Sessions", "Learners", "Trainers", "Revenue", "Costs"], ActivityTemplate.Sheets.Select(sheet => sheet.Name));
    }

    [Fact]
    public void Coded_columns_and_only_them_have_a_reference_list()
    {
        Assert.All(AllColumns, column => Assert.Equal(column.Kind == ColumnKind.Code, column.List is not null));
    }

    [Fact]
    public void Every_column_is_explained_with_a_valid_example()
    {
        Assert.All(AllColumns, column =>
        {
            Assert.False(string.IsNullOrWhiteSpace(column.Description));
            Assert.True(IsValid(column, column.Example), $"{column.Header}: {column.Example}");
        });
    }

    [Fact]
    public void Has_no_name_columns_for_people()
    {
        Assert.DoesNotContain(AllColumns, column => column.Header.Contains("name", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Sample_rows_match_their_sheet_columns()
    {
        Assert.Equal(ActivityTemplate.Sheets.Select(sheet => sheet.Name), SampleActivity.Rows.Keys);

        foreach (var sheet in ActivityTemplate.Sheets)
        {
            Assert.NotEmpty(SampleActivity.Rows[sheet.Name]);
            Assert.All(SampleActivity.Rows[sheet.Name], row =>
            {
                Assert.Equal(sheet.Columns.Count, row.Count);
                Assert.All(sheet.Columns.Zip(row), pair => Assert.True(
                    pair.Second is null ? !pair.First.Required : IsValid(pair.First, pair.Second),
                    $"{sheet.Name} / {pair.First.Header}: {pair.Second}"));
            });
        }
    }

    [Fact]
    public void Sample_activity_is_consistent()
    {
        var sessions = SampleActivity.Rows["Sessions"].ToDictionary(row => (string)row[0]!);

        Assert.All(sessions.Values, session =>
        {
            Assert.True((DateOnly)session[3]! >= (DateOnly)session[2]!);
            Assert.Equal((string?)session[7] == ReferenceLists.RncpGoal, session[8] is not null);
        });
        Assert.All(SampleActivity.Rows["Learners"], learner =>
            Assert.True((decimal)learner[3]! <= (decimal)sessions[(string)learner[1]!][4]!));
        Assert.All(SampleActivity.Rows["Trainers"], trainer => Assert.Contains((string)trainer[1]!, sessions.Keys));
        Assert.All(SampleActivity.Rows["Revenue"], revenue =>
            Assert.True(revenue[3] is null || sessions.ContainsKey((string)revenue[3]!)));
    }

    private static bool IsValid(TemplateColumn column, object value) => column.Kind switch
    {
        ColumnKind.Date => value is DateOnly || DateOnly.TryParseExact(value as string, "yyyy-MM-dd", out _),
        ColumnKind.Number => value is decimal number ? number >= 0 : decimal.TryParse(value as string, out var parsed) && parsed >= 0,
        ColumnKind.Code => column.List!.Find(value as string) is not null,
        _ => value is string text && !string.IsNullOrWhiteSpace(text),
    };
}
