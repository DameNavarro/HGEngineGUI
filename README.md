## HGEngineGUI

Modern Windows GUI editor for HG Engine projects with safe Preview/Save and live file watching. Built for reliability and speed on Windows 10/11.

### Overview

HGEngineGUI provides an intuitive way to edit HG Engine project data without hand-editing assembly/C files. It focuses on practical workflows:
- Project picker with live file watching (reload on external changes)
- Preview/Save with automatic .bak backups
- Editors for Species, Trainers, Moves, Encounters, Items, and Config
- Crash logs written to LocalState; `Ctrl+S` saves on editor pages

### Requirements

- Windows 10 version 2004 (build 19041) or newer
- Architectures: x86, x64, ARM64
- [HG Engine](https://github.com/BluRosie/hg-engine) (with `armips/`, `data/`, `include/`)
- For building from source: Visual Studio 2022, .NET 8 SDK, Windows SDK (see Development)

### Supported data

- Species: stats, types, abilities (1/2 + Hidden), base EXP, catch rate, EV yields, egg groups, gender ratio, egg cycles, friendship, growth rate, wild items, Pokédex text, run chance
- TM/HM compatibility and Tutor moves
- Level‑up moves, Evolutions, Egg moves (read‑only summaries in UI; Preview/Save supported)
- Trainers: full party configuration (species, level, form, IVs/EVs, nature, item, ability slot, moves + PP, shiny lock, nickname, ball, flags)
- Moves: name/description, split, type, power/accuracy/PP/priority, effect, target, flags
- Encounters: grass (time variants), water (surf/rock smash), fishing (old/good/super), rates and probabilities
- Items: prices; Mart Items addon for `armips/asm/custom/mart_items.s` (per‑shop sections and General table)
- Config: feature toggles (e.g., Fairy Type, Level Cap) written to `include/config.h`

### Install (packaged MSIX)

1) Download the `.msix`/`.msixbundle` (and `.cer` if provided) from Releases
2) Install the certificate to Trusted People (and Trusted Root if required)
3) Double‑click the package to install
4) Optional: run `install.bat` to install the cert and app in one step

### Build from source

- Open `HGEngineGUI.sln` in Visual Studio 2022 and build for x86/x64/ARM64, or use CLI:
```powershell
 dotnet build HGEngineGUI\HGEngineGUI.csproj -c Debug -p:Platform=x64 -r win-x64
```

### First steps

1) Launch HGEngineGUI → pick your HG Engine root folder
2) Edit via Species/Trainers/Moves/Encounters/Items/Config
3) Use Preview to review diffs → Save to write (backups are automatic)

### Quick links

- [Getting started](https://github.com/DameNavarro/HGEngineGUI/wiki/Getting-Started)
- [Interface tour](https://github.com/DameNavarro/HGEngineGUI/wiki/User-Interface)
- [Species editing](https://github.com/DameNavarro/HGEngineGUI/wiki/Species-Editing)
- [Trainer editing](https://github.com/DameNavarro/HGEngineGUI/wiki/Trainer-Editing)
- [Moves editor]()
- [Data mapping](https://github.com/DameNavarro/HGEngineGUI/wiki/Data-Mapping)
- [Troubleshooting](https://github.com/DameNavarro/HGEngineGUI/wiki/Troubleshooting)
- [Development](https://github.com/DameNavarro/HGEngineGUI/wiki/Development)
- [FAQ](https://github.com/DameNavarro/HGEngineGUI/wiki/FAQ)

### Data mapping (summary)

- Types: `include/battle.h`
- Abilities: `asm/include/abilities.inc` (preferred); fallback `include/constants/ability.h`
- Growth rates: `armips/include/constants.s`
- Base EXP: `data/BaseExperienceTable.c` (preferred over `mondata.s`)
- Hidden Ability: `data/HiddenAbilityTable.c`
- Dex text/overview: `armips/data/mondata.s`
- TM/HM: `armips/data/tmlearnset.txt`
- Tutors: `armips/data/tutordata.txt`
- Level‑up/Evos/Egg Moves: `armips/data/*`
- Trainers: `armips/data/trainers/trainers.s`
- Encounters: `armips/data/encounters.s` (+ `headbutt.s`)
- Items: `data/itemdata/itemdata.c`; Mart Items addon: `armips/asm/custom/mart_items.s`

### Contributing / Issues

Please open issues and PRs with clear repro steps. Include Windows version, app version, and logs if applicable.


