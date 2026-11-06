namespace BpfBuilder.Application.Imports;

public enum ImportStatus
{
    Imported,
    NotFound,
    AlreadyImported,
    InvalidFile,
    HasErrors,
}

/// <param name="Report">Set when the workbook was read: <see cref="ImportStatus.Imported"/> or <see cref="ImportStatus.HasErrors"/>.</param>
/// <param name="FileError">Set for <see cref="ImportStatus.InvalidFile"/>.</param>
public sealed record ImportResult(ImportStatus Status, ImportReportResponse? Report = null, string? FileError = null);

/// <summary>Reads a filled activity template into one fiscal year, all or nothing.</summary>
public sealed class ActivityImportService(
    IOrganizationRepository organizations,
    IActivityImportRepository imports,
    IActivityWorkbookReader reader,
    TimeProvider timeProvider)
{
    public const long MaxFileBytes = 10 * 1024 * 1024;

    public const string FileExtension = ".xlsx";

    /// <returns>Null when the fiscal year is not the user's or has no imported data.</returns>
    public async Task<ActivityImportResponse?> GetAsync(Guid ownerId, Guid fiscalYearId, CancellationToken cancellationToken = default)
    {
        var fiscalYear = await FindFiscalYearAsync(ownerId, fiscalYearId, cancellationToken);
        var import = fiscalYear is null ? null : await imports.FindByFiscalYearAsync(fiscalYear.Id, cancellationToken);

        return import is null
            ? null
            : new(
                import.FileName,
                import.ImportedAtUtc,
                new(import.Sessions.Count, import.Learners.Count, import.Trainers.Count, import.Revenue.Count, import.Costs.Count));
    }

    /// <param name="fileName">Name of the uploaded file, used for its extension and kept with the import.</param>
    /// <param name="content">Uploaded content; null when no file was sent.</param>
    /// <param name="length">Size of the upload in bytes.</param>
    public async Task<ImportResult> ImportAsync(
        Guid ownerId,
        Guid fiscalYearId,
        string? fileName,
        Stream? content,
        long length,
        CancellationToken cancellationToken = default)
    {
        var fiscalYear = await FindFiscalYearAsync(ownerId, fiscalYearId, cancellationToken);
        if (fiscalYear is null)
        {
            return new(ImportStatus.NotFound);
        }

        if (await imports.ExistsAsync(fiscalYear.Id, cancellationToken))
        {
            return new(ImportStatus.AlreadyImported);
        }

        var name = Path.GetFileName(fileName?.Trim());
        if (FileError(name, content, length) is { } fileError)
        {
            return new(ImportStatus.InvalidFile, FileError: fileError);
        }

        if (reader.Read(content!) is not { } workbook)
        {
            return new(ImportStatus.InvalidFile, FileError: "This file is not a readable Excel workbook.");
        }

        var parsed = ActivityWorkbookParser.Parse(workbook, fiscalYear.StartDate, fiscalYear.EndDate);
        var report = new ImportReportResponse(
            !parsed.HasErrors,
            new(parsed.Sessions.Count, parsed.Learners.Count, parsed.Trainers.Count, parsed.Revenue.Count, parsed.Costs.Count),
            [.. parsed.Issues.Select(i => new ImportIssueResponse(i.Sheet, i.Row, i.Column, i.Message, i.Severity.ToString()))]);
        if (parsed.HasErrors)
        {
            return new(ImportStatus.HasErrors, report);
        }

        imports.Add(new ActivityImport(
            fiscalYear.Id,
            name!,
            timeProvider.GetUtcNow().UtcDateTime,
            parsed.Sessions,
            parsed.Learners,
            parsed.Trainers,
            parsed.Revenue,
            parsed.Costs));
        await imports.SaveChangesAsync(cancellationToken);

        return new(ImportStatus.Imported, report);
    }

    private static string? FileError(string? name, Stream? content, long length)
    {
        if (content is null || length <= 0)
        {
            return "Choose a filled activity template.";
        }

        if (length > MaxFileBytes)
        {
            return "The file is larger than 10 MB.";
        }

        if (!string.Equals(Path.GetExtension(name), FileExtension, StringComparison.OrdinalIgnoreCase))
        {
            return "Only Excel .xlsx files can be imported.";
        }

        return name!.Length > ActivityImport.FileNameMaxLength
            ? $"The file name is longer than {ActivityImport.FileNameMaxLength} characters."
            : null;
    }

    private async Task<FiscalYear?> FindFiscalYearAsync(Guid ownerId, Guid fiscalYearId, CancellationToken cancellationToken)
    {
        var organization = await organizations.FindByOwnerAsync(ownerId, cancellationToken);

        return organization is null
            ? null
            : await organizations.FindFiscalYearAsync(organization.Id, fiscalYearId, cancellationToken);
    }
}
