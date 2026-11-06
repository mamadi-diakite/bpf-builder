namespace BpfBuilder.Application.Contracts.FiscalYears;

/// <summary>Leave both dates empty for the last full calendar year.</summary>
/// <param name="TotalTurnover">Turnover of the whole organization excluding VAT, training or not.</param>
public sealed record CreateFiscalYearRequest(DateOnly? StartDate, DateOnly? EndDate, decimal TotalTurnover);

public sealed record FiscalYearResponse(Guid Id, DateOnly StartDate, DateOnly EndDate, decimal TotalTurnover, DateTime CreatedAtUtc);
