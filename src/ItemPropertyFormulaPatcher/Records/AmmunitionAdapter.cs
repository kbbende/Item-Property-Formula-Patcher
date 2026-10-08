using ItemPropertyFormulaPatcher.Classification;
using ItemPropertyFormulaPatcher.Rules;
using Mutagen.Bethesda.Skyrim;

namespace ItemPropertyFormulaPatcher.Records;

public sealed class AmmunitionAdapter : RecordAdapter<IAmmunitionGetter, Ammunition>
{
    public override ItemState Read(IAmmunitionGetter record, CategoryClassifier classifier) =>
        Create(record, ItemCategory.Ammunition, record.Keywords, playable: !record.MajorFlags.HasFlag(Ammunition.MajorFlag.NonPlayable))
            .Add(PropertyTarget.Value, record.Value, StorageKind.UInt32)
            .Add(PropertyTarget.Weight, record.Weight, StorageKind.Float32)
            .Add(PropertyTarget.Damage, record.Damage, StorageKind.Float32);

    public override void Write(Ammunition record, ItemState item)
    {
        if (item.ChangedTarget(PropertyTarget.Value)) record.Value = (uint)item.Current(PropertyTarget.Value);
        if (item.ChangedTarget(PropertyTarget.Weight)) record.Weight = (float)item.Current(PropertyTarget.Weight);
        if (item.ChangedTarget(PropertyTarget.Damage)) record.Damage = (float)item.Current(PropertyTarget.Damage);
    }
}
