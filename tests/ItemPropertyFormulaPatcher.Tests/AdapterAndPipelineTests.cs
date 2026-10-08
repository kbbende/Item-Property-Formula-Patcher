using ItemPropertyFormulaPatcher.Classification;
using ItemPropertyFormulaPatcher.Records;
using ItemPropertyFormulaPatcher.Rules;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Xunit;

namespace ItemPropertyFormulaPatcher.Tests;

public sealed class AdapterAndPipelineTests
{
    private static SkyrimMod Mod(string name) => new(ModKey.FromNameAndExtension(name), SkyrimRelease.SkyrimSE);
    private static readonly CategoryClassifier Classifier = new(new KeywordResolver([]));

    [Fact]
    public void EveryAdapterWritesOnlyItsSupportedProperties()
    {
        var source = Mod("Source.esp");
        var weapon = source.Weapons.AddNew(); weapon.BasicStats = new WeaponBasicStats { Value = 10, Weight = 2, Damage = 3 };
        var armor = source.Armors.AddNew(); armor.Value = 10; armor.Weight = 2; armor.ArmorRating = 3;
        var ammo = source.Ammunitions.AddNew(); ammo.Value = 10; ammo.Weight = 2; ammo.Damage = 3;
        var ingestible = source.Ingestibles.AddNew(); ingestible.Value = 10; ingestible.Weight = 2;
        var ingredient = source.Ingredients.AddNew(); ingredient.Value = 10; ingredient.Weight = 2;
        var book = source.Books.AddNew(); book.Value = 10; book.Weight = 2;
        var misc = source.MiscItems.AddNew(); misc.Value = 10; misc.Weight = 2;
        Check(new WeaponAdapter(), weapon, weapon, PropertyTarget.Damage);
        Check(new ArmorAdapter(), armor, armor, PropertyTarget.Armor);
        Check(new AmmunitionAdapter(), ammo, ammo, PropertyTarget.Damage);
        Check(new IngestibleAdapter(), ingestible, ingestible);
        Check(new IngredientAdapter(), ingredient, ingredient);
        Check(new BookAdapter(), book, book);
        Check(new MiscItemAdapter(), misc, misc);
        Assert.Equal(11u, weapon.BasicStats.Value); Assert.Equal(3.25f, weapon.BasicStats.Weight); Assert.Equal((ushort)6, weapon.BasicStats.Damage);
        Assert.Equal(11u, armor.Value); Assert.Equal(3.25f, armor.Weight); Assert.Equal(5.5f, armor.ArmorRating);
        Assert.Equal(5.5f, ammo.Damage);
        Assert.Equal(3.25f, ingestible.Weight); Assert.Equal(3.25f, ingredient.Weight);
        Assert.Equal(3.25f, book.Weight); Assert.Equal(3.25f, misc.Weight);

        static void Check<TGetter, TMutable>(RecordAdapter<TGetter, TMutable> adapter, TGetter getter, TMutable mutable, PropertyTarget? extra = null)
            where TGetter : class, Mutagen.Bethesda.Plugins.Records.IMajorRecordGetter where TMutable : class
        {
            var item = adapter.Read(getter, Classifier);
            Assert.True(item.Supports(PropertyTarget.Value)); Assert.True(item.Supports(PropertyTarget.Weight));
            Assert.Equal(extra == PropertyTarget.Damage, item.Supports(PropertyTarget.Damage));
            Assert.Equal(extra == PropertyTarget.Armor, item.Supports(PropertyTarget.Armor));
            item.Set(PropertyTarget.Value, 10.5); item.Set(PropertyTarget.Weight, 3.25);
            if (extra is { } target) item.Set(target, 5.5);
            adapter.Write(mutable, item);
        }
    }

    [Fact]
    public void MissingWeaponStatsAreUnsupportedAndNotCreated()
    {
        var source = Mod("Source.esp");
        var weapon = source.Weapons.AddNew(); weapon.BasicStats = null;
        var patch = Mod("Patch.esp");
        var settings = new Settings { Rules = [RuleEngineTests.Rule(PropertyTarget.Damage, "100"), RuleEngineTests.Rule(PropertyTarget.Value, "1")] };
        var result = PatchRunner.Run([source], patch, settings, _ => { });
        Assert.Equal(2, result.UnsupportedTargets); Assert.Empty(patch.Weapons); Assert.Null(weapon.BasicStats);
    }

    [Theory]
    [InlineData(StorageKind.UInt32, -1, 0)]
    [InlineData(StorageKind.UInt32, 1e20, 4294967295)]
    [InlineData(StorageKind.UInt32, 10.5, 11)]
    [InlineData(StorageKind.UInt16, -1, 0)]
    [InlineData(StorageKind.UInt16, 1e20, 65535)]
    [InlineData(StorageKind.UInt16, 10.49, 10)]
    [InlineData(StorageKind.Int32, 1e20, 2147483647)]
    [InlineData(StorageKind.Int32, -1, 0)]
    [InlineData(StorageKind.Int32, 10.5, 11)]
    [InlineData(StorageKind.Float32, -1, 0)]
    [InlineData(StorageKind.Float32, 1.25, 1.25)]
    public void StorageConversionClampsAndRounds(StorageKind storage, double input, double expected) =>
        Assert.Equal(expected, PropertyConversion.Normalize(input, storage));

    [Fact]
    public void FloatOverflowClampsAndNonfiniteValuesAreRejected()
    {
        Assert.Equal((double)float.MaxValue, PropertyConversion.Normalize(double.MaxValue, StorageKind.Float32));
        foreach (var storage in Enum.GetValues<StorageKind>())
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => PropertyConversion.Normalize(double.NaN, storage));
            Assert.Throws<ArgumentOutOfRangeException>(() => PropertyConversion.Normalize(double.PositiveInfinity, storage));
        }
    }

    [Fact]
    public void ValueWritesDisableAlchemyAutoCalculationAndWeightWritesPreserveIt()
    {
        var source = Mod("Source.esp");
        var potion = source.Ingestibles.AddNew(); potion.Value = 100; potion.Flags = Ingestible.Flag.FoodItem;
        var ingredient = source.Ingredients.AddNew(); ingredient.Value = 100; ingredient.IngredientValue = 100;
        var potionAdapter = new IngestibleAdapter(); var ingredientAdapter = new IngredientAdapter();
        var potionState = potionAdapter.Read(potion, Classifier); potionState.Set(PropertyTarget.Weight, 2);
        potionAdapter.Write(potion, potionState);
        Assert.False(potion.Flags.HasFlag(Ingestible.Flag.NoAutoCalc));
        var ingredientState = ingredientAdapter.Read(ingredient, Classifier); ingredientState.Set(PropertyTarget.Weight, 2);
        ingredientAdapter.Write(ingredient, ingredientState);
        Assert.False(ingredient.Flags.HasFlag(Ingredient.Flag.NoAutoCalculation));
        potionState.Set(PropertyTarget.Value, 50); ingredientState.Set(PropertyTarget.Value, 60);
        potionAdapter.Write(potion, potionState); ingredientAdapter.Write(ingredient, ingredientState);
        Assert.True(potion.Flags.HasFlag(Ingestible.Flag.NoAutoCalc)); Assert.True(potion.Flags.HasFlag(Ingestible.Flag.FoodItem));
        Assert.True(ingredient.Flags.HasFlag(Ingredient.Flag.NoAutoCalculation));
        Assert.Equal(60u, ingredient.Value); Assert.Equal(60, ingredient.IngredientValue);
    }

    [Fact]
    public void FractionalArmorRatingSerializesAtHundredthPrecision()
    {
        var name = $"ArmorPrecisionTest-{Guid.NewGuid():N}.esp";
        var mod = Mod(name); var armor = mod.Armors.AddNew();
        var adapter = new ArmorAdapter(); var state = adapter.Read(armor, Classifier);
        state.Set(PropertyTarget.Armor, 20.123); adapter.Write(armor, state);
        var path = Path.Combine(Path.GetTempPath(), name);
        try
        {
            mod.WriteToBinary(path);
            using var loaded = SkyrimMod.CreateFromBinaryOverlay(path, SkyrimRelease.SkyrimSE);
            Assert.Equal(20.12f, Assert.Single(loaded.Armors).ArmorRating);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public void PipelineUsesWinningRecordsAndCreatesOnlyChangedOverrides()
    {
        var low = Mod("Low.esp"); var high = Mod("High.esp"); var patch = Mod("Patch.esp");
        var lowWeapon = low.Weapons.AddNew(); lowWeapon.EditorID = "Sword";
        lowWeapon.BasicStats = new WeaponBasicStats { Value = 100, Weight = 4, Damage = 10 };
        var highWeapon = high.Weapons.GetOrAddAsOverride(lowWeapon);
        highWeapon.BasicStats!.Damage = 20;
        var book = low.Books.AddNew(); book.Value = 100;
        var settings = new Settings { Rules = [RuleEngineTests.Rule(PropertyTarget.Damage, "baseDamage*2", "Weapon"),
            RuleEngineTests.Rule(PropertyTarget.Value, "damage*100", "Weapon")] };
        var result = PatchRunner.Run([high, low], patch, settings, _ => { });
        var changed = Assert.Single(patch.Weapons);
        Assert.Equal((ushort)40, changed.BasicStats!.Damage); Assert.Equal(4000u, changed.BasicStats.Value);
        Assert.Equal(4f, changed.BasicStats.Weight); Assert.Equal("Sword", changed.EditorID);
        Assert.Equal((ushort)20, highWeapon.BasicStats.Damage); Assert.Equal((ushort)10, lowWeapon.BasicStats.Damage);
        Assert.Empty(patch.Books); Assert.Equal(1, result.ChangedRecords);
        var neutral = Mod("Neutral.esp");
        PatchRunner.Run([high, low], neutral, new Settings(), _ => { }); Assert.Empty(neutral.Weapons);
    }

    [Fact]
    public void DeletedWinningRecordsAreNotResurrected()
    {
        var low = Mod("Low.esp"); var high = Mod("High.esp"); var patch = Mod("Patch.esp");
        var item = low.MiscItems.AddNew(); item.Value = 5;
        high.MiscItems.GetOrAddAsOverride(item).IsDeleted = true;
        PatchRunner.Run([high, low], patch, new Settings { Rules = [RuleEngineTests.Rule(PropertyTarget.Value, "10")] }, _ => { });
        Assert.Empty(patch.MiscItems);
    }

    [Fact]
    public void ClassificationAndMetadataAreAvailableToFormulas()
    {
        var source = Mod("Source.esp");
        var heavy = source.Keywords.AddNew(); heavy.EditorID = "ArmorHeavy";
        var jewelry = source.Keywords.AddNew(); jewelry.EditorID = "VendorItemJewelry";
        var classifier = new CategoryClassifier(new KeywordResolver([heavy, jewelry]));
        var armor = source.Armors.AddNew(); armor.Keywords = []; armor.Keywords.Add(heavy); armor.Keywords.Add(jewelry);
        armor.MajorFlags |= Armor.MajorFlag.NonPlayable | Armor.MajorFlag.Shield;
        var effect = source.ObjectEffects.AddNew(); armor.ObjectEffect.SetTo(effect);
        var item = new ArmorAdapter().Read(armor, classifier);
        Assert.Contains(ItemCategory.HeavyArmor, item.Categories); Assert.Contains(ItemCategory.Jewelry, item.Categories);
        Assert.Contains(ItemCategory.Shield, item.Categories); Assert.DoesNotContain(ItemCategory.Clothing, item.Categories);
        Assert.True(item.IsEnchanted); Assert.False(item.IsPlayable); Assert.Equal(2, item.Keywords.Count);
        var food = source.Ingestibles.AddNew(); food.Flags |= Ingestible.Flag.FoodItem;
        Assert.Contains(ItemCategory.Food, new IngestibleAdapter().Read(food, classifier).Categories);
        var poison = source.Ingestibles.AddNew(); poison.Flags |= Ingestible.Flag.Poison;
        Assert.Contains(ItemCategory.Poison, new IngestibleAdapter().Read(poison, classifier).Categories);
        Assert.DoesNotContain(ItemCategory.Potion, new IngestibleAdapter().Read(poison, classifier).Categories);
        var book = source.Books.AddNew(); book.Teaches = new BookSpell(); book.Flags |= Book.Flag.CantBeTaken;
        var bookState = new BookAdapter().Read(book, classifier);
        Assert.Contains(ItemCategory.SpellTome, bookState.Categories); Assert.False(bookState.IsPlayable);
    }

    [Fact]
    public void PatchedPropertiesSurviveEspBinaryRoundTrip()
    {
        var source = Mod("Source.esp");
        var fileName = $"ItemPropertyFormulaTest-{Guid.NewGuid():N}.esp";
        var patch = Mod(fileName);
        var weapon = source.Weapons.AddNew(); weapon.BasicStats = new WeaponBasicStats { Value = 100, Weight = 3, Damage = 10 };
        var armor = source.Armors.AddNew(); armor.ArmorRating = 20;
        var ammo = source.Ammunitions.AddNew(); ammo.Damage = 10;
        var settings = new Settings { Rules = [RuleEngineTests.Rule(PropertyTarget.Weight, "1.25"),
            RuleEngineTests.Rule(PropertyTarget.Value, "1e20"), RuleEngineTests.Rule(PropertyTarget.Damage, "12.5"),
            RuleEngineTests.Rule(PropertyTarget.Armor, "1e20")] };
        PatchRunner.Run([source], patch, settings, _ => { });
        var path = Path.Combine(Path.GetTempPath(), fileName);
        try
        {
            patch.WriteToBinary(path);
            using var loaded = SkyrimMod.CreateFromBinaryOverlay(path, SkyrimRelease.SkyrimSE);
            Assert.Equal((ushort)13, loaded.Weapons.First().BasicStats!.Damage);
            Assert.Equal(uint.MaxValue, loaded.Weapons.First().BasicStats!.Value);
            Assert.Equal(1.25f, loaded.Weapons.First().BasicStats!.Weight);
            Assert.Equal(12.5f, loaded.Ammunitions.First().Damage);
            Assert.InRange(loaded.Armors.First().ArmorRating, PropertyConversion.MaxArmorRating - 10, PropertyConversion.MaxArmorRating);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }
}
