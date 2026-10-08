# Item Property Formula Patcher v3

A Skyrim Special Edition Synthesis patcher for item **Value, Weight, Damage, and Armor**. One globally ordered rule list applies constrained mathematical formulas to winning records in your load order.

**Rules stack. Later formulas see properties changed by earlier formulas.** Original winning-record values remain available throughout the run.

## Build and run

Requirements: .NET **10** SDK and Synthesis supporting .NET 10 patchers. This project pins [Mutagen.Bethesda 0.54.4](https://www.nuget.org/packages/Mutagen.Bethesda/0.54.4) and [Mutagen.Bethesda.Synthesis 0.36.6](https://www.nuget.org/packages/Mutagen.Bethesda.Synthesis/0.36.6), with no prerelease packages. Package references use exact NuGet version ranges; checked-in lock files also pin transitive dependencies.

```powershell
dotnet restore ItemPropertyFormulaPatcher.sln --locked-mode
dotnet build ItemPropertyFormulaPatcher.sln --configuration Release --no-restore
dotnet test ItemPropertyFormulaPatcher.sln --configuration Release --no-build
```

In Synthesis, add a local solution patcher using **ItemPropertyFormulaPatcher.sln**. Select the executable project `src/ItemPropertyFormulaPatcher/ItemPropertyFormulaPatcher.csproj` if prompted; the other project contains tests. Choose Skyrim Special Edition and your normal load order. Synthesis creates/loads the patcher's `settings.json` in its settings directory. Edit that configuration in Synthesis, or replace its contents with the example JSON. The copy at the repository root is a reference configuration; it is not copied over your settings during builds.

The standalone typical-open output is `ItemPropertyFormulaPatch.esp`; within a Synthesis group, the group controls the output. Review changed records in xEdit before using a balance-changing preset in a save.

The shipped defaults have an empty `Rules` list and make no changes. `examples/settings.demo.json` deliberately enables three demonstration rules: lighter heavy armor, higher Daedric weapon damage, then weapon prices based on the resulting damage. Other examples are disabled. Missing material keywords warn and skip their rules.

## Rules

```json
{
  "Rules": [
    { "Enabled": true, "Match": "Keyword", "Selector": "WeapMaterialDaedric", "Target": "Damage", "Formula": "round(target * 1.1)" },
    { "Enabled": true, "Match": "Category", "Selector": "Weapon", "Target": "Value", "Formula": "round(max(value, damage * 100), 5)" }
  ],
  "UnsupportedTargetHandling": "WarnAndSkip",
  "MaxUnsupportedTargetWarnings": 100,
  "MaxFormulaWarnings": 100,
  "LogChangedRecords": false,
  "MaxLoggedRecords": 200
}
```

Every rule has `Enabled`, `Match`, `Selector`, `Target`, and `Formula`. They run top to bottom exactly as listed, with category and keyword rules freely interleaved. A rule runs once per matching item, even if several matching keywords or categories are present. Disabled rules are not compiled. There is no implicit final formula phase: append any desired final rules to the same list.

`Match` is `Category` or `Keyword`. Category and keyword EditorID matching is case-insensitive. A keyword selector can also be an explicit Mutagen FormKey, for example `012345:Example.esp`; the referenced keyword must exist in the load order. Duplicate keyword EditorIDs match any winning keyword with that EditorID; explicit FormKeys disambiguate them.

Supported categories:

```text
Item, Weapon, Ammunition, Armor, HeavyArmor, LightArmor, Clothing,
Jewelry, Shield, Ingestible, Food, Potion, Poison, Ingredient,
Book, SpellTome, MiscItem
```

`Item` includes the seven record families below. Armor can belong to several subcategories: heavy/light/jewelry use the corresponding vanilla keywords; shields use the shield keyword or major flag. Clothing uses its vendor keyword or the fallback when no jewelry/shield/heavy/light keyword applies. Food uses the food flag or vendor keyword; poison uses the poison flag or vendor keyword; potion uses its vendor keyword or the non-food/non-poison fallback. Spell tomes use the vendor keyword or the typed spell-teaching field. Category membership and keyword sets are fixed for each item during evaluation.

## Supported writes and storage

| Record | Value | Weight | Damage | Armor |
|---|---|---|---|---|
| Weapon | UInt32 | Float32 | UInt16 | Unsupported |
| Armor | UInt32 | Float32 | Unsupported | Float rating / UInt32 hundredths on disk |
| Ammunition | UInt32 | Float32 | Float32 | Unsupported |
| Ingestible | UInt32 | Float32 | Unsupported | Unsupported |
| Ingredient | UInt32 DATA + Int32 ENIT (nonnegative shared range) | Float32 | Unsupported | Unsupported |
| Book | UInt32 | Float32 | Unsupported | Unsupported |
| MiscItem | UInt32 | Float32 | Unsupported | Unsupported |

All formula arithmetic uses finite `double` values. After **each successful rule**, the result is clamped and converted to the target's in-memory storage type, then made immediately visible to subsequent formulas. UInt32 values clamp to `0..4294967295`; ingredient values clamp to `0..2147483647` because their explicit ENIT price is signed; weapon damage clamps to `0..65535`; integers round with midpoint ties away from zero. Floats preserve fractions and clamp to `0..float.MaxValue`. Armor ratings are bounded below the safe UInt32-hundredths storage limit. [Mutagen's armor schema](https://github.com/Mutagen-Modding/Mutagen/blob/dev/Mutagen.Bethesda.Skyrim/Records/Major%20Records/Armor.xml) serializes ratings at hundredth precision; later formulas see the in-memory float until emission. Negative results clamp to zero. A missing weapon `BasicStats` subrecord makes its targets unsupported; the patcher never invents missing stats.

For example, weapon damage `10.5` becomes `11`, so a subsequent `Value = damage * 10` becomes `110`. Ammunition damage `10.5` stays fractional. Explicit `round`, `floor`, and `ceil` let formulas control rounding before conversion. The tool imposes storage bounds rather than arbitrary gameplay caps.

Only targets with a final value different from their original value are written. Records with no net changes produce no override. Winning deleted records are excluded. Existing unrelated record fields are preserved through typed overrides. A changed ingestible or ingredient price also enables the no-auto-calculation flag so that the explicit price is used; ingredient DATA and ENIT price fields are synchronized. These flags/fields are defined in [xEdit's Skyrim record schema](https://github.com/TES5Edit/TES5Edit/blob/dev-4.1.5/Core/wbDefinitionsTES5.pas). Weight-only changes preserve auto-calculation flags. Formula variables read stored winning-record values, not prices dynamically computed by the game from effects. Auto-calculated items can therefore have a stored zero price; multiplying zero remains zero and produces no override. Use a nonzero explicit price or a weight-based price for those records. Armor/weapon enchantment references are used for metadata only. Ingredient/ingestible effect lists do not count as item enchantments.

## Variables

| Current value | Original value | Additional aliases |
|---|---|---|
| `value` | `baseValue` | `currentValue`, `originalValue` |
| `weight` | `baseWeight` | `currentWeight`, `originalWeight` |
| `damage`, `dmg` | `baseDamage`, `baseDmg` | `currentDamage`, `currentDmg`, `originalDamage`, `originalDmg` |
| `armor`, `armorRating` | `baseArmor`, `baseArmorRating` | `currentArmor`, `currentArmorRating`, `originalArmor`, `originalArmorRating` |
| `target`, `currentTarget` | `baseTarget`, `originalTarget` | The property targeted by this rule |

`base` and `original` retain v2's meaning: **original gold value**, even in a weight/damage/armor rule. Use `baseTarget` for a formula that should follow the selected property. Unsupported *input* properties read as zero; unsupported *writes* follow the configured policy. `hasValue`, `hasWeight`, `hasDamage`, and `hasArmor` distinguish unsupported inputs from supported zero values.

Metadata variables: `keywordCount`, `isEnchanted`, `isPlayable`, and `is<Category>` for every category listed above, such as `isWeapon`, `isHeavyArmor`, `isPotion`, or `isSpellTome`. Nonplayable weapon/armor/ammunition flags and the book `CantBeTaken` flag set `isPlayable` to zero; other supported families default to one. All items are processed regardless of this flag; use it in formulas when needed. Boolean variables return `1` or `0`. Names and function names are case-insensitive. Constants: `pi`, `e`, `true`, `false`.

## Formula language

Operators, lowest to highest precedence:

```text
||
&&
== !=
> >= < <=
+ -
* / %
unary + - !
^
```

Parentheses override precedence. Powers associate rightward: `2^3^2 = 512`, `-2^2 = -4`, and `2^-2 = 0.25`. Other binary operators associate leftward. Comparisons return numeric booleans; any nonzero finite value is true. `&&` and `||` short-circuit. Decimal literals use a dot, independent of system locale; scientific notation is supported.

Functions:

```text
min(a, ...), max(a, ...)                         # one or more arguments
clamp(x, minimum, maximum)
round(x), round(x, step)                        # ties away from zero
floor(x), floor(x, step)
ceil(x), ceil(x, step)                          # step must be positive
abs(x), sqrt(x), pow(x, y)
log(x), log(x, base), log10(x), exp(x)
sign(x), trunc(x)
if(condition, trueValue, falseValue)             # evaluates only the chosen branch
```

Examples: `round(max(value * 10 - 100, 25))`, `round(weight * 0.8, 0.1)`, `if(hasDamage, damage * 100, baseValue)`, `if(weight > 0, value / weight, value)`.

Formulas are parsed once at startup into an AST, then reused for all records. This is a constrained evaluator, with no C# execution, file access, assignment, or member access. Unknown variables/functions, bad argument counts, invalid selectors, invalid enums, and malformed syntax fail configuration before traversal. Limits are 8192 characters, 1024 tokens, and 64 parser nesting levels per formula.

Division/remainder by zero, invalid math domains, NaN, infinity, and intermediate overflow skip the affected rule with a capped warning. The previous property values remain intact, and later rules continue. Unused branches are lazy, so `if(weight > 0, value / weight, value)` is safe for zero-weight items.

## Unsupported targets and logging

`UnsupportedTargetHandling` supports:

- `WarnAndSkip` (default): count skipped writes and log at most `MaxUnsupportedTargetWarnings` messages for the entire run.
- `Ignore`: count and skip unsupported writes silently.
- `Error`: fail the run on the first matching unsupported target.

Support is checked **before evaluating** a matching formula. Broad `Item → Damage` rules therefore affect weapon/ammunition and safely skip other families under the default policy. Warning caps accept zero to suppress messages, while summary counters remain available. `MaxFormulaWarnings` separately caps runtime math warnings. `LogChangedRecords` and `MaxLoggedRecords` control changed-record logging. Missing keyword selectors receive a startup warning and their rules are omitted.

## Migration and provenance

v1 multiplier/add settings and v2 `CategoryRules`/`KeywordRules`/`FinalFormula` settings are rejected as a v3 schema. Unknown root/rule JSON fields also fail validation, which prevents misspelled targets from silently using default values. Recreate old rules as `Rules`, explicitly set their targets, and keep the desired sequence. To reproduce v2 order, put category rules first, then keyword rules, then an `Item → Value` rule for the old final formula.

The referenced ChatGPT conversation supplied the agreed v3 design and documented v2 language. The two local sources inspected during this implementation were v1 packages, so the original v2 parser file could not be recovered. **With the user's approval, v3 rebuilds the parser/evaluator from those documented semantics**, rather than claiming a source-level refactor of unavailable v2 code. The local v1 source informed the Synthesis entry point, winning-record traversal, categories, and typed record access. Undocumented v2 edge behavior cannot be guaranteed identical.

## Project layout and verification

```text
ItemPropertyFormulaPatcher.sln                   # classic solution, usable in Synthesis
src/ItemPropertyFormulaPatcher/
  Program.cs, Settings.cs, PatchRunner.cs
  Rules/                                       # compilation and ordered evaluation
  Formula/                                     # lexer, parser, AST, context
  Records/                                     # state, conversion, seven typed adapters
  Classification/                              # categories and keyword resolution
  Diagnostics/                                 # capped logs and counters
tests/ItemPropertyFormulaPatcher.Tests/          # xUnit tests; no installed game needed
settings.json                                  # neutral reference configuration
examples/settings.demo.json                     # intentional active demonstrations
.github/workflows/build.yml                     # .NET 10 build/test CI
```

Tests cover expression precedence/functions, lazy evaluation, invalid math, invariant parsing, aliases, global rule ordering, cross-property dependencies, keyword resolution, policies/log caps, type bounds, all seven adapters, winning overrides, deleted records, unchanged records, and binary ESP serialization. These are synthetic record tests, not a balance validation against a real modded load order.

`Directory.Build.targets` removes only the unused transitive Noggog generator, which otherwise emits a duplicate global `AssemblyVersions` type against Synthesis 0.36.6. Other compiler analyzers remain enabled. No warning codes are suppressed.

## GitHub repository

The project repository is [kbbende/Item-Property-Formula-Patcher](https://github.com/kbbende/Item-Property-Formula-Patcher).

To get a local checkout:

```powershell
git clone https://github.com/kbbende/Item-Property-Formula-Patcher.git
cd Item-Property-Formula-Patcher
```

Add the repository URL in Synthesis and select the executable project. The [Build and test workflow](https://github.com/kbbende/Item-Property-Formula-Patcher/actions/workflows/build.yml) restores locked dependencies, builds the solution, and runs the tests on pushes and pull requests. It can also be started manually from GitHub Actions.
