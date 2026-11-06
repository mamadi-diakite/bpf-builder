namespace BpfBuilder.Infrastructure.Tests;

public sealed class ClosedXmlActivityWorkbookWriterTests
{
    [Fact]
    public void Template_has_instructions_data_sheets_and_reference_lists()
    {
        using var workbook = Write(withSample: false);

        Assert.Equal(
            ["Instructions", "Sessions", "Learners", "Trainers", "Revenue", "Costs", "Reference lists"],
            workbook.Worksheets.Select(sheet => sheet.Name));
        Assert.Equal("1", workbook.CustomProperty(ClosedXmlActivityWorkbookWriter.VersionProperty).GetValue<string>());

        var instructions = workbook.Worksheet("Instructions");
        Assert.Equal("1", instructions.Cell(2, 2).GetString());
        Assert.StartsWith("Fill each sheet", instructions.Cell(3, 1).GetString());
        var explained = instructions.Range(8, 1, 7 + ActivityTemplate.Sheets.Sum(sheet => sheet.Columns.Count), 5).Rows().ToList();
        Assert.Equal(
            ActivityTemplate.Sheets.SelectMany(sheet => sheet.Columns.Select(column => $"{sheet.Name}/{column.Header}")),
            explained.Select(row => $"{row.Cell(1).GetString()}/{row.Cell(2).GetString()}"));
        Assert.Contains(explained, row => row.Cell(3).GetString() == "No");
        Assert.Contains(explained, row => row.Cell(3).GetString() == "Yes");
    }

    [Fact]
    public void Data_sheets_have_headers_and_no_rows()
    {
        using var workbook = Write(withSample: false);

        foreach (var sheet in ActivityTemplate.Sheets)
        {
            var worksheet = workbook.Worksheet(sheet.Name);
            Assert.Equal(sheet.Columns.Select(column => column.Header), worksheet.Row(1).CellsUsed().Select(cell => cell.GetString()));
            Assert.Equal(1, worksheet.LastRowUsed()!.RowNumber());
            Assert.Equal(1, worksheet.SheetView.SplitRow);
        }
    }

    [Fact]
    public void Every_column_has_a_check_matching_its_kind()
    {
        using var workbook = Write(withSample: false);

        foreach (var sheet in ActivityTemplate.Sheets)
        {
            var worksheet = workbook.Worksheet(sheet.Name);
            for (var index = 0; index < sheet.Columns.Count; index++)
            {
                var column = sheet.Columns[index];
                var validation = ValidationOf(worksheet, index);

                Assert.Equal(column.Description, validation.InputMessage);
                Assert.False(string.IsNullOrEmpty(validation.ErrorMessage));
                Assert.Equal(Expected(column), validation.AllowedValues);
                if (column.Kind == ColumnKind.SessionCode)
                {
                    Assert.Equal("Sessions!$A$2:$A$1000", validation.Value.TrimStart('='));
                }
            }
        }
    }

    [Fact]
    public void Drop_down_lists_point_to_their_reference_list()
    {
        using var workbook = Write(withSample: false);
        var lists = workbook.Worksheet("Reference lists");

        Assert.True(lists.IsProtected);
        foreach (var sheet in ActivityTemplate.Sheets)
        {
            for (var index = 0; index < sheet.Columns.Count; index++)
            {
                if (sheet.Columns[index].List is not { } list)
                {
                    continue;
                }

                var formula = ValidationOf(workbook.Worksheet(sheet.Name), index).Value;
                var codes = lists.Range(formula.Split('!')[1].Replace("$", "", StringComparison.Ordinal)).Cells().Select(cell => cell.GetString());

                Assert.Equal(list.Items.Select(item => item.Code), codes);
            }
        }

        Assert.Equal(ReferenceLists.All.Select(list => list.Name), lists.Row(1).CellsUsed().Where(cell => cell.GetString() != "Meaning")
            .Select(cell => cell.GetString()));
    }

    [Fact]
    public void Sample_has_every_sample_row()
    {
        using var workbook = Write(withSample: true);

        Assert.StartsWith("This file is filled", workbook.Worksheet("Instructions").Cell(3, 1).GetString());
        AssertHasSampleRows(workbook);
    }

    [Fact]
    public void Committed_sample_file_matches_the_sample_activity()
    {
        var path = Path.Combine(RepositoryRoot(), "samples", $"bpf-activity-sample-v{ActivityTemplate.Version}.xlsx");

        using var workbook = new XLWorkbook(path);

        AssertHasSampleRows(workbook);
    }

    private static void AssertHasSampleRows(XLWorkbook workbook)
    {
        foreach (var sheet in ActivityTemplate.Sheets)
        {
            var worksheet = workbook.Worksheet(sheet.Name);
            var rows = SampleActivity.Rows[sheet.Name];
            Assert.Equal(rows.Count + 1, worksheet.LastRowUsed()!.RowNumber());

            for (var row = 0; row < rows.Count; row++)
            {
                for (var column = 0; column < sheet.Columns.Count; column++)
                {
                    var cell = worksheet.Cell(row + 2, column + 1);
                    object? actual = rows[row][column] switch
                    {
                        null => cell.IsEmpty() ? null : cell.GetString(),
                        DateOnly => DateOnly.FromDateTime(cell.GetDateTime()),
                        decimal => (decimal)cell.GetDouble(),
                        _ => cell.GetString(),
                    };
                    Assert.Equal(rows[row][column], actual);
                }
            }
        }
    }

    private static IXLDataValidation ValidationOf(IXLWorksheet worksheet, int columnIndex)
    {
        var address = worksheet.Range(2, columnIndex + 1, 1000, columnIndex + 1).RangeAddress.ToString();

        return Assert.Single(worksheet.DataValidations, rule => rule.Ranges.Any(range => range.RangeAddress.ToString() == address));
    }

    private static XLAllowedValues Expected(TemplateColumn column) => column.Kind switch
    {
        ColumnKind.Date => XLAllowedValues.Date,
        ColumnKind.Number => XLAllowedValues.Decimal,
        ColumnKind.Code or ColumnKind.SessionCode => XLAllowedValues.List,
        _ => XLAllowedValues.TextLength,
    };

    private static XLWorkbook Write(bool withSample)
    {
        var output = new MemoryStream();
        new ClosedXmlActivityWorkbookWriter().Write(output, withSample);
        output.Position = 0;

        return new XLWorkbook(output);
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (!File.Exists(Path.Combine(directory.FullName, "ROADMAP.md")))
        {
            directory = directory.Parent!;
        }

        return directory.FullName;
    }
}
