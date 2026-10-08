using ItemPropertyFormulaPatcher.Classification;
using ItemPropertyFormulaPatcher.Rules;
using Mutagen.Bethesda.Skyrim;

namespace ItemPropertyFormulaPatcher.Records;

public sealed class IngestibleAdapter : RecordAdapter<IIngestibleGetter, Ingestible>
{
    public override ItemState Read(IIngestibleGetter record, CategoryClassifier classifier)
    {
        var item = Create(record, ItemCategory.Ingestible, record.Keywords);
        classifier.Ingestible(item, record);
        return item.Add(PropertyTarget.Value, record.Value, StorageKind.UInt32)
            .Add(PropertyTarget.Weight, record.Weight, StorageKind.Float32);
    }

    public override void Write(Ingestible record, ItemState item)
    {
        if (item.ChangedTarget(PropertyTarget.Value))
        {
            record.Value = (uint)item.Current(PropertyTarget.Value);
            record.Flags |= Ingestible.Flag.NoAutoCalc;
        }
        if (item.ChangedTarget(PropertyTarget.Weight)) record.Weight = (float)item.Current(PropertyTarget.Weight);
    }
}
