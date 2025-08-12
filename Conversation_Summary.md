## HGEngineGUI MVP Development and Release — Detailed Conversation Summary

### Project context and goals
- **Objective**: Build a GUI tool for the HG Engine to edit Pokémon and Trainer data, ship an MVP as a distributable app.
- **Primary areas**: Species editor (Overview, Level‑up, Egg Moves, Evolutions, TM/HM, Tutors) and Trainers editor.
- **Constraints**: Prioritize stability in packaged (MSIX) builds; accept simplified UI in crash‑prone tabs for MVP.

### Species editor: Overview (expanded and stable)
- **New Overview fields**: Catch Rate, Base Exp (sourced from `data/BaseExperienceTable.c`), Gender Ratio, Base Friendship, Growth Rate, Egg Groups, Run Chance, Dex Classification, Dex Entry, Dex Height, Dex Weight.
- **Abilities**:
  - Added third slot for Hidden Ability, then gave it its own row.
  - Alphabetized ability dropdowns for easier navigation.
  - Hidden ability is read/written via `data/HiddenAbilityTable.c` and included in the main Overview Save.
- **Macro sources (specific to this repo)**:
  - **Types**: `include/battle.h`.
  - **Abilities**: prefer `asm/include/abilities.inc`, fallback `include/constants/ability.h`.
  - **Growth rates**: `armips/include/constants.s`.
  - **Egg groups**: fallback list if `egg_groups.h` is missing.
- **Dex data**: classification, entry (multiline), height, weight serialized via `mondata.s` macros.

### Species editor: Level‑up, Egg Moves, Evolutions (stabilized via summaries)
- **Initial problems**: Default values failed to appear; opening tabs crashed the entire app in packaged mode.
- **Tried**:
  - ComboBox `Loaded` handlers to set `ItemsSource` and default selection.
  - Switching from `x:Bind` inside DataTemplates to classic `Binding`, or explicit code‑behind population.
- **Root cause (observed)**: `System.NullReferenceException` deep in WinRT/Microsoft.UI.Xaml during virtualized templated list materialization in MSIX.
- **MVP resolution**: Remove virtualized ComboBox/ListView editors and render read‑only, monospaced summaries:
  - Level‑up summary
  - Egg Moves summary
  - Evolutions summary
- **Result**: No crashes; Save/Preview paths still function.

### TM/HM and Tutors
- **TM/HM**: Read/Preview/Save against `armips/data/tmlearnset.txt`; infer species inclusion per block header.
- **Tutors**: Read/Preview/Save via `tutordata.txt`.

### Trainer editor improvements
- **Layout**: Added vertical scrolling on the detail pane; fixed text wrapping for headers.
- **Fields**: Clarified layout for Moves, Nature, Form, Ball, Shiny Lock, PP, Nickname.
- **Shiny Lock**: Label as `TextBlock` above a `CheckBox` for clarity.

### Data parsing and serialization updates
- **Parsing (`HGParsers.cs`)**:
  - Extended `SpeciesOverview` with `BaseExp`, `RunChance`, `DexClassification`, `DexEntry`, `DexHeight`, `DexWeight`, `AbilityHidden`.
  - Always read `BaseExp` from `data/BaseExperienceTable.c` (ignore `mondata.s` for this field).
  - Updated parsing to correct sources (Types, Abilities, Growth Rates) and fallback lists.
  - Added parsing for `HiddenAbilityTable.c`.
  - Fixed regex escaping and null `Path.Combine` guards.
- **Serialization (`HGSerializers.cs`)**:
  - Overview Save/Preview include RunChance and all Dex fields; exclude BaseExp from `mondata.s` edits.
  - Save/Preview for Hidden Ability writes to `data/HiddenAbilityTable.c`.
  - Added `EscapeQuotes` helper for safe Dex text serialization.

### Stability and crash diagnostics
- **Global handlers (`App.xaml.cs`)**:
  - `UnhandledException` shows a dialog and writes `last_crash.txt` in LocalState.
  - First‑chance exception logging writes to `first_chance.log` in LocalState.
- **Outcome**: Identified projection null dereferences during templated list materialization; resolved by simplifying the three problem tabs to summary views.

### Packaging and distribution (MSIX)
- **MSIX setup**:
  - Created a Windows Application Packaging Project.
  - Set manifest fields (Display Name, Version) and created a test signing certificate.
  - Sideloading instructions documented (install cert to Trusted People and Trusted Root; then install MSIX/MSIXBundle).
- **One‑click installer**:
  - `install.ps1` imports the `.cer` and installs the MSIX using `Add-AppxPackage`.
  - `install.bat` invokes the PowerShell script with execution policy bypass for non‑technical users.
- **Naming**: Explained how to change Start menu/app display name via packaging manifest.

### Folder publish (unpackaged) note
- Running unpackaged requires the Windows App SDK Runtime.
- Alternative is to use the WinAppSDK Bootstrapper (`Microsoft.WindowsAppRuntime.Bootstrap.Net`) and call `Bootstrap.Initialize(...)` before `Application.Start` (still less friendly than MSIX for end users).

### File watcher and Recent changes
- **File watcher**: Monitors `armips/data` and prompts to reload.
- **Recent changes**: Open file and Restore from backup actions supported.

### Current MVP capabilities
- **Overview**: Full editing; saves to `mondata.s` (except BaseExp which writes to `BaseExperienceTable.c`); Hidden Ability writes to `HiddenAbilityTable.c`.
- **TM/HM, Tutors**: Preview and Save work.
- **Trainers**: Stable UI with scroll; edits and Save/Preview work.
- **Level‑up, Egg Moves, Evolutions**: Read‑only summaries; Preview/Save paths intact.
- **Packaging**: MSIX build and guided install flow with optional one‑click script.

### Known limitations and next steps
- **Limitations**: Three tabs are read‑only in MVP; no interactive dropdowns in those tabs.
- **Next**:
  - Reintroduce editors using non‑virtualized patterns (e.g., `ItemsRepeater` + `StackPanel`, or per‑row flyouts) to avoid projection crashes.
  - Optional diagnostics pane to surface binding errors in‑app.
  - Optional `.appinstaller` for auto‑updates over HTTPS.

---
If you want, I can branch and prototype a safer editor pattern for the three tabs while keeping the main branch stable for release.
