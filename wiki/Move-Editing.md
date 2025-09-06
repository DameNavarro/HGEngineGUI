# Move Editing

This editor lets you edit core move data stored in `armips/data/moves.s`.

Edited fields per move:
- Name and description
- Split (SPLIT_PHYSICAL/SPLIT_SPECIAL/SPLIT_STATUS)
- Type (TYPE_*)
- Base Power, Accuracy, PP, Priority
- Secondary Effect Chance
- Effect (MOVE_EFFECT_*)
- Target (RANGE_*)
- Flags (FLAG_CONTACT/FLAG_PROTECT/etc.)
- Contest appeal and contest type

Sources and constants:
- Move IDs: `include/constants/moves.h`
- Effects: `asm/include/move_effects.inc`
- Targets, flags, splits, contest types: `armips/include/movemacros.s`
- Engine uses ARC_MOVE_DATA (NARC index 11), queried via `src/moves.c`

Related data where moves are referenced:
- Level-up learnsets: `armips/data/levelupdata.s`
- Egg moves: `armips/data/eggmoves.s`
- TM/HM mapping: `armips/data/tmlearnset.txt`
- Tutor data: `armips/data/tutordata.txt`
- Trainers: `armips/data/trainers/trainers.s`

Animations and effect scripts:
- Move animations: `armips/move/move_anim/*.s`
- Move sub-animations: `armips/move/move_sub_anim/*.s`
- Effect scripts: `data/battle_scripts/effects/*.s`

Notes:
- Changing Move Effect switches behavior to the selected effect; ensure a script exists/works for it.
- Names/descriptions are emitted via macros in the same file and updated automatically.
- Creating new moves requires updating `include/constants/moves.h` (IDs) and possibly `asm/include/moves.inc`; this editor currently focuses on editing existing moves.


