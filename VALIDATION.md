# v3 verification

Verified on **2026-10-08** with Windows and .NET SDK **10.0.203**.

| Check | Result |
|---|---|
| Locked dependency restore | Passed |
| Release solution build | Passed: **0 warnings, 0 errors** |
| Automated xUnit tests | **117 passed, 0 failed, 0 skipped** |
| ESP binary write/read | Passed for integer values/damage, fractional weight/ammo damage, and armor rating storage bounds/precision |
| Synthesis settings discovery | Executable returned the `ItemPropertyFormulaPatcher.Settings` configuration at `settings.json` |

Direct patcher dependencies are exactly Mutagen.Bethesda **0.54.4** and Mutagen.Bethesda.Synthesis **0.36.6**. The solution targets **net10.0**. Checked-in NuGet lock files record the full resolved dependency graph.

The tests exercise formula functions and precedence, lazy branches, invalid syntax/math, invariant culture, original/current aliases, ordered category/keyword rules, cross-property dependencies, unsupported-target policies and warning limits, all seven typed adapters, conversion bounds, alchemy value flags, settings schema validation, winning overrides, deletion handling, and binary serialization.

Verification uses synthetic Skyrim records. A real modded load order and in-game balance were not evaluated. Inspect a generated patch in xEdit before adopting an active preset.

The unavailable v2 parser was rebuilt from the conversation's documented semantics with the user's approval. See README for provenance and differences that matter when migrating.

GitHub repository: [kbbende/Item-Property-Formula-Patcher](https://github.com/kbbende/Item-Property-Formula-Patcher). The included GitHub Actions workflow repeats the locked restore, release build, and automated tests for repository changes.
