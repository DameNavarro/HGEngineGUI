# Species Editing Guide

This comprehensive guide covers all aspects of Pokemon species data editing in HG Engine Editor, from basic stats to complex move learnsets and evolution chains.

## 🏗️ Understanding Pokemon Data Structure

Each Pokemon species in HG Engine projects is defined by multiple data files and structures. HG Engine Editor provides a unified interface to edit all these interconnected data sources.

### Core Data Components

1. **Base Stats & Properties** - `armips/data/mondata.s`
2. **Level-up Moves** - `armips/data/levelupdata.s`
3. **Evolution Data** - `armips/data/evodata.s`
4. **Egg Moves** - `armips/data/eggmoves.s`
5. **TM/HM Compatibility** - `armips/data/tmlearnset.txt`
6. **Tutor Moves** - `armips/data/tutordata.txt`
7. **Base Experience** - `data/BaseExperienceTable.c`
8. **Hidden Abilities** - `data/HiddenAbilityTable.c`

## 📊 Overview Tab - Complete Pokemon Profile

### Basic Information

- **Species Name**: Read-only display from project constants
- **National Dex Number**: Read-only Pokédex position
- **Internal ID**: Technical species identifier used in code

### Base Stats (0-255 range)

The six fundamental stats that define a Pokemon's combat capabilities:

- **HP (Hit Points)**: Health pool, determines survivability
- **Attack**: Physical move damage modifier
- **Defense**: Physical damage resistance
- **Special Attack**: Special move damage modifier
- **Special Defense**: Special damage resistance
- **Speed**: Turn order in battle
- **Total**: Automatic sum of all six stats (display only)

### Type System

- **Type 1**: Primary elemental type (required)
- **Type 2**: Secondary elemental type (optional, can be TYPE_NONE)
- **Type Effectiveness**: Determined by type matchups in battle calculations

### Abilities

- **Ability 1/2**: Regular abilities available to the Pokemon
- **Hidden Ability**: Special ability only obtainable through specific methods
- **Ability Slot**: Determines which ability is active (used in trainer Pokemon)

### Experience & Capture Mechanics

- **Catch Rate**: Base probability of capturing (0-255, higher = easier)
- **Base EXP**: Experience points awarded when defeated (used in EXP calculations)
- **EV Yields**: Effort Values distributed to defeated Pokemon (1-3 per stat, total ≤ 3)

### Breeding & Growth

- **Gender Ratio**: Percentage chance of male Pokemon (0 = always female, 254 = always male, 255 = genderless)
- **Egg Cycles**: Time to hatch from egg (1 cycle = 255 steps, so 1 cycle = ~4 minutes)
- **Base Friendship**: Starting friendship value (0-255, affects some mechanics)
- **Growth Rate**: Experience required per level (Erratic, Fast, Medium Fast, Medium Slow, Slow, Fluctuating)
- **Egg Groups**: Breeding compatibility (1-2 groups, determines breeding possibilities)

### Pokédex Information

- **Classification**: Category name (e.g., "Seed Pokemon", "Flame Pokemon")
- **Height**: Formatted as feet'inches" (e.g., "2'04\"")
- **Weight**: Formatted as pounds with decimal (e.g., "15.2 lbs")
- **Entry**: Multi-line description text (typically 2-3 sentences)

### Wild Encounter Items

- **Wild Item 1/2**: Items that can be held by wild Pokemon (50% chance each)
- **Item Availability**: Only items defined in the project are available

### Miscellaneous

- **Run Chance**: Probability of successfully running from wild battles (0-255)

## ⚔️ TM/HM Compatibility System

### Understanding TM/HM Data

- **TM**: Technical Machines (TM001-TM092) - single use, teach moves
- **HM**: Hidden Machines (HM001-HM008) - reusable, often for field moves
- **Compatibility**: Binary yes/no for each species-move combination

### Editing TM/HM Compatibility

1. **Browse Available Moves**
   - Scroll through complete list of TMs and HMs
   - Each entry shows TM/HM number, move name, and type
   - Compatible moves are highlighted

2. **Search and Filter**
   - Type in search box to find specific moves
   - Filter by move name, TM number, or type

3. **Modify Compatibility**
   - Click to toggle individual moves
   - Hold Ctrl to select multiple moves
   - Use "Select All" for batch operations

4. **Preview Changes**
   - Review which moves are being added/removed
   - See the exact file modifications
   - Verify compatibility changes

### TM/HM File Structure

```
TM001: MOVE_TACKLE
TM002: MOVE_SWIFT
...
```

Each line represents one TM/HM with its corresponding move. Species compatibility is managed separately in the data structure.

## 🎯 Move Tutor System

### Tutor Mechanics

- **Battle Points (BP)**: Currency system for purchasing tutor moves
- **Cost Structure**: Each tutor has an individual BP cost
- **Compatibility**: Species-specific availability

### Tutor Data Format

```
TUTOR_001: MOVE_FIRE_PUNCH 10
TUTOR_002: MOVE_THUNDER_PUNCH 10
TUTOR_003: MOVE_ICE_PUNCH 10
...
```

### Editing Tutor Compatibility

1. **View Available Tutors**
   - List shows tutor name, move taught, and BP cost
   - Compatible tutors are highlighted

2. **Search Functionality**
   - Filter by tutor name, move name, or cost

3. **Modify Access**
   - Toggle individual tutor compatibility
   - Batch select multiple tutors

4. **Cost Management**
   - View current BP costs
   - Costs are project-defined and not editable in GUI

## 📈 Level-up Move System

### Level-up Learnsets

- **Level-based Learning**: Moves learned at specific levels
- **Multiple Moves per Level**: Pokemon can learn multiple moves at once
- **Technical Format**: Assembly macro structure in `levelupdata.s`

### Learnset Structure

```
levelup SPECIES_BULBASAUR
    learnset MOVE_TACKLE, 1
    learnset MOVE_GROWL, 1
    learnset MOVE_VINE_WHIP, 13
    learnset MOVE_SLEEP_POWDER, 13
    ...
terminatelearnset
```

### Viewing Level-up Moves

- **Read-only Summary**: Current implementation shows moves as text
- **Level Organization**: Moves grouped by level number
- **Move Count**: Total moves in learnset displayed

### Editing Level-up Moves

**Note**: Full GUI editing for level-up moves is planned for future versions. Currently:

- View existing learnsets
- Preview changes (when available)
- Manual editing required for complex modifications

## 🔄 Evolution System

### Evolution Mechanics

- **Evolution Methods**: 20+ different evolution triggers
- **Parameters**: Method-specific requirements (level, item, location, etc.)
- **Multiple Evolutions**: Some species have multiple evolution paths

### Evolution Methods

| Method | Parameter Type | Example |
|--------|---------------|---------|
| EVO_LEVEL | Level number | Evolve at level 16 |
| EVO_ITEM | Item name | Use FIRE_STONE |
| EVO_TRADE | Trade partner | Trade with any Pokemon |
| EVO_FRIENDSHIP | Friendship threshold | High friendship |
| EVO_MAP | Map name | Step on specific map |
| EVO_LEVEL_ATK_GT_DEF | Level with stat comparison | Level 20 if Attack > Defense |
| And many more... | | |

### Evolution Data Structure

```
evodata SPECIES_BULBASAUR
    evolution EVO_LEVEL, 16, SPECIES_IVYSAUR
terminateevodata
```

### Viewing Evolutions

- **Evolution Chains**: Complete evolution lines displayed
- **Method Details**: Specific requirements shown
- **Target Species**: Evolution result displayed

### Editing Evolutions

**Note**: Full GUI editing for evolution data is planned for future versions. Currently:

- View existing evolution chains
- Preview changes (when available)
- Manual editing required for modifications

## 🥚 Egg Move System

### Egg Move Mechanics

- **Breeding Inheritance**: Moves passed down through breeding
- **Compatibility Rules**: Determined by egg groups and species
- **Generation Limit**: Cannot inherit moves from different generations

### Egg Move Structure

```
eggmoveentry SPECIES_BULBASAUR
    eggmove MOVE_AMNESIA
    eggmove MOVE_CHARM
    eggmove MOVE_CURSE
    ...
```

### Viewing Egg Moves

- **Species-specific Lists**: Moves available through breeding
- **Alphabetical Organization**: Moves sorted for easy browsing
- **Move Count**: Total inheritable moves displayed

### Editing Egg Moves

**Note**: Full GUI editing for egg moves is planned for future versions. Currently:

- View existing egg move lists
- Preview changes (when available)
- Manual editing required for modifications

## ⚙️ Advanced Configuration

### Data Validation

- **Range Checking**: Automatic validation of stat ranges (0-255)
- **Type Validation**: Ensures valid type selections
- **Name Validation**: Prevents invalid characters in text fields

### Backup and Safety

- **Automatic Backups**: `.bak` files created before modifications
- **Preview System**: Review changes before applying
- **Change Tracking**: Detailed logs of all modifications

### Performance Considerations

- **Lazy Loading**: Data loaded only when needed
- **Incremental Updates**: Only modified fields are written
- **Memory Management**: Efficient handling of large datasets

## 🔧 Technical Details

### File Format Compatibility

HG Engine Editor is designed to work with HG Engine's specific data formats:

- **Assembly Macros**: Uses `.s` file format with specific macro structures
- **C Arrays**: For experience tables and ability data
- **Text Files**: For TM/HM and tutor compatibility
- **Header Files**: For constant definitions

### Data Dependencies

- **Cross-references**: Types, abilities, items, moves all reference constants
- **Validation**: Ensures referenced constants exist in project
- **Fallbacks**: Graceful handling when data is missing

### Error Handling

- **File Not Found**: Clear error messages for missing data files
- **Parse Errors**: Detailed feedback for malformed data
- **Write Errors**: Protection against file system issues

This comprehensive species editing guide covers all current capabilities and planned features. As HG Engine Editor evolves, this documentation will be updated to reflect new editing capabilities and improvements.
