namespace DnDSourceConversion;

public interface IConfig
{
    public string OutputFilePath { get; }
    public string InputFileName { get; }
    public string OutputFileExtension => ".md";

    public IAdjustments Adjustments { get; }
    public IFileNameProvider FileNameProvider { get; }
    public IMarkdownGenerator MarkdownGenerator { get; }
}