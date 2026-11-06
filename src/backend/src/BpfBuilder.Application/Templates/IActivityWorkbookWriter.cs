namespace BpfBuilder.Application.Templates;

public interface IActivityWorkbookWriter
{
    /// <summary>Writes the activity workbook, empty or filled with the sample activity.</summary>
    void Write(Stream output, bool withSample);
}
