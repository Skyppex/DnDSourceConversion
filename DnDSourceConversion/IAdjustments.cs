using System.Text.Json.Nodes;

namespace DnDSourceConversion;

public interface IAdjustments
{
    void Adjust(JsonObject? jsonObject);
}