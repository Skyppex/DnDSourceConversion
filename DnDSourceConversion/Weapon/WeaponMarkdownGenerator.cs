using System.Text;

namespace DnDSourceConversion;

public class WeaponMarkdownGenerator : IMarkdownGenerator
{
    private static readonly string[] s_tags =
    {
        "weapon",
        "mundane",
        "non-magical",
        "official-source",
    };

    public string Generate(string frontMatterYaml, string statblockYaml)
    {
        StringBuilder builder = new();
        
        AppendFrontMatter(frontMatterYaml, builder);
        builder.AppendLine();
        AppendStatBlock(statblockYaml, builder);
        
        return builder.ToString();
    }
    
    private static void AppendFrontMatter(string yaml, StringBuilder builder)
    {
        builder.AppendLine("---");

        builder.Append("tags: ");
        builder.AppendJoin(", ", s_tags);
        builder.AppendLine();
        
        builder.AppendLine(yaml);
        builder.AppendLine("---");
    }
    
    private static void AppendStatBlock(string yaml, StringBuilder builder)
    {
        builder.AppendLine("```statblock");
        builder.AppendLine("layout: Weapon");
        builder.AppendLine(yaml);
        builder.AppendLine("```");
    }
}