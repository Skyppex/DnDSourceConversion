using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

using static DnDSourceConversion.StatblockUtils;

namespace DnDSourceConversion;

public class WeaponAdjustments : IAdjustments
{
    private static readonly string[] s_unwantedProperties =
    {
        "entries",
        "type",
        "property",
        "sword",
        "dagger",
        "weapon",
        "firearm",
    };

    public void Adjust(JsonObject? objectNode, string name)
    {
        FixProperties(objectNode, name);
        FilterUnwantedProperties(objectNode);
        FixDamage(objectNode, name);
    }

    public void AdjustStatblock(JsonObject? objectNode, string name)
    {
        FixRarity(objectNode, name);
        FixDamageStatblock(objectNode, name);
        FixWeaponCategory(objectNode, name);
        FixPropertiesForStatblock(objectNode, name);
        SeparatedTextFromArray(objectNode, "notes", ", ", name);
        RemoveUnusedPropertiesInStatblock(objectNode);
    }

    public string HandleReplacements(string yaml, string name)
    {
        yaml = yaml.ReplaceConditionIdents()
                   .ReplaceItemIdents()
                   .ReplaceQuickRefIdents()
                   .ReplaceIdentWithValue("dice", roll => $"0 ({roll})")
                   .ReplaceIdentWithValue("action", action => $"[[Actions|{action}]]")
                   .ReplaceIdentWithValue("note", note => $"Note: {note}")
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

    public void Dispose() { }
    
    private static void FixProperties(JsonObject? objectNode, string name)
    {
        if (!objectNode.TryGetPropertyValue("_fullEntries", out JsonNode? fullEntriesNode))
        {
            Console.WriteLine($"No _fullEntries property. | {name}");
            return;
        }
        
        JsonArray? fullEntriesArray = fullEntriesNode.AsArray();
        List<PropertyEntry> properties = new();
        
        foreach (JsonNode? entry in fullEntriesArray)
        {
            switch (entry)
            {
                case JsonValue value:
                {
                    AddValueToNotes(objectNode, value);
                    break;
                }
        
                case JsonObject entryObject:
                {
                    if (!entryObject.TryGetPropertyValue("wrapped", out JsonNode? wrappedNode))
                    {
                        if (!entryObject.TryGetPropertyValue("name", out JsonNode? nameNode))
                            return;
                        
                        if (!entryObject.TryGetPropertyValue("entries", out JsonNode? entriesNode))
                            throw new InvalidProgramException("No entries in entry with name 'Special'.");
            
                        JsonArray? entriesArray = entriesNode.AsArray();
                        List<string> special = entriesArray.Select(e => e.GetValue<string>())
                                                           .Where(s => !string.IsNullOrEmpty(s))
                                                           .ToList();
        
                        string propertyName = nameNode.GetValue<string>();
                        properties.Add(new PropertyEntry(propertyName, special));
                        
                        continue;
                    }
        
                    switch (wrappedNode)
                    {
                        case JsonValue value:
                        {
                            AddValueToNotes(objectNode, value);
                            break;
                        }
        
                        case JsonObject wrappedObject:
                        {
                            if (!wrappedObject.TryGetPropertyValue("name", out JsonNode? nameNode))
                                throw new InvalidProgramException("No property name in wrapped object.");
            
                            string propertyName = nameNode.GetValue<string>();
        
                            if (propertyName != "Special")
                            {
                                properties.Add(new PropertyEntry(propertyName, null));
                                continue;
                            }
            
                            break;
                        }
                    }
        
                    break;
                }
            }
        }
        
        objectNode.Remove("_fullEntries");
        objectNode.Add("properties", new JsonArray(properties.Select(p => JsonValue.Create(p.Name)).ToArray()));
        
        if (properties.Any(p => p.Special is not null))
        {
            List<string> specialStrings = properties.FirstOrDefault(p => p.Special is not null).Special;
            objectNode.Add("special", new JsonArray(specialStrings.Select(s => JsonValue.Create(s)).ToArray()));
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

    private static void FixDamage(JsonObject? objectNode, string name)
    {
        if (!objectNode.TryGetPropertyValue("dmg1", out JsonNode? dam1Node))
        {
            Console.WriteLine($"No damage property. | {name}");
            return;
        }
        
        string damage = dam1Node.GetValue<string>();
        objectNode.Remove("dmg1");
        objectNode.Add("damage", JsonValue.Create(damage));
        
        if (!objectNode.TryGetPropertyValue("dmgType", out JsonNode? dmgTypeNode))
        {
            Console.WriteLine($"No damage type property. | {name}");
            return;
        }
        
        string damageType = dmgTypeNode.GetValue<string>();
        objectNode.Remove("dmgType");
        objectNode.Add("damageType", JsonValue.Create(Maps.DamageTypeMap[damageType]));
        
        if (!objectNode.TryGetPropertyValue("dmg2", out JsonNode? dam2Node))
            return;
        
        string versatileDamageType = dam2Node.GetValue<string>();
        objectNode.Remove("dmg2");
        objectNode.Add("versatileDamage", JsonValue.Create(versatileDamageType));
    }
    
    private static void FixRarity(JsonObject? objectNode, string name)
    {
        if (!objectNode.TryGetPropertyValue("rarity", out JsonNode? rarityNode))
            return;

        string rarity = rarityNode.GetValue<string>();

        objectNode.Remove("rarity");

        if (rarity is "none")
            return;
        
        objectNode.Add("rarity", JsonValue.Create(rarity.ToUpperFirstLetter()));
    }

    private static void FixDamageStatblock(JsonObject? objectNode, string name)
    {
        if (!objectNode.TryGetPropertyValue("damage", out JsonNode? damageNode))
        {
            Console.WriteLine($"No damage property. | {name}");
            return;
        }
        
        if (!objectNode.TryGetPropertyValue("damageType", out JsonNode? damageTypeNode))
        {
            Console.WriteLine($"No damage type property. | {name}");
            return;
        }
        
        string damage = damageNode.GetValue<string>();
        string damageType = damageTypeNode.GetValue<string>();

        if (objectNode.TryGetPropertyValue("versatileDamage", out JsonNode? versatileDamageNode))
        {
            string versatileDamage = versatileDamageNode.GetValue<string>();
            
            objectNode.Remove("damage");
            objectNode.Remove("damageType");
            objectNode.Remove("versatileDamage");
            objectNode.Add("damage", JsonValue.Create($"0 ({damage}) or 0 ({versatileDamage}) {damageType}"));
            return;
        }
        
        objectNode.Remove("damage");
        objectNode.Remove("damageType");
        objectNode.Add("damage", JsonValue.Create(damage.Contains('d') ? $"0 ({damage}) {damageType}" : $"{damage} {damageType}"));
    }
    
    private static void FixWeaponCategory(JsonObject? objectNode, string name)
    {
        if (!objectNode.TryGetPropertyValue("weaponCategory", out JsonNode? categoryNode))
        {
            Console.WriteLine($"No weaponCategory property. | {name}");
            return;
        }
        
        string category = categoryNode.GetValue<string>().ToUpperFirstLetter();
        
        objectNode.Remove("weaponCategory");
        objectNode.Add("weaponCategory", JsonValue.Create($"{category} Weapon"));
    }
    
    private static void FixPropertiesForStatblock(JsonObject? objectNode, string name)
    {
        if (!objectNode.TryGetPropertyValue("properties", out JsonNode? propertiesNode))
            return;

        JsonArray propertiesArray = propertiesNode.AsArray();
        
        string propertiesString = string.Join(", ", propertiesArray.Select(p => $"[[{p.GetValue<string>()}]]"));
        
        objectNode.Remove("properties");
        objectNode.Add("properties", JsonValue.Create(propertiesString));

        if (objectNode.TryGetPropertyValue("special", out JsonNode? specialNode) && specialNode is JsonArray specialArray)
        {
            objectNode.Remove("special");
            objectNode.Add("special", JsonValue.Create(string.Join(". ", specialArray.Select(p => p.GetValue<string>()))));
        }
    }

    private static void RemoveUnusedPropertiesInStatblock(JsonObject? objectNode)
    {
        string[] unusedProperties =
        {
            "page",
            "source",
            "otherSources",
            "srd",
            "basicRules",
        };

        foreach (string property in unusedProperties)
            objectNode.Remove(property);
    }

    private static void AddValueToNotes(JsonObject objectNode, JsonValue value)
    {
        value = JsonValue.Create(value.GetValue<string>());
        
        if (!objectNode.TryGetPropertyValue("notes", out JsonNode? notesNode))
        {
            objectNode.Add("notes", new JsonArray(value));
            return;
        }

        JsonArray notesArray = notesNode.AsArray();
        notesArray.Add(value);
    }

    private struct PropertyEntry
    {
        public PropertyEntry(string name, List<string>? special)
        {
            Name = name;
            Special = special;
        }
        
        public string Name { get; }
        public List<string>? Special { get; }
    }
}
