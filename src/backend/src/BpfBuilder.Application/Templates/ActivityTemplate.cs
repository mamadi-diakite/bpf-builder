using BpfBuilder.Domain.Activity;

namespace BpfBuilder.Application.Templates;

public enum ColumnKind
{
    Text,
    Date,
    Number,
    Code,
    SessionCode,
}

public sealed record TemplateColumn(
    string Header,
    ColumnKind Kind,
    bool Required,
    string Description,
    string Example,
    ReferenceList? List = null);

public sealed record TemplateSheet(string Name, IReadOnlyList<TemplateColumn> Columns);

/// <summary>Sheets and columns of the activity workbook a user fills for one fiscal year.</summary>
public static class ActivityTemplate
{
    public const string Version = "1";

    public const string InstructionsSheet = "Instructions";

    public const string ReferenceSheet = "Reference lists";

    /// <summary>Rows that carry drop-down lists and format checks, header included.</summary>
    public const int LastDataRow = 1000;

    public static TemplateSheet Sessions { get; } = new("Sessions", [
        new("Session code", ColumnKind.Text, true, "Your own unique code for one run of a course.", "S-2025-01"),
        new("Title", ColumnKind.Text, true, "Title of the course.", "Spreadsheet basics"),
        new("Start date", ColumnKind.Date, true, "First day of the session.", "2025-02-03"),
        new("End date", ColumnKind.Date, true, "Last day of the session.", "2025-02-05"),
        new("Planned hours per learner", ColumnKind.Number, true, "Hours planned for one learner.", "21"),
        new("Delivery mode", ColumnKind.Code, true, "Who delivers the session.", "Direct", ReferenceLists.DeliveryModes),
        new("Distance learning", ColumnKind.Code, true, "Share of the hours given at a distance.", "None", ReferenceLists.DistanceLearning),
        new("Training goal", ColumnKind.Code, true, "Goal of the course.", "Other", ReferenceLists.TrainingGoals),
        new("RNCP level", ColumnKind.Code, false, "Only when the training goal is RNCP; leave empty otherwise.", "5", ReferenceLists.RncpLevels),
        new("NSF code", ColumnKind.Code, true, "Specialty of the course.", "324", ReferenceLists.NsfCodes),
    ]);

    public static TemplateSheet Learners { get; } = new("Learners", [
        new("Learner code", ColumnKind.Text, true, "Anonymous code of the learner. Never a name.", "Student 01"),
        new("Session code", ColumnKind.SessionCode, true, "Session the learner attended, from the Sessions sheet.", "S-2025-01"),
        new("Category", ColumnKind.Code, true, "Status of the learner during the session.", "Employee", ReferenceLists.LearnerCategories),
        new("Hours attended", ColumnKind.Number, true, "Hours the learner actually attended.", "21"),
    ]);

    public static TemplateSheet Trainers { get; } = new("Trainers", [
        new("Trainer code", ColumnKind.Text, true, "Anonymous code of the trainer. Never a name.", "Trainer 01"),
        new("Session code", ColumnKind.SessionCode, true, "Session the trainer taught, from the Sessions sheet.", "S-2025-01"),
        new("Type", ColumnKind.Code, true, "Employed by the organization or external.", "Internal", ReferenceLists.TrainerTypes),
        new("Hours delivered", ColumnKind.Number, true, "Hours the trainer taught in the session.", "21"),
    ]);

    public static TemplateSheet Revenue { get; } = new("Revenue", [
        new("Form line", ColumnKind.Code, true, "Line of the revenue section the income belongs to.", "1", ReferenceLists.FormLines),
        new("Amount excl. VAT", ColumnKind.Number, true, "Amount excluding VAT, in euros.", "4200"),
        new("Date", ColumnKind.Date, true, "Invoice date, inside the fiscal year.", "2025-02-28"),
        new("Session code", ColumnKind.SessionCode, false, "Session the income relates to, if any.", "S-2025-01"),
        new("Label", ColumnKind.Text, false, "Free note, e.g. an invoice number.", "Invoice 2025-014"),
    ]);

    public static TemplateSheet Costs { get; } = new("Costs", [
        new("Category", ColumnKind.Code, true, "Kind of training cost.", "Trainer salaries", ReferenceLists.CostCategories),
        new("Amount excl. VAT", ColumnKind.Number, true, "Amount excluding VAT, in euros.", "18500"),
        new("Label", ColumnKind.Text, false, "Free note.", "Internal trainer salaries"),
    ]);

    public static IReadOnlyList<TemplateSheet> Sheets { get; } = [Sessions, Learners, Trainers, Revenue, Costs];
}
