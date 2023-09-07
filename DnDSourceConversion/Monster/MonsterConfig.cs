namespace DnDSourceConversion;

public class MonsterConfig : IConfig
{
    private const string IMAGE_SAVE_FOLDER = "C:/Users/brage/OneDrive/Obsidian/Vaults/D&D/_Source/Official/Images/";
    
    public string InputFileName => "bestiary-sublist-data";
    public string OutputFilePath => @"C:/Users/brage/OneDrive/Obsidian/Vaults/D&D/_Source/Official/Reference/Bestiary/";

    public IAdjustments Adjustments => new MonsterAdjustments(IMAGE_SAVE_FOLDER);
    public IFileNameProvider FileNameProvider => new MonsterFileNameProvider();
    public IMarkdownGenerator MarkdownGenerator => new MonsterMarkdownGenerator();
}