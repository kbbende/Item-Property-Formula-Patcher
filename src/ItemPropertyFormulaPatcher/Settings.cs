using ItemPropertyFormulaPatcher.Rules;

namespace ItemPropertyFormulaPatcher;

public sealed class Settings
{
    [Newtonsoft.Json.JsonExtensionData(ReadData = true, WriteData = false)]
    private IDictionary<string, Newtonsoft.Json.Linq.JToken>? _unknownFields = null;

    public List<Rule> Rules { get; set; } = [];
    public UnsupportedTargetPolicy UnsupportedTargetHandling { get; set; } = UnsupportedTargetPolicy.WarnAndSkip;
    public int MaxUnsupportedTargetWarnings { get; set; } = 100;
    public int MaxFormulaWarnings { get; set; } = 100;
    public bool LogChangedRecords { get; set; }
    public int MaxLoggedRecords { get; set; } = 200;

    public void ValidateSchema()
    {
        if (_unknownFields is { Count: > 0 })
            throw new ArgumentException($"Unknown settings fields: {string.Join(", ", _unknownFields.Keys)}. Use the v3 Rules schema.");
    }
}
