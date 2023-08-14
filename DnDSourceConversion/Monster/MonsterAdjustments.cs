using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

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
        FixSize(objectNode, name);
        FixAlignment(objectNode, name);
        FixSpeed(objectNode, name);
        FixSummonedBySpell(objectNode, name);
        FixAc(objectNode, name);
        FixHp(objectNode, name);
        FixSenses(objectNode, name);
        FixStats(objectNode, name);
        FixSpellcasting(objectNode, name);
        RemoveUnusedPropertiesInStatblock(objectNode);
    }
    
    private static readonly Dictionary<string, string> s_attackMap = new()
    {
        { "mw", "Melee Weapon Attack" },
        { "m", "Melee Weapon Attack" },
        { "rw", "Ranged Weapon Attack" },
        { "r", "Ranged Weapon Attack" },
        { "mw,rw", "Melee or Ranged Weapon Attack" },
        { "ms", "Melee Spell Attack" },
        { "rs", "Ranged Spell Attack" },
        { "ms,rs", "Melee or Ranged Spell Attack" },
    };

    private static readonly Dictionary<string, string> s_statusMap = new()
    {
        {"concentration", "[[Duration|Concentration]]"},
        {"concentration||concentrating", "[[Duration|Concentrating]]"}
    };

    public string HandleReplacements(string yaml, string name)
    {
        yaml = ReplaceHitIdents(yaml);
        yaml = ReplaceAttackIdents(yaml);
        yaml = ReplaceAttackBonusIdents(yaml);
        yaml = ReplaceDamageRollIdents(yaml);
        yaml = ReplaceStatusIdents(yaml);
        yaml = ReplaceSpellIdents(yaml);
        yaml = ReplaceConditionIdents(yaml);
        
        return yaml;
        
        static string ReplaceHitIdents(string yaml) => yaml.Replace("{@h}", "Hit: ");
        
        static string ReplaceAttackIdents(string yaml)
        {
            var regex = new Regex("{@atk .*?}");
            MatchCollection matches = regex.Matches(yaml);

            List<int> endIndexes = new(matches.Count);
            List<string> attackStrings = new(matches.Count);

            foreach (Match match in matches)
            {
                int index = yaml.IndexOf('}', match.Index);
                string substring = yaml[match.Index..index];
                substring = substring.Remove(0, 6);

                if (!s_attackMap.ContainsKey(substring))
                {
                    int f;
                }

                string attackString = s_attackMap[substring];

                endIndexes.Add(index);
                attackStrings.Add(attackString);
            }

            for (int i = matches.Count - 1; i >= 0; i--)
            {
                Match match = matches[i];
                int endIndex = endIndexes[i];
                string attackString = attackStrings[i];
                yaml = yaml.Remove(match.Index, endIndex - match.Index + 1);
                yaml = yaml.Insert(match.Index, $"{attackString}.");
            }

            return yaml;
        }
        
        static string ReplaceAttackBonusIdents(string yaml)
        {
            var regex = new Regex("{@hit .*?}");
            MatchCollection matches = regex.Matches(yaml);

            List<int> endIndexes = new(matches.Count);
            List<string> hitStrings = new(matches.Count);

            for (int i = 0; i < matches.Count; i++)
            {
                Match match = matches[i];
                int index = yaml.IndexOf('}', match.Index);
                string substring = yaml[match.Index..index];
                substring = substring.Remove(0, 6);
                substring = substring.Replace("summonSpellLevel", "summon spell level");
                substring = substring.Trim();

                endIndexes.Add(index);
                hitStrings.Add(substring);
            }

            for (int i = matches.Count - 1; i >= 0; i--)
            {
                Match match = matches[i];
                int endIndex = endIndexes[i];
                string hitString = hitStrings[i];
                yaml = yaml.Remove(match.Index, endIndex - match.Index + 1);
                yaml = yaml.Insert(match.Index, $"+ {hitString}");
            }

            return yaml;
        }
        
        static string ReplaceDamageRollIdents(string yaml)
        {
            var regex = new Regex("{@damage .*?}");
            MatchCollection matches = regex.Matches(yaml);

            List<int> endIndexes = new(matches.Count);
            List<string> damageStrings = new(matches.Count);

            foreach (Match match in matches)
            {
                int index = yaml.IndexOf('}', match.Index);
                string substring = yaml[match.Index..index];
                substring = substring.Remove(0, 9);

                endIndexes.Add(index);
                damageStrings.Add(substring);
            }

            for (int i = matches.Count - 1; i >= 0; i--)
            {
                Match match = matches[i];
                int endIndex = endIndexes[i];
                string damageString = damageStrings[i];
                yaml = yaml.Remove(match.Index, endIndex - match.Index + 1);
                yaml = yaml.Insert(match.Index, $"{damageString}");
            }
            
            return yaml;
        }
        
        static string ReplaceStatusIdents(string yaml)
        {
            var regex = new Regex("{@status .*?}");
            MatchCollection matches = regex.Matches(yaml);

            List<int> endIndexes = new(matches.Count);
            List<string> statusStrings = new(matches.Count);

            foreach (Match match in matches)
            {
                int index = yaml.IndexOf('}', match.Index);
                string substring = yaml[match.Index..index];
                substring = substring.Remove(0, 9);

                endIndexes.Add(index);
                statusStrings.Add(s_statusMap[substring]);
            }

            for (int i = matches.Count - 1; i >= 0; i--)
            {
                Match match = matches[i];
                int endIndex = endIndexes[i];
                string statusString = statusStrings[i];
                yaml = yaml.Remove(match.Index, endIndex - match.Index + 1);
                yaml = yaml.Insert(match.Index, statusString);
            }

            return yaml;
        }

        static string ReplaceSpellIdents(string yaml)
        {
            var regex = new Regex("{@spell .*?}");
            MatchCollection matches = regex.Matches(yaml);
            
            List<int> endIndexes = new(matches.Count);
            List<string> spellStrings = new(matches.Count);
            
            foreach (Match match in matches)
            {
                int index = yaml.IndexOf('}', match.Index);
                string substring = yaml[match.Index..index];
                substring = substring.Remove(0, 8);

                endIndexes.Add(index);
                spellStrings.Add($"[[{substring}]]");
            }
            
            for (int i = matches.Count - 1; i >= 0; i--)
            {
                Match match = matches[i];
                int endIndex = endIndexes[i];
                string spellString = spellStrings[i];
                yaml = yaml.Remove(match.Index, endIndex - match.Index + 1);
                yaml = yaml.Insert(match.Index, spellString);
            }

            return yaml;
        }

        static string ReplaceConditionIdents(string yaml)
        {
            var regex = new Regex("{@condition .*?}");
            MatchCollection matches = regex.Matches(yaml);
            
            List<int> endIndexes = new(matches.Count);
            List<string> conditionStrings = new(matches.Count);
            
            foreach (Match match in matches)
            {
                int index = yaml.IndexOf('}', match.Index);
                string substring = yaml[match.Index..index];
                substring = substring.Remove(0, 12);

                endIndexes.Add(index);
                conditionStrings.Add(substring);
            }
            
            for (int i = matches.Count - 1; i >= 0; i--)
            {
                Match match = matches[i];
                int endIndex = endIndexes[i];
                string conditionString = conditionStrings[i];
                yaml = yaml.Remove(match.Index, endIndex - match.Index + 1);
                yaml = yaml.Insert(match.Index, $"[[{conditionString}]]");
            }

            return yaml;
        }
    }

    private static void FixSpeed(JsonObject? objectNode, string name)
    {
        if (!objectNode.TryGetPropertyValue("speed", out JsonNode? speedNode))
        {
            Console.WriteLine($"No speed property found. | {name}");
            return;
        }

        JsonObject speedObject = speedNode.AsObject();
        string speedString = "";
        
        if (speedObject.TryGetPropertyValue("walk", out JsonNode? walkNode))
            speedString += $"{walkNode} ft.";
        
        if (speedObject.TryGetPropertyValue("fly", out JsonNode? flyNode))
            speedString += $", fly {flyNode} ft.";

        objectNode.Remove("speed");
        objectNode.Add("speed", speedString);
    }
    
    private static void FixSummonedBySpell(JsonObject? objectNode, string name)
    {
        if (!objectNode.TryGetPropertyValue("summonedBySpell", out JsonNode? spellNode))
            return;

        string spell = spellNode.GetValue<string>();
        string spellName = spell.Until('|');
        
        objectNode.Remove("summonedBySpell");
        objectNode.Add("summonedBySpell", $"[[{spellName}]]");
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

    private static void FixSenses(JsonObject? objectNode, string name)
    {
        List<string> senses = new();

        if (objectNode.TryGetPropertyValue("passive", out JsonNode? passiveNode))
        {
            objectNode.Remove("passive");
            senses.Add($"passive Perception {passiveNode}");
        }

        if (objectNode.TryGetPropertyValue("senses", out JsonNode? sensesNode))
        {
            var sensesArray = sensesNode as JsonArray;

            foreach (JsonNode? sense in sensesArray)
            {
                string senseString = sense.GetValue<string>();
                senses.Add(senseString);
            }
        }

        if (senses.Count == 0)
            return;
        
        objectNode.Remove("senses");
        objectNode.Add("senses", JsonValue.Create(senses));
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
        
        objectNode.Remove("size");
        objectNode.Add("size", sizeArray.SeparatedList(' '));
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
        
        objectNode.Remove("alignment");
        objectNode.Add("alignment", alignmentArray.SeparatedList(' '));
    }

    private static void FixSpellcasting(JsonObject? objectNode, string name)
    {
        if (!objectNode.TryGetPropertyValue("spellcasting", out JsonNode? spellcastingNode))
            return;
        
        JsonObject spellcastingObject = spellcastingNode.AsArray()[0].AsObject();
        
        List<string> spells = new();
        
        if (!spellcastingObject.TryGetPropertyValue("name", out JsonNode? nameNode))
            return;

        string spellcastingName = nameNode.GetValue<string>();

        if (spellcastingName != "Spellcasting")
            spells.Add(spellcastingName);
        
        if (!spellcastingObject.TryGetPropertyValue("headerEntries", out JsonNode? headerEntriesNode))
            return;
        
        spells.Add(headerEntriesNode.AsArray()[0].GetValue<string>());

        if (spellcastingObject.TryGetPropertyValue("spells", out JsonNode? willNode))
        {
            JsonObject willObject = willNode.AsObject();
            AddAtWillSpells(willObject, spells);
        }

        if (!spellcastingObject.TryGetPropertyValue("spells", out JsonNode? spellsNode))
            return;

        JsonObject spellsObject = spellsNode.AsObject();
        
        AddSpells(spellsObject, 0, spells);
        AddSpells(spellsObject, 1, spells);
        AddSpells(spellsObject, 2, spells);
        AddSpells(spellsObject, 3, spells);
        AddSpells(spellsObject, 4, spells);
        AddSpells(spellsObject, 5, spells);
        AddSpells(spellsObject, 6, spells);
        AddSpells(spellsObject, 7, spells);
        AddSpells(spellsObject, 8, spells);
        AddSpells(spellsObject, 9, spells);

        static void AddAtWillSpells(JsonObject spellsObject, List<string> spells)
        {
            
        }
        
        static void AddSpells(JsonObject spellsObject, int level, List<string> spells)
        {
            if (!spellsObject.TryGetPropertyValue(level.ToString(), out JsonNode? levelNode))
                return;

            JsonObject levelObject = levelNode.AsObject();
            string spellsString = level switch
            {
                0 => "Cantrips (at will): ",
                1 => "1st level ",
                2 => "2nd level ",
                3 => "3rd level ",
                _ => $"{level}th level ",
            };
            
            if (levelObject.TryGetPropertyValue("slots", out JsonNode? slotsNode))
            {
                int slots = slotsNode.GetValue<int>();
                spellsString += slots switch
                {
                    1 => "(1 slot): ",
                    _ => $"({slots} slots): ",
                };
            }

            if (!levelObject.TryGetPropertyValue("spells", out JsonNode? spellsNode))
            {
                Console.WriteLine($"No spells found in spellcasting level. | {level}");
                return;
            }
            
            JsonArray spellsArray = spellsNode.AsArray();

            for (int i = 0; i < spellsArray.Count; i++)
            {
                JsonNode? spell = spellsArray[(Index)i];
                spellsString += spell.GetValue<string>();
                
                if (i != spellsArray.Count - 1)
                    spellsString += ", ";
            }
            
            spells.Add(spellsString);
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