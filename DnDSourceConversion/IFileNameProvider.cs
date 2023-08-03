using System.Text.Json.Nodes;

namespace DnDSourceConversion;

public interface IFileNameProvider
{
    string GetFileName(JsonObject? jsonObject, string defaultName);
}