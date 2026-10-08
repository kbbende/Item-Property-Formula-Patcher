using ItemPropertyFormulaPatcher.Classification;
using ItemPropertyFormulaPatcher.Rules;
using Mutagen.Bethesda.Skyrim;

namespace ItemPropertyFormulaPatcher.Records;

public sealed class BookAdapter : RecordAdapter<IBookGetter, Book>
{
    public override ItemState Read(IBookGetter record, CategoryClassifier classifier)
    {
        var item = Create(record, ItemCategory.Book, record.Keywords, playable: !record.Flags.HasFlag(Book.Flag.CantBeTaken));
        classifier.Book(item, record);
        return item.Add(PropertyTarget.Value, record.Value, StorageKind.UInt32)
            .Add(PropertyTarget.Weight, record.Weight, StorageKind.Float32);
    }

    public override void Write(Book record, ItemState item)
    {
        if (item.ChangedTarget(PropertyTarget.Value)) record.Value = (uint)item.Current(PropertyTarget.Value);
        if (item.ChangedTarget(PropertyTarget.Weight)) record.Weight = (float)item.Current(PropertyTarget.Weight);
    }
}
