using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BpfBuilder.Api.Auth;
using BpfBuilder.Application.Common;
using BpfBuilder.Application.Contracts.FiscalYears;
using BpfBuilder.Application.FiscalYears;

namespace BpfBuilder.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/fiscal-years")]
public sealed class FiscalYearsController(FiscalYearService fiscalYearService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FiscalYearResponse>>> List(CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not { } userId)
        {
            return Unauthorized();
        }

        return Ok(await fiscalYearService.ListAsync(userId, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<FiscalYearResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not { } userId)
        {
            return Unauthorized();
        }

        var fiscalYear = await fiscalYearService.GetAsync(userId, id, cancellationToken);

        return fiscalYear is null ? NotFound() : Ok(fiscalYear);
    }

    [HttpPost]
    public async Task<ActionResult<FiscalYearResponse>> Create(CreateFiscalYearRequest request, CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not { } userId)
        {
            return Unauthorized();
        }

        var result = await fiscalYearService.CreateAsync(userId, request, cancellationToken);

        return result.Status switch
        {
            ServiceResultStatus.Ok => CreatedAtAction(nameof(Get), new { id = result.Value!.Id }, result.Value),
            ServiceResultStatus.NotFound => Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Save the organization's identification first."),
            _ => this.InvalidRequest(result.Errors),
        };
    }
}
