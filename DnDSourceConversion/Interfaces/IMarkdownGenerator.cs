using System.Text.Json.Nodes;

namespace DnDSourceConversion;

public interface IMarkdownGenerator
{
    string Generate(string frontMatterYaml, string statblockYaml);
}