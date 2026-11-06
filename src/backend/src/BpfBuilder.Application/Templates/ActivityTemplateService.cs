namespace BpfBuilder.Application.Templates;

public sealed record WorkbookFile(string FileName, byte[] Content);

public sealed class ActivityTemplateService(IActivityWorkbookWriter writer)
{
    public const string ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public WorkbookFile Template() => Write($"bpf-activity-template-v{ActivityTemplate.Version}.xlsx", withSample: false);

    public WorkbookFile Sample() => Write($"bpf-activity-sample-v{ActivityTemplate.Version}.xlsx", withSample: true);

    private WorkbookFile Write(string fileName, bool withSample)
    {
        using var output = new MemoryStream();
        writer.Write(output, withSample);

        return new WorkbookFile(fileName, output.ToArray());
    }
}
