using System.Text.Json.Nodes;

namespace DnDSourceConversion;

public interface IAdjustments
{
    public void Adjust(JsonObject? jsonObject);
    public void AdjustStatblock(JsonObject? objectNode);
    public string HandleReplacements(string yaml);
}