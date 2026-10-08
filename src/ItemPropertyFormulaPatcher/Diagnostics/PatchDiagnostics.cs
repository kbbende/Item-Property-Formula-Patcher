namespace ItemPropertyFormulaPatcher.Diagnostics;

public sealed class PatchDiagnostics(Settings settings, Action<string>? log = null)
{
    private readonly Action<string> _log = log ?? Console.WriteLine;
    public int UnsupportedTargets { get; private set; }
    public int FormulaFailures { get; private set; }
    public int ChangedRecords { get; private set; }

    public void Unsupported(string message)
    {
        UnsupportedTargets++;
        if (settings.UnsupportedTargetHandling == Rules.UnsupportedTargetPolicy.WarnAndSkip &&
            UnsupportedTargets <= settings.MaxUnsupportedTargetWarnings) _log($"[WARN] {message}");
    }

    public void FormulaFailure(string message)
    {
        FormulaFailures++;
        if (FormulaFailures <= settings.MaxFormulaWarnings) _log($"[WARN] {message}");
    }

    public void Changed(string message)
    {
        ChangedRecords++;
        if (settings.LogChangedRecords && ChangedRecords <= settings.MaxLoggedRecords) _log(message);
    }

    public void Summary() => _log($"Changed records: {ChangedRecords}; unsupported targets skipped: {UnsupportedTargets}; formula failures skipped: {FormulaFailures}. Warning and record logs respect the configured caps.");
}
