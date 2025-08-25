## HGEngineGUI

**MVP Release - Modern Windows GUI Editor for HG Engine**

GUI editor for the HG Engine project. It provides a modern Windows app to browse and edit Pokémon Species data and Trainer data, with safe Preview/Save flows and packaged distribution. This MVP release focuses on stable, production-ready features with comprehensive data editing capabilities.

### Features

- **Project selection and file watching**
  - Pick your HG Engine root folder (the one containing `armips/`, `data/`, etc.).
  - Automatic file watcher monitors `armips/data` and prompts to reload when external changes are detected.

- **Species editor**
  - **Overview (fully editable)**: Types, Base Stats, EV yields, Abilities (1/2 + Hidden Ability), Egg Groups, Growth Rate, Gender Ratio, Catch Rate, Base Exp, Egg Cycles, Friendship, Run Chance, Pokédex Classification, Pokédex Entry (multiline), Dex Height, Dex Weight.
  - **Hidden Ability**: Dedicated row with full read/write support to `data/HiddenAbilityTable.c`.
  - **TM/HM Compatibility**: Filterable list with search, Preview and Save to `armips/data/tmlearnset.txt`.
  - **Tutor Moves**: Filterable list with search, Preview and Save to `tutordata.txt`.
  - **Level‑up, Evolutions, Egg Moves**: Read-only summaries for stability (Preview/Save still functional).

- **Trainers editor**
  - Enhanced detail layout with vertical scrolling and improved text wrapping.
  - Comprehensive Pokemon configuration: Moves, Nature, Form, Ball, Shiny Lock, PP, Nickname.
  - Clear Shiny Lock checkbox with descriptive label.
  - Preview and Save supported with safe file operations.

- **Config editor**
  - Game configuration toggles for features like Fairy Type implementation and Level Cap
  - Toggle switches with Preview/Save to `include/config.h`
  - Automatic management of related configuration variables

- **Encounters editor**
  - Wild Pokemon encounter data management for grass, water, and special encounters
  - Route-based encounter editing with searchable area list
  - Time-based grass encounters (Morning, Day, Night, Hoenn, Sinnoh)
  - Water encounters (Surf, Rock Smash, Old/Good/Super Rod fishing)
  - Editable encounter rates and probabilities
  - Pokemon sprite display next to species selectors
  - Compact 4-column grid layout for better space utilization

- **Items editor**
  - Item price editing with Preview/Save to `data/itemdata/itemdata.c`.
  - Mart Items (custom addon): If `armips/asm/custom/mart_items.s` exists, an editor is enabled to modify per‑shop inventories and badge gates. Writes update only the targeted `.org` block (or, for the General Poké Mart Table, only the item/body region between the header comment and the next section), preserving whitespace.

- **Safety and diagnostics**
  - Global crash dialog with logs written to app LocalState (`last_crash.txt`, `first_chance.log`).
  - Keyboard shortcut: `Ctrl+S` on editor pages.

### Data mapping (sources/targets)

- **Types**: `include/battle.h`
- **Abilities**: prefer `asm/include/abilities.inc`; fallback `include/constants/ability.h`
- **Growth rates**: `armips/include/constants.s`
- **Egg groups**: fallback list if `egg_groups.h` is missing
- **Base Exp**: `data/BaseExperienceTable.c` (read/write, preferred over mondata.s)
- **Hidden Ability**: `data/HiddenAbilityTable.c` (read/write)
- **Dex fields (classification/entry/height/weight)**: serialized via `mondata.s` macros
- **TM/HM Compatibility**: `armips/data/tmlearnset.txt`
- **Tutor Moves**: `tutordata.txt`
- **Species Overview**: `armips/data/mondata.s` (all fields except Base Exp)
- **Trainer Data**: parsed from trainer definition files
- **Game Config**: `include/config.h` (feature toggles and settings)
- **Encounter Data**: `armips/data/encounters.s` (wild Pokemon encounters by area)
- **Headbutt Data**: `armips/data/headbutt.s` (headbutt tree encounters)
- **Item Prices**: `data/itemdata/itemdata.c` (`.price = N` per `[ITEM_*]` entry)
- **Mart Items (custom addon)**: `armips/asm/custom/mart_items.s` (per‑shop `.org` blocks and General Poké Mart table)

### Install (packaged MSIX)

1. Download the release `.msix`/`.msixbundle` and the accompanying `.cer` (test certificate) if provided.
2. Install the certificate:
   - Double‑click the `.cer` → Install Certificate → Local Machine → Place in store → Trusted People.
   - Repeat for the Trusted Root Certification Authorities store if required by your system.
   - Alternatively, use your organization’s signing certificate and skip this step.
3. Install the app: double‑click the `.msix`/`.msixbundle` and follow the prompt.
4. Optional: If your release includes `install.ps1`/`install.bat`, you can run `install.bat` to import the cert and install in one step.

### Build from source

- Requirements: Windows 10 2004 (build 19041) or newer, .NET 8 SDK, Visual Studio 2022 with UWP/WinAppSDK workloads.
- Open `HGEngineGUI.sln`, set configuration, and build. You can run unpackaged (requires Windows App SDK Runtime present) or use the Packaging Project/MSIX for end‑user installs.
- If building via CLI, specify a concrete Platform and RuntimeIdentifier to avoid AnyCPU packaging errors:
  - `dotnet build HGEngineGUI.csproj -c Debug -p:Platform=x64 -r win-x64`

### Usage

1. Launch HGEngineGUI.
2. On the Project page, select your HG Engine root folder (the folder that contains `armips/`, `data/`, etc.).
3. Open the Species page:
   - Pick a species from the list.
   - Edit in the Overview tab; use Preview to review file changes; Save to write.
   - Adjust TM/HM and Tutor moves using search and multi‑select; use Preview/Save.
   - View Level‑up, Evolutions, Egg Moves summaries; use Preview/Save as needed.
4. Open the Trainers page to edit trainer parties and related fields; use Preview/Save.
5. Open the Items page:
   - Adjust item prices and Preview/Save to `data/itemdata/itemdata.c`.
   - If `armips/asm/custom/mart_items.s` is detected, expand Mart Items. Choose a shop section by name (e.g., “Cherrygrove City 2nd Clerk → Violet City 2nd Clerk”) or the General table, edit items/badge gates, Preview, then Save. Only that section is rewritten; whitespace is preserved.
5. The app will prompt when external changes are detected in `armips/data`.

### Troubleshooting

- **Crashes**: Global exception handler shows dialog and writes logs to app LocalState (`last_crash.txt`, `first_chance.log`). Include these when filing issues.
- **Empty lists**: Confirm project root is set correctly and expected files exist (see Data mapping above).
- **File watcher**: App monitors `armips/data` and prompts to reload on external changes.
- **MVP limitations**: Level-up, Egg Moves, and Evolutions tabs show read-only summaries (Preview/Save still work).
- **MSIX issues**: Ensure certificate is installed to Trusted People/Trusted Root stores.
- **Packaged .NET AnyCPU error**: Use an explicit platform/RID when building (`-p:Platform=x64 -r win-x64`).
- **Performance**: Some operations may be slower in packaged builds due to virtualization constraints.

### Contributing / Issues

Please open issues and pull requests on GitHub with clear reproduction steps. Include your Windows version, HGEngineGUI version, and logs if applicable.


