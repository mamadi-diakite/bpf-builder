namespace BpfBuilder.Application.Tests;

/// <summary>Builds what the Excel reader returns, starting from the sample activity.</summary>
internal sealed class WorkbookBuilder
{
    private readonly Dictionary<string, List<string>> _headers = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<object?[]>> _rows = new(StringComparer.OrdinalIgnoreCase);

    public static WorkbookBuilder Sample()
    {
        var builder = new WorkbookBuilder();
        foreach (var sheet in ActivityTemplate.Sheets)
        {
            builder._headers[sheet.Name] = [.. sheet.Columns.Select(c => c.Header)];
            builder._rows[sheet.Name] = [.. SampleActivity.Rows[sheet.Name].Select(row => row.Select(ToCell).ToArray())];
        }

        return builder;
    }

    /// <param name="row">Index of the data row, 0 for Excel row 2.</param>
    public WorkbookBuilder Set(TemplateSheet sheet, int row, int column, object? value)
    {
        _rows[sheet.Name][row][column] = ToCell(value);
        return this;
    }

    public WorkbookBuilder Add(TemplateSheet sheet, params object?[] cells)
    {
        _rows[sheet.Name].Add([.. cells.Select(ToCell)]);
        return this;
    }

    public WorkbookBuilder Clear(TemplateSheet sheet)
    {
        _rows[sheet.Name].Clear();
        return this;
    }

    public WorkbookBuilder Remove(TemplateSheet sheet)
    {
        _headers.Remove(sheet.Name);
        _rows.Remove(sheet.Name);
        return this;
    }

    public WorkbookBuilder Headers(TemplateSheet sheet, params string[] headers)
    {
        _headers[sheet.Name] = [.. headers];
        return this;
    }

    public ActivityWorkbook Build() => new(_headers.ToDictionary(
        h => h.Key,
        h => new WorkbookSheet(h.Value, [.. _rows[h.Key].Select((cells, index) => new WorkbookRow(index + 2, cells))]),
        StringComparer.OrdinalIgnoreCase));

    private static object? ToCell(object? value) => value switch
    {
        DateOnly date => date.ToDateTime(TimeOnly.MinValue),
        decimal number => (double)number,
        int number => (double)number,
        _ => value,
    };
}
