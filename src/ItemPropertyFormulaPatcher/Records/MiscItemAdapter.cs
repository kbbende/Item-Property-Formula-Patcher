using ItemPropertyFormulaPatcher.Classification;
using ItemPropertyFormulaPatcher.Rules;
using Mutagen.Bethesda.Skyrim;

namespace ItemPropertyFormulaPatcher.Records;

public sealed class MiscItemAdapter : RecordAdapter<IMiscItemGetter, MiscItem>
{
    public override ItemState Read(IMiscItemGetter record, CategoryClassifier classifier) =>
        Create(record, ItemCategory.MiscItem, record.Keywords)
            .Add(PropertyTarget.Value, record.Value, StorageKind.UInt32)
            .Add(PropertyTarget.Weight, record.Weight, StorageKind.Float32);

    public override void Write(MiscItem record, ItemState item)
    {
        if (item.ChangedTarget(PropertyTarget.Value)) record.Value = (uint)item.Current(PropertyTarget.Value);
        if (item.ChangedTarget(PropertyTarget.Weight)) record.Weight = (float)item.Current(PropertyTarget.Weight);
    }
}
