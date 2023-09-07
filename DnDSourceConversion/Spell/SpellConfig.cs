namespace DnDSourceConversion;

public class SpellConfig : IConfig
{
    public string InputFileName => "spells-sublist-data";
    public string OutputFilePath => @"C:/Users/brage/OneDrive/Obsidian/Vaults/D&D/_Source/Official/Reference/Spells/";
    
    public IAdjustments Adjustments => new SpellAdjustments();
    public IFileNameProvider FileNameProvider => new SpellFileNameProvider();
    public IMarkdownGenerator MarkdownGenerator => new SpellMarkdownGenerator();
}