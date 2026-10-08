using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;

namespace ItemPropertyFormulaPatcher.Classification;

public sealed class KeywordResolver
{
    private readonly Dictionary<string, HashSet<FormKey>> _byEditorId = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<FormKey> _keys = [];

    public KeywordResolver(IEnumerable<IKeywordGetter> keywords)
    {
        foreach (var keyword in keywords)
        {
            _keys.Add(keyword.FormKey);
            if (string.IsNullOrWhiteSpace(keyword.EditorID)) continue;
            if (!_byEditorId.TryGetValue(keyword.EditorID, out var keys))
                _byEditorId[keyword.EditorID] = keys = [];
            keys.Add(keyword.FormKey);
        }
    }

    public IReadOnlySet<FormKey> Resolve(string selector)
    {
        selector = selector.Trim();
        if (_byEditorId.TryGetValue(selector, out var keys)) return keys;
        if (FormKey.TryFactory(selector, out var key) && _keys.Contains(key)) return new HashSet<FormKey> { key };
        return new HashSet<FormKey>();
    }

    public bool Has(IReadOnlySet<FormKey> keys, string editorId) => Resolve(editorId).Overlaps(keys);
}
