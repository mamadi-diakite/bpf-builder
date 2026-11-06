namespace BpfBuilder.Application.Imports;

/// <param name="Number">Excel row number, 2 for the first row under the header.</param>
/// <param name="Cells">
/// One value per header column: null when empty, a trimmed <see cref="string"/>, a <see cref="double"/>
/// or a <see cref="DateTime"/>.
/// </param>
public sealed record WorkbookRow(int Number, IReadOnlyList<object?> Cells);

/// <param name="Rows">Rows under the header that have at least one value.</param>
public sealed record WorkbookSheet(IReadOnlyList<string> Headers, IReadOnlyList<WorkbookRow> Rows);

/// <param name="Sheets">Sheets by name, ignoring case.</param>
public sealed record ActivityWorkbook(IReadOnlyDictionary<string, WorkbookSheet> Sheets);

public interface IActivityWorkbookReader
{
    /// <returns>The sheets of the workbook, or null when the content is not a readable .xlsx file.</returns>
    ActivityWorkbook? Read(Stream content);
}
