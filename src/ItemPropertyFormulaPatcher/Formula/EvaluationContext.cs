using ItemPropertyFormulaPatcher.Classification;
using ItemPropertyFormulaPatcher.Records;
using ItemPropertyFormulaPatcher.Rules;

namespace ItemPropertyFormulaPatcher.Formula;

public sealed class EvaluationContext(ItemState item, PropertyTarget target)
{
    private static readonly Dictionary<string, (PropertyTarget Target, bool Original)> Properties = BuildProperties();
    private static readonly Dictionary<string, ItemCategory> Categories = Enum.GetValues<ItemCategory>()
        .ToDictionary(c => "is" + c, c => c, StringComparer.OrdinalIgnoreCase);

    private static Dictionary<string, (PropertyTarget, bool)> BuildProperties()
    {
        var names = new Dictionary<string, (PropertyTarget, bool)>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, property) in new[] { ("value", PropertyTarget.Value), ("weight", PropertyTarget.Weight),
                     ("damage", PropertyTarget.Damage), ("dmg", PropertyTarget.Damage),
                     ("armor", PropertyTarget.Armor), ("armorRating", PropertyTarget.Armor) })
        {
            names[name] = (property, false);
            names["current" + name] = (property, false);
            names["base" + name] = (property, true);
            names["original" + name] = (property, true);
        }
        names["base"] = names["original"] = (PropertyTarget.Value, true);
        return names;
    }

    public static bool IsKnownVariable(string name) => Properties.ContainsKey(name) || Categories.ContainsKey(name) ||
        name.ToLowerInvariant() is "target" or "currenttarget" or "basetarget" or "originaltarget" or
            "keywordcount" or "isenchanted" or "isplayable" or "hasvalue" or "hasweight" or "hasdamage" or "hasarmor" or
            "pi" or "e" or "true" or "false";

    public double Get(string name)
    {
        if (Properties.TryGetValue(name, out var p)) return p.Original ? item.Original(p.Target) : item.Current(p.Target);
        if (Categories.TryGetValue(name, out var category)) return item.Categories.Contains(category) ? 1 : 0;
        return name.ToLowerInvariant() switch
        {
            "target" or "currenttarget" => item.Current(target),
            "basetarget" or "originaltarget" => item.Original(target),
            "keywordcount" => item.Keywords.Count,
            "isenchanted" => item.IsEnchanted ? 1 : 0,
            "isplayable" => item.IsPlayable ? 1 : 0,
            "hasvalue" => item.Supports(PropertyTarget.Value) ? 1 : 0,
            "hasweight" => item.Supports(PropertyTarget.Weight) ? 1 : 0,
            "hasdamage" => item.Supports(PropertyTarget.Damage) ? 1 : 0,
            "hasarmor" => item.Supports(PropertyTarget.Armor) ? 1 : 0,
            "pi" => Math.PI, "e" => Math.E, "true" => 1, "false" => 0,
            _ => throw new FormulaException($"Unknown variable '{name}'.")
        };
    }
}
