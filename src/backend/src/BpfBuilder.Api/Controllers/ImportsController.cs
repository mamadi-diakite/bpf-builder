namespace BpfBuilder.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/fiscal-years/{fiscalYearId:guid}/import")]
public sealed class ImportsController(ActivityImportService importService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ActivityImportResponse>> Get(Guid fiscalYearId, CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not { } userId)
        {
            return Unauthorized();
        }

        var import = await importService.GetAsync(userId, fiscalYearId, cancellationToken);

        return import is null ? NotFound() : Ok(import);
    }

    /// <summary>Imports a filled activity template; nothing is saved while the report has errors.</summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ImportReportResponse>> Import(
        Guid fiscalYearId,
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not { } userId)
        {
            return Unauthorized();
        }

        await using var content = file?.OpenReadStream();
        var result = await importService.ImportAsync(userId, fiscalYearId, file?.FileName, content, file?.Length ?? 0, cancellationToken);

        return result.Status switch
        {
            ImportStatus.Imported => CreatedAtAction(nameof(Get), new { fiscalYearId }, result.Report),
            ImportStatus.HasErrors => UnprocessableEntity(result.Report),
            ImportStatus.AlreadyImported => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "This fiscal year already has imported activity."),
            ImportStatus.InvalidFile => this.InvalidRequest(new Dictionary<string, string[]> { ["file"] = [result.FileError!] }),
            _ => NotFound(),
        };
    }
}
