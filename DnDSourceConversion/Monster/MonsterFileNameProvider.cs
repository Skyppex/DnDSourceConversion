using System.Text.Json.Nodes;

namespace DnDSourceConversion;

public class MonsterFileNameProvider : IFileNameProvider
{
    public string GetFileName(JsonObject? objectNode, string defaultName)
    {
        if (objectNode is null)
        {
            Console.WriteLine($"JsonObject is null. Using default name. {defaultName}");   
            return defaultName;
        }
        
        if (objectNode.AsObject().TryGetPropertyValue("name", out JsonNode nameNode))
            return nameNode!.AsValue().GetValue<string>() // Change the file name to the name property if it exists.
                .Replace('"', '`'); // Replace illegal quotes with a legal one.

        Console.WriteLine($"JsonObject has no name property. Using default name: {defaultName}.");
        return defaultName;
    }
}