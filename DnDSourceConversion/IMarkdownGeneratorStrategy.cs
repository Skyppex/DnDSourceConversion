using System.Text.Json.Nodes;

namespace DnDSourceConversion;

public interface IMarkdownGeneratorStrategy
{
    string Generate(string frontMatterYaml, string statblockYaml);
}