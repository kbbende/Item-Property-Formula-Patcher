using ItemPropertyFormulaPatcher.Classification;
using ItemPropertyFormulaPatcher.Rules;
using Mutagen.Bethesda.Skyrim;

namespace ItemPropertyFormulaPatcher.Records;

public sealed class IngredientAdapter : RecordAdapter<IIngredientGetter, Ingredient>
{
    public override ItemState Read(IIngredientGetter record, CategoryClassifier classifier) =>
        Create(record, ItemCategory.Ingredient, record.Keywords)
            // DATA.Value is UInt32, but ENIT.IngredientValue is Int32. Both are
            // written for an explicit price, so use their shared nonnegative range.
            .Add(PropertyTarget.Value, record.Value, StorageKind.Int32)
            .Add(PropertyTarget.Weight, record.Weight, StorageKind.Float32);

    public override void Write(Ingredient record, ItemState item)
    {
        if (item.ChangedTarget(PropertyTarget.Value))
        {
            record.Value = (uint)item.Current(PropertyTarget.Value);
            record.IngredientValue = (int)record.Value;
            record.Flags |= Ingredient.Flag.NoAutoCalculation;
        }
        if (item.ChangedTarget(PropertyTarget.Weight)) record.Weight = (float)item.Current(PropertyTarget.Weight);
    }
}
