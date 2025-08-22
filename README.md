## HGEngineGUI

GUI editor for the HG Engine project. It provides a modern Windows app to browse and edit Pokémon Species data and Trainer data, with safe Preview/Save flows and packaged distribution.

### Features

- **Project selection and file watching**
  - Pick your HG Engine root folder (the one containing `armips/`, `data/`, etc.).
  - Automatic file watcher monitors `armips/data` and prompts to reload when external changes are detected.

- **Species editor**
  - **Overview (fully editable)**: Types, Base Stats, EV yields, Abilities (1/2), Hidden Ability, Egg Groups, Growth Rate, Gender Ratio, Catch Rate, Base Exp, Egg Cycles, Friendship, Run Chance, Pokédex Classification, Pokédex Entry (multiline), Dex Height, Dex Weight.
  - **TM/HM**: Filterable list, Preview and Save to `armips/data/tmlearnset.txt`.
  - **Tutor Moves**: Filterable list, Preview and Save to `tutordata.txt`.
  - **Level‑up, Evolutions, Egg Moves**: Summaries are displayed; Preview/Save are available. (These were simplified to ensure stability in packaged builds.)

- **Trainers editor**
  - **Overview (trainer header)**: Class, Items (x4), AI Flags (multi‑select dialog + toolbar buttons), Battle Type. The `trainermontype` flags are auto‑computed from your edits (moves/items/ability/ball/nature/shiny/IV‑EV/extra fields). Preview/Save header supported.
  - **Party editor (per‑Pokémon cards)**
    - **Basics**: Slot, Species, Level, IVs, AbilitySlot, Item.
    - **Moves**: Move 1–4.
    - **Details**: Nature (dropdown), Form, Ball (dropdown), Shiny Lock.
    - **Advanced (collapsible)**: Ability, BallSeal, IV/EV arrays, per‑stat overrides (stathp/statatk/statdef/statspeed/statspatk/statspdef), Status, Types, PP Counts, Nickname. The correct `additionalflags` mask and field macros are written automatically on Save.
  - **Live header summary**: the `mons=` count updates as you add/remove party rows; header reflects current Class/AI/Type selections.
  - **Preview/Save**: Preview Party changes and Save Party; global `Ctrl+S` works on this page as well.

### Data mapping (sources/targets)

- **Types**: `include/battle.h`
- **Abilities**: prefer `asm/include/abilities.inc`; fallback `include/constants/ability.h`
- **Growth rates**: `armips/include/constants.s`
- **Base Exp**: `data/BaseExperienceTable.c` (read/write)
- **Hidden Ability**: `data/HiddenAbilityTable.c` (read/write)
- **Dex fields (classification/entry/height/weight)**: serialized via `mondata.s` macros
- **TM/HM**: `armips/data/tmlearnset.txt`
- **Tutor Moves**: `tutordata.txt`

### Install (packaged MSIX)

1. Download the release `.msix`/`.msixbundle` and the accompanying `.cer` (test certificate) if provided.
2. If your release includes `install.ps1`/`install.bat`, you can run `install.bat` to import the cert and install in one step.
3. Load HGEngine GUI program that was installed

Optional:Install the certificate:
   - Double‑click the `.cer` → Install Certificate → Local Machine → Place in store → Trusted People.
   - Repeat for the Trusted Root Certification Authorities store if required by your system.
   - Alternatively, use your organization’s signing certificate and skip this step.
   - Install the app: double‑click the `.msix`/`.msixbundle` and follow the prompt.

### Build from source

- Requirements: Windows 10 2004 (build 19041) or newer, .NET 8 SDK, Visual Studio 2022 with UWP/WinAppSDK workloads.
- Open `HGEngineGUI.sln`, set configuration, and build. You can run unpackaged (requires Windows App SDK Runtime present) or use the Packaging Project/MSIX for end‑user installs.

### Usage

1. Launch HGEngineGUI.
2. On the Project page, select your HG Engine root folder (the folder that contains `armips/`, `data/`, etc.).
3. Open the Species page:
   - Pick a species from the list.
   - Edit in the Overview tab; use Preview to review file changes; Save to write.
   - Adjust TM/HM and Tutor moves using search and multi‑select; use Preview/Save.
   - View Level‑up, Evolutions, Egg Moves summaries; use Preview/Save as needed.
4. Open the Trainers page to edit trainer parties and related fields; use Preview/Save.
5. The app will prompt when external changes are detected in `armips/data`.

### Troubleshooting

- If a crash occurs, a dialog will appear and logs will be written under the app’s LocalState (`last_crash.txt`, `first_chance.log`). Include these logs when filing issues.
- If lists appear empty, confirm the project root is correctly set and the expected files exist (see Data mapping above).

### Contributing / Issues

Please open issues and pull requests on GitHub with clear reproduction steps. Include your Windows version, HGEngineGUI version, and logs if applicable.


