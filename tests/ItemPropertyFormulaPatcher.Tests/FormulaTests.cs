using System.Globalization;
using ItemPropertyFormulaPatcher.Formula;
using ItemPropertyFormulaPatcher.Records;
using ItemPropertyFormulaPatcher.Rules;
using Xunit;

namespace ItemPropertyFormulaPatcher.Tests;

public sealed class FormulaTests
{
    private static double Eval(string formula) => FormulaParser.Parse(formula).Evaluate(new EvaluationContext(
        new ItemState().Add(PropertyTarget.Value, 100, StorageKind.UInt32).Add(PropertyTarget.Weight, 2.5, StorageKind.Float32), PropertyTarget.Weight));

    [Theory]
    [InlineData("2 + 3 * 4", 14)]
    [InlineData("(2 + 3) * 4", 20)]
    [InlineData("2 ^ 3 ^ 2", 512)]
    [InlineData("-2^2", -4)]
    [InlineData("2^-2", 0.25)]
    [InlineData("+2 + -3", -1)]
    [InlineData("20 / 4 / 2", 2.5)]
    [InlineData("10-3-2", 5)]
    [InlineData("10 % 3", 1)]
    [InlineData(".5 + 1.5e2", 150.5)]
    [InlineData("1e-2", 0.01)]
    [InlineData("3 > 2", 1)]
    [InlineData("3 >= 3", 1)]
    [InlineData("2 < 3", 1)]
    [InlineData("2 <= 2", 1)]
    [InlineData("2 == 3", 0)]
    [InlineData("2 != 3", 1)]
    [InlineData("1 || 0 && 0", 1)]
    [InlineData("2 && -2", 1)]
    [InlineData("!0", 1)]
    [InlineData("!5", 0)]
    [InlineData("min(4, 2, 9)", 2)]
    [InlineData("max(4, 2, 9)", 9)]
    [InlineData("clamp(50, 1, 25)", 25)]
    [InlineData("round(2.5)", 3)]
    [InlineData("round(-2.5)", -3)]
    [InlineData("round(12.5, 5)", 15)]
    [InlineData("round(1.24, .1)", 1.2)]
    [InlineData("floor(-1.1)", -2)]
    [InlineData("floor(12, 5)", 10)]
    [InlineData("ceil(-1.1)", -1)]
    [InlineData("ceil(12, 5)", 15)]
    [InlineData("abs(-2)", 2)]
    [InlineData("sqrt(9)", 3)]
    [InlineData("pow(2, 3)", 8)]
    [InlineData("log(e)", 1)]
    [InlineData("log(8, 2)", 3)]
    [InlineData("log10(100)", 2)]
    [InlineData("exp(0)", 1)]
    [InlineData("sign(-4)", -1)]
    [InlineData("trunc(-2.9)", -2)]
    [InlineData("if(value < 200, value * 2, value)", 200)]
    [InlineData("ROUND(MAX(value * 10 - 100, 25))", 900)]
    [InlineData("target + baseTarget + base + original", 205)]
    [InlineData("currentWeight + originalWeight + baseValue", 105)]
    [InlineData("damage + dmg + armor + armorRating", 0)]
    [InlineData("hasDamage || hasArmor", 0)]
    [InlineData("hasWeight && hasValue", 1)]
    [InlineData("pi > 3 && true && !false", 1)]
    public void DocumentedSemantics(string formula, double expected) => Assert.Equal(expected, Eval(formula), 8);

    [Theory]
    [InlineData("if(0, 1/0, 7)", 7)]
    [InlineData("if(1, 7, sqrt(-1))", 7)]
    [InlineData("0 && 1/0", 0)]
    [InlineData("1 || 1/0", 1)]
    public void UnusedBranchesAreLazy(string formula, double expected) => Assert.Equal(expected, Eval(formula));

    [Theory]
    [InlineData("1/0")]
    [InlineData("1%0")]
    [InlineData("sqrt(-1)")]
    [InlineData("log(0)")]
    [InlineData("log(8, 1)")]
    [InlineData("exp(10000)")]
    [InlineData("1e308*1e308")]
    [InlineData("round(2, 0)")]
    [InlineData("floor(2, -1)")]
    [InlineData("ceil(1e308, 1e-308)")]
    [InlineData("clamp(1, 3, 2)")]
    public void InvalidRuntimeResultsAreRejected(string formula) => Assert.Throws<FormulaException>(() => Eval(formula));

    [Theory]
    [InlineData("")]
    [InlineData("1e999")]
    [InlineData("1e+")]
    [InlineData(".")]
    [InlineData("2 3")]
    [InlineData("value; File.Delete()")]
    [InlineData("unknown + 1")]
    [InlineData("min()")]
    [InlineData("round(1, 2, 3)")]
    [InlineData("if(1, 2)")]
    [InlineData("sin(1)")]
    [InlineData("(2 + 1")]
    [InlineData("pow(2,)")]
    [InlineData("if(0, unknown, 1)")]
    public void InvalidSyntaxAndSymbolsFailDuringCompilation(string formula) => Assert.Throws<FormulaException>(() => FormulaParser.Parse(formula));

    [Fact]
    public void ParsingUsesInvariantCulture()
    {
        var previous = CultureInfo.CurrentCulture;
        try { CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("hu-HU"); Assert.Equal(3.75, Eval("1.25 * 3")); }
        finally { CultureInfo.CurrentCulture = previous; }
    }

    [Fact]
    public void ExcessiveInputFailsPredictably()
    {
        Assert.Throws<FormulaException>(() => FormulaParser.Parse(new string('(', 100) + "1" + new string(')', 100)));
        Assert.Throws<FormulaException>(() => FormulaParser.Parse(string.Join("+", Enumerable.Repeat("1", 600))));
        Assert.Throws<FormulaException>(() => FormulaParser.Parse(new string(' ', 8193)));
    }
}
