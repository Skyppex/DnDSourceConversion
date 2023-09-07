using System.Text.Json.Nodes;

namespace DnDSourceConversion;

public static class JsonUtils
{
    public static string? GetJson(string path)
    {
        using var stream = new FileStream(path, FileMode.Open);
                                  
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public static async Task<string> GetJsonAsync(string path)
    {
        await using var stream = new FileStream(path, FileMode.Open);
                                  
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }
    
    public static string PrepareJsonForDynamicTyping(string json)
    {
        json = json.TrimStart();

        if (json[0] != '{')
            json = "{\"head\":" + json + '}';

        return json;
    }

    public static JsonNode? FixJsonNode(JsonNode? rootNode) => rootNode!.AsObject().TryGetPropertyValue("head", out JsonNode? headNode) ? headNode : rootNode;
    
    public static JsonNode DeepCopy(this JsonNode node)
    {
        if (node is JsonArray array)
            return new JsonArray(array.Select(DeepCopy).ToArray());
        
        if (node is JsonObject obj)
            return new JsonObject(obj.Select(pair => new KeyValuePair<string, JsonNode>(pair.Key, DeepCopy(pair.Value))));
        
        return JsonValue.Create(node.GetValue<object>());
    }
}
