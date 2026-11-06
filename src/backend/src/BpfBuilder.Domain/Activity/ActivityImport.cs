namespace BpfBuilder.Domain.Activity;

/// <param name="Code">The organization's own session code, unique in the import.</param>
public sealed record ImportedSession(
    string Code,
    string Title,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal PlannedHours,
    string DeliveryMode,
    string DistanceLearning,
    string TrainingGoal,
    string? RncpLevel,
    string NsfCode);

public sealed record ImportedLearner(string LearnerCode, string SessionCode, string Category, decimal HoursAttended);

public sealed record ImportedTrainer(string TrainerCode, string SessionCode, string Type, decimal HoursDelivered);

public sealed record ImportedRevenue(string FormLine, decimal Amount, DateOnly Date, string? SessionCode, string? Label);

public sealed record ImportedCost(string Category, decimal Amount, string? Label);

/// <summary>One fiscal year of activity read from a filled Excel template. Rows are checked before they get here.</summary>
public sealed class ActivityImport
{
    public const int FileNameMaxLength = 255;

    private ActivityImport()
    {
    }

    public ActivityImport(
        Guid fiscalYearId,
        string fileName,
        DateTime importedAtUtc,
        IEnumerable<ImportedSession> sessions,
        IEnumerable<ImportedLearner> learners,
        IEnumerable<ImportedTrainer> trainers,
        IEnumerable<ImportedRevenue> revenue,
        IEnumerable<ImportedCost> costs)
    {
        Id = Guid.CreateVersion7();
        FiscalYearId = Guard.NotEmpty(fiscalYearId, nameof(fiscalYearId));
        FileName = Guard.Required(fileName, nameof(fileName), FileNameMaxLength);
        ImportedAtUtc = Guard.Utc(importedAtUtc, nameof(importedAtUtc));
        Sessions = [.. sessions];
        Learners = [.. learners];
        Trainers = [.. trainers];
        Revenue = [.. revenue];
        Costs = [.. costs];
    }

    public Guid Id { get; private set; }

    public Guid FiscalYearId { get; private set; }

    public string FileName { get; private set; } = string.Empty;

    public DateTime ImportedAtUtc { get; private set; }

    public List<ImportedSession> Sessions { get; private set; } = [];

    public List<ImportedLearner> Learners { get; private set; } = [];

    public List<ImportedTrainer> Trainers { get; private set; } = [];

    public List<ImportedRevenue> Revenue { get; private set; } = [];

    public List<ImportedCost> Costs { get; private set; } = [];
}
