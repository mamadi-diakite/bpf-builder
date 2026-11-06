namespace BpfBuilder.Infrastructure.Excel;

public sealed class ClosedXmlActivityWorkbookReader : IActivityWorkbookReader
{
    public ActivityWorkbook? Read(Stream content)
    {
        ArgumentNullException.ThrowIfNull(content);

        XLWorkbook workbook;
        try
        {
            workbook = new XLWorkbook(content);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            return null;
        }

        using (workbook)
        {
            var sheets = new Dictionary<string, WorkbookSheet>(StringComparer.OrdinalIgnoreCase);
            foreach (var worksheet in workbook.Worksheets)
            {
                sheets[worksheet.Name] = ReadSheet(worksheet);
            }

            return new(sheets);
        }
    }

    private static WorkbookSheet ReadSheet(IXLWorksheet worksheet)
    {
        var lastColumn = worksheet.LastColumnUsed(XLCellsUsedOptions.Contents)?.ColumnNumber() ?? 0;
        var headers = Enumerable.Range(1, lastColumn).Select(c => worksheet.Cell(1, c).GetString().Trim()).ToList();
        var rows = new List<WorkbookRow>();
        foreach (var row in worksheet.RowsUsed(XLCellsUsedOptions.Contents).Where(r => r.RowNumber() > 1))
        {
            var cells = Enumerable.Range(1, lastColumn).Select(c => Value(row.Cell(c))).ToList();
            if (cells.Any(c => c is not null))
            {
                rows.Add(new(row.RowNumber(), cells));
            }
        }

        return new(headers, rows);
    }

    private static object? Value(IXLCell cell) => cell.Value.Type switch
    {
        XLDataType.Number => cell.Value.GetNumber(),
        XLDataType.DateTime => cell.Value.GetDateTime(),
        _ => cell.GetString().Trim() is { Length: > 0 } text ? text : null,
    };
}
