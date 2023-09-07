namespace DnDSourceConversion;

public class MagicWeaponConfig : IConfig
{
    public string InputFileName => "magical-weapons-sublist-data";
    public string OutputFilePath => @"C:/Users/brage/OneDrive/Obsidian/Vaults/D&D/_Source/Official/Reference/Items/Magical/Weapons/";
    
    public IAdjustments Adjustments => new MagicWeaponAdjustments();
    public IFileNameProvider FileNameProvider => new MagicWeaponFileNameProvider();
    public IMarkdownGenerator MarkdownGenerator => new MagicWeaponMarkdownGenerator();
}