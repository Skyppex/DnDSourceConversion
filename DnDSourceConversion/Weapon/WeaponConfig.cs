namespace DnDSourceConversion;

public class WeaponConfig : IConfig
{
    public IAdjustments Adjustments => new WeaponAdjustments();
    public IFileNameProvider FileNameProvider => new WeaponFileNameProvider();
    public IMarkdownGenerator MarkdownGenerator => new WeaponMarkdownGenerator();
}