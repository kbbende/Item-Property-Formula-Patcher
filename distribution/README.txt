ITEM PROPERTY FORMULA PATCHER v3 - SYNTHESIS INSTALLER

Requirements
- Synthesis with .NET 10 patcher support.
- The .NET 10 SDK.
- A Skyrim Special Edition profile configured in Synthesis.
- Internet access so Synthesis can download and build the patcher.

Installation
1. Extract this archive to a convenient folder.
2. Open Synthesis and select your Skyrim Special Edition profile and the group
   that should contain this patcher.
3. Double-click ItemPropertyFormulaPatcher.synth. Synthesis should add the Git
   patcher and select its executable project.
4. Review the patcher's settings. Defaults contain no rules and change nothing.
5. Optionally copy examples/settings.v3.example.json into this patcher's
   settings.json using Synthesis's settings controls. Read the accompanying notes
   before enabling extra rules. This preset changes prices; its weight, damage,
   and armor-rating examples are disabled until you enable them.
6. Run your Synthesis group, inspect the generated plugin in xEdit, and enable
   the output in your load order.

If Windows opens the .synth file as text, add a Git Repository patcher manually:
Repository: https://github.com/kbbende/Item-Property-Formula-Patcher
Project: src/ItemPropertyFormulaPatcher/ItemPropertyFormulaPatcher.csproj

The .synth file adds the patcher. It does not apply the optional preset or run
the patcher automatically. Keep your existing settings backed up before replacing
them. Rules stack, and later formulas see earlier property changes.

This download contains the installer and optional example settings. Synthesis
downloads the actual patcher source from GitHub and builds it locally.

Documentation and source:
https://github.com/kbbende/Item-Property-Formula-Patcher

Synthesis downloads:
https://github.com/Mutagen-Modding/Synthesis/releases

Synthesis installer documentation:
https://mutagen-modding.github.io/Synthesis/Synth-File/
