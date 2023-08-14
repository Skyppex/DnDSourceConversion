using System.Text.Json.Nodes;

namespace DnDSourceConversion;

public class WeaponAdjustments : IAdjustments
{
    public void Adjust(JsonObject? jsonObject, string name)
    {
        
    }

    public void AdjustStatblock(JsonObject? objectNode, string name)
    {
        
    }

    public string HandleReplacements(string yaml, string name)
    {
        return default;
    }
}