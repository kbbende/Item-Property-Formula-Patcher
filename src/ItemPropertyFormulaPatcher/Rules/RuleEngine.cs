using ItemPropertyFormulaPatcher.Diagnostics;
using ItemPropertyFormulaPatcher.Formula;
using ItemPropertyFormulaPatcher.Records;

namespace ItemPropertyFormulaPatcher.Rules;

public sealed class RuleEngine(IReadOnlyList<CompiledRule> rules, UnsupportedTargetPolicy policy, PatchDiagnostics diagnostics)
{
    public bool Apply(ItemState item)
    {
        foreach (var rule in rules)
        {
            if (!rule.Matches(item)) continue;
            if (!item.Supports(rule.Target))
            {
                var message = $"{item.Identity}: rule {rule.Number} targets unsupported {rule.Target}.";
                if (policy == UnsupportedTargetPolicy.Error) throw new InvalidOperationException(message);
                diagnostics.Unsupported(message);
                continue;
            }
            try
            {
                var result = rule.Expression.Evaluate(new EvaluationContext(item, rule.Target));
                item.Set(rule.Target, result);
            }
            catch (FormulaException ex)
            {
                diagnostics.FormulaFailure($"{item.Identity}: rule {rule.Number} ({rule.Target}) skipped: {ex.Message}");
            }
        }
        return item.Changed;
    }
}
