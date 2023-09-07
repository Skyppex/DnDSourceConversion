using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace DnDSourceConversion;

public static class StatblockUtils
{
    public static void SeparatedTextFromArray(JsonObject? objectNode, string identifier, string separator, string name)
    {
        if (!objectNode.TryGetPropertyValue(identifier, out JsonNode? node))
            return;

        JsonArray array = node.AsArray();
        
        string joinedValue = string.Join(separator, array);
        
        objectNode.Remove(identifier);
        objectNode.Add(identifier, joinedValue);
    }
}

public static class StatblockExtensions
{
    public static string ReplaceHitIdents(this string yaml) => yaml.Replace("{@h}", "Hit: ");
    
    public static string ReplaceAttackIdents(this string yaml)
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

            string attackString = Maps.AttackMap[substring];

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
    
    public static string ReplaceAttackBonusIdents(this string yaml)
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
            yaml = yaml.Insert(match.Index, $"+{hitString}");
        }

        return yaml;
    }
    
    public static string ReplaceDamageRollIdents(this string yaml)
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
    
    public static string ReplaceSpellIdents(this string yaml)
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
            spellStrings.Add(substring);
        }
        
        for (int i = matches.Count - 1; i >= 0; i--)
        {
            Match match = matches[i];
            int endIndex = endIndexes[i];
            string spellString = spellStrings[i];
            yaml = yaml.Remove(match.Index, endIndex - match.Index + 1);
            yaml = yaml.Insert(match.Index, $"[[{spellString}]]");
        }

        return yaml;
    }

    public static string ReplaceConditionIdents(this string yaml)
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

    public static string ReplaceItemIdents(this string yaml)
    {
        var regex = new Regex("{@item .*?}");
        MatchCollection matches = regex.Matches(yaml);
        
        List<int> endIndexes = new(matches.Count);
        List<string> itemStrings = new(matches.Count);
        
        foreach (Match match in matches)
        {
            int index = yaml.IndexOf('}', match.Index);
            string substring = yaml[match.Index..index];
            int pipeIndex = substring.IndexOf('|');

            if (pipeIndex >= 0)
                substring = substring[7..pipeIndex];
            else
                substring = substring.Remove(0, 7);

            endIndexes.Add(index);
            itemStrings.Add(substring);
        }
        
        for (int i = matches.Count - 1; i >= 0; i--)
        {
            Match match = matches[i];
            int endIndex = endIndexes[i];
            string itemString = itemStrings[i];
            yaml = yaml.Remove(match.Index, endIndex - match.Index + 1);
            yaml = yaml.Insert(match.Index, $"[[{itemString}]]");
        }

        return yaml;
    }

    public static string ReplaceQuickRefIdents(this string yaml)
    {
        var regex = new Regex("{@quickref .*?}");
        MatchCollection matches = regex.Matches(yaml);
        
        List<int> endIndexes = new(matches.Count);
        List<string> quickRefStrings = new(matches.Count);
        
        foreach (Match match in matches)
        {
            int endIndex = yaml.IndexOf('}', match.Index);
            string substring = yaml[match.Index..endIndex];
            substring = substring.Remove(0, 11);
            
            int firstPipeIndex = substring.IndexOf('|');
            int lastPipeIndex = substring.LastIndexOf('|');

            string linkText = substring[..firstPipeIndex];
            string displayText = substring[lastPipeIndex..].Remove(0, 1);

            endIndexes.Add(endIndex);
            quickRefStrings.Add($"{linkText}{(displayText.ToUpper() != displayText ? $"|{displayText}" : "")}");
        }
        
        for (int i = matches.Count - 1; i >= 0; i--)
        {
            Match match = matches[i];
            int endIndex = endIndexes[i];
            string quickRefString = quickRefStrings[i];
            yaml = yaml.Remove(match.Index, endIndex - match.Index + 1);
            yaml = yaml.Insert(match.Index, $"[[{quickRefString}]]");
        }
        
        return yaml;
    }
    
    public static string ReplaceIdentWithValue(this string yaml, string ident, Func<string, string> replacement, string endOn = "}")
    {
        var regex = new Regex($"{{@{ident} .*?}}");
        MatchCollection matches = regex.Matches(yaml);
        
        List<int> endIndexes = new(matches.Count);
        List<string> values = new(matches.Count);

        foreach (Match match in matches)
        {
            int endIndex = yaml.IndexOf("}", match.Index);
            int index = yaml.IndexOf(endOn, match.Index);
            
            string substring = yaml[match.Index..index];
            substring = substring.Remove(0, ident.Length + 3);

            endIndexes.Add(endIndex);
            values.Add(substring);
        }
        
        for (int i = matches.Count - 1; i >= 0; i--)
        {
            Match match = matches[i];
            int endIndex = endIndexes[i];
            string value = values[i];
            yaml = yaml.Remove(match.Index, endIndex - match.Index + 1);
            yaml = yaml.Insert(match.Index, replacement(value));
        }
        
        return yaml;
    }

    public static string SurroundWithLinkBrackets(this string yaml, string regexString, string linkTo)
        {
            var regex = new Regex(regexString);
            MatchCollection matches = regex.Matches(yaml);
            
            for (int i = matches.Count - 1; i >= 0; i--)
            {
                Match match = matches[i];
                yaml = yaml.Remove(match.Index, match.Length);
                yaml = yaml.Insert(match.Index, $"[[{linkTo}|{match.Value}]]");
            }

            return yaml;
        }
    
    public static string ReplaceStatusIdents(this string yaml)
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
            statusStrings.Add(Maps.StatusMap[substring]);
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

    public static string ReplaceScaleDamageIdents(this string yaml)
    {
        var regex = new Regex("{@scaledamage .*?}");
        MatchCollection matches = regex.Matches(yaml);
        
        List<int> endIndexes = new(matches.Count);
        List<string> scaleDamageStrings = new(matches.Count);
        
        foreach (Match match in matches)
        {
            int index = yaml.IndexOf('}', match.Index);
            string substring = yaml[match.Index..index];
            int pipeIndex = substring.LastIndexOf('|') + 1;
            substring = substring[pipeIndex..];

            endIndexes.Add(index);
            scaleDamageStrings.Add(substring);
        }
        
        for (int i = matches.Count - 1; i >= 0; i--)
        {
            Match match = matches[i];
            int endIndex = endIndexes[i];
            string scaleDamageString = scaleDamageStrings[i];
            yaml = yaml.Remove(match.Index, endIndex - match.Index + 1);
            yaml = yaml.Insert(match.Index, $"{scaleDamageString}");
        }
        
        return yaml;
    }

    public static string ReplaceBookIdents(this string yaml)
    {
        var regex = new Regex("{@book .*?}");
        MatchCollection matches = regex.Matches(yaml);
        
        List<int> endIndexes = new(matches.Count);
        List<string> bookStrings = new(matches.Count);
        
        foreach (Match match in matches)
        {
            int index = yaml.IndexOf('}', match.Index);
            string substring = yaml[match.Index..index];
            substring = substring.Remove(0, 7);
            int firstPipeIndex = substring.IndexOf('|');
            int lastPipeIndex = substring.LastIndexOf('|') + 1;
            
            string text = substring[..firstPipeIndex];
            string link = substring[lastPipeIndex..];

            if (link == "Jumping")
                link = "Movement";

            string linkText = $"[[{link}|{text}]]";

            endIndexes.Add(index);
            bookStrings.Add(linkText);
        }
        
        for (int i = matches.Count - 1; i >= 0; i--)
        {
            Match match = matches[i];
            int endIndex = endIndexes[i];
            string bookString = bookStrings[i];
            yaml = yaml.Remove(match.Index, endIndex - match.Index + 1);
            yaml = yaml.Insert(match.Index, $"{bookString}");
        }
        
        return yaml;
    }
}