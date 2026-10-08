using ItemPropertyFormulaPatcher.Classification;
using ItemPropertyFormulaPatcher.Formula;
using Mutagen.Bethesda.Plugins;

namespace ItemPropertyFormulaPatcher.Rules;

public sealed record CompiledRule(int Number, PropertyTarget Target, Expression Expression,
    ItemCategory? Category, IReadOnlySet<FormKey>? Keywords)
{
    public bool Matches(Records.ItemState item) => Category is { } category
        ? item.Categories.Contains(category) : Keywords!.Overlaps(item.Keywords);
}

public static class RuleCompiler
{
    public static IReadOnlyList<CompiledRule> Compile(Settings settings, KeywordResolver keywords, Action<string>? log = null)
    {
        settings.ValidateSchema();
        if (settings.Rules is null) throw new ArgumentException("Rules must be a list, not null.");
        if (!Enum.IsDefined(settings.UnsupportedTargetHandling)) throw new ArgumentException("Invalid unsupported-target policy.");
        if (settings.MaxUnsupportedTargetWarnings < 0 || settings.MaxFormulaWarnings < 0 || settings.MaxLoggedRecords < 0)
            throw new ArgumentException("Log caps must be nonnegative.");
        var compiled = new List<CompiledRule>();
        for (var index = 0; index < settings.Rules.Count; index++)
        {
            var rule = settings.Rules[index] ?? throw new ArgumentException($"Rule {index + 1} is null.");
            if (!rule.Enabled) continue;
            rule.ValidateSchema();
            if (!Enum.IsDefined(rule.Match) || !Enum.IsDefined(rule.Target)) throw new ArgumentException($"Rule {index + 1}: invalid Match or Target.");
            if (string.IsNullOrWhiteSpace(rule.Selector)) throw new ArgumentException($"Rule {index + 1}: Selector is empty.");
            Expression expression;
            try { expression = FormulaParser.Parse(rule.Formula); }
            catch (FormulaException ex) { throw new ArgumentException($"Rule {index + 1}: {ex.Message}", ex); }
            ItemCategory? category = null;
            IReadOnlySet<FormKey>? keywordKeys = null;
            if (rule.Match == MatchKind.Category)
            {
                if (!Enum.TryParse<ItemCategory>(rule.Selector.Trim(), true, out var parsed) || !Enum.IsDefined(parsed) ||
                    !Enum.GetNames<ItemCategory>().Contains(rule.Selector.Trim(), StringComparer.OrdinalIgnoreCase))
                    throw new ArgumentException($"Rule {index + 1}: unknown category '{rule.Selector}'.");
                category = parsed;
            }
            else
            {
                keywordKeys = keywords.Resolve(rule.Selector);
                if (keywordKeys.Count == 0)
                {
                    (log ?? Console.WriteLine)($"[WARN] Rule {index + 1}: keyword '{rule.Selector}' is absent from the load order. Rule skipped.");
                    continue;
                }
            }
            compiled.Add(new CompiledRule(index + 1, rule.Target, expression, category, keywordKeys));
        }
        return compiled;
    }
}
