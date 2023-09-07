using System.Text.Json.Nodes;

using static DnDSourceConversion.StatblockUtils;

namespace DnDSourceConversion;

public class SpellAdjustments : IAdjustments
{
    private static readonly string[] s_unwantedProperties =
    {
        "otherSource",
    };
    
    public void Adjust(JsonObject? objectNode, string name)
    {
        FixSchool(objectNode, name);
        FixClasses(objectNode, name);
        FilterUnwantedProperties(objectNode);
        FixDuration(objectNode, name);
    }

    public void AdjustStatblock(JsonObject? objectNode, string name)
    {
        FixLevel(objectNode, name);
        FixCastingTime(objectNode, name);
        FixDurationStatblock(objectNode, name);
        FixRange(objectNode, name);
        FixComponents(objectNode, name);
        FixEntries(objectNode, name);
        FixClassesStatblock(objectNode, name);
        FixRaces(objectNode, name);
        FixFeats(objectNode, name);
        FixBackgrounds(objectNode, name);
        FixOptionalFeatures(objectNode, name);
        FixStats(objectNode, name);
        FixUpcast(objectNode, name);
        RemoveUnwantedProperties(objectNode, name);
    }

    public string HandleReplacements(string yaml, string name)
    {
        yaml = yaml.ReplaceDamageRollIdents()
                   .ReplaceStatusIdents()
                   .ReplaceSpellIdents()
                   .ReplaceConditionIdents()
                   .ReplaceQuickRefIdents()
                   .ReplaceScaleDamageIdents()
                   .ReplaceItemIdents()
                   .ReplaceBookIdents()
                   .ReplaceIdentWithValue("action", action => $"[[Actions|{action}]]")
                   .ReplaceIdentWithValue("creature", creature => $"[[{creature}]]")
                   .ReplaceIdentWithValue("sense", sense => $"[[{sense}]]")
                   .ReplaceIdentWithValue("dice", roll => roll)
                   .ReplaceIdentWithValue("skill", skill => $"[[{skill}]]")
                   .ReplaceIdentWithValue("chance", chance => $"{chance}%", "|")
                   .ReplaceIdentWithValue("d20", value =>
                    {
                        string prefix = int.Parse(value) switch
                        {
                            >= 10 => "+",
                            <= 9 => "-",
                        };
                        
                        return $"({prefix}{value})";
                    })
                   .SurroundWithLinkBrackets("[Dd]ifficult [Tt]errain", "Movement")
                   .SurroundWithLinkBrackets("AC", "Armor Class")
                   .SurroundWithLinkBrackets("DC", "Difficulty Class")
                   .SurroundWithLinkBrackets("[Ss]aving [Tt]hrows?", "Saving Throws")
                   .SurroundWithLinkBrackets("[Ss]pell [Ss]aves?", "Saving Throws")
                   .SurroundWithLinkBrackets("[Aa]ttack [Rr]olls?", "Attack Rolls")
                   .SurroundWithLinkBrackets("[Ss]pell [Aa]ttacks?", "Attack Rolls")
                   .SurroundWithLinkBrackets("[Mm]elee [Aa]ttacks?", "Attack Rolls")
                   .SurroundWithLinkBrackets("[Rr]anged [Aa]ttacks?", "Attack Rolls")
                   .SurroundWithLinkBrackets("[Dd]?i?s?[Aa]dvantage", "Advantage & Disadvantage")
                   .SurroundWithLinkBrackets("[Aa]cid [Dd]amage", "Damage")
                   .SurroundWithLinkBrackets("[Bb]ludgeoning [Dd]amage", "Damage")
                   .SurroundWithLinkBrackets("[Cc]old [Dd]amage", "Damage")
                   .SurroundWithLinkBrackets("[Ff]ire [Dd]amage", "Damage")
                   .SurroundWithLinkBrackets("[Ff]orce [Dd]amage", "Damage")
                   .SurroundWithLinkBrackets("[Ll]ightning [Dd]amage", "Damage")
                   .SurroundWithLinkBrackets("[Nn]ecrotic [Dd]amage", "Damage")
                   .SurroundWithLinkBrackets("[Pp]iercing [Dd]amage", "Damage")
                   .SurroundWithLinkBrackets("[Pp]oison [Dd]amage", "Damage")
                   .SurroundWithLinkBrackets("[Pp]sychic [Dd]amage", "Damage")
                   .SurroundWithLinkBrackets("[Rr]adiant [Dd]amage", "Damage")
                   .SurroundWithLinkBrackets("[Ss]lashing [Dd]amage", "Damage")
                   .SurroundWithLinkBrackets("[Tt]hunder [Dd]amage", "Damage");

        return yaml;
    }

    private static void FixSchool(JsonObject? objectNode, string name)
    {
        if (!objectNode.TryGetPropertyValue("school", out JsonNode? schoolNode))
            return;
        
        string school = schoolNode.GetValue<string>();
        string fullSchool = Maps.SchoolMap[school];
        
        objectNode.Remove("school");
        objectNode.Add("school", JsonValue.Create(fullSchool));
    }

    private static void FixLevel(JsonObject? objectNode, string name)
    {
        if (objectNode.TryGetPropertyValue("level", out JsonNode? levelNode))
        {
            int level = levelNode.GetValue<int>();

            string suffix = level switch
            {
                1 => "st",
                2 => "nd",
                3 => "rd",
                _ => "th",
            };

            string levelString;
            if (level is 0)
                levelString = "Cantrip";
            else
                levelString = $"{level}{suffix} level";
            
            objectNode.Remove("level");
            objectNode.Add("level", JsonValue.Create(levelString));
        }
    }

    private static void FixClasses(JsonObject? objectNode, string name)
    {
        if (!objectNode.TryGetPropertyValue("classes", out JsonNode? classesNode))
            return;
        
        JsonObject classesObject = classesNode.AsObject();
        
        string classesText = string.Empty;

        {
            if (AddToClassList("fromClassList", classesObject, out string classes))
                classesText += classes;
        }
        {
            if (AddToClassList("fromClassListVariant", classesObject, out string classes))
                classesText += classesText.Length > 0 ? $", {classes}" : classes;
        }
        {
            if (AddSubClassToClassList("fromSubclass", classesObject, out string classes))
                classesText += classesText.Length > 0 ? $", {classes}" : classes;
        }
        
        objectNode.Remove("classes");
        objectNode.Add("classes", JsonValue.Create(classesText));

        static bool AddToClassList(string propertyName, JsonObject classesObject, out string classes)
        {
            if (!classesObject.TryGetPropertyValue(propertyName, out JsonNode? propertyNode))
            {
                classes = string.Empty;
                return false;
            }

            JsonArray propertyArray = propertyNode.AsArray();

            classes = propertyArray.Select(c =>
            {
                if (!c.AsObject().TryGetPropertyValue("name", out JsonNode? nameNode))
                    return string.Empty;

                string className = nameNode.GetValue<string>();
                return $"{className}";
            }).Join(", ");

            return true;
        }
        
        static bool AddSubClassToClassList(string propertyName, JsonObject classesObject, out string classes)
        {
            if (!classesObject.TryGetPropertyValue(propertyName, out JsonNode? propertyNode))
            {
                classes = string.Empty;
                return false;
            }
            
            JsonArray propertyArray = propertyNode.AsArray();
            
            classes = propertyArray.Select(pn =>
            {
                JsonObject propertyObject = pn.AsObject();

                if (!propertyObject.TryGetPropertyValue("class", out JsonNode? classNode))
                    return string.Empty;
            
                JsonObject classObject = classNode.AsObject();

                if (!classObject.TryGetPropertyValue("name", out JsonNode? nameNode))
                    return string.Empty;
            
                string className = nameNode.GetValue<string>();

                if (!propertyObject.TryGetPropertyValue("subclass", out JsonNode? subclassNode))
                    return className;

                JsonObject subclassObject = subclassNode.AsObject();
            
                if (!subclassObject.TryGetPropertyValue("name", out JsonNode? subclassNameNode))
                    return className;

                className += $" - {subclassNameNode.GetValue<string>()}";
                return $"{className}";
            }).Join(", ");

            return true;
        }
    }

    private static void FixCastingTime(JsonObject? objectNode, string name)
    {
        if (!objectNode.TryGetPropertyValue("time", out JsonNode? castingTimeNode))
        {
            Console.WriteLine($"No casting time. | {name}");
            return;
        }
        
        JsonObject castingTimeObject = castingTimeNode.AsArray()[0].AsObject();
        
        int number = castingTimeObject.TryGetPropertyValue("number", out JsonNode? numberNode)
            ? numberNode.GetValue<int>()
            : throw new InvalidProgramException("No number in casting time.");
        
        string unit = castingTimeObject.TryGetPropertyValue("unit", out JsonNode? unitNode)
            ? unitNode.GetValue<string>()
            : throw new InvalidProgramException("No unit in casting time.");

        if (unit == "bonus")
            unit = "bonus action";
        
        string? condition = castingTimeObject.TryGetPropertyValue("condition", out JsonNode? conditionNode)
            ? conditionNode.GetValue<string>()
            : null;
        
        objectNode.Remove("time");
        objectNode.Add("time", JsonValue.Create($"{number} {unit}{(condition != null ? $" {condition}" : string.Empty)}"));
    }
    
    private static void FixDurationStatblock(JsonObject? objectNode, string name)
    {
        if (!objectNode.TryGetPropertyValue("duration", out JsonNode? durationNode))
        {
            Console.WriteLine($"No duration. | {name}");
            return;
        }
        
        JsonObject durationObject = durationNode.AsObject();
        
        bool concentration = durationObject.TryGetPropertyValue("concentration", out _);

        int? amount = durationObject.TryGetPropertyValue("amount", out JsonNode? amountNode)
            ? amountNode.GetValue<int>()
            : null;
        
        string? type = durationObject.TryGetPropertyValue("type", out JsonNode? typeNode)
            ? typeNode.GetValue<string>()
            : null;
        
        bool upTo = durationObject.TryGetPropertyValue("upTo", out _);
        
        objectNode.Remove("duration");
        objectNode.Add("duration", JsonValue.Create($"{(concentration ? "Concentration, up to " : string.Empty)}{(upTo ? "Up to " : string.Empty)}{(amount != null ? $"{amount} " : " ")}{type}"));
    }
    
    private static void FixRange(JsonObject? objectNode, string name)
    {
        if (!objectNode.TryGetPropertyValue("range", out JsonNode? rangeNode))
            return;
        
        JsonObject rangeObject = rangeNode.AsObject();
        
        if (!rangeObject.TryGetPropertyValue("type", out JsonNode? typeNode))
            return;

        if (!rangeObject.TryGetPropertyValue("distance", out JsonNode? distanceNode))
            return;
        
        JsonObject distanceObject = distanceNode.AsObject();
        
        string type = typeNode.GetValue<string>();

        string rangeText = null;
        
        if (!distanceObject.TryGetPropertyValue("type", out JsonNode? distanceTypeNode))
            return;
        
        string distanceType = distanceTypeNode.GetValue<string>();

        if (type == "special")
            rangeText = type;
        else
        {
            if (distanceObject.TryGetPropertyValue("amount", out JsonNode? amountNode))
            {
                int amount = amountNode.GetValue<int>();
                rangeText = $"{amount} {distanceType} {(type == "point" ? "" : type)}";
            }
            else
                rangeText = $"{distanceType} {(type == "point" ? "" : type)}";
        }

        if (rangeText is null)
            return;
        
        objectNode.Remove("range");
        objectNode.Add("range", JsonValue.Create(rangeText));
    }
    
    private static void FixComponents(JsonObject? objectNode, string name)
    {
        if (!objectNode.TryGetPropertyValue("components", out JsonNode? componentsNode))
        {
            Console.WriteLine($"No components. | {name}");
            return;
        }
        
        JsonObject componentsObject = componentsNode.AsObject();
        string componentsText = string.Empty;
        string? materialText = null;
        
        if (componentsObject.TryGetPropertyValue("v", out _))
            componentsText += "V";
        
        if (componentsObject.TryGetPropertyValue("s", out _))
            componentsText += componentsText.Length > 0 ? ", S" : "S";

        if (componentsObject.TryGetPropertyValue("m", out JsonNode? materialNode))
        {
            componentsText += componentsText.Length > 0 ? ", M" : "M";
            
            if (materialNode is JsonValue)
                materialText = materialNode.GetValue<string>();   
            else if (materialNode is JsonObject materialObject)
                if (materialObject.TryGetPropertyValue("text", out JsonNode? materialTextNode))
                    materialText = materialTextNode.GetValue<string>();
        }
        
        objectNode.Remove("components");
        objectNode.Add("components", JsonValue.Create(componentsText));
        
        if (materialText is not null)
            objectNode.Add("materials", JsonValue.Create(materialText));
    }

    private static void FixEntries(JsonObject? objectNode, string name)
    {
        if (!objectNode.TryGetPropertyValue("entries", out JsonNode? entriesNode))
        {
            Console.WriteLine($"No entries. | {name}");
            return;
        }
        
        JsonArray entriesArray = entriesNode.AsArray();

        List<string> entries = new();
        
        foreach (JsonNode? entry in entriesArray)
        {
            switch (entry)
            {
                case JsonValue value:
                {
                    entries.Add(value.GetValue<string>());
                    break;
                }

                case JsonObject obj:
                {
                    if (!obj.TryGetPropertyValue("name", out JsonNode? nameNode))
                        break;
                    
                    if (!obj.TryGetPropertyValue("entries", out JsonNode? subEntriesNode))
                        break;

                    string subEntries = $"{nameNode.GetValue<string>()}: ";
                    subEntries += subEntriesNode.AsArray()
                                                .Select(e =>
                                                {
                                                    switch (e)
                                                    {
                                                        case JsonObject o:
                                                        {
                                                            if (!o.TryGetPropertyValue("items", out JsonNode? itemsNode))
                                                                throw new Exception("No items.");

                                                            return itemsNode.AsArray()
                                                                            .Select(i => i.GetValue<string>())
                                                                            .Join("\n");
                                                        }
                                                        
                                                        default:
                                                            return e.GetValue<string>();
                                                    }
                                                })
                                                .Join("\n");
                    
                    entries.Add(subEntries);
                    break;
                }
            }
        }
        
        objectNode.Remove("entries");
        objectNode.Add("entries", JsonValue.Create(entries.Join("\n\n")));
    }
    
    private static void FixClassesStatblock(JsonObject? objectNode, string name)
    {
        if (!objectNode.TryGetPropertyValue("classes", out JsonNode? classesNode))
        {
            Console.WriteLine($"No classes. | {name}");
            return;
        }
        
        string classesText = classesNode.GetValue<string>().Split(", ")
                                        .Select(c => $"[[{c}]]").Join(", ");

        objectNode.Remove("classes");
        objectNode.Add("classes", JsonValue.Create(classesText));
    }
    
    private static void FixRaces(JsonObject? objectNode, string name)
    {
        if (!objectNode.TryGetPropertyValue("races", out JsonNode? racesNode))
            return;
        
        JsonArray racesArray = racesNode.AsArray();

        string racesText = racesArray.Select(r =>
        {
            if (!r.AsObject().TryGetPropertyValue("name", out JsonNode nameNode))
                return string.Empty;
            
            string raceName = nameNode.GetValue<string>();
            return $"[[{raceName}]]";
        }).Join(", ");

        objectNode.Remove("races");
        objectNode.Add("races", racesText);
    }

    private static void FixFeats(JsonObject? objectNode, string name)
    {
        if (!objectNode.TryGetPropertyValue("feats", out JsonNode? featsNode))
            return;
        
        JsonArray featsArray = featsNode.AsArray();

        string featsText = featsArray.Select(r =>
        {
            if (!r.AsObject().TryGetPropertyValue("name", out JsonNode nameNode))
                return string.Empty;
            
            string featName = nameNode.GetValue<string>();
            return $"[[{featName}]]";
        }).Join(", ");

        objectNode.Remove("feats");
        objectNode.Add("feats", featsText);
    }
    
    private static void FixBackgrounds(JsonObject? objectNode, string name)
    {
        if (!objectNode.TryGetPropertyValue("backgrounds", out JsonNode? backgroundsNode))
            return;
        
        JsonArray backgroundsArray = backgroundsNode.AsArray();

        string backgroundsText = backgroundsArray.Select(r =>
        {
            if (!r.AsObject().TryGetPropertyValue("name", out JsonNode nameNode))
                return string.Empty;
            
            string backgroundName = nameNode.GetValue<string>();
            return $"[[{backgroundName}]]";
        }).Join(", ");

        objectNode.Remove("backgrounds");
        objectNode.Add("backgrounds", backgroundsText);
    }

    private static void FixOptionalFeatures(JsonObject? objectNode, string name)
    {
        if (!objectNode.TryGetPropertyValue("optionalfeatures", out JsonNode? optionalFeaturesNode))
            return;
        
        JsonArray optionalFeaturesArray = optionalFeaturesNode.AsArray();

        string optionalFeaturesText = optionalFeaturesArray.Select(r =>
        {
            if (!r.AsObject().TryGetPropertyValue("name", out JsonNode nameNode))
                return string.Empty;
            
            string optionalFeatureName = nameNode.GetValue<string>();
            return $"[[{optionalFeatureName}]]";
        }).Join(", ");

        objectNode.Remove("optionalfeatures");
        objectNode.Add("optionalfeatures", optionalFeaturesText);
    }

    private static void FixStats(JsonObject? objectNode, string name)
    {
        string castingTime, duration, rangeTarget, target, components;

        if (!objectNode.TryGetPropertyValue("time", out JsonNode? timeNode))
        {
            return;
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

    private static void FixDuration(JsonObject? objectNode, string name)
    {
        if (!objectNode.TryGetPropertyValue("duration", out JsonNode? durationNode))
            return;

        JsonObject durationObject = durationNode.AsArray()[0].AsObject();
        
        if (!durationObject.TryGetPropertyValue("type", out JsonNode? typeNode))
            return;

        string type = typeNode.GetValue<string>();

        if (type == "timed")
        {
            if (!durationObject.TryGetPropertyValue("duration", out JsonNode? nestedDurationNode))
            {
                Console.WriteLine($"Spell has a duration type of timed but no duration property. | {name}");
                return;
            }
            
            JsonObject nestedDurationObject = nestedDurationNode.AsObject();

            if (!nestedDurationObject.TryGetPropertyValue("type", out JsonNode? nestedTypeNode))
            {
                Console.WriteLine($"Spell has a duration type of timed but no nested type property. | {name}");
                return;
            }

            durationObject.Remove("duration");
            durationObject.Remove("type");
            durationObject.Add("type", nestedTypeNode.DeepCopy());
            
            if (!nestedDurationObject.TryGetPropertyValue("amount", out JsonNode? amountNode))
            {
                Console.WriteLine($"Spell has a duration type of timed but no nested amount property. | {name}");
                return;
            }
            
            durationObject.Add("amount", amountNode.DeepCopy());
        }

        objectNode.Remove("duration");
        objectNode.Add("duration", durationObject.DeepCopy());
    }

    private static void FixUpcast(JsonObject? objectNode, string name)
    {
        if (!objectNode.TryGetPropertyValue("entriesHigherLevel", out JsonNode? entriesHigherLevelNode))
            return;
        
        JsonObject entriesObject = entriesHigherLevelNode.AsArray()[0].AsObject();
        
        if (!entriesObject.TryGetPropertyValue("entries", out JsonNode? entriesNode))
            return;
        
        JsonArray entriesArray = entriesNode.AsArray();

        string upcast = entriesArray.Select(e => e.GetValue<string>()).Join(", ");
        objectNode.Remove("entriesHigherLevel");
        objectNode.Add("upcast", upcast);
    }
    
    static void RemoveUnwantedProperties(JsonNode? objectNode, string name)
    {
        if (objectNode is null)
            return;
        
        foreach (string unwantedProperty in s_unwantedProperties)
            objectNode.AsObject().Remove(unwantedProperty);
    }
    
    public void Dispose() { }
}