namespace BpfBuilder.Infrastructure.Excel;

public sealed class ClosedXmlActivityWorkbookWriter : IActivityWorkbookWriter
{
    public const string VersionProperty = "TemplateVersion";

    private const string DateFormat = "yyyy-mm-dd";

    private static readonly XLColor HeaderFill = XLColor.FromHtml("#DCE6F1");

    public void Write(Stream output, bool withSample)
    {
        using var workbook = new XLWorkbook();
        workbook.CustomProperties.Add(VersionProperty, ActivityTemplate.Version);

        WriteInstructions(workbook.AddWorksheet(ActivityTemplate.InstructionsSheet), withSample);
        var worksheets = ActivityTemplate.Sheets.Select(sheet => workbook.AddWorksheet(sheet.Name)).ToList();
        var lists = WriteReferenceLists(workbook.AddWorksheet(ActivityTemplate.ReferenceSheet));
        var sessionCodes = worksheets[0].Range(2, 1, ActivityTemplate.LastDataRow, 1);

        foreach (var (sheet, worksheet) in ActivityTemplate.Sheets.Zip(worksheets))
        {
            WriteSheet(worksheet, sheet, lists, sessionCodes);
            if (withSample)
            {
                WriteRows(worksheet, SampleActivity.Rows[sheet.Name]);
            }
        }

        workbook.SaveAs(output);
    }

    private static void WriteInstructions(IXLWorksheet worksheet, bool withSample)
    {
        worksheet.Cell(1, 1).Value = "BPF activity template";
        worksheet.Cell(1, 1).Style.Font.SetBold().Font.SetFontSize(14);
        worksheet.Cell(2, 1).Value = "Template version";
        worksheet.Cell(2, 2).Value = ActivityTemplate.Version;
        worksheet.Cell(3, 1).Value = withSample
            ? "This file is filled with fictional sample data for the 2025 calendar year."
            : "Fill each sheet with one fiscal year of activity, then import the file in the app.";
        worksheet.Cell(4, 1).Value = "Learners and trainers are anonymous codes such as Student 01 and Trainer 01. Never enter names.";
        worksheet.Cell(5, 1).Value =
            $"Coded columns have a drop-down list. Every allowed value and its meaning is on the {ActivityTemplate.ReferenceSheet} sheet.";

        string[] headers = ["Sheet", "Column", "Required", "Description", "Example"];
        WriteHeader(worksheet, 7, headers);

        var row = 8;
        foreach (var sheet in ActivityTemplate.Sheets)
        {
            foreach (var column in sheet.Columns)
            {
                worksheet.Cell(row, 1).Value = sheet.Name;
                worksheet.Cell(row, 2).Value = column.Header;
                worksheet.Cell(row, 3).Value = column.Required ? "Yes" : "No";
                worksheet.Cell(row, 4).Value = column.Description;
                worksheet.Cell(row, 5).Value = column.Example;
                row++;
            }
        }

        worksheet.Column(1).Width = 18;
        worksheet.Column(2).Width = 28;
        worksheet.Column(3).Width = 10;
        worksheet.Column(4).Width = 60;
        worksheet.Column(5).Width = 22;
    }

    private static Dictionary<ReferenceList, IXLRange> WriteReferenceLists(IXLWorksheet worksheet)
    {
        var ranges = new Dictionary<ReferenceList, IXLRange>();
        var column = 1;

        foreach (var list in ReferenceLists.All)
        {
            WriteHeader(worksheet, 1, [list.Name, "Meaning"], column);
            for (var index = 0; index < list.Items.Count; index++)
            {
                worksheet.Cell(index + 2, column).Value = list.Items[index].Code;
                worksheet.Cell(index + 2, column + 1).Value = list.Items[index].Label;
            }

            ranges[list] = worksheet.Range(2, column, list.Items.Count + 1, column);
            worksheet.Column(column).Width = 20;
            worksheet.Column(column + 1).Width = 50;
            column += 3;
        }

        worksheet.Protect();

        return ranges;
    }

    private static void WriteSheet(
        IXLWorksheet worksheet, TemplateSheet sheet, Dictionary<ReferenceList, IXLRange> lists, IXLRange sessionCodes)
    {
        WriteHeader(worksheet, 1, [.. sheet.Columns.Select(column => column.Header)]);
        worksheet.SheetView.FreezeRows(1);

        for (var index = 0; index < sheet.Columns.Count; index++)
        {
            var column = sheet.Columns[index];
            var cells = worksheet.Range(2, index + 1, ActivityTemplate.LastDataRow, index + 1);
            var validation = cells.CreateDataValidation();
            validation.IgnoreBlanks = true;
            validation.InputTitle = column.Header;
            validation.InputMessage = column.Description;
            validation.ErrorTitle = column.Header;

            switch (column.Kind)
            {
                case ColumnKind.Date:
                    validation.Date.Between(new DateTime(2000, 1, 1), new DateTime(2099, 12, 31));
                    validation.ErrorMessage = "Enter a date, e.g. 2025-02-03.";
                    cells.Style.DateFormat.Format = DateFormat;
                    break;
                case ColumnKind.Number:
                    validation.Decimal.EqualOrGreaterThan(0);
                    validation.ErrorMessage = "Enter a number of zero or more.";
                    break;
                case ColumnKind.Code:
                    validation.List(lists[column.List!], true);
                    validation.ErrorMessage = $"Pick a value from the list. See the {ActivityTemplate.ReferenceSheet} sheet.";
                    break;
                case ColumnKind.SessionCode:
                    validation.List(sessionCodes, true);
                    validation.ErrorMessage = "Pick a session code from the Sessions sheet.";
                    break;
                default:
                    validation.TextLength.Between(1, 200);
                    validation.ErrorMessage = "Use at most 200 characters.";
                    break;
            }

            worksheet.Column(index + 1).Width = Math.Max(14, column.Header.Length + 4);
        }
    }

    private static void WriteRows(IXLWorksheet worksheet, IReadOnlyList<IReadOnlyList<object?>> rows)
    {
        for (var row = 0; row < rows.Count; row++)
        {
            for (var column = 0; column < rows[row].Count; column++)
            {
                var cell = worksheet.Cell(row + 2, column + 1);
                switch (rows[row][column])
                {
                    case DateOnly date:
                        cell.Value = date.ToDateTime(TimeOnly.MinValue);
                        break;
                    case decimal number:
                        cell.Value = number;
                        break;
                    case string text:
                        cell.Value = text;
                        break;
                }
            }
        }
    }

    private static void WriteHeader(IXLWorksheet worksheet, int row, string[] headers, int firstColumn = 1)
    {
        for (var index = 0; index < headers.Length; index++)
        {
            var cell = worksheet.Cell(row, firstColumn + index);
            cell.Value = headers[index];
            cell.Style.Font.SetBold().Fill.SetBackgroundColor(HeaderFill);
        }
    }
}
