using ItemPropertyFormulaPatcher.Records;
using Mutagen.Bethesda.Skyrim;
using Skyrim = Mutagen.Bethesda.Skyrim;

namespace ItemPropertyFormulaPatcher.Classification;

public sealed class CategoryClassifier(KeywordResolver keywords)
{
    public void Armor(ItemState item, IArmorGetter armor)
    {
        item.Categories.Add(ItemCategory.Armor);
        var jewelry = keywords.Has(item.Keywords, "VendorItemJewelry");
        var shield = keywords.Has(item.Keywords, "ArmorShield") || armor.MajorFlags.HasFlag(Skyrim.Armor.MajorFlag.Shield);
        var heavy = keywords.Has(item.Keywords, "ArmorHeavy");
        var light = keywords.Has(item.Keywords, "ArmorLight");
        if (jewelry) item.Categories.Add(ItemCategory.Jewelry);
        if (shield) item.Categories.Add(ItemCategory.Shield);
        if (heavy) item.Categories.Add(ItemCategory.HeavyArmor);
        if (light) item.Categories.Add(ItemCategory.LightArmor);
        if (keywords.Has(item.Keywords, "VendorItemClothing") || (!jewelry && !shield && !heavy && !light))
            item.Categories.Add(ItemCategory.Clothing);
    }

    public void Ingestible(ItemState item, IIngestibleGetter ingestible)
    {
        item.Categories.Add(ItemCategory.Ingestible);
        var food = ingestible.Flags.HasFlag(Skyrim.Ingestible.Flag.FoodItem) || keywords.Has(item.Keywords, "VendorItemFood");
        var poison = ingestible.Flags.HasFlag(Skyrim.Ingestible.Flag.Poison) || keywords.Has(item.Keywords, "VendorItemPoison");
        var potion = keywords.Has(item.Keywords, "VendorItemPotion") || (!food && !poison);
        if (food) item.Categories.Add(ItemCategory.Food);
        if (poison) item.Categories.Add(ItemCategory.Poison);
        if (potion) item.Categories.Add(ItemCategory.Potion);
    }

    public void Book(ItemState item, IBookGetter book)
    {
        item.Categories.Add(ItemCategory.Book);
        if (keywords.Has(item.Keywords, "VendorItemSpellTome") || book.Teaches is IBookSpellGetter)
            item.Categories.Add(ItemCategory.SpellTome);
    }
}
