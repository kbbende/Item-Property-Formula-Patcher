using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
using ItemPropertyFormulaPatcher.Classification;

namespace ItemPropertyFormulaPatcher.Records;

public abstract class RecordAdapter<TGetter, TMutable>
    where TGetter : class, IMajorRecordGetter
    where TMutable : class
{
    public abstract ItemState Read(TGetter record, CategoryClassifier classifier);
    public abstract void Write(TMutable record, ItemState item);

    protected static ItemState Create(TGetter record, ItemCategory category,
        IEnumerable<IFormLinkGetter<IKeywordGetter>>? keywords, bool enchanted = false, bool playable = true)
    {
        var item = new ItemState
        {
            Identity = $"{record.EditorID ?? "<no EditorID>"} [{record.FormKey}]",
            IsEnchanted = enchanted,
            IsPlayable = playable
        };
        item.Categories.Add(category);
        if (keywords is not null) item.Keywords.UnionWith(keywords.Select(x => x.FormKey));
        return item;
    }
}
