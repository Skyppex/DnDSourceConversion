using System.Text.Json.Nodes;

namespace DnDSourceConversion;

public interface IFileNameProvider
{
    string GetFileName(JsonObject? objectNode, string defaultName);
}