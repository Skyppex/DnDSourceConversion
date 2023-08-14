namespace DnDSourceConversion;

public interface IConfig
{
    public IAdjustments Adjustments { get; }
    public IFileNameProvider FileNameProvider { get; }
    public IMarkdownGenerator MarkdownGenerator { get; }
}