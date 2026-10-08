using ItemPropertyFormulaPatcher.Classification;
using ItemPropertyFormulaPatcher.Diagnostics;
using ItemPropertyFormulaPatcher.Formula;
using ItemPropertyFormulaPatcher.Records;
using ItemPropertyFormulaPatcher.Rules;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Xunit;

namespace ItemPropertyFormulaPatcher.Tests;

public sealed class RuleEngineTests
{
    internal static Rule Rule(PropertyTarget target, string formula, string selector = "Item", MatchKind match = MatchKind.Category) =>
        new() { Target = target, Formula = formula, Selector = selector, Match = match };

    internal static (RuleEngine Engine, PatchDiagnostics Diagnostics) Compile(Settings settings, List<string>? messages = null, params IKeywordGetter[] keywords)
    {
        Action<string> log = s => messages?.Add(s);
        var diagnostics = new PatchDiagnostics(settings, log);
        return (new RuleEngine(RuleCompiler.Compile(settings, new KeywordResolver(keywords), log), settings.UnsupportedTargetHandling, diagnostics), diagnostics);
    }

    [Fact]
    public void GlobalOrderingInterleavesCategoryKeywordAndCrossPropertyRules()
    {
        var mod = new SkyrimMod(ModKey.FromNameAndExtension("Source.esp"), SkyrimRelease.SkyrimSE);
        var keyword = mod.Keywords.AddNew(); keyword.EditorID = "WeapMaterialDaedric";
        var item = new ItemState().Add(PropertyTarget.Damage, 10, StorageKind.UInt16)
            .Add(PropertyTarget.Value, 100, StorageKind.UInt32).Add(PropertyTarget.Weight, 5, StorageKind.Float32);
        item.Categories.Add(ItemCategory.Weapon); item.Keywords.Add(keyword.FormKey);
        var settings = new Settings { Rules = [
            Rule(PropertyTarget.Damage, "target * 1.2", "Weapon"),
            Rule(PropertyTarget.Damage, "dmg + 2", "weapmaterialdaedric", MatchKind.Keyword),
            Rule(PropertyTarget.Value, "damage * 100", "Weapon"),
            Rule(PropertyTarget.Weight, "value / 1000", "Weapon") ] };
        var (engine, _) = Compile(settings, null, keyword);
        Assert.True(engine.Apply(item));
        Assert.Equal(14, item.Current(PropertyTarget.Damage));
        Assert.Equal(1400, item.Current(PropertyTarget.Value));
        Assert.Equal((double)1.4f, item.Current(PropertyTarget.Weight));
        Assert.Equal(10, item.Original(PropertyTarget.Damage));
        Assert.Equal(100, item.Original(PropertyTarget.Value));
        Assert.Equal(5, item.Original(PropertyTarget.Weight));
        settings.Rules.Reverse();
        var reversed = new ItemState().Add(PropertyTarget.Damage, 10, StorageKind.UInt16).Add(PropertyTarget.Value, 100, StorageKind.UInt32)
            .Add(PropertyTarget.Weight, 5, StorageKind.Float32);
        reversed.Categories.Add(ItemCategory.Weapon); reversed.Keywords.Add(keyword.FormKey);
        Compile(settings, null, keyword).Engine.Apply(reversed);
        Assert.Equal(1000, reversed.Current(PropertyTarget.Value));
        Assert.Equal((double)0.1f, reversed.Current(PropertyTarget.Weight));
    }

    [Fact]
    public void TargetAliasesFollowEachRuleAndOriginalAliasesStayFixed()
    {
        var item = new ItemState().Add(PropertyTarget.Damage, 10, StorageKind.UInt16).Add(PropertyTarget.Value, 100, StorageKind.UInt32)
            .Add(PropertyTarget.Weight, 5, StorageKind.Float32).Add(PropertyTarget.Armor, 20, StorageKind.ArmorRating);
        var (engine, _) = Compile(new Settings { Rules = [Rule(PropertyTarget.Damage, "target * 2"),
            Rule(PropertyTarget.Weight, "target + baseDmg + baseDamage + originalDamage"),
            Rule(PropertyTarget.Value, "target + baseTarget + originalValue + base + original + damage"),
            Rule(PropertyTarget.Armor, "armorRating + baseArmorRating + originalArmor + baseArmor")] });
        engine.Apply(item);
        Assert.Equal(20, item.Current(PropertyTarget.Damage));
        Assert.Equal(35, item.Current(PropertyTarget.Weight));
        Assert.Equal(520, item.Current(PropertyTarget.Value));
        Assert.Equal(80, item.Current(PropertyTarget.Armor));
        Assert.Equal(20, item.Original(PropertyTarget.Armor));
    }

    [Fact]
    public void LaterRulesSeeConvertedAndClampedValues()
    {
        var item = new ItemState().Add(PropertyTarget.Damage, 10, StorageKind.UInt16).Add(PropertyTarget.Value, 100, StorageKind.UInt32);
        Compile(new Settings { Rules = [Rule(PropertyTarget.Damage, "10.5"), Rule(PropertyTarget.Value, "damage * 10")] }).Engine.Apply(item);
        Assert.Equal(110, item.Current(PropertyTarget.Value));
        Compile(new Settings { Rules = [Rule(PropertyTarget.Damage, "1e9"), Rule(PropertyTarget.Value, "damage")] }).Engine.Apply(item);
        Assert.Equal(ushort.MaxValue, item.Current(PropertyTarget.Value));
    }

    [Theory]
    [InlineData(UnsupportedTargetPolicy.WarnAndSkip, 1)]
    [InlineData(UnsupportedTargetPolicy.Ignore, 0)]
    public void UnsupportedTargetsAreSkippedBeforeFormulaEvaluation(UnsupportedTargetPolicy policy, int warnings)
    {
        var messages = new List<string>();
        var settings = new Settings { UnsupportedTargetHandling = policy, Rules = [Rule(PropertyTarget.Damage, "1/0"), Rule(PropertyTarget.Value, "value+2")] };
        var item = new ItemState().Add(PropertyTarget.Value, 100, StorageKind.UInt32);
        var (engine, diagnostics) = Compile(settings, messages);
        engine.Apply(item);
        Assert.Equal(102, item.Current(PropertyTarget.Value));
        Assert.Equal(warnings, messages.Count);
        Assert.Equal(1, diagnostics.UnsupportedTargets);
        Assert.Equal(0, diagnostics.FormulaFailures);
    }

    [Fact]
    public void ErrorPolicyFailsExplicitly()
    {
        var settings = new Settings { UnsupportedTargetHandling = UnsupportedTargetPolicy.Error, Rules = [Rule(PropertyTarget.Armor, "1")] };
        Assert.Throws<InvalidOperationException>(() => Compile(settings).Engine.Apply(new ItemState()));
    }

    [Fact]
    public void RuntimeFailuresPreservePreviousResultsAndContinue()
    {
        var messages = new List<string>();
        var item = new ItemState().Add(PropertyTarget.Value, 100, StorageKind.UInt32);
        var (engine, diagnostics) = Compile(new Settings { MaxFormulaWarnings = 1, Rules = [Rule(PropertyTarget.Value, "value*2"),
            Rule(PropertyTarget.Value, "1/0"), Rule(PropertyTarget.Value, "sqrt(-1)"), Rule(PropertyTarget.Value, "value+1")] }, messages);
        engine.Apply(item);
        Assert.Equal(201, item.Current(PropertyTarget.Value));
        Assert.Equal(100, item.Original(PropertyTarget.Value));
        Assert.Equal(2, diagnostics.FormulaFailures); Assert.Single(messages);
    }

    [Fact]
    public void WarningCapAppliesAcrossItems()
    {
        var messages = new List<string>();
        var (engine, diagnostics) = Compile(new Settings { MaxUnsupportedTargetWarnings = 2, Rules = [Rule(PropertyTarget.Damage, "1")] }, messages);
        for (var i = 0; i < 5; i++) engine.Apply(new ItemState());
        Assert.Equal(5, diagnostics.UnsupportedTargets); Assert.Equal(2, messages.Count);
    }

    [Fact]
    public void DisabledAndUnmatchedRulesAreInertAndNetNeutralChangesProduceNoOverride()
    {
        var disabled = Rule(PropertyTarget.Value, "not valid!!"); disabled.Enabled = false;
        var item = new ItemState().Add(PropertyTarget.Value, 100, StorageKind.UInt32);
        var settings = new Settings { Rules = [disabled, Rule(PropertyTarget.Value, "1/0", "Armor"),
            Rule(PropertyTarget.Value, "value*2"), Rule(PropertyTarget.Value, "baseValue")] };
        var (engine, diagnostics) = Compile(settings);
        Assert.False(engine.Apply(item)); Assert.Equal(0, diagnostics.FormulaFailures);
    }

    [Fact]
    public void InvalidSettingsFailBeforeProcessing()
    {
        Assert.Throws<ArgumentException>(() => Compile(new Settings { Rules = [Rule(PropertyTarget.Value, "weightt")] }));
        Assert.Throws<ArgumentException>(() => Compile(new Settings { Rules = [Rule(PropertyTarget.Value, "1", "Wepon")] }));
        Assert.Throws<ArgumentException>(() => Compile(new Settings { Rules = [Rule(PropertyTarget.Value, "1", "0")] }));
        Assert.Throws<ArgumentException>(() => Compile(new Settings { MaxLoggedRecords = -1 }));
        Assert.Throws<ArgumentException>(() => Compile(new Settings { Rules = null! }));
    }

    [Fact]
    public void MissingKeywordWarnsAndExplicitFormKeysDisambiguateDuplicates()
    {
        var source = new SkyrimMod(ModKey.FromNameAndExtension("Source.esp"), SkyrimRelease.SkyrimSE);
        var first = source.Keywords.AddNew(); first.EditorID = "Shared";
        var second = source.Keywords.AddNew(); second.EditorID = "Shared";
        var resolver = new KeywordResolver([first, second]);
        Assert.Equal(2, resolver.Resolve("shared").Count);
        Assert.Equal(first.FormKey, Assert.Single(resolver.Resolve(first.FormKey.ToString())));
        var messages = new List<string>();
        var settings = new Settings { Rules = [Rule(PropertyTarget.Value, "1", "Absent", MatchKind.Keyword)] };
        Assert.Empty(RuleCompiler.Compile(settings, resolver, messages.Add)); Assert.Single(messages);
    }
}
