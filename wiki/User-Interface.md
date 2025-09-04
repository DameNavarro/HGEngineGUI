# User Interface Guide

This comprehensive guide explains every element of the HG Engine Editor interface, helping you navigate and utilize all features effectively.

## 🏠 Main Window Layout

### Navigation Pane
Located on the left side of the main window:

- **Project** - Project selection and status (default landing page)
- **Species** - Pokemon species editor
- **Trainers** - Trainer party editor
- **Config** - Game configuration toggles (level caps, fairy typing, etc.)
- **Encounters** - Wild Pokemon encounter data editor
- **Items** - Item price and Mart inventory editor (custom addon aware) 

### Status Bar
Located at the bottom of the window:

- **Project Path** - Currently selected project directory
- **Message** - Current operation status or information
- **Timing** - Performance information for operations (in milliseconds)

## 📄 Project Page

The starting page where you configure your HG Engine project.

### Elements:

- **Pick Folder Button** - Opens folder browser to select your HG Engine root directory
- **Current Path Display** - Shows selected project path or "(not selected)"
- **Species Count** - Displays "Species: X" where X is the number of detected Pokemon species
- **Change Log** - Lists recent file modifications with options to:
  - **Open** - Opens the modified file in your default editor
  - **Restore** - Reverts the file to its backup (.bak) version

### File Change Detection

- Monitors `armips/data` folder for external modifications
- Shows dialog when changes are detected:
  - **"External change detected"** - File path and reload prompt
  - **"Reload"** - Refresh all cached data
  - **"Ignore"** - Dismiss the notification

## 🐾 Species Page

The comprehensive Pokemon species editor with multiple tabs.

### Species List
- **Left Panel** - Scrollable list of all Pokemon species
- **Search/Filter** - Type to filter species by name
- **Selection** - Click any species to open the detail editor

### Overview Tab

Complete Pokemon data editing interface:

#### Basic Information
- **Name** - Pokemon species name (read-only, from project data)
- **National Dex #** - National Pokedex number (read-only)
- **Internal ID** - Internal species ID (read-only)

#### Stats Section
- **HP, Attack, Defense, Sp. Attack, Sp. Defense, Speed** - Base stat values (0-255)
- **Total** - Automatic calculation of all six stats

#### Types and Abilities
- **Type 1/Type 2** - Primary and secondary types (dropdown selection)
- **Ability 1/Ability 2** - Regular abilities (dropdown from project abilities)
- **Hidden Ability** - Hidden ability (dropdown selection)

#### Experience and Capture
- **Catch Rate** - Base catch rate (0-255)
- **Base EXP** - Base experience yield
- **EV Yields** - Effort value distribution (HP/Atk/Def/Spd/SpAtk/SpDef)

#### Breeding and Growth
- **Gender Ratio** - Gender distribution percentage
- **Egg Cycles** - Steps needed to hatch (0-255)
- **Base Friendship** - Base friendship value (0-255)
- **Growth Rate** - Experience growth curve (dropdown)
- **Egg Group 1/2** - Breeding compatibility groups

#### Pokedex Information
- **Classification** - Pokedex category (e.g., "Seed Pokemon")
- **Height** - Pokemon height in feet and inches (formatted text)
- **Weight** - Pokemon weight in pounds (formatted text)
- **Pokedex Entry** - Multi-line description text

#### Items
- **Wild Item 1/2** - Items held by wild Pokemon (50% chance each)

#### Miscellaneous
- **Run Chance** - Chance to run from battle (0-255)

### TM/HM Tab

Technical Machine and Hidden Machine compatibility editor:

- **Available Moves** - List of all TM/HM moves (TM001-TM092, HM001-HM008)
- **Search** - Filter moves by name or TM number
- **Multi-select** - Hold Ctrl to select/deselect multiple moves
- **Compatibility** - Shows which moves are currently compatible
- **Preview/Save** - Review and apply TM/HM changes

### Tutor Moves Tab

Move tutor compatibility and pricing:

- **Available Tutors** - List of move tutors with move names and costs
- **Search** - Filter tutors by name or move
- **Multi-select** - Select multiple tutors for batch operations
- **Cost Display** - Shows BP (Battle Points) cost for each tutor
- **Compatibility Management** - Add/remove tutor compatibility

### Level-up, Evolutions, and Egg Moves Tabs

- **Level-up Moves** - Shows moves learned by level (read-only summary)
- **Evolutions** - Displays evolution chains (read-only summary)
- **Egg Moves** - Lists moves that can be inherited (read-only summary)
- **Preview/Save** - Available for all three data types

## 👥 Trainers Page

Complete trainer party management interface.

### Trainer List
- **Left Panel** - List of all trainers showing:
  - Trainer name
  - Trainer class
  - Number of Pokemon in party
- **Search/Filter** - Find trainers by name or class

### Trainer Detail Editor

#### Trainer Header Information
- **Name** - Trainer's display name
- **Class** - Trainer class (dropdown from project classes)
- **Party Size** - Number of Pokemon (automatically calculated)
- **AI Flags** - Special AI behaviors (multi-select checkboxes)
- **Battle Type** - Battle configuration (dropdown)
- **Items** - Held items (up to 4 items)

#### Pokemon Party Editor

For each Pokemon in the trainer's party:

##### Basic Pokemon Data
- **Index** - Position in party (0-5)
- **Level** - Pokemon level (1-100)
- **Species** - Pokemon species (dropdown selection)
- **Form** - Alternate form number (0+)

##### Advanced Configuration
- **Ability Slot** - Which ability to use (1 or 2)
- **IVs** - Individual Values (0-31, or 255 for random)
- **Item** - Held item (dropdown from project items)
- **Nature** - Pokemon nature (dropdown selection)
- **Ball** - Pokeball type (dropdown selection)

##### Moves
- **Move 1-4** - Moves known by the Pokemon (dropdown selection)
- **PP Values** - Power Points for each move (individual or shared)

##### Technical Details
- **Shiny Lock** - Prevents/requires shiny (true/false/random)
- **Nickname** - Custom nickname (optional)
- **PP** - PP configuration (individual/shared/max)
- **Ball Seal** - Ball seal item ID
- **IV Numbers** - Individual IV specification
- **EV Numbers** - Effort Values specification

##### Advanced Flags
- **Status** - Battle status condition
- **HP/Stats** - Current stat values (for specific battle states)
- **Additional Flags** - Special battle flags and behaviors

## ⚙️ Config Page

Game configuration editor for toggling various HG Engine features and settings.

### Configuration Options

#### Game Features
- **Fairy Type Implementation** - Enables/disables Fairy type in the game
- **Level Cap** - Enables/disables the level cap system

#### Additional Settings
- **Level Cap Variable** - Memory address for level cap variable (automatically managed)

### Controls
- **Toggle Switches** - Enable/disable each configuration option
- **Preview Changes** - Shows file modifications before saving
- **Save** - Applies configuration changes to `include/config.h`
- **Auto-backup** - Creates `.bak` files before modifications

## 🐾 Encounters Page

Wild Pokemon encounter data editor for managing grass, water, and headbutt encounters.

### Layout
- **Left Panel** - Scrollable list of encounter areas (routes, caves, etc.)
- **Right Panel** - Detailed encounter editor for selected area

### Encounter Area List
- **Search/Filter** - Find areas by name or ID
- **Route Names** - Shows actual route names like "Route 31" instead of "Area 1"
- **Area Labels** - Displays descriptive labels from encounter data comments

### Encounter Editor

#### Basic Settings
- **Walk Rate** - Base encounter rate for walking in grass (0-255)
- **Surf Rate** - Encounter rate while surfing (0-255)
- **Rock Smash Rate** - Encounter rate for Rock Smash (0-255)
- **Old/Good/Super Rod Rates** - Fishing encounter rates (0-255)

#### Grass Encounters
- **Time-based Slots** - Morning, Day, Night, Hoenn, Sinnoh encounter slots
- **Pokemon Selection** - Dropdown selection with species sprites
- **Level Ranges** - Individual level settings for each slot
- **Probability Editing** - Customizable encounter probabilities

#### Water Encounters
- **Surf Encounters** - Pokemon encountered while surfing (5 slots)
- **Rock Smash Encounters** - Pokemon from breaking rocks (2 slots)
- **Fishing Encounters** - Old, Good, and Super Rod Pokemon (5 slots each)
- **Level Ranges** - Min/max level for each water encounter

#### Special Features
- **Swarm Pokemon** - Special swarm encounter Pokemon for grass, surf, and fishing
- **Form Support** - Pokemon forms using `monwithform` and `encounterwithform` macros
- **Sprite Display** - Shows Pokemon sprite next to species selector

### Controls
- **Save** - Located at top of page, applies all encounter changes
- **Refresh** - Reloads encounter data from project files
- **Preview** - Shows file modifications before saving
- **Compact Layout** - Optimized 4-column grid for grass encounters

## 🧰 Items Page

Item data editors. Contains two parts:

### Item Prices
- Left list: all `ITEM_*` macros
- Detail: current price field
- Actions: Preview/Save writes to `data/itemdata/itemdata.c` by replacing `.price = N` in the `[ITEM_*]` entry

### Mart Items (custom addon)
- Enabled only if `armips/asm/custom/mart_items.s` exists
- Section selector shows friendly shop names (e.g., “Cherrygrove City 2nd Clerk → Violet City 2nd Clerk”) and General Poké Mart Table
- Editor:
  - For General table: item with badge gate per row
  - For specific shops: item list terminated by `0xFFFF`
- Preview: Shows diff against `mart_items.s`
- Save: Rewrites only the targeted section
  - General table: only the body between `/* General Poké Mart Table */` and the next comment/.org/.close
  - Shops: the block starting at `.org <address>` up to (not including) the next `.org`
  - Whitespace/blank lines are preserved

## 🎮 Control Elements

### Dialogs and Popups

#### Preview Dialog
- **File Changes** - Shows before/after comparison of modified files
- **Line Numbers** - Reference for changed lines
- **Syntax Highlighting** - Color-coded assembly syntax
- **Action Buttons**:
  - **Save** - Apply changes to files
  - **Cancel** - Discard changes

#### Change Log Dialog
- **Modified Files** - List of recently changed files
- **Timestamps** - When changes were made
- **File Sizes** - Size information for change tracking

#### Error and Crash Dialogs
- **Exception Details** - Technical error information
- **Log File Location** - Path to crash logs
- **Contact Information** - Bug reporting guidance

### Context Menus

- **Right-click on species** - Quick actions (not implemented in current version)
- **Right-click on trainers** - Quick actions (not implemented)

## ⌨️ Keyboard Shortcuts

- `Ctrl+S` - Save changes in editor pages
- `Ctrl+F` - Focus search/filter boxes (when available)
- `F5` - Refresh data (not implemented in current version)

## 🎨 Visual Indicators

### Status Colors
- **Green** - Success operations
- **Yellow** - Warning conditions
- **Red** - Error states

### Loading States
- **Progress Rings** - Show during data loading operations
- **Disabled Controls** - UI elements locked during operations
- **Status Messages** - Operation progress updates

### Data States
- **Modified Indicators** - Visual cues for unsaved changes
- **Validation Colors** - Red borders for invalid data
- **Required Fields** - Asterisks (*) for mandatory fields

## 📱 Responsive Design

The interface adapts to different window sizes:

- **Wide Windows** - Side-by-side panels for list and editor
- **Narrow Windows** - Stacked layout with collapsible panels
- **Minimum Size** - 800x600 pixels for optimal usability

## 🌙 Theme Support

Currently uses system theme (light/dark mode based on Windows settings):

- **Light Theme** - Clean, bright interface
- **Dark Theme** - Easy on the eyes for long editing sessions
- **High Contrast** - Accessibility support for visual impairments

## 🔧 Accessibility Features

- **Keyboard Navigation** - Full keyboard accessibility
- **Screen Reader Support** - Proper labeling and descriptions
- **High DPI Support** - Scales properly on high-resolution displays
- **Color Blind Support** - High contrast elements and clear indicators

## 📊 Performance Indicators

- **Load Times** - Displayed in status bar for operations
- **Memory Usage** - Monitored for large projects
- **File Operation Speed** - Performance feedback for save operations

## 🔄 Real-time Features

- **Auto-refresh** - Data updates when external changes detected
- **Live validation** - Immediate feedback on data entry
- **Incremental search** - Real-time filtering as you type
- **Auto-save prompts** - Prevents data loss on window close

This interface guide covers all current features. As HG Engine Editor evolves, this documentation will be updated to reflect new capabilities and improvements.
