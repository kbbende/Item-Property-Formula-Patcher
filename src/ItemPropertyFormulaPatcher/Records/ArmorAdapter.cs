using ItemPropertyFormulaPatcher.Classification;
using ItemPropertyFormulaPatcher.Rules;
using Mutagen.Bethesda.Skyrim;

namespace ItemPropertyFormulaPatcher.Records;

public sealed class ArmorAdapter : RecordAdapter<IArmorGetter, Armor>
{
    public override ItemState Read(IArmorGetter record, CategoryClassifier classifier)
    {
        var item = Create(record, ItemCategory.Armor, record.Keywords, !record.ObjectEffect.IsNull,
            !record.MajorFlags.HasFlag(Armor.MajorFlag.NonPlayable));
        classifier.Armor(item, record);
        return item.Add(PropertyTarget.Value, record.Value, StorageKind.UInt32)
            .Add(PropertyTarget.Weight, record.Weight, StorageKind.Float32)
            .Add(PropertyTarget.Armor, record.ArmorRating, StorageKind.ArmorRating);
    }

    public override void Write(Armor record, ItemState item)
    {
        if (item.ChangedTarget(PropertyTarget.Value)) record.Value = (uint)item.Current(PropertyTarget.Value);
        if (item.ChangedTarget(PropertyTarget.Weight)) record.Weight = (float)item.Current(PropertyTarget.Weight);
        if (item.ChangedTarget(PropertyTarget.Armor)) record.ArmorRating = (float)item.Current(PropertyTarget.Armor);
    }
}
