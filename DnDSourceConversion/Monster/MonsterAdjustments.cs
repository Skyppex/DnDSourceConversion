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
    
    public void Adjust(JsonObject? objectNode, string name)
    {
        FilterUnwantedProperties(objectNode);
        AddConditionInflictAll(objectNode);
        FixSpecialAc(objectNode, name);
        FixTraits(objectNode, "trait");
        FixTraits(objectNode, "action");
        FixTraits(objectNode, "reaction");
        FixTraits(objectNode, "legendary");
    }

    public void AdjustStatblock(JsonObject? objectNode, string name)
    {
        FixAc(objectNode, name);
        FixHp(objectNode, name);
        FixStats(objectNode, name);
        FixSize(objectNode, name);
        FixAlignment(objectNode, name);
        RemoveUnusedPropertiesInStatblock(objectNode);
    }

    public string HandleReplacements(string yaml, string name)
    {
        yaml = yaml.Replace("{@h}", "Hit: ");
        return yaml;
    }
    
    private static void FixSpecialAc(JsonObject? objectNode, string name)
    {
        if (!objectNode.TryGetPropertyValue("ac", out JsonNode acNode))
        {
            Console.WriteLine($"No AC property found. | {name}");
            return;
        }
        
        var acArray = acNode as JsonArray;
        JsonNode? firstElement = acArray[0];

        if (firstElement is JsonObject innerObject)
        {
            const string PROPERTY_NAME = "special";
            if (!innerObject.TryGetPropertyValue(PROPERTY_NAME, out JsonNode? specialNode))
                return;

            innerObject.Remove(PROPERTY_NAME);
            string acText = specialNode.GetValue<string>();
            int startOfFrom = acText.IndexOf('(');

            string acValue = "";

            foreach (char c in acText)
            {
                if (!char.IsDigit(c))
                    break;
                    
                acValue += c;
            }
            
            string special = acText[startOfFrom..];

            int ac = int.Parse(acValue);
            
            innerObject.Add("ac", JsonValue.Create(ac));
            innerObject.Add("special", JsonValue.Create(special));
        }
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

        if (conditionInflicts.Count == 0)
            return;
        
        objectNode.Add("conditionInflictAll", JsonValue.Create(conditionInflicts));
    }

    private static void FixTraits(JsonObject? objectNode, string traitsIdentifier)
    {
        if (!objectNode.TryGetPropertyValue(traitsIdentifier, out JsonNode? traitNode))
            return;

        JsonArray traitArray = traitNode.AsArray();

        foreach (JsonNode? traitEntry in traitArray)
        {
            var traitEntryObject = traitEntry as JsonObject;
            if (!traitEntryObject.TryGetPropertyValue("entries", out JsonNode entriesNode))
                return;
            
            JsonArray entriesArray = entriesNode.AsArray();

            string description = "";

            foreach (JsonNode? entry in entriesArray)
            {
                switch (entry)
                {
                    case JsonValue entryValue:
                    {
                        string value = entryValue.GetValue<string>().Trim();
                        description += value;
                        break;
                    }

                    case JsonObject entryObject:
                    {
                        if (!entryObject.TryGetPropertyValue("items", out JsonNode itemsNode)) 
                            continue;

                        JsonArray itemsArray = itemsNode.AsArray();

                        foreach (JsonNode? item in itemsArray)
                        {
                            switch (item)
                            {
                                case JsonObject itemObject:
                                {
                                    if (!itemObject.TryGetPropertyValue("name", out JsonNode nameNode))
                                        continue;

                                    if (!itemObject.TryGetPropertyValue("entry", out JsonNode entryNode))
                                        continue;

                                    string name = nameNode.GetValue<string>();
                                    string entryText = entryNode.GetValue<string>();

                                    description += $"{name}: {entryText}";
                                    break;
                                }

                                case JsonValue itemValue:
                                {
                                    description += itemValue.GetValue<string>();
                                    break;
                                }
                            }
                        }
                        
                        break;
                    }
                }
            }
            
            traitEntryObject.Remove("entries");
            traitEntryObject.Add("desc", JsonValue.Create(description));
        }
    }

    private static void FixAc(JsonObject? objectNode, string name)
    {
        if (!objectNode.TryGetPropertyValue("ac", out JsonNode acNode))
        {
            Console.WriteLine($"No AC property found. | {name}");
            return;
        }
        
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
                if (!innerObject.TryGetPropertyValue("ac", out JsonNode innerAcNode))
                {
                    Console.WriteLine($"Inner AC does not have an 'ac' property. | {name}");
                    return;
                }

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

            default:
                Console.WriteLine($"Unknown AC type. | {name}");
                return;
        }
    }

    private static void FixHp(JsonObject? objectNode, string name)
    {
        if (!objectNode.TryGetPropertyValue("hp", out JsonNode? hpNode))
            return;
        
        var hpObject = hpNode as JsonObject;

        if (hpObject.TryGetPropertyValue("special", out JsonNode? specialNode))
        {
            string value = specialNode.GetValue<string>();
            objectNode.Remove("hp");

            if (int.TryParse(value, out int valueInt))
            {
                objectNode.Add("hp", JsonValue.Create(valueInt));
                return;
            }
            
            objectNode.Add("hp", JsonValue.Create(value));
            return;
        }

        if (!hpObject.TryGetPropertyValue("average", out JsonNode? averageNode))
        {
            Console.WriteLine($"Hp has no average. | {name}");
            return;
        }

        if (!hpObject.TryGetPropertyValue("formula", out JsonNode? formulaNode))
        {
            Console.WriteLine($"Hp has no formula. | {name}");
            return;
        }

        int average = averageNode.GetValue<int>();
        string formula = formulaNode.GetValue<string>();

        objectNode.Remove("hp");
        objectNode.Add("hp", JsonValue.Create(average));
        objectNode.Add("hit_dice", JsonValue.Create(formula));
    }

    private static void FixStats(JsonObject? objectNode, string name)
    {
        if (!objectNode.TryGetPropertyValue("str", out JsonNode? strNode))
        {
            Console.WriteLine($"No 'str' property on monster. | {name}");
            return;
        }
        
        if (!objectNode.TryGetPropertyValue("dex", out JsonNode? dexNode))
        {
            Console.WriteLine($"No 'dex' property on monster. | {name}");
            return;
        }
        
        if (!objectNode.TryGetPropertyValue("con", out JsonNode? conNode))
        {
            Console.WriteLine($"No 'con' property on monster. | {name}");
            return;
        }
        
        if (!objectNode.TryGetPropertyValue("int", out JsonNode? intNode))
        {
            Console.WriteLine($"No 'int' property on monster. | {name}");
            return;
        }
        
        if (!objectNode.TryGetPropertyValue("wis", out JsonNode? wisNode))
        {
            Console.WriteLine($"No 'wis' property on monster. | {name}");
            return;
        }
        
        if (!objectNode.TryGetPropertyValue("cha", out JsonNode? chaNode))
        {
            Console.WriteLine($"No 'cha' property on monster. | {name}");
            return;
        }

        objectNode.Remove("str");
        objectNode.Remove("dex");
        objectNode.Remove("con");
        objectNode.Remove("int");
        objectNode.Remove("wis");
        objectNode.Remove("cha");

        objectNode.Add("stats", new JsonArray(strNode, dexNode, conNode, intNode, wisNode, chaNode));
    }

    private static readonly Dictionary<string, string> s_sizeMap = new()
    {
        { "T", "Tiny" },
        { "S", "Small" },
        { "M", "Medium" },
        { "L", "Large" },   
        { "H", "Huge" },
        { "G", "Gargantuan" },
    };
    
    private static void FixSize(JsonObject? objectNode, string name)
    {
        if (!objectNode.TryGetPropertyValue("size", out JsonNode? sizeNode))
        {
            Console.WriteLine($"No size found. | {name}");
            return;
        }

        JsonArray sizeArray = sizeNode.AsArray();

        for (int i = 0; i < sizeArray.Count; i++)
        {
            JsonNode? size = sizeArray[i];
            sizeArray.RemoveAt(i);
            sizeArray.Insert(i, JsonValue.Create(s_sizeMap[size.GetValue<string>()]));
        }
    }

    
    private static readonly Dictionary<string, string> s_alignmentMap = new()
    {
        {"U", "Unaligned"},
        {"A", "Any Alignment"},
        {"L", "Lawful"},
        {"C", "Chaotic"},
        {"G", "Good"},
        {"E", "Evil"},
        {"N", "Neutral"},
    };

    private static void FixAlignment(JsonObject? objectNode, string name)
    {
        if (!objectNode.TryGetPropertyValue("alignment", out JsonNode? alignmentNode))
        {
            Console.WriteLine($"Monster has no alignment. | {name}");
            return;
        }

        JsonArray alignmentArray = alignmentNode.AsArray();

        int count = alignmentArray.Count;
        
        for (int i = 0; i < count; i++)
        {
            switch (alignmentArray[i])
            {
                case JsonValue value:
                {
                    string alignment = value.GetValue<string>();
                    
                    if (!s_alignmentMap.ContainsKey(alignment))
                        break;
                    
                    alignmentArray.RemoveAt(i);
                    alignmentArray.Insert(i, JsonValue.Create(s_alignmentMap[alignment]));
                    break;
                }

                case JsonObject obj:
                {
                    if (!obj.TryGetPropertyValue("alignment", out JsonNode? innerAlignmentNode))
                    {
                        Console.WriteLine($"Monster has no inner alignment. | {name}");
                        return;
                    }

                    JsonArray innerAlignmentArray = innerAlignmentNode.AsArray();

                    for (int j = 0; j < innerAlignmentArray.Count; j++)
                    {
                        string alignment = innerAlignmentArray[j].GetValue<string>();

                        if (!s_alignmentMap.ContainsKey(alignment))
                        {
                            alignmentArray.Add(JsonValue.Create(alignment));   
                            continue;
                        }

                        alignmentArray.Add(JsonValue.Create(s_alignmentMap[alignment]));
                    }

                    if (i != count - 1)
                        alignmentArray.Add("|");
                    else
                        for (int j = count - 1; j >= 0; j--)
                            alignmentArray.RemoveAt(j);
                    
                    break;
                }
            }
        }
    }
    
    private static void RemoveUnusedPropertiesInStatblock(JsonObject? objectNode)
    {
        string[] unusedProperties =
        {
            "miscTags",
            "damageTags",
            "languageTags",
            "attachedItems",
            "environment",
            "otherSources",
            "variant",
            "legendaryGroup",
            "dragonAge",
            "dragonCastingColor",
            "traitTags",
            "senseTags",
            "actionTags",
            "damageTagsLegendary",
            "conditionInflict",
            "conditionInflictSpell",
            "conditionInflictLegendary",
            "conditionInflictAll",
        };

        foreach (string property in unusedProperties)
            objectNode.Remove(property);
    }
}