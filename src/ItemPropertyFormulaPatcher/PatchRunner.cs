using ItemPropertyFormulaPatcher.Classification;
using ItemPropertyFormulaPatcher.Diagnostics;
using ItemPropertyFormulaPatcher.Records;
using ItemPropertyFormulaPatcher.Rules;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;

namespace ItemPropertyFormulaPatcher;

public static class PatchRunner
{
    /// <param name="priorityOrder">Mods in highest-priority-first order, as supplied by Synthesis.</param>
    public static PatchDiagnostics Run(IEnumerable<ISkyrimModGetter> priorityOrder, ISkyrimMod patch,
        Settings settings, Action<string>? log = null)
    {
        var mods = priorityOrder.ToArray();
        var keywords = new KeywordResolver(mods.WinningOverrides<IKeywordGetter>());
        var classifier = new CategoryClassifier(keywords);
        var diagnostics = new PatchDiagnostics(settings, log);
        var rules = RuleCompiler.Compile(settings, keywords, log);
        var engine = new RuleEngine(rules, settings.UnsupportedTargetHandling, diagnostics);
        Process(mods.WinningOverrides<IWeaponGetter>(), new WeaponAdapter(), r => patch.Weapons.GetOrAddAsOverride(r));
        Process(mods.WinningOverrides<IArmorGetter>(), new ArmorAdapter(), r => patch.Armors.GetOrAddAsOverride(r));
        Process(mods.WinningOverrides<IAmmunitionGetter>(), new AmmunitionAdapter(), r => patch.Ammunitions.GetOrAddAsOverride(r));
        Process(mods.WinningOverrides<IIngestibleGetter>(), new IngestibleAdapter(), r => patch.Ingestibles.GetOrAddAsOverride(r));
        Process(mods.WinningOverrides<IIngredientGetter>(), new IngredientAdapter(), r => patch.Ingredients.GetOrAddAsOverride(r));
        Process(mods.WinningOverrides<IBookGetter>(), new BookAdapter(), r => patch.Books.GetOrAddAsOverride(r));
        Process(mods.WinningOverrides<IMiscItemGetter>(), new MiscItemAdapter(), r => patch.MiscItems.GetOrAddAsOverride(r));
        diagnostics.Summary();
        return diagnostics;

        void Process<TGetter, TMutable>(IEnumerable<TGetter> records, RecordAdapter<TGetter, TMutable> adapter, Func<TGetter, TMutable> getOverride)
            where TGetter : class, IMajorRecordGetter where TMutable : class
        {
            foreach (var record in records)
            {
                var item = adapter.Read(record, classifier);
                if (!engine.Apply(item)) continue;
                adapter.Write(getOverride(record), item);
                diagnostics.Changed($"Changed {item.Identity}");
            }
        }
    }
}
