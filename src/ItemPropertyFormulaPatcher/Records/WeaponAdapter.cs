using ItemPropertyFormulaPatcher.Classification;
using ItemPropertyFormulaPatcher.Rules;
using Mutagen.Bethesda.Skyrim;

namespace ItemPropertyFormulaPatcher.Records;

public sealed class WeaponAdapter : RecordAdapter<IWeaponGetter, Weapon>
{
    public override ItemState Read(IWeaponGetter record, CategoryClassifier classifier)
    {
        var item = Create(record, ItemCategory.Weapon, record.Keywords, !record.ObjectEffect.IsNull,
            !record.MajorFlags.HasFlag(Weapon.MajorFlag.NonPlayable));
        // A missing DATA subrecord is unsupported, never synthesized from zero.
        if (record.BasicStats is { } stats)
            item.Add(PropertyTarget.Value, stats.Value, StorageKind.UInt32)
                .Add(PropertyTarget.Weight, stats.Weight, StorageKind.Float32)
                .Add(PropertyTarget.Damage, stats.Damage, StorageKind.UInt16);
        return item;
    }

    public override void Write(Weapon record, ItemState item)
    {
        if (record.BasicStats is not { } stats) throw new InvalidOperationException("Weapon DATA is missing.");
        if (item.ChangedTarget(PropertyTarget.Value)) stats.Value = (uint)item.Current(PropertyTarget.Value);
        if (item.ChangedTarget(PropertyTarget.Weight)) stats.Weight = (float)item.Current(PropertyTarget.Weight);
        if (item.ChangedTarget(PropertyTarget.Damage)) stats.Damage = (ushort)item.Current(PropertyTarget.Damage);
    }
}
