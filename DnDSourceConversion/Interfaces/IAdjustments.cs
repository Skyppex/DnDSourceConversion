using System.Text.Json.Nodes;

namespace DnDSourceConversion;

public interface IAdjustments : IDisposable
{
    public void Adjust(JsonObject? objectNode, string name);
    public void AdjustStatblock(JsonObject? objectNode, string name);
    public string HandleReplacements(string yaml, string name);
}