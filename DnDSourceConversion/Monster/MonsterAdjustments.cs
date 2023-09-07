using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using DnDSourceConversion.ImageLoading;

using static DnDSourceConversion.StatblockUtils;

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

    private readonly ImageDownloader _imageDownloader = new();
    private readonly string _imageSaveFolder;
    
    public MonsterAdjustments(string imageSaveFolder)
    {
        if (imageSaveFolder.EndsWith('/') || imageSaveFolder.EndsWith('\\'))
            imageSaveFolder = imageSaveFolder[..^1];
        
        _imageSaveFolder = imageSaveFolder;
    }

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

        if (GlobalConfig.FIND_IMAGES)
            AddImage(objectNode, name);
        
        FixAc(objectNode, name);
        FixHp(objectNode, name);
        FixSpeed(objectNode, name);
        FixSummonedBySpell(objectNode, name);
        FixStats(objectNode, name);
        FixSaves(objectNode, name);
        CombineNestedModifiers(objectNode, "resist", "note", name);
        CombineNestedModifiers(objectNode, "immune", "note", name);
        SeparatedTextFromArray(objectNode, "resist", ", ", name);
        SeparatedTextFromArray(objectNode, "immune", ", ", name);
        SeparatedTextFromArray(objectNode, "conditionImmune", ", ", name);
        SeparatedTextFromArray(objectNode, "languages", ", " , name);
        FixSenses(objectNode, name);
        SeparatedTextFromArray(objectNode, "senses", ", ", name);
        FixSpellcasting(objectNode, name);
        RemoveUnusedPropertiesInStatblock(objectNode);
    }
    
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public string HandleReplacements(string yaml, string name)
    {
        return yaml.Replace('×', '*')
                   .ReplaceHitIdents()
                   .ReplaceAttackIdents()
                   .ReplaceAttackBonusIdents()
                   .ReplaceDamageRollIdents()
                   .ReplaceStatusIdents()
                   .ReplaceSpellIdents()
                   .ReplaceConditionIdents()
                   .ReplaceItemIdents()
                   .ReplaceQuickRefIdents()
                   .ReplaceIdentWithValue("dc", dc => $"[[Difficulty Class|DC]] {dc}")
                   .ReplaceIdentWithValue("dice", roll => roll)
                   .ReplaceIdentWithValue("damage", roll => $"{roll}")
                   .ReplaceIdentWithValue("action", action => $"[[Actions|{action}]]")
                   .ReplaceIdentWithValue("spell", spell => $"[[{spell}]]")
                   .ReplaceIdentWithValue("skill", skill => $"[[{skill}]]")
                   .ReplaceIdentWithValue("note", note => $"Note: {note}")
                   .ReplaceIdentWithValue("creature", creature => $"[[{creature}]]")
                   .ReplaceIdentWithValue("recharge", value =>
                    {
                        if (string.IsNullOrEmpty(value))
                            Console.WriteLine($"Warning: Recharge value is empty. | {name}");
                           
                        return $"(recharge {value})";
                    })
                   .SurroundWithLinkBrackets("[Dd]ifficult [Tt]errain", "Movement")
                   .SurroundWithLinkBrackets("AC", "Armor Class")
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
            sizeArray.Insert(i, JsonValue.Create(Maps.SizeMap[size.GetValue<string>()]));
        }
        
        objectNode.Remove("size");
        objectNode.Add("size", sizeArray.SeparatedList(' '));
    }

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
                    
                    if (!Maps.AlignmentMap.ContainsKey(alignment))
                        break;
                    
                    alignmentArray.RemoveAt(i);
                    alignmentArray.Insert(i, JsonValue.Create(Maps.AlignmentMap[alignment]));
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

                        if (!Maps.AlignmentMap.ContainsKey(alignment))
                        {
                            alignmentArray.Add(JsonValue.Create(alignment));   
                            continue;
                        }

                        alignmentArray.Add(JsonValue.Create(Maps.AlignmentMap[alignment]));
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

    private void AddImage(JsonObject? objectNode, string name)
    {
        if (!objectNode.TryGetPropertyValue("source", out JsonNode? sourceNode))
            return;

        if (File.Exists($"{_imageSaveFolder}/{name}.jpg"))
            return;

        string urlName = name.Replace(" ", "")
                             .Replace("`", "")
                             .Replace("(", "")
                             .Replace(")", "");
        
        string source = sourceNode.GetValue<string>();

        Uri apiAddress = new($"https://5e.tools/img/{source}/{urlName}.png");

        byte[]? image = _imageDownloader.DownloadImage(apiAddress);

        if (image is null)
            return;
        
        PngImageSaver imageSaver = new();
        
        if (!GlobalConfig.DRY_RUN)
            imageSaver.Save(image, $"{_imageSaveFolder}/{name}.png");
        
        objectNode.Add("image", JsonValue.Create($"{name}.png"));
    }
    
    private async void AddImageAsync(JsonObject? objectNode, string name)
    {
        if (!objectNode.TryGetPropertyValue("source", out JsonNode? sourceNode))
            return;

        if (File.Exists($"{_imageSaveFolder}/{name}.jpg"))
            return;
        
        string urlName = name.Replace(" ", "")
                             .Replace("`", "")
                             .Replace("(", "")
                             .Replace(")", "");

        string source = sourceNode.GetValue<string>();
        
        Uri apiAddress = new($"https://5e.tools/img/{source}/{urlName}.png");

        byte[]? image = await _imageDownloader.DownloadImageAsync(apiAddress);

        if (image is null)
            return;
        
        PngImageSaver imageSaver = new();
        
        if (!GlobalConfig.DRY_RUN)
            imageSaver.Save(image, $"{_imageSaveFolder}/{name}.png");
        
        objectNode.Add("image", JsonValue.Create($"{name}.png"));
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
        {
            string flyText = flyNode switch
            {
                JsonValue value => value.GetValue<int>().ToString(),
                JsonObject obj => $"{obj["number"].GetValue<int>()} ft. {obj["condition"].GetValue<string>()}",
            };
            
            speedString += $", fly {flyText}";
        }

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
            JsonArray sensesArray = sensesNode.AsArray();

            foreach (JsonNode? sense in sensesArray)
            {
                string senseString = sense.GetValue<string>();
                senses.Add(senseString);
            }
        }

        if (senses.Count == 0)
            return;
        
        objectNode.Remove("senses");
        objectNode.Add("senses", new JsonArray(senses.Select(sense => JsonValue.Create(sense)).ToArray()));
    }
    
    private static void FixSaves(JsonObject? objectNode, string name)
    {
        if (!objectNode.TryGetPropertyValue("save", out JsonNode? saveNode))
            return;
        
        JsonObject saveObject = saveNode.AsObject();

        List<KeyValuePair<string, JsonNode?>> pairs = saveObject.ToList();
        
        for (int i = pairs.Count - 1; i >= 0; i--)
        {
            string key = pairs[i].Key;
            JsonNode? value = pairs[i].Value;

            saveObject.Remove(key);
            saveObject.Add(Maps.SkillMap[key], value);
        }
    }
    
    private static void CombineNestedModifiers(JsonObject? objectNode, string identifier, string noteIdentifier, string name)
    {
        if (!objectNode.TryGetPropertyValue(identifier, out JsonNode? node))
            return;
        
        var array = node as JsonArray;

        for (int i = array.Count - 1; i >= 0; i--)
        {
            JsonNode? elementNode = array[(Index)i];

            switch (elementNode)
            {
                case JsonObject objectElementNode:
                {
                    if (objectElementNode.TryGetPropertyValue(identifier, out JsonNode? nestedNode))
                    {
                        JsonArray nestedArrayNode = nestedNode.AsArray();
                        string joinedValue = string.Join(", ", nestedArrayNode);

                        if (objectElementNode.TryGetPropertyValue(noteIdentifier, out JsonNode? noteNode))
                            joinedValue += $" {noteNode.GetValue<string>()}";

                        
                        array.RemoveAt(i);
                        array.Add(joinedValue);
                    }

                    break;
                }
            }
        }
    }

    private static void CommaSeparatedTextFromArray(JsonObject? objectNode, string identifier, string name)
    {
        if (!objectNode.TryGetPropertyValue(identifier, out JsonNode? node))
            return;

        JsonArray array = node.AsArray();
        
        string joinedValue = string.Join(", ", array);
        
        objectNode.Remove(identifier);
        objectNode.Add(identifier, joinedValue);
    }
    
    private static void FixSpellcasting(JsonObject? objectNode, string name)
    {
        if (!objectNode.TryGetPropertyValue("spellcasting", out JsonNode? spellcastingNode))
            return;
        
        JsonObject spellcastingObject = spellcastingNode.AsArray()[0].AsObject();
        
        List<(string level, string  list)> spells = new();
        
        if (!spellcastingObject.TryGetPropertyValue("name", out JsonNode? nameNode))
            return;

        string spellcastingName = nameNode.GetValue<string>();

        if (spellcastingName != "Spellcasting")
            spells.Add(("", spellcastingName.Replace("Spellcasting", "").Trim()));
        
        if (!spellcastingObject.TryGetPropertyValue("headerEntries", out JsonNode? headerEntriesNode))
            return;
        
        spells.Add(("", headerEntriesNode.AsArray()[0].GetValue<string>()));

        if (spellcastingObject.TryGetPropertyValue("will", out JsonNode? willNode))
        {
            JsonArray willArray = willNode.AsArray();
            AddAtWillSpells(willArray, spells, name);
        }
        
        if (spellcastingObject.TryGetPropertyValue("daily", out JsonNode? dailyNode))
        {
            JsonObject dailyObject = dailyNode.AsObject();
            AddDailySpells(dailyObject, spells, name);
        }

        if (spellcastingObject.TryGetPropertyValue("spells", out JsonNode? spellsNode))
        {
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
        }

        List<object> pain = spells.Select(tuple =>
        {
            string level = tuple.level;
            string list = tuple.list;

            object spell;

            if (string.IsNullOrEmpty(level))
                spell = list;
            else
                spell = new Dictionary<string, string> { { level, list } };
            
            return spell;
        }).ToList();
        
        objectNode.Remove("spellcasting");
        objectNode.Add("spell", JsonValue.Create(pain));

        static void AddAtWillSpells(JsonArray willArray, List<(string level, string  list)> spells, string name)
        {
            string commaSeparatedSpells = "";

            foreach (JsonNode? valueNode in willArray)
            {
                switch (valueNode)
                {
                    case JsonValue:
                    {
                        commaSeparatedSpells += $"{valueNode.GetValue<string>()}, ";
                        break;
                    }

                    case JsonObject valueObject:
                    {
                        if (!valueObject.TryGetPropertyValue("entry", out JsonNode entryNode))
                        {
                            Console.WriteLine($"At Will Spell has no entry. | {name}");
                            break;
                        }
                            
                        if (!valueObject.TryGetPropertyValue("hidden", out JsonNode hiddenNode))
                        {
                            Console.WriteLine($"At Will Spell has no hidden. | {name}");
                            break;
                        }
                        
                        commaSeparatedSpells += $"{entryNode.GetValue<string>()} {(hiddenNode.GetValue<bool>() ? "(hidden)" : "")}, ";
                        break;
                    }
                }
            }
            
            commaSeparatedSpells = commaSeparatedSpells.Remove(commaSeparatedSpells.Length - 2);
            spells.Add(("At will", commaSeparatedSpells));
        }
        
        static void AddDailySpells(JsonObject dailyObject, List<(string level, string  list)> spells, string name)
        {
            foreach (KeyValuePair<string,JsonNode?> pair in dailyObject)
            {
                string propertyName = pair.Key;
                int index = propertyName.IndexOf("e");
                bool each = index >= 0;

                int amountDaily = int.Parse(each ? propertyName[..index] : propertyName); // If this throws, the JSON is invalid.

                JsonArray valueArray = pair.Value.AsArray();
                
                if (valueArray.Count == 0)
                    continue;
                
                string commaSeparatedSpells = "";
                
                foreach (JsonNode? valueNode in valueArray)
                {
                    switch (valueNode)
                    {
                        case JsonValue:
                        {
                            commaSeparatedSpells += $"{valueNode.GetValue<string>()}, ";
                            break;
                        }

                        case JsonObject valueObject:
                        {
                            if (!valueObject.TryGetPropertyValue("entry", out JsonNode entryNode))
                            {
                                Console.WriteLine($"Daily Spell has no entry. | {name}");
                                break;
                            }
                            
                            if (!valueObject.TryGetPropertyValue("hidden", out JsonNode hiddenNode))
                            {
                                Console.WriteLine($"Daily Spell has no hidden. | {name}");
                                break;
                            }
                            
                            commaSeparatedSpells += $"{entryNode.GetValue<string>()} {(hiddenNode.GetValue<bool>() ? "(hidden)" : "")}, ";
                            break;
                        }
                    }
                }

                commaSeparatedSpells = commaSeparatedSpells.Remove(commaSeparatedSpells.Length - 2);
                spells.Add(($"{amountDaily}/day{(each ? " each" : "")}", commaSeparatedSpells));
            }
        }
        
        static void AddSpells(JsonObject spellsObject, int level, List<(string level, string  list)> spells)
        {
            if (!spellsObject.TryGetPropertyValue(level.ToString(), out JsonNode? levelNode))
                return;

            JsonObject levelObject = levelNode.AsObject();
            
            string commaSeparatedSpells = "";
            
            if (levelObject.TryGetPropertyValue("slots", out JsonNode? slotsNode))
            {
                int slots = slotsNode.GetValue<int>();
                commaSeparatedSpells += slots switch
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
                commaSeparatedSpells += spell.GetValue<string>();
                
                if (i != spellsArray.Count - 1)
                    commaSeparatedSpells += ", ";
            }
            
            string spellsString = level switch
            {
                0 => "Cantrips (at will)",
                1 => "1st level",
                2 => "2nd level",
                3 => "3rd level",
                _ => $"{level}th level",
            };

            spells.Add((spellsString, commaSeparatedSpells));
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
            "page",
            "source",
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

    public void Dispose() => _imageDownloader.Dispose();
}