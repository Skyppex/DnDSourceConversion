namespace DnDSourceConversion;

public class MonsterConfig : IConfig
{
    public IAdjustments Adjustments => new MonsterAdjustments();
    public IFileNameProvider FileNameProvider => new MonsterFileNameProvider();
    public IMarkdownGenerator MarkdownGenerator => new MonsterMarkdownGenerator();
}