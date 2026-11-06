using BpfBuilder.Application.Templates;

namespace BpfBuilder.Application.Tests;

public sealed class ActivityTemplateServiceTests
{
    private readonly FakeWorkbookWriter writer = new();

    [Fact]
    public void Template_is_the_empty_workbook_named_with_its_version()
    {
        var file = new ActivityTemplateService(writer).Template();

        Assert.Equal("bpf-activity-template-v1.xlsx", file.FileName);
        Assert.Equal("empty"u8.ToArray(), file.Content);
    }

    [Fact]
    public void Sample_is_the_filled_workbook_named_with_its_version()
    {
        var file = new ActivityTemplateService(writer).Sample();

        Assert.Equal("bpf-activity-sample-v1.xlsx", file.FileName);
        Assert.Equal("sample"u8.ToArray(), file.Content);
    }

    private sealed class FakeWorkbookWriter : IActivityWorkbookWriter
    {
        public void Write(Stream output, bool withSample) => output.Write(withSample ? "sample"u8 : "empty"u8);
    }
}
