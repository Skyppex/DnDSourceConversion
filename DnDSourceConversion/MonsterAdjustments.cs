using System.Text.Json.Nodes;

namespace DnDSourceConversion;

public class MonsterAdjustments : IAdjustments
{
    private static readonly string[] s_unwantedProperties =
    {
        "hasToken",
        "hasFluff",
        "hasFluffImages",
        "soundClip"
    };
    
    public void Adjust(JsonObject? objectNode)
    {
        FilterUnwantedProperties(objectNode);
        FixAc(objectNode);
        AddConditionInflictAll(objectNode);
    }

    private static void FilterUnwantedProperties(JsonObject? objectNode)
    {
        List<string> invalidNames = objectNode
                                   .Select(kvp => kvp.Key)
                                   .Where(propertyName =>
                                        propertyName.StartsWith('_') ||
                                        s_unwantedProperties.Contains(propertyName))
                                   .ToList();

        invalidNames.ForEach(propertyName => objectNode.Remove(propertyName));
    }

    private static void FixAc(JsonObject? objectNode)
    {
        if (objectNode.TryGetPropertyValue("ac", out JsonNode acNode))
        {
            var acArray = acNode as JsonArray;
            JsonNode? firstElement = acArray[0];

            switch (firstElement)
            {
                case JsonValue innerValue:
                {
                    int value = acArray[0].GetValue<int>();
                    objectNode.Remove("ac");
                    objectNode.Add("ac", JsonValue.Create(value.ToString()));
                    return;
                }
                
                case JsonObject innerObject:
                {
                    if (innerObject.TryGetPropertyValue("ac", out JsonNode innerAcNode))
                    {
                        int value = innerAcNode.GetValue<int>();
                        string? from = null;
                        objectNode.Remove("ac");

                        if (innerObject.TryGetPropertyValue("from", out JsonNode fromNode))
                        {
                            var fromArray = fromNode as JsonArray;

                            from += '(';

                            for (int i = 0; i < fromArray.Count; i++)
                            {
                                JsonNode? valueNode = fromArray[(Index)i];
                                from += valueNode.GetValue<string>();

                                if (i != fromArray.Count - 1)
                                    from += ", ";
                            }

                            from += ')';
                        }
                        
                        objectNode.Add("ac", JsonValue.Create($"{value + " " + from}"));
                        return;
                    }

                    
                    Console.WriteLine("Inner AC does not have an 'ac' property.");
                    return;
                }
                
                default:
                    Console.WriteLine("Unknown AC type.");
                    return;
            }
        }
        
        Console.WriteLine("No AC property found.");
    }

    private static void AddConditionInflictAll(JsonObject? objectNode)
    {
        List<JsonValue> conditionInflicts = new();
        
        if (objectNode.TryGetPropertyValue("conditionInflict", out JsonNode conditionInflictNode))
        {
            var conditionInflictArray = conditionInflictNode as JsonArray;

            foreach (JsonNode? conditionInflict in conditionInflictArray)
                conditionInflicts.Add(conditionInflict as JsonValue);
        }
        
        if (objectNode.TryGetPropertyValue("conditionInflictSpell", out JsonNode conditionInflictSpellNode))
        {
            var conditionInflictSpellArray = conditionInflictSpellNode as JsonArray;

            foreach (JsonNode? conditionInflictSpell in conditionInflictSpellArray)
                conditionInflicts.Add(conditionInflictSpell as JsonValue);
        }
        
        if (objectNode.TryGetPropertyValue("conditionInflictLegendary", out JsonNode conditionInflictLegendaryNode))
        {
            var conditionInflictLegendaryArray = conditionInflictLegendaryNode as JsonArray;

            foreach (JsonNode? conditionInflictLegendary in conditionInflictLegendaryArray)
                conditionInflicts.Add(conditionInflictLegendary as JsonValue);
        }
        
        objectNode.Add("conditionInflictAll", JsonValue.Create(conditionInflicts));
    }
}