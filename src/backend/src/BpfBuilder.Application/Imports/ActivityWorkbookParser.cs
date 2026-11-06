namespace BpfBuilder.Application.Imports;

public enum IssueSeverity
{
    Error,
    Warning,
}

/// <param name="Row">Excel row number; null for a problem with the whole sheet.</param>
/// <param name="Column">Column header; null for a problem with the whole sheet or row.</param>
public sealed record ImportIssue(string Sheet, int? Row, string? Column, string Message, IssueSeverity Severity);

/// <summary>Rows read without error, and every problem found in the workbook.</summary>
public sealed record ParsedActivity(
    IReadOnlyList<ImportedSession> Sessions,
    IReadOnlyList<ImportedLearner> Learners,
    IReadOnlyList<ImportedTrainer> Trainers,
    IReadOnlyList<ImportedRevenue> Revenue,
    IReadOnlyList<ImportedCost> Costs,
    IReadOnlyList<ImportIssue> Issues)
{
    public bool HasErrors => Issues.Any(i => i.Severity == IssueSeverity.Error);
}

/// <summary>Checks a filled activity workbook against <see cref="ActivityTemplate"/> and one fiscal year.</summary>
public static class ActivityWorkbookParser
{
    public const int TextMaxLength = 200;

    /// <summary>Larger numbers are rejected rather than rounded.</summary>
    public const double NumberMaxValue = 1_000_000_000_000;

    public static ParsedActivity Parse(ActivityWorkbook workbook, DateOnly yearStart, DateOnly yearEnd)
    {
        ArgumentNullException.ThrowIfNull(workbook);

        var issues = new List<ImportIssue>();
        var sessionCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sessions = new Dictionary<string, (ImportedSession Session, int Row)>(StringComparer.OrdinalIgnoreCase);
        ReadSessions(workbook, yearStart, yearEnd, issues, sessionCodes, sessions);
        var learners = ReadLearners(workbook, issues, sessionCodes, sessions);
        var trainers = ReadTrainers(workbook, issues, sessionCodes);
        var revenue = ReadRevenue(workbook, yearStart, yearEnd, issues, sessionCodes);
        var costs = ReadCosts(workbook, issues);

        var attended = learners.Select(l => l.SessionCode).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var (session, row) in sessions.Values.Where(s => !attended.Contains(s.Session.Code)))
        {
            issues.Add(new(
                ActivityTemplate.Sessions.Name,
                row,
                null,
                $"Session {session.Code} has no learner on the Learners sheet.",
                IssueSeverity.Warning));
        }

        return new([.. sessions.Values.Select(s => s.Session)], learners, trainers, revenue, costs, issues);
    }

    private static void ReadSessions(
        ActivityWorkbook workbook,
        DateOnly yearStart,
        DateOnly yearEnd,
        List<ImportIssue> issues,
        HashSet<string> sessionCodes,
        Dictionary<string, (ImportedSession Session, int Row)> sessions)
    {
        foreach (var row in Rows(workbook, ActivityTemplate.Sessions, issues))
        {
            var code = row.Text(0);
            if (code is not null && !sessionCodes.Add(code))
            {
                row.Error(0, $"Session code {code} is used on more than one row.");
            }

            var title = row.Text(1);
            var start = row.Date(2);
            var end = row.Date(3);
            var plannedHours = row.Number(4);
            var deliveryMode = row.Code(5);
            var distanceLearning = row.Code(6);
            var goal = row.Code(7);
            var rncpLevel = row.Code(8);
            var nsfCode = row.Code(9);
            if (row.HasErrors)
            {
                continue;
            }

            if (end!.Value < start!.Value)
            {
                row.Error(3, "End date must be on or after the start date.");
            }
            else if (end.Value < yearStart || start.Value > yearEnd)
            {
                row.Error(2, "The session is entirely outside the fiscal year.");
            }
            else if (start.Value < yearStart || end.Value > yearEnd)
            {
                row.Warning(2, "The session runs over the edge of the fiscal year; check its hours belong to this year.");
            }

            if (goal == ReferenceLists.RncpGoal && rncpLevel is null)
            {
                row.Error(8, "Enter the RNCP level of an RNCP training goal.");
            }
            else if (goal != ReferenceLists.RncpGoal && rncpLevel is not null)
            {
                row.Error(8, "Leave the RNCP level empty unless the training goal is RNCP.");
            }

            if (!row.HasErrors)
            {
                var session = new ImportedSession(
                    code!,
                    title!,
                    start.Value,
                    end.Value,
                    plannedHours!.Value,
                    deliveryMode!,
                    distanceLearning!,
                    goal!,
                    rncpLevel,
                    nsfCode!);
                sessions[session.Code] = (session, row.RowNumber);
            }
        }
    }

    private static List<ImportedLearner> ReadLearners(
        ActivityWorkbook workbook,
        List<ImportIssue> issues,
        HashSet<string> sessionCodes,
        Dictionary<string, (ImportedSession Session, int Row)> sessions)
    {
        var learners = new List<ImportedLearner>();
        foreach (var row in Rows(workbook, ActivityTemplate.Learners, issues))
        {
            var code = row.Text(0);
            var sessionCode = row.SessionCode(1, sessionCodes);
            var category = row.Code(2);
            var hours = row.Number(3);
            if (!row.HasErrors
                && sessions.TryGetValue(sessionCode!, out var session)
                && hours!.Value > session.Session.PlannedHours)
            {
                var planned = session.Session.PlannedHours.ToString("0.##", CultureInfo.InvariantCulture);
                row.Error(3, $"Hours attended are above the {planned} hours planned for the session.");
            }

            if (!row.HasErrors)
            {
                learners.Add(new(code!, sessionCode!, category!, hours!.Value));
            }
        }

        return learners;
    }

    private static List<ImportedTrainer> ReadTrainers(ActivityWorkbook workbook, List<ImportIssue> issues, HashSet<string> sessionCodes)
    {
        var trainers = new List<ImportedTrainer>();
        foreach (var row in Rows(workbook, ActivityTemplate.Trainers, issues))
        {
            var code = row.Text(0);
            var sessionCode = row.SessionCode(1, sessionCodes);
            var type = row.Code(2);
            var hours = row.Number(3);
            if (!row.HasErrors)
            {
                trainers.Add(new(code!, sessionCode!, type!, hours!.Value));
            }
        }

        return trainers;
    }

    private static List<ImportedRevenue> ReadRevenue(
        ActivityWorkbook workbook,
        DateOnly yearStart,
        DateOnly yearEnd,
        List<ImportIssue> issues,
        HashSet<string> sessionCodes)
    {
        var revenue = new List<ImportedRevenue>();
        foreach (var row in Rows(workbook, ActivityTemplate.Revenue, issues))
        {
            var formLine = row.Code(0);
            var amount = row.Number(1);
            var date = row.Date(2);
            if (date is { } invoiced && (invoiced < yearStart || invoiced > yearEnd))
            {
                row.Error(2, "The date is outside the fiscal year.");
            }

            var sessionCode = row.SessionCode(3, sessionCodes);
            var label = row.Text(4);
            if (!row.HasErrors)
            {
                revenue.Add(new(formLine!, amount!.Value, date!.Value, sessionCode, label));
            }
        }

        return revenue;
    }

    private static List<ImportedCost> ReadCosts(ActivityWorkbook workbook, List<ImportIssue> issues)
    {
        var costs = new List<ImportedCost>();
        foreach (var row in Rows(workbook, ActivityTemplate.Costs, issues))
        {
            var category = row.Code(0);
            var amount = row.Number(1);
            var label = row.Text(2);
            if (!row.HasErrors)
            {
                costs.Add(new(category!, amount!.Value, label));
            }
        }

        return costs;
    }

    private static List<RowReader> Rows(ActivityWorkbook workbook, TemplateSheet sheet, List<ImportIssue> issues)
    {
        if (!workbook.Sheets.TryGetValue(sheet.Name, out var data))
        {
            issues.Add(new(sheet.Name, null, null, $"The {sheet.Name} sheet is missing. Start from the latest template.", IssueSeverity.Error));
            return [];
        }

        var wrongHeaders = sheet.Columns
            .Select((column, index) => (column, index))
            .Where(c => !string.Equals(data.Headers.ElementAtOrDefault(c.index), c.column.Header, StringComparison.OrdinalIgnoreCase))
            .ToList();
        foreach (var (column, index) in wrongHeaders)
        {
            issues.Add(new(
                sheet.Name,
                1,
                column.Header,
                $"Column {(char)('A' + index)} must be headed \"{column.Header}\". Start from the latest template.",
                IssueSeverity.Error));
        }

        return wrongHeaders.Count > 0 ? [] : [.. data.Rows.Select(row => new RowReader(sheet, row, issues))];
    }

    private sealed class RowReader(TemplateSheet sheet, WorkbookRow row, List<ImportIssue> issues)
    {
        public int RowNumber => row.Number;

        public bool HasErrors { get; private set; }

        public string? Text(int index)
        {
            var text = AsText(Cell(index));
            if (text is null)
            {
                Missing(index);
                return null;
            }

            if (text.Length > TextMaxLength)
            {
                Error(index, $"Use at most {TextMaxLength} characters.");
                return null;
            }

            return text;
        }

        public string? Code(int index)
        {
            var list = sheet.Columns[index].List!;
            var text = AsText(Cell(index));
            if (text is null)
            {
                Missing(index);
                return null;
            }

            if (list.Find(text) is not { } item)
            {
                Error(index, $"\"{text}\" is not in the {list.Name} list.");
                return null;
            }

            return item.Code;
        }

        public string? SessionCode(int index, HashSet<string> sessionCodes)
        {
            var code = Text(index);
            if (code is not null && !sessionCodes.Contains(code))
            {
                Error(index, $"Session {code} is not on the Sessions sheet.");
                return null;
            }

            return code;
        }

        public DateOnly? Date(int index)
        {
            switch (Cell(index))
            {
                case null:
                    Missing(index);
                    return null;
                case DateTime dateTime:
                    return DateOnly.FromDateTime(dateTime);
                case string text when DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date):
                    return date;
                default:
                    Error(index, "Enter a date, for example 2025-02-03.");
                    return null;
            }
        }

        public decimal? Number(int index)
        {
            var value = Cell(index);
            if (value is null)
            {
                Missing(index);
                return null;
            }

            decimal? number = value switch
            {
                double d when Math.Abs(d) < NumberMaxValue => Math.Round((decimal)d, 2),
                string text when decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) => parsed,
                _ => null,
            };
            if (number is not { } valid || Math.Abs(valid) >= (decimal)NumberMaxValue)
            {
                Error(index, "Enter a number.");
                return null;
            }

            if (valid < 0)
            {
                Error(index, "Enter zero or a positive number.");
                return null;
            }

            return valid;
        }

        public void Error(int index, string message)
        {
            HasErrors = true;
            issues.Add(new(sheet.Name, row.Number, sheet.Columns[index].Header, message, IssueSeverity.Error));
        }

        public void Warning(int index, string message) =>
            issues.Add(new(sheet.Name, row.Number, sheet.Columns[index].Header, message, IssueSeverity.Warning));

        private object? Cell(int index) => row.Cells.ElementAtOrDefault(index);

        private void Missing(int index)
        {
            if (sheet.Columns[index].Required)
            {
                Error(index, $"{sheet.Columns[index].Header} is required.");
            }
        }

        private static string? AsText(object? value) => value switch
        {
            string text => text,
            double number => number.ToString("0.##########", CultureInfo.InvariantCulture),
            DateTime dateTime => dateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            _ => null,
        };
    }
}
