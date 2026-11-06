using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BpfBuilder.Application.Templates;

namespace BpfBuilder.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/template")]
public sealed class TemplateController(ActivityTemplateService templateService) : ControllerBase
{
    [HttpGet]
    public FileContentResult Download() => AsFile(templateService.Template());

    [HttpGet("sample")]
    public FileContentResult DownloadSample() => AsFile(templateService.Sample());

    private FileContentResult AsFile(WorkbookFile file) => File(file.Content, ActivityTemplateService.ContentType, file.FileName);
}
