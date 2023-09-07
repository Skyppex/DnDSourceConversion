namespace DnDSourceConversion;

public class WeaponConfig : IConfig
{
    public string InputFileName => "mundane-weapons-sublist-data";
    public string OutputFilePath => @"C:/Users/brage/OneDrive/Obsidian/Vaults/D&D/_Source/Official/Reference/Items/Mundane/Weapons/";
    
    public IAdjustments Adjustments => new WeaponAdjustments();
    public IFileNameProvider FileNameProvider => new WeaponFileNameProvider();
    public IMarkdownGenerator MarkdownGenerator => new WeaponMarkdownGenerator();
}
