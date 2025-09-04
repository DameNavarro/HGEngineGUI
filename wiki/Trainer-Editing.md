# Trainer Editing Guide

This comprehensive guide covers all aspects of trainer data editing in HG Engine Editor, including trainer configuration, Pokemon party management, and advanced battle mechanics.

## 👥 Understanding Trainer Data Structure

Trainer data in HG Engine projects is stored in `armips/data/trainers/trainers.s` and consists of multiple interconnected components:

### Core Components

1. **Trainer Header** - Basic trainer information and configuration
2. **Party Data** - Individual Pokemon in the trainer's party
3. **AI Systems** - Battle behavior and decision making
4. **Item Management** - Items held by trainers
5. **Battle Configuration** - Battle type and special rules

## 📋 Trainer List Interface

### Trainer Overview

The main trainer list displays all trainers in the project:

- **Trainer Name** - Display name of the trainer
- **Trainer Class** - Class type (e.g., YOUNGSTER, ELITE_FOUR, RIVAL)
- **Party Size** - Number of Pokemon in party (1-6)
- **Search/Filter** - Find trainers by name or class

### Trainer Selection

- **Click to Edit** - Select any trainer to open the detail editor
- **Quick Navigation** - Jump between trainers in the list
- **Sort Options** - Alphabetical or numerical ordering

## ⚙️ Trainer Header Configuration

### Basic Information

- **Name** - Trainer's display name (editable text field)
- **Class** - Trainer classification from project constants (dropdown selection)
- **Party Size** - Automatically calculated from party data (read-only)

### Battle Configuration

- **Battle Type** - Battle rules and conditions (dropdown)
  - `BATTLE_TYPE_NORMAL` - Standard single battle
  - `BATTLE_TYPE_DOUBLE` - Double battle format
  - `BATTLE_TYPE_MULTI` - Multi-battle (2v2)
  - `BATTLE_TYPE_TAG` - Tag battle system

- **AI Flags** - Advanced battle behavior (multi-select checkboxes)
  - `AI_FLAG_CHECK_BAD_MOVE` - Avoid ineffective moves
  - `AI_FLAG_TRY_TO_FAINT` - Prioritize fainting opponents
  - `AI_FLAG_CHECK_VIABILITY` - Evaluate move effectiveness
  - `AI_FLAG_SETUP_FIRST_TURN` - Set up conditions first
  - `AI_FLAG_RISKY` - Use risky but powerful moves
  - And many more project-specific AI behaviors

### Trainer Items

- **Item Slots** - Up to 4 items carried by the trainer
- **Item Selection** - Dropdown from project item constants
- **Item Usage** - Items available during battle (depending on AI)

## 🏆 Pokemon Party Management

### Party Structure

Each trainer can have 1-6 Pokemon with detailed configurations:

- **Index** - Position in party (0-5)
- **Species** - Pokemon species (dropdown from all available)
- **Level** - Pokemon level (1-100)
- **Form** - Alternate form number (0 for base form)

### Advanced Pokemon Configuration

#### Stats and Abilities

- **Ability Slot** - Which ability to use (1 or 2)
- **IVs** - Individual Values (0-31, or 255 for random)
  - 255 = Random IVs assigned at battle start
  - 0-31 = Fixed IV values for specific stats

#### Battle Items and Nature

- **Held Item** - Item carried by the Pokemon (dropdown selection)
- **Nature** - Pokemon nature affecting stats (dropdown selection)
- **Ball Type** - Pokeball used for capture (cosmetic effect)

#### Move Configuration

- **Move 1-4** - Moves known by the Pokemon (dropdown selection)
- **PP Settings** - Power Points configuration
  - Individual PP per move
  - Shared PP pool
  - Maximum PP values

### Special Features

#### Shiny and Nickname

- **Shiny Lock** - Controls shiny status
  - `false` - Normal shiny chance
  - `true` - Always shiny
  - Special values for forced non-shiny
- **Nickname** - Custom name for the Pokemon (optional)

#### Technical Details

- **Ball Seal** - Special ball seal item ID
- **IV Numbers** - Specific IV values per stat (advanced)
- **EV Numbers** - Effort Values specification (advanced)
- **Status** - Battle status condition on appearance
- **HP/Stats** - Current stat values (for specific scenarios)

#### Advanced Flags

- **Additional Flags** - Special battle behaviors and mechanics
- **Type Override** - Force specific type effectiveness
- **Special Behaviors** - Custom battle logic flags

## 🎮 AI System Configuration

### AI Flag Categories

#### Offensive AI
- `AI_FLAG_CHECK_BAD_MOVE` - Avoid using ineffective moves
- `AI_FLAG_TRY_TO_FAINT` - Focus on defeating opponents
- `AI_FLAG_CHECK_VIABILITY` - Evaluate move effectiveness

#### Defensive AI
- `AI_FLAG_SETUP_FIRST_TURN` - Prioritize setup moves
- `AI_FLAG_PREFER_STATUS_MOVES` - Use status conditions
- `AI_FLAG_STALL` - Use stall tactics

#### Item Usage AI
- `AI_FLAG_HP_AWARE` - Use healing items strategically
- `AI_FLAG_POWERFUL_ITEMS` - Use powerful consumables
- `AI_FLAG_CONSERVATIVE_ITEMS` - Save items for critical moments

#### Strategic AI
- `AI_FLAG_RISKY` - Use high-risk, high-reward strategies
- `AI_FLAG_SMART_SWITCHING` - Intelligent Pokemon switching
- `AI_FLAG_ACE_POKEMON` - Prioritize ace Pokemon

### AI Flag Combinations

Different trainer classes use specific AI flag combinations:

- **Rookie Trainers**: Basic flags like `CHECK_BAD_MOVE`
- **Gym Leaders**: Complex strategies with `SMART_SWITCHING`
- **Elite Four**: Advanced tactics with multiple strategic flags
- **Champions**: Maximum difficulty with all available flags

## 📊 Trainer Class System

### Class Categories

- **Rookie** - Basic trainers (Youngster, Lass, Bug Catcher)
- **Skilled** - Experienced trainers (Ace Trainer, Veteran)
- **Specialist** - Type experts (Dragon Tamer, Psychic)
- **Authority** - Gym Leaders, Elite Four
- **Antagonists** - Rivals, Team members
- **Special** - Unique trainers (Mysterious Figure, etc.)

### Class-specific Behaviors

Each trainer class may have:
- **Unique AI patterns** - Class-specific battle strategies
- **Item preferences** - Certain items used more frequently
- **Pokemon preferences** - Type or species preferences
- **Battle music** - Unique battle themes

## 🎯 Battle Type Configurations

### Battle Types

- **Single Battle** - Standard 1v1 format
- **Double Battle** - 2v2 simultaneous battle
- **Multi Battle** - 2v2 with partner Pokemon
- **Tag Battle** - Partner battle system
- **Special Battles** - Custom battle formats

### Battle Rules

Each battle type has specific rules:
- **Pokemon limits** - Number of Pokemon that can participate
- **Targeting rules** - Which Pokemon can be targeted
- **Win conditions** - How victory is determined
- **Special mechanics** - Unique battle behaviors

## 🔧 Advanced Trainer Features

### Custom Trainer Creation

While HG Engine Editor focuses on editing existing trainers, understanding the structure allows for:

- **Trainer cloning** - Copy configurations between trainers
- **Template creation** - Standard configurations for similar trainers
- **Balance testing** - Verify trainer difficulty

### Data Validation

- **Species validation** - Ensures selected species exist
- **Move validation** - Verifies moves are compatible with species
- **Level validation** - Checks level ranges are valid
- **Item validation** - Confirms items exist in project

### Backup and Recovery

- **Automatic backups** - `.bak` files for all trainer data
- **Change tracking** - Detailed modification logs
- **Rollback capability** - Restore previous configurations

## 📈 Trainer Difficulty Balancing

### Difficulty Metrics

- **Average Level** - Mean level of party Pokemon
- **Type Coverage** - Effectiveness against common types
- **Move Diversity** - Variety of moves and strategies
- **Item Support** - Healing and status items available

### Balance Considerations

- **Player Progress** - Trainer strength relative to story position
- **Pokemon Availability** - Species accessible at that point
- **Move Learnsets** - Moves available to party Pokemon
- **AI Intelligence** - Strategic complexity appropriate to position

## 🛠️ Technical Implementation

### Trainer Data Format

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

### File Structure

- **Header Section** - Trainer metadata and configuration
- **Party Section** - Individual Pokemon data
- **Technical Fields** - Advanced battle parameters
- **Validation** - Data integrity checks

This comprehensive trainer editing guide covers all current capabilities and advanced features. As HG Engine Editor evolves, additional trainer customization options and battle mechanics may be implemented.
