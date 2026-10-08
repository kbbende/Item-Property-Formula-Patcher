using ItemPropertyFormulaPatcher.Classification;
using ItemPropertyFormulaPatcher.Rules;
using Mutagen.Bethesda.Plugins;

namespace ItemPropertyFormulaPatcher.Records;

public sealed class ItemState
{
    private sealed class Slot(double original, StorageKind storage)
    {
        public double Original { get; } = original;
        public double Current { get; set; } = original;
        public StorageKind Storage { get; } = storage;
    }

    private readonly Dictionary<PropertyTarget, Slot> _properties = [];
    public string Identity { get; init; } = "item";
    public HashSet<ItemCategory> Categories { get; } = [ItemCategory.Item];
    public HashSet<FormKey> Keywords { get; } = [];
    public bool IsEnchanted { get; init; }
    public bool IsPlayable { get; init; } = true;
    public bool Changed => _properties.Values.Any(x => !x.Current.Equals(x.Original));
    public bool ChangedTarget(PropertyTarget target) => _properties.TryGetValue(target, out var s) && !s.Current.Equals(s.Original);

    public ItemState Add(PropertyTarget target, double original, StorageKind storage)
    {
        _properties.Add(target, new Slot(original, storage));
        return this;
    }

    public bool Supports(PropertyTarget target) => _properties.ContainsKey(target);
    public double Current(PropertyTarget target) => _properties.TryGetValue(target, out var s) ? s.Current : 0;
    public double Original(PropertyTarget target) => _properties.TryGetValue(target, out var s) ? s.Original : 0;

    public void Set(PropertyTarget target, double value)
    {
        if (!_properties.TryGetValue(target, out var s)) throw new InvalidOperationException($"Unsupported target {target}.");
        s.Current = PropertyConversion.Normalize(value, s.Storage);
    }
}
