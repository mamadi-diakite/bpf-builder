namespace BpfBuilder.Application.Contracts.Imports;

public sealed record ImportCountsResponse(int Sessions, int Learners, int Trainers, int RevenueLines, int CostLines);

/// <param name="Row">Excel row number; null for a problem with the whole sheet.</param>
/// <param name="Column">Column header; null for a problem with the whole sheet or row.</param>
/// <param name="Severity">Error (blocks the import) or Warning.</param>
public sealed record ImportIssueResponse(string Sheet, int? Row, string? Column, string Message, string Severity);

/// <param name="Imported">False when at least one issue is an error; nothing is saved then.</param>
/// <param name="Counts">Rows read without error.</param>
public sealed record ImportReportResponse(bool Imported, ImportCountsResponse Counts, IReadOnlyList<ImportIssueResponse> Issues);

public sealed record ActivityImportResponse(string FileName, DateTime ImportedAtUtc, ImportCountsResponse Counts);
