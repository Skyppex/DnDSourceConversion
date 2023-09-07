namespace DnDSourceConversion;

public static class Maps
{
    public static readonly Dictionary<string, string> AttackMap = new()
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

    public static readonly Dictionary<string, string> StatusMap = new()
    {
        {"concentration", "[[Duration|Concentration]]"},
        {"concentration||concentrating", "[[Duration|Concentrating]]"}
    };

    public static readonly Dictionary<string, string> SizeMap = new()
    {
        { "T", "Tiny" },
        { "S", "Small" },
        { "M", "Medium" },
        { "L", "Large" },   
        { "H", "Huge" },
        { "G", "Gargantuan" },
    };

    public static readonly Dictionary<string, string> AlignmentMap = new()
    {
        {"U", "Unaligned"},
        {"A", "Any Alignment"},
        {"L", "Lawful"},
        {"C", "Chaotic"},
        {"G", "Good"},
        {"E", "Evil"},
        {"N", "Neutral"},
    };
    
    public static readonly Dictionary<string, string> SkillMap = new()
    {
        {"str", "Strength"},
        {"Str", "Strength"},
        {"dex", "Dexterity"},
        {"Dex", "Dexterity"},
        {"con", "Constitution"},
        {"Con", "Constitution"},
        {"int", "Intelligence"},
        {"Int", "Intelligence"},
        {"wis", "Wisdom"},
        {"Wis", "Wisdom"},
        {"cha", "Charisma"},
        {"Cha", "Charisma"},
    };

    public static readonly Dictionary<string, string> DamageTypeMap = new()
    {
        {"A", "Acid"},
        {"B", "Bludgeoning"},
        {"C", "Cold"},
        {"F", "Fire"},
        {"FO", "Force"},
        {"L", "Lightning"},
        {"N", "Necrotic"},
        {"P", "Piercing"},
        {"PO", "Poison"},
        {"PS", "Psychic"},
        {"R", "Radiant"},
        {"S", "Slashing"},
        {"T", "Thunder"},
    };

    public static readonly Dictionary<string, string> WeaponPropertyMap = new()
    {
        {"A", "Ammunition"},
        {"AF", "Ammunition"},
        {"BF", "Burst Fire"},
        {"F", "Finesse"},
        {"H", "Heavy"},
        {"L", "Light"},
        {"LD", "Loading"},
        {"R", "Reach"},
        {"RN", "Range"},
        {"RLD", "Reload"},
        {"S", "Special"},
        {"T", "Thrown"},
        {"V", "Versatile"},
    };

    public static readonly Dictionary<string, string> SchoolMap = new()
    {
        { "A", "Abjuration" },
        { "C", "Conjuration" },
        { "D", "Divination" },
        { "E", "Enchantment" },
        { "V", "Evocation" },
        { "I", "Illusion" },
        { "N", "Necromancy" },
        { "T", "Transmutation" },
    };
}