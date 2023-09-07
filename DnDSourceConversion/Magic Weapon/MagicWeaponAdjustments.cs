using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace DnDSourceConversion;

public class MagicWeaponAdjustments : IAdjustments
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
        FixBaseItem(objectNode, name);
        FixRarity(objectNode, name);
        FixAttunement(objectNode, name);
        FixDamageStatblock(objectNode, name);
        FixWeaponCategory(objectNode, name);
        FixPropertiesForStatblock(objectNode, name);
        FixAttachedSpells(objectNode, name);
        ConsolidateNotes(objectNode, name);
        RemoveUnusedPropertiesInStatblock(objectNode);
    }

    public string HandleReplacements(string yaml, string name)
    {
        if (name is "Will of the Talon")
        {
            int f;
        }
        
        yaml = yaml.ReplaceConditionIdents()
                   .ReplaceItemIdents()
                   .ReplaceQuickRefIdents()
                   .ReplaceIdentWithValue("dice", roll => $"{roll}")
                   .ReplaceIdentWithValue("damage", roll => $"{roll}")
                   .ReplaceIdentWithValue("action", action => $"[[Actions|{action}]]")
                   .ReplaceIdentWithValue("spell", spell => $"[[{spell}]]")
                   .ReplaceIdentWithValue("skill", skill => $"[[{skill}]]")
                   .ReplaceIdentWithValue("note", note => $"Note: {note}")
                   .ReplaceIdentWithValue("creature", creature => $"[[{creature}]]")
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
            
                        string propertyName = nameNode.GetValue<string>();
                        JsonArray? entriesArray = entriesNode.AsArray();

                        switch (propertyName)
                        {
                            case "Special":
                            {
                                properties.Add(new PropertyEntry("Special", entriesArray.Select(e => e.GetValue<string>()).ToList()));
                                break;
                            }

                            case "Random Properties":
                            {
                                List<string> items = new();

                                foreach (JsonNode? e in entriesArray)
                                {
                                    switch (e)
                                    {
                                        case JsonValue value:
                                            items.Add(value.GetValue<string>());
                                            break;
        
                                        case JsonObject obj:
                                            obj.TryGetPropertyValue("type", out JsonNode? typeNode);
                                            if (typeNode.GetValue<string>() != "list")
                                                throw new InvalidProgramException("Random Properties entry is not a list.");
            
                                            obj.TryGetPropertyValue("items", out JsonNode? itemsNode);
                                            items.AddRange(itemsNode.AsArray().Select(i => i.GetValue<string>()).ToList());
                                            break;
                                    }
                                }
                                
                                properties.Add(new PropertyEntry("Random Properties", items));
                                break;
                            }
                        }
                        
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
                            {
                                wrappedObject.TryGetPropertyValue("items", out JsonNode? itemsNode);
                                properties.Add(new PropertyEntry("Variants", itemsNode.AsArray().Select(i => i.GetValue<string>()).ToList()));
                                continue;
                            }

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
        objectNode.Add("properties", new JsonArray(properties
                                                        .Where(p => p.Name != "Random Properties")
                                                        .Where(p => p.Name != "Variants")
                                                        .Select(p => JsonValue.Create(p.Name))
                                                        .ToArray()));

        foreach (PropertyEntry propertyEntry in properties.Where(p => p.Values is not null))
        {
            switch (propertyEntry.Name)
            {
                case "Special":
                {
                    List<string> specialStrings = properties.FirstOrDefault(p => p.Values is not null).Values;
                    objectNode.Add("special", new JsonArray(specialStrings.Select(s => JsonValue.Create(s)).ToArray()));
                    break;
                }
                
                case "Random Properties":
                {
                    List<string> randomProperties = propertyEntry.Values;
                    int i = -1;
                    List<object> entries = randomProperties.Select<string, object>(rp =>
                    {
                        i++;
                        if (i == 0)
                            return rp;
                        
                        int index = rp.IndexOf("{@table");
                        int semiColonIndex = rp.IndexOf(';', index);
                        int firstPipeIndex = rp.IndexOf('|', semiColonIndex);
                        int lastPipeIndex = rp.LastIndexOf('|');
                        int endIndex = rp.IndexOf('}');
                        string number = rp[..index].Trim();
                        string itemName = rp[(index + 7)..semiColonIndex].Trim();
                        string tableName = rp[(semiColonIndex + 1)..firstPipeIndex].Trim();
                        string tableNameAlias = rp[(lastPipeIndex + 1)..endIndex].Trim();

                        return new Dictionary<string, string>() {{$"{itemName}", $"{number} [[{tableName}|{tableNameAlias}]]"}};
                    }).ToList();
                    
                    objectNode.Add("randomProperties", JsonValue.Create(entries));
                    break;
                }

                case "Variants":
                {
                    List<string> variants = propertyEntry.Values;
                    objectNode.Add("variants", JsonValue.Create(string.Join(", ", variants)));
                    break;
                }
            }
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

    private static void FixBaseItem(JsonObject? objectNode, string name)
    {
        if (!objectNode.TryGetPropertyValue("baseItem", out JsonNode? baseItemNode))
            return;
        
        string baseItem = baseItemNode.GetValue<string>();
        int pipeIndex = baseItem.IndexOf('|');
        baseItem = baseItem[..pipeIndex];

        objectNode.Remove("baseItem");
        objectNode.Add("baseItem", JsonValue.Create($"({baseItem})"));
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

    private static void FixAttunement(JsonObject? objectNode, string name)
    {
        if (!objectNode.TryGetPropertyValue("reqAttune", out JsonNode? reqAttuneNode))
            return;
        
        object reqAttune = reqAttuneNode.GetValue<object>();
        string reqAttuneString = "(requires attunement)";

        switch (reqAttune)
        {
            case string s:
            {
                reqAttuneString += $" {s}";
                break;
            }
        }
        
        objectNode.Remove("reqAttune");
        objectNode.Add("reqAttune", JsonValue.Create(reqAttuneString));
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

    private static void FixAttachedSpells(JsonObject? objectNode, string name)
    {
        if (!objectNode.TryGetPropertyValue("attachedSpells", out JsonNode? attachedSpellsNode))
            return;

        JsonArray spellsArray = attachedSpellsNode.AsArray();

        string spells = spellsArray.Select(spell => $"[[{spell.GetValue<string>()
                .Split(" ")
                .Select(s => s.ToUpperFirstLetter())
                .Join(" ")}]]")
            .Join(", ");
        
        bool requiresAttunement = objectNode.TryGetPropertyValue("reqAttune", out _);
        spells = spells.Insert(0, requiresAttunement ? "While attuned to this item you can cast the following spells:\n" : "You can cast the following spells:\n");

        objectNode.Remove("attachedSpells");
        objectNode.Add("attachedSpells", JsonValue.Create(spells));
    }
    
    private static void ConsolidateNotes(JsonObject? objectNode, string name)
    {
        if (!objectNode.TryGetPropertyValue("notes", out JsonNode? notesNode))
            return;
        
        JsonArray notesArray = notesNode.AsArray();

        string notes = string.Join('\n', notesArray);

        objectNode.Remove("notes");
        objectNode.Add("notes", JsonValue.Create(notes));
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
        public PropertyEntry(string name, List<string>? values)
        {
            Name = name;
            Values = values;
        }
        
        public string Name { get; }
        public List<string>? Values { get; }
    }
}
