# Getting Started with HG Engine Editor

This guide will walk you through installing, setting up, and starting to use HG Engine Editor for your Pokemon ROM hacking projects.

## 📥 Installation

### Option 1: MSIX Package (Recommended)

1. **Download the Release**
   - Visit the [GitHub Releases](https://github.com/your-repo/HG Engine Editor/releases) page
   - Download the latest `.msix` or `.msixbundle` file
   - Also download the accompanying `.cer` certificate file

2. **Install the Certificate**
   - Double-click the `.cer` file
   - Click "Install Certificate"
   - Choose "Local Machine" and click "Next"
   - Select "Place all certificates in the following store"
   - Browse to "Trusted People" and click "OK"
   - Repeat the process for "Trusted Root Certification Authorities" store if required

3. **Install the Application**
   - Double-click the `.msix` or `.msixbundle` file
   - Follow the installation prompts
   - The app will be installed and appear in your Start Menu

4. **Alternative Installation Script**
   - If provided, run the included `install.ps1` or `install.bat` script
   - This will automatically handle certificate installation and app deployment

### Option 2: Build from Source

If you prefer to build from source or need development access:

1. **Prerequisites**
   - Windows 10 version 2004 (19041) or newer
   - Visual Studio 2022 with:
     - .NET 8 SDK
     - UWP/WinAppSDK workloads
     - MSVC v143 build tools

2. **Clone and Build**
   ```bash
   git clone https://github.com/your-repo/HG Engine Editor.git
   cd HG Engine Editor
   open HG Engine Editor.sln
   ```
   - Set your configuration (Debug/Release, x86/x64/ARM64)
   - Build the solution
   - Run or deploy the application

Or build from CLI with explicit platform and RID to avoid AnyCPU packaging errors:
```powershell
dotnet build HGEngineGUI\HGEngineGUI.csproj -c Debug -p:Platform=x64 -r win-x64
```

## 🎯 Project Setup

### 1. Launch HG Engine Editor

- Open HG Engine Editor from your Start Menu or desktop shortcut
- The application will start on the **Project** page

### 2. Select Your HG Engine Project

- Click the **"Pick Folder"** button on the Project page
- Navigate to and select your HG Engine root directory
- The root folder should contain:
  - `armips/` - Assembly data files
  - `data/` - Game data files
  - `include/` - Header files
  - Other project-specific directories

### 3. Verify Project Detection

After selecting your project folder, HG Engine Editor will:

- ✅ Display the project path in the status bar
- ✅ Show the number of detected Pokemon species
- ✅ Enable the Species and Trainers navigation buttons
- ✅ Start monitoring the `armips/data` folder for changes

## 🗂️ Understanding Project Structure

HG Engine Editor expects your HG Engine project to follow this structure:

```
Your-Project-Root/
├── armips/
│   ├── data/
│   │   ├── mondata.s          # Pokemon species data
│   │   ├── levelupdata.s      # Level-up moves
│   │   ├── evodata.s          # Evolution data
│   │   ├── eggmoves.s         # Egg moves
│   │   ├── tmlearnset.txt     # TM/HM compatibility
│   │   ├── tutordata.txt      # Move tutor data
│   │   ├── encounters.s       # Wild Pokemon encounter data
│   │   ├── headbutt.s         # Headbutt encounter data
│   │   └── trainers/
│   │       └── trainers.s     # Trainer data
│   ├── asm/custom/
│   │   └── mart_items.s       # Poké Mart inventory data
│   └── include/
│       └── constants.s        # Assembly constants
├── data/
│   ├── BaseExperienceTable.c  # Base experience values
│   └── HiddenAbilityTable.c   # Hidden abilities
├── include/
│   ├── config.h               # Game configuration toggles
│   ├── constants/
│   │   ├── species.h          # Pokemon species definitions
│   │   ├── moves.h            # Move definitions
│   │   ├── abilities.h        # Ability definitions
│   │   ├── item.h             # Item definitions
│   │   ├── trainerclass.h     # Trainer class definitions
│   │   └── *.h                # Other header files
│   └── battle.h               # Battle-related constants
└── ... (other project files)
```

## 🚀 First-Time Usage

### Editing Pokemon Data

1. **Navigate to Species**
   - Click the **"Species"** button in the navigation pane
   - Browse or search for a Pokemon species
   - Click on a species to open the detail editor

2. **Make Your First Edit**
   - Go to the **"Overview"** tab
   - Try changing a Pokemon's base stats (HP, Attack, etc.)
   - Click **"Preview"** to see the changes
   - Review the file modifications in the preview dialog
   - Click **"Save"** to apply the changes

3. **Explore Other Features**
   - Check the **"TM/HM"** tab for move compatibility
   - Visit the **"Tutor Moves"** tab for move tutor settings
   - View **"Level-up"**, **"Evolutions"**, and **"Egg Moves"** sections

### Editing Game Configuration

1. **Navigate to Config**
   - Click the **"Config"** button in the navigation pane
   - Toggle game features like Fairy Type implementation
   - Enable/disable the level cap system

2. **Modify Settings**
   - Use toggle switches to enable/disable features
   - Preview changes before saving
   - Apply configuration to `include/config.h`

### Editing Encounter Data

1. **Navigate to Encounters**
   - Click the **"Encounters"** button in the navigation pane
   - Browse encounter areas like "Route 31", "Mt. Moon", etc.

2. **Edit Wild Pokemon**
   - Select an area from the left panel
   - Modify grass encounters (morning/day/night)
   - Adjust water encounters (surfing, fishing, rock smash)
   - Set encounter rates and probabilities
   - Use the Refresh button to reload data
   - Save changes to update encounter tables

### Editing Trainer Data

1. **Navigate to Trainers**
   - Click the **"Trainers"** button in the navigation pane
   - The list shows all trainers with their class and party size

2. **Edit a Trainer**
   - Select a trainer from the list
   - Modify trainer details, class, AI flags, items, etc.
   - Edit individual Pokemon in their party
   - Use **"Preview"** and **"Save"** to apply changes

### Editing Items and Mart Inventories

1. **Navigate to Items**
   - Adjust item prices in the detail panel; Preview and Save writes `.price = N` to `data/itemdata/itemdata.c`
2. **Mart Items (if present)**
   - If `armips/asm/custom/mart_items.s` exists, expand Mart Items
   - Choose a shop section by name or the “General Poké Mart Table”
   - Edit items and badge gates (General)
   - Preview then Save; only the selected section is updated and whitespace is preserved

## ⚡ File Monitoring

HG Engine Editor automatically monitors your project files for external changes:

- **Detection**: Changes to files in `armips/data` are detected in real-time
- **Notification**: You'll see a dialog asking if you want to reload data
- **Choice**: You can reload to get the latest changes or ignore them
- **Safety**: Your unsaved edits are preserved when reloading

## 🔧 Configuration

### Keyboard Shortcuts

- `Ctrl+S` - Save changes in editor pages

### Settings and Preferences

Currently, HG Engine Editor uses sensible defaults for all operations. Future versions may include:
- Custom file path mappings
- UI theme preferences
- Auto-save intervals
- Backup retention settings

## 🔍 Troubleshooting First Launch

### "No Species Found"
- Verify your project path is correct
- Ensure `include/constants/species.h` exists and contains species definitions
- Check that the file follows the expected format with `#define SPECIES_*` entries

### "Empty Lists"
- Confirm the expected data files exist in `armips/data/`
- Check file permissions and read access
- Verify file format matches HG Engine standards

### "Installation Failed"
- Ensure you have administrator privileges
- Check that the certificate was installed correctly
- Try the PowerShell installation script if available

## 📚 Next Steps

Now that you have HG Engine Editor set up, explore these sections:

- **[User Interface Guide](User-Interface.md)** - Learn about all UI elements
- **[Species Editing](Species-Editing.md)** - Master Pokemon data editing
- **[Data Mapping](Data-Mapping.md)** - Understand file structures
- **[FAQ](FAQ.md)** - Get answers to common questions

## 🆘 Getting Help

If you encounter issues:

1. Check the **[Troubleshooting](Troubleshooting.md)** section
2. Look through the **[FAQ](FAQ.md)**
3. Include your crash logs (found in app LocalState) when reporting issues
4. Provide your Windows version and HG Engine Editor version number

---

**Happy ROM hacking!** 🎮
