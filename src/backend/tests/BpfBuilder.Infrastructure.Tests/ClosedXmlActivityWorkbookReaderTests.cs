namespace BpfBuilder.Infrastructure.Tests;

public sealed class ClosedXmlActivityWorkbookReaderTests
{
    private readonly ClosedXmlActivityWorkbookReader _reader = new();

    [Fact]
    public void Reads_back_the_sample_written_by_the_template_writer()
    {
        using var stream = new MemoryStream();
        new ClosedXmlActivityWorkbookWriter().Write(stream, withSample: true);
        stream.Position = 0;

        var workbook = _reader.Read(stream)!;
        var parsed = ActivityWorkbookParser.Parse(workbook, new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31));

        Assert.Empty(parsed.Issues);
        Assert.Equal([5, 10, 6, 7, 3], [parsed.Sessions.Count, parsed.Learners.Count, parsed.Trainers.Count, parsed.Revenue.Count, parsed.Costs.Count]);
        Assert.True(workbook.Sheets.ContainsKey("sessions"));
    }

    [Fact]
    public void Returns_trimmed_text_numbers_and_dates_and_skips_blank_rows()
    {
        using var stream = Workbook(sheet =>
        {
            sheet.Cell(1, 1).Value = " Code ";
            sheet.Cell(1, 2).Value = "Amount";
            sheet.Cell(1, 3).Value = "Date";
            sheet.Cell(2, 1).Value = "  A-1 ";
            sheet.Cell(2, 2).Value = 12.5;
            sheet.Cell(2, 3).Value = new DateTime(2025, 2, 3);
            sheet.Cell(3, 1).Value = "   ";
            sheet.Cell(4, 1).Value = true;
        });

        var sheet = _reader.Read(stream)!.Sheets["Data"];

        Assert.Equal(["Code", "Amount", "Date"], sheet.Headers);
        Assert.Equal(2, sheet.Rows.Count);
        Assert.Equal(2, sheet.Rows[0].Number);
        Assert.Equal<object?>(["A-1", 12.5, new DateTime(2025, 2, 3)], sheet.Rows[0].Cells);
        Assert.Equal(4, sheet.Rows[1].Number);
        Assert.Equal<object?>(["TRUE", null, null], sheet.Rows[1].Cells);
    }

    [Fact]
    public void Reads_an_empty_sheet()
    {
        using var stream = Workbook(_ => { });

        var sheet = _reader.Read(stream)!.Sheets["Data"];

        Assert.Empty(sheet.Headers);
        Assert.Empty(sheet.Rows);
    }

    [Fact]
    public void Returns_null_for_content_that_is_not_a_workbook()
    {
        using var stream = new MemoryStream("not an excel file"u8.ToArray());

        Assert.Null(_reader.Read(stream));
    }

    [Fact]
    public void Rejects_a_missing_stream()
    {
        Assert.Throws<ArgumentNullException>(() => _reader.Read(null!));
    }

    private static MemoryStream Workbook(Action<IXLWorksheet> fill)
    {
        using var workbook = new XLWorkbook();
        fill(workbook.AddWorksheet("Data"));
        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        return stream;
    }
}
