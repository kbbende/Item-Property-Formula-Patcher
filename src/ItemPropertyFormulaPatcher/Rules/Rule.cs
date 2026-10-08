namespace ItemPropertyFormulaPatcher.Rules;

[Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
public enum MatchKind { Category, Keyword }
[Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
public enum PropertyTarget { Value, Weight, Damage, Armor }
[Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
public enum UnsupportedTargetPolicy { Ignore, WarnAndSkip, Error }

public sealed class Rule
{
    [Newtonsoft.Json.JsonExtensionData(ReadData = true, WriteData = false)]
    private IDictionary<string, Newtonsoft.Json.Linq.JToken>? _unknownFields = null;

    public bool Enabled { get; set; } = true;
    public MatchKind Match { get; set; } = MatchKind.Category;
    public string Selector { get; set; } = "Item";
    public PropertyTarget Target { get; set; } = PropertyTarget.Value;
    public string Formula { get; set; } = "target";

    public void ValidateSchema()
    {
        if (_unknownFields is { Count: > 0 })
            throw new ArgumentException($"Unknown rule fields: {string.Join(", ", _unknownFields.Keys)}.");
    }
}
