using BpfBuilder.Application.Common;
using BpfBuilder.Application.Contracts.FiscalYears;
using BpfBuilder.Application.Organizations;
using BpfBuilder.Domain;

namespace BpfBuilder.Application.FiscalYears;

public sealed class FiscalYearService(IOrganizationRepository organizations, TimeProvider timeProvider)
{
    private const string BothDatesMessage = "Enter both dates, or none for the last calendar year.";

    /// <returns>The fiscal years of the user's organization, latest first; empty without an organization.</returns>
    public async Task<IReadOnlyList<FiscalYearResponse>> ListAsync(Guid ownerId, CancellationToken cancellationToken = default)
    {
        var organization = await organizations.FindByOwnerAsync(ownerId, cancellationToken);
        if (organization is null)
        {
            return [];
        }

        var fiscalYears = await organizations.ListFiscalYearsAsync(organization.Id, cancellationToken);

        return [.. fiscalYears.Select(ToResponse)];
    }

    public async Task<FiscalYearResponse?> GetAsync(Guid ownerId, Guid fiscalYearId, CancellationToken cancellationToken = default)
    {
        var organization = await organizations.FindByOwnerAsync(ownerId, cancellationToken);
        var fiscalYear = organization is null
            ? null
            : await organizations.FindFiscalYearAsync(organization.Id, fiscalYearId, cancellationToken);

        return fiscalYear is null ? null : ToResponse(fiscalYear);
    }

    /// <returns>Not found while the user has no organization.</returns>
    public async Task<ServiceResult<FiscalYearResponse>> CreateAsync(
        Guid ownerId,
        CreateFiscalYearRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var organization = await organizations.FindByOwnerAsync(ownerId, cancellationToken);
        if (organization is null)
        {
            return ServiceResult<FiscalYearResponse>.NotFound;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var errors = new Dictionary<string, string[]>();
        var period = ResolvePeriod(request, now, errors);
        if (period is { } requested)
        {
            var existing = await organizations.ListFiscalYearsAsync(organization.Id, cancellationToken);
            if (existing.Any(y => y.StartDate <= requested.End && requested.Start <= y.EndDate))
            {
                errors["startDate"] = ["This period overlaps another fiscal year."];
            }
        }

        if (request.TotalTurnover < 0)
        {
            errors["totalTurnover"] = ["Turnover cannot be negative."];
        }

        if (period is not { } valid || errors.Count > 0)
        {
            return ServiceResult<FiscalYearResponse>.Invalid(errors);
        }

        var fiscalYear = new FiscalYear(organization.Id, valid.Start, valid.End, request.TotalTurnover, now);
        organizations.AddFiscalYear(fiscalYear);
        await organizations.SaveChangesAsync(cancellationToken);

        return ServiceResult<FiscalYearResponse>.Ok(ToResponse(fiscalYear));
    }

    private static (DateOnly Start, DateOnly End)? ResolvePeriod(
        CreateFiscalYearRequest request,
        DateTime nowUtc,
        Dictionary<string, string[]> errors)
    {
        if (request.StartDate is null && request.EndDate is null)
        {
            var lastYear = nowUtc.Year - 1;
            return (new DateOnly(lastYear, 1, 1), new DateOnly(lastYear, 12, 31));
        }

        if (request.StartDate is not { } start)
        {
            errors["startDate"] = [BothDatesMessage];
            return null;
        }

        if (request.EndDate is not { } end)
        {
            errors["endDate"] = [BothDatesMessage];
            return null;
        }

        if (end <= start)
        {
            errors["endDate"] = ["End date must be after the start date."];
            return null;
        }

        return (start, end);
    }

    private static FiscalYearResponse ToResponse(FiscalYear y) =>
        new(y.Id, y.StartDate, y.EndDate, y.TotalTurnover, y.CreatedAtUtc);
}
