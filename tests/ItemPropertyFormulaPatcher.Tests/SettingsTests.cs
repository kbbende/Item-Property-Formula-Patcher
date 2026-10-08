using ItemPropertyFormulaPatcher.Classification;
using ItemPropertyFormulaPatcher.Rules;
using Newtonsoft.Json;
using Xunit;

namespace ItemPropertyFormulaPatcher.Tests;

public sealed class SettingsTests
{
    private static readonly KeywordResolver Keywords = new([]);

    [Theory]
    [InlineData("{\"CategoryRules\":[]}")]
    [InlineData("{\"KeywordRules\":[],\"FinalFormula\":\"value\"}")]
    [InlineData("{\"Rulez\":[]}")]
    [InlineData("{\"Rules\":[{\"Targte\":\"Weight\"}]}")]
    public void LegacyAndMisspelledFieldsAreRejected(string json)
    {
        var settings = JsonConvert.DeserializeObject<Settings>(json)!;
        Assert.Throws<ArgumentException>(() => RuleCompiler.Compile(settings, Keywords));
    }

    [Fact]
    public void HumanReadableEnumsRoundTrip()
    {
        var settings = new Settings { Rules = [RuleEngineTests.Rule(PropertyTarget.Weight, "target*0.5")] };
        var json = JsonConvert.SerializeObject(settings);
        Assert.Contains("\"Target\":\"Weight\"", json);
        Assert.Contains("\"Match\":\"Category\"", json);
        Assert.Contains("\"UnsupportedTargetHandling\":\"WarnAndSkip\"", json);
        var loaded = JsonConvert.DeserializeObject<Settings>(json)!;
        Assert.Equal(PropertyTarget.Weight, Assert.Single(RuleCompiler.Compile(loaded, Keywords)).Target);
    }

    [Fact]
    public void ExampleSettingsCompileAndDefaultsAreNeutral()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "examples", "settings.demo.json");
        var settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(path))!;
        var rules = RuleCompiler.Compile(settings, Keywords, _ => { });
        Assert.Equal(2, rules.Count); // absent Daedric keyword skips that one demo rule
        Assert.Empty(RuleCompiler.Compile(new Settings(), Keywords));
    }
}
