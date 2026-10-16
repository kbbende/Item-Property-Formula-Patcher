# v3 example settings based on the previous published preset

Use `settings.v3.example.json` with Item Property Formula Patcher v3. Copy its contents into the patcher's `settings.json` through Synthesis. The file uses the new single ordered `Rules` list.

## What is enabled

The original category and keyword catalogue is preserved, with formulas replacing multiplier/add/min controls. Category rules run first, then keyword rules, in the same order as your previous preset.

| Category | Value formula |
|---|---|
| Weapon | `round(target * 1.5 + 25)` |
| Ammunition | `round(target + 2)` |
| HeavyArmor | `round(target * 3)` |
| LightArmor | `round(target * 2)` |
| Shield | `target` |
| Clothing | `round(target * 3 + 50)` |
| Jewelry | `round(target * 3)` |
| Food | `round(max(target * 0.75, 1))` |
| Potion | `round(target * 1.5)` |
| Poison | `round(target * 1.5)` |
| Ingredient | `target` |
| Book | `round(target + 30)` |
| SpellTome | `round(target * 3 + 50)` |
| MiscItem | `target` |

Keyword adjustments include gems ×3, animal hides ×2, rings ×0.9, circlets ×1.25, and tools +10. The remaining supported keyword entries are neutral `target` formulas, ready to customize. Neutral formulas leave their target unchanged.

**Matching rules stack.** A spell tome also matches Book, so a starting value of 100 becomes 130, then 440. Jewelry starting at 100 becomes 300, then 270 for a ring or 375 for a circlet. This preserves your original preset's ordering and overlap.

## Optional property examples

The last ten rules are disabled templates. Enable individual rules to try:

| Category | Target | Example change |
|---|---|---|
| Weapon | Weight | Reduce by 10%, minimum 0.1, round to 0.1 |
| Armor | Weight | Reduce by 15%, minimum 0.1, round to 0.1 |
| Ammunition | Weight | Halve, round to 0.001 |
| Ingestible | Weight | Reduce by 25%, round to 0.01 |
| Ingredient | Weight | Reduce by 25%, round to 0.01 |
| Book | Weight | Reduce by 25%, round to 0.1 |
| MiscItem | Weight | Reduce by 25%, round to 0.1 |
| Weapon | Damage | Increase by 10%, round to a whole number |
| Ammunition | Damage | Increase by 10%, round to 0.1 |
| Armor | Armor | Increase armor rating by 10%, round to 0.01 |

These are demonstration choices, not a tested balance overhaul. They remain disabled so importing the preset initially changes prices only. The positive minimum in the Weapon/Armor weight examples will give previously weightless equipment a weight of 0.1 if enabled; remove `max(..., 0.1)` if you want those items to stay weightless.

`target` means the current value of the property selected by that rule. `baseTarget` means its original winning-record value. For example, `round(baseTarget * 0.8, 0.1)` can set weight from the original rather than stacking another reduction.

The optional property rules are after the price rules. If you later make prices depend on changed damage or weight, move the relevant property rule before that price formula. For example, a weapon price rule could use `round(max(target, damage * damage * 4), 5)` after a damage rule.

## Differences and limits

- The old `VendorItemScroll` and `VendorItemSoulGem` value rules are retained but disabled: v3 currently processes Weapon, Armor, Ammunition, Ingestible, Ingredient, Book, and MiscItem records. It does not process Scroll or SoulGem records. A keyword alone cannot extend the record families supported by the patcher.
- Missing keyword selectors warn and skip. Unsupported target writes use `WarnAndSkip`, capped at 100 warnings; formula warnings are also capped at 100. Record-by-record logging is off.
- Formulas use stored winning-record prices, which can differ from prices calculated by the game from effects. Changed potion/ingredient prices use v3's explicit-price handling; multiplying a stored zero price still produces zero unless the formula supplies a floor or another nonzero basis. The Food rule supplies a minimum of 1.
- v3 applies its target-specific conversion and bounds after each rule. Ingredient prices are limited by their signed price field; other values follow their adapters. This is a v3 adaptation, not a guarantee of byte-identical output to the old patcher.

## Validation

All 44 formulas, including disabled templates, were parsed by the v3 formula engine. All 32 enabled rules compiled using synthetic keyword records, and 16 representative price results were checked, including the Book/SpellTome and Jewelry overlaps. No installed game or real load order was modified for this check.
