# Data Mapping and Technical Reference

This comprehensive technical reference explains how HG Engine Editor maps Pokemon and trainer data to HG Engine project files, including file structures, parsing logic, and data relationships.

## 📁 Project Structure Overview

HG Engine Editor is designed to work with HG Engine projects, which follow a specific directory structure and file organization. Understanding this structure is crucial for effective use of the GUI.

### Required Directory Structure

```
Your-HG-Engine-Project/
├── armips/
│   ├── data/
│   │   ├── mondata.s              # Pokemon species data
│   │   ├── levelupdata.s          # Level-up moves
│   │   ├── evodata.s              # Evolution data
│   │   ├── eggmoves.s             # Egg moves
│   │   ├── tmlearnset.txt         # TM/HM compatibility
│   │   ├── tutordata.txt          # Move tutor data
│   │   └── trainers/
│   │       └── trainers.s         # Trainer data
│   └── include/
│       └── constants.s            # Assembly constants
├── data/
│   ├── BaseExperienceTable.c      # Base experience values
│   └── HiddenAbilityTable.c       # Hidden abilities
├── include/
│   ├── constants/
│   │   ├── species.h              # Pokemon species definitions
│   │   ├── moves.h                # Move definitions
│   │   ├── abilities.h            # Ability definitions
│   │   ├── item.h                 # Item definitions
│   │   ├── trainerclass.h         # Trainer class definitions
│   │   └── *.h                    # Other header files
│   └── battle.h                   # Battle-related constants
└── src/                          # Source code (if present)
```

## 🗂️ File Format Specifications

### Assembly Files (.s)

HG Engine uses assembly macros for most data structures. These follow specific formatting rules:

#### Basic Syntax Rules

- **Comments**: `//` for single line, `/* */` for multi-line
- **Macros**: Custom assembly macros defined in the project
- **Constants**: `#define` directives from header files
- **Labels**: Named sections ending with `:`
- **Directives**: `.equ`, `.include`, etc.

#### Common Macro Patterns

```
macro_name parameter1, parameter2, parameter3
    field_name value1
    field_name value2
    field_name value3
endmacro_name
```

### C Files (.c)

Used for array-based data structures:

```c
const uint16_t BaseExperienceTable[] = {
    [SPECIES_BULBASAUR] = 64,
    [SPECIES_IVYSAUR] = 142,
    [SPECIES_VENUSAUR] = 236,
    // ...
};
```

### Text Files (.txt)

Used for compatibility matrices and tutor data:

```
TUTOR_001: MOVE_FIRE_PUNCH 10
TUTOR_002: MOVE_THUNDER_PUNCH 10
```

## 🐾 Pokemon Data Mapping

### Species Definitions (`include/constants/species.h`)

**File Purpose**: Defines all Pokemon species constants

**Format**:
```c
#define SPECIES_BULBASAUR 1
#define SPECIES_IVYSAUR 2
#define SPECIES_VENUSAUR 3
// ...
#define SPECIES_NONE 0
```

**HG Engine Editor Usage**:
- Parsed to populate species dropdown lists
- Used for species ID to name mapping
- Validates species references in other files

### Pokemon Data (`armips/data/mondata.s`)

**File Purpose**: Core Pokemon species statistics and properties

**Structure**:
```
mondata SPECIES_BULBASAUR, "Bulbasaur"
    basestats 45, 49, 49, 45, 65, 65
    types TYPE_GRASS, TYPE_POISON
    catchrate 45
    baseexp 64
    evyields 0, 0, 0, 0, 1, 0
    items ITEM_NONE, ITEM_NONE
    genderratio 31
    eggcycles 20
    basefriendship 70
    growthrate GROWTH_MEDIUM_SLOW
    egggroups EGG_GROUP_MONSTER, EGG_GROUP_GRASS
    abilities ABILITY_OVERGROW, ABILITY_NONE
    runchance 0
    mondexclassification "Seed", 0
    mondexentry "A strange seed was planted on its back at birth. The plant sprouts and grows with this POKéMON.", 0
    mondexheight "2'04\"", 0
    mondexweight "15.2 lbs", 0
```

**Field Mapping**:
- **basestats**: HP, Attack, Defense, Speed, SpAttack, SpDefense
- **types**: Primary type, Secondary type
- **catchrate**: Capture probability (0-255)
- **baseexp**: Experience yield
- **evyields**: EV distribution (hp, atk, def, spd, spatk, spdef)
- **items**: Wild-held items
- **genderratio**: Gender distribution
- **eggcycles**: Breeding cycles
- **basefriendship**: Starting friendship
- **growthrate**: Experience curve
- **egggroups**: Breeding compatibility
- **abilities**: Regular abilities
- **runchance**: Wild battle escape chance

### Level-up Moves (`armips/data/levelupdata.s`)

**File Purpose**: Moves learned by leveling up

**Structure**:
```
levelup SPECIES_BULBASAUR
    learnset MOVE_TACKLE, 1
    learnset MOVE_GROWL, 1
    learnset MOVE_VINE_WHIP, 13
    learnset MOVE_SLEEP_POWDER, 13
    learnset MOVE_TAKE_DOWN, 27
    ...
terminatelearnset
```

**Field Mapping**:
- **learnset**: Move constant, Level number
- **terminatelearnset**: End of learnset marker

### Evolution Data (`armips/data/evodata.s`)

**File Purpose**: Evolution requirements and chains

**Structure**:
```
evodata SPECIES_BULBASAUR
    evolution EVO_LEVEL, 16, SPECIES_IVYSAUR
terminateevodata
```

**Evolution Methods**:
- **EVO_LEVEL**: Level threshold
- **EVO_ITEM**: Required item
- **EVO_TRADE**: Trading (with optional held item)
- **EVO_FRIENDSHIP**: Friendship threshold
- **EVO_LEVEL_ATK_GT_DEF**: Level with stat condition
- **EVO_LEVEL_ATK_LT_DEF**: Level with stat condition
- **EVO_LEVEL_SILCOON**: Level for specific evolution path
- **EVO_BEAUTY**: Beauty condition
- **EVO_MAP**: Specific map location
- **EVO_ITEM_MALE/FEMALE**: Gender-specific item evolution
- **EVO_LEVEL_DAY/NIGHT**: Time-based evolution
- **EVO_LEVEL_DUSK**: Dusk time evolution
- **EVO_ITEM_HOLD_DAY**: Item held during day
- **EVO_MOVE**: Specific move known
- **EVO_LEVEL_FEMALE/MALE**: Gender-specific level evolution
- **EVO_LEVEL_RAIN**: Weather-based evolution
- **EVO_SPECIFIC_MON**: Evolve near specific Pokemon

### Egg Moves (`armips/data/eggmoves.s`)

**File Purpose**: Moves that can be inherited through breeding

**Structure**:
```
eggmoveentry SPECIES_BULBASAUR
    eggmove MOVE_AMNESIA
    eggmove MOVE_CHARM
    eggmove MOVE_CURSE
    eggmove MOVE_GRASS_WHISTLE
    eggmove MOVE_PETAL_DANCE
    eggmove MOVE_RAZOR_WIND
```

**Field Mapping**:
- **eggmoveentry**: Species identifier
- **eggmove**: Move that can be inherited

### TM/HM Compatibility (`armips/data/tmlearnset.txt`)

**File Purpose**: Technical Machine and Hidden Machine compatibility

**Structure**:
```
TM001: MOVE_FOCUS_PUNCH
TM002: MOVE_DRAGON_CLAW
TM003: MOVE_WATER_PULSE
...
```

**Compatibility Logic**:
- Each TM/HM line defines the move it teaches
- Species compatibility is managed separately through data structures
- HG Engine Editor parses the header list and manages compatibility matrices

### Move Tutor Data (`armips/data/tutordata.txt`)

**File Purpose**: Move tutor configurations and costs

**Structure**:
```
TUTOR_001: MOVE_FIRE_PUNCH 10
TUTOR_002: MOVE_THUNDER_PUNCH 10
TUTOR_003: MOVE_ICE_PUNCH 10
...
```

**Field Mapping**:
- **TUTOR_XXX**: Tutor identifier
- **MOVE_XXX**: Move taught by tutor
- **Number**: Battle Points cost

### Base Experience Table (`data/BaseExperienceTable.c`)

**File Purpose**: Experience yields for each species

**Structure**:
```c
const uint16_t BaseExperienceTable[] = {
    [SPECIES_BULBASAUR] = 64,
    [SPECIES_IVYSAUR] = 142,
    [SPECIES_VENUSAUR] = 236,
    // ...
};
```

### Hidden Ability Table (`data/HiddenAbilityTable.c`)

**File Purpose**: Hidden abilities for each species

**Structure**:
```c
const uint16_t HiddenAbilityTable[] = {
    [SPECIES_BULBASAUR] = ABILITY_CHLOROPHYLL,
    // ...
};
```

## 👥 Trainer Data Mapping

### Trainer Data (`armips/data/trainers/trainers.s`)

**File Purpose**: Complete trainer configurations and parties

**Structure**:
```
trainerdata 1, "YOUNGSTER_JOE"
    trainerclass TRAINERCLASS_YOUNGSTER
    nummons 2
    aiflags AI_FLAG_CHECK_BAD_MOVE
    battletype BATTLE_TYPE_NORMAL
    item ITEM_POTION
    item ITEM_NONE

party 1
    // mon 0
    mon 0
    ivs 10
    abilityslot 1
    level 5
    item ITEM_NONE
    move MOVE_TACKLE
    move MOVE_LEER
    move MOVE_NONE
    move MOVE_NONE
    nature NATURE_HARDY
    form 0
    ball ITEM_POKE_BALL
    shiny_lock false
    pp 0
    ability ABILITY_NONE
    ballseal 0
    ivnums 10,10,10,10,10,10
    evnums 0,0,0,0,0,0
    status 0
    hp 0
    atk 0
    def 0
    speed 0
    spatk 0
    spdef 0
    types TYPE_NONE, TYPE_NONE
    ppcounts 0,0,0,0
    additionalflags 0
endparty
```

**Trainer Header Fields**:
- **trainerdata**: ID, Name
- **trainerclass**: Trainer class constant
- **nummons**: Number of Pokemon
- **aiflags**: AI behavior flags
- **battletype**: Battle format
- **item**: Held items (up to 4)

**Party Fields**:
- **mon**: Pokemon index in party
- **ivs**: Individual Values (0-31 or 255 for random)
- **abilityslot**: Which ability to use (1 or 2)
- **level**: Pokemon level
- **item**: Held item
- **move**: Moves known (up to 4)
- **nature**: Pokemon nature
- **form**: Alternate form number
- **ball**: Pokeball type
- **shiny_lock**: Shiny status control
- **pp**: PP configuration
- **ability**: Specific ability override
- **ballseal**: Ball seal item
- **ivnums**: Individual IV values
- **evnums**: Effort Values
- **status**: Battle status condition
- **hp/atk/def/speed/spatk/spdef**: Current stat values
- **types**: Type override
- **ppcounts**: Individual PP counts
- **additionalflags**: Special flags

## 🔧 Constants and Definitions

### Type Constants (`include/battle.h`)

```c
#define TYPE_NONE 255
#define TYPE_NORMAL 0
#define TYPE_FIRE 1
#define TYPE_WATER 2
// ...
```

### Ability Constants (`include/constants/ability.h` or `asm/include/abilities.inc`)

```c
#define ABILITY_NONE 0
#define ABILITY_STENCH 1
#define ABILITY_DRIZZLE 2
// ...
```

### Item Constants (`include/constants/item.h`)

```c
#define ITEM_NONE 0
#define ITEM_MASTER_BALL 1
#define ITEM_ULTRA_BALL 2
// ...
```

### Move Constants (`include/constants/moves.h`)

```c
#define MOVE_NONE 0
#define MOVE_POUND 1
#define MOVE_KARATE_CHOP 2
// ...
```

### Growth Rate Constants (`armips/include/constants.s`)

```c
.equ GROWTH_MEDIUM_FAST, 0
.equ GROWTH_ERRATIC, 1
.equ GROWTH_FLUCTUATING, 2
.equ GROWTH_MEDIUM_SLOW, 3
.equ GROWTH_FAST, 4
.equ GROWTH_SLOW, 5
```

### AI Flag Constants (`armips/include/constants.s`)

```c
.equ AI_FLAG_CHECK_BAD_MOVE, 1 << 0
.equ AI_FLAG_TRY_TO_FAINT, 1 << 1
.equ AI_FLAG_CHECK_VIABILITY, 1 << 2
// ...
```

## 🔄 Data Parsing Logic

### Regex Patterns Used

HG Engine Editor uses sophisticated regex patterns to parse various data formats:

```csharp
// Define constants
private static readonly Regex DefineRegex = new(
    @"^\s*#define\s+(?<name>[A-Z0-9_]+)\s+(?<value>[-+*/()A-Z0-9_]+)\s*$",
    RegexOptions.Compiled);

// Pokemon data blocks
private static readonly Regex MondataRegex = new(
    @"mondata\s+(?<species>[A-Z0-9_]+)\s*,\s*""(?<name>[^""]*)""(?<body>.*?)(\n\nmondata|\n\s*$)",
    RegexOptions.Singleline);

// Evolution data
private static readonly Regex EvolutionRegex = new(
    @"evolution\s+(?<method>[A-Z0-9_]+)\s*,\s*(?<param>\d+)\s*,\s*(?<target>[A-Z0-9_]+)");
```

### Parsing Strategy

1. **File Discovery**: Scan project structure for required files
2. **Constant Loading**: Parse all header files for constants
3. **Data File Parsing**: Extract structured data using regex patterns
4. **Validation**: Cross-reference constants and validate data integrity
5. **Caching**: Store parsed data for GUI operations
6. **Change Tracking**: Monitor files for external modifications

### Error Handling

- **Missing Files**: Graceful fallback with clear error messages
- **Parse Errors**: Detailed error reporting with line numbers
- **Validation Failures**: Highlight invalid data with suggestions
- **File Conflicts**: Detect and resolve simultaneous modifications

## 📊 Data Relationships

### Cross-References

- **Species → Moves**: Learnsets, egg moves, TM/HM compatibility
- **Species → Abilities**: Regular and hidden abilities
- **Species → Items**: Wild-held items
- **Species → Evolutions**: Evolution chains and methods
- **Trainers → Species**: Pokemon in trainer parties
- **Moves → Types**: Move type effectiveness
- **Items → Effects**: Item functionality and battle effects

### Dependencies

- **Constants First**: All constants must be loaded before data parsing
- **Species Priority**: Pokemon species definitions loaded before other data
- **Reference Validation**: All referenced constants must exist
- **Fallback Values**: Default values for missing optional data

## 🔍 Data Validation Rules

### Pokemon Data Validation

- **Stat Ranges**: 0-255 for all base stats
- **Type Validation**: Must be valid type constants
- **Ability Checks**: Abilities must exist in project
- **Item Verification**: Items must be defined
- **Growth Rate**: Must be valid growth rate constant
- **Egg Groups**: Must be valid egg group constants

### Trainer Data Validation

- **Species Existence**: All Pokemon species must be defined
- **Move Compatibility**: Moves must be valid and learnable
- **Level Ranges**: 1-100 for Pokemon levels
- **IV Validation**: 0-31 or 255 for Individual Values
- **AI Flag Checks**: All AI flags must be valid constants
- **Item Availability**: Items must exist in project

### File Structure Validation

- **Required Files**: Critical files must exist
- **Format Compliance**: Files must follow expected syntax
- **Encoding**: UTF-8 encoding required
- **Line Endings**: Consistent line ending format

This technical reference provides the foundation for understanding how HG Engine Editor interacts with HG Engine project data. Understanding these data mappings and relationships is essential for effective ROM hacking and data editing.
