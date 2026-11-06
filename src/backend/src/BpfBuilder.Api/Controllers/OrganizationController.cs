namespace BpfBuilder.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/organization")]
public sealed class OrganizationController(OrganizationService organizationService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<OrganizationResponse>> Get(CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not { } userId)
        {
            return Unauthorized();
        }

        var organization = await organizationService.GetAsync(userId, cancellationToken);

        return organization is null ? NotFound() : Ok(organization);
    }

    [HttpPut("identification")]
    public async Task<ActionResult<OrganizationResponse>> SaveIdentification(
        OrganizationIdentificationRequest request,
        CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not { } userId)
        {
            return Unauthorized();
        }

        return ToActionResult(await organizationService.SaveIdentificationAsync(userId, request, cancellationToken));
    }

    [HttpPut("signatory")]
    public async Task<ActionResult<OrganizationResponse>> SaveSignatory(
        OrganizationSignatoryRequest request,
        CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not { } userId)
        {
            return Unauthorized();
        }

        return ToActionResult(await organizationService.SaveSignatoryAsync(userId, request, cancellationToken));
    }

    private ActionResult<OrganizationResponse> ToActionResult(ServiceResult<OrganizationResponse> result) => result.Status switch
    {
        ServiceResultStatus.Ok => Ok(result.Value),
        ServiceResultStatus.NotFound => Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Save the organization's identification first."),
        _ => this.InvalidRequest(result.Errors),
    };
}
