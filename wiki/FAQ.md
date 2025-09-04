# Frequently Asked Questions

This FAQ covers the most common questions about HG Engine Editor, from basic usage to advanced features and troubleshooting. If you don't find your answer here, check the Troubleshooting guide or open an issue on GitHub.

## 🚀 Getting Started

### What is HG Engine Editor?

HG Engine Editor is a modern Windows application for editing Pokemon ROM data in HG Engine projects. It provides a user-friendly interface for editing Pokemon species data, trainer information, and other game mechanics without manually modifying assembly files.

### What is HG Engine?

HG Engine is a decompilation and enhancement project for Pokemon HeartGold. It provides the source code and tools needed to modify Pokemon game mechanics, create custom content, and develop ROM hacks.

### What platforms does HG Engine Editor support?

- **Operating System**: Windows 10 version 2004 (19041) or newer
- **Architecture**: x86, x64, and ARM64
- **Distribution**: MSIX packages for modern Windows deployment

## 📥 Installation and Setup

### How do I install HG Engine Editor?

**Option 1: install.bat (Recommended)**
1. Download the latest release from GitHub
2. Extract and run install.bat as admin
3. Wait until the installation is done and then you can open up HG Engine Editor

**Option 2: Build from Source**
1. Clone the repository
2. Open in Visual Studio 2022
3. Build and run the solution

See the [Getting Started](Getting-Started.md) guide for detailed instructions.

### What are the system requirements?

- **Windows 10**: Version 2004 (19041) or newer
- **Memory**: 4GB RAM minimum, 8GB recommended
- **Storage**: 500MB free space
- **Display**: 1366x768 resolution minimum

### Do I need HG Engine to use HG Engine Editor?

Yes, you need an existing HG Engine project. HG Engine Editor is designed to work with HG Engine's specific file structure and data formats. You'll need to:

1. Obtain or create an HG Engine project
2. Point HG Engine Editor to your project directory
3. Ensure all required data files are present

### Can I use HG Engine Editor with other Pokemon ROM hacking tools?

Yes! HG Engine Editor works well alongside other tools like:
- **Hex editors** for low-level modifications
- **Graphics editors** for sprites and tilesets
- **Script editors** for event scripting
- **Build tools** for compiling your project

## 🎮 Basic Usage

### How do I select my project?

1. Launch HG Engine Editor
2. Click the "Pick Folder" button
3. Navigate to your HG Engine project root
4. The app will automatically detect your data files
5. Start editing!

### What data can I edit?

HG Engine Editor allows you to edit:

- **Pokemon Data**: Stats, types, abilities, moves, evolution chains
- **Trainer Data**: Party Pokemon, AI behavior, items, battle settings
- **TM/HM Compatibility**: Technical and Hidden Machine learnsets
- **Move Tutor Data**: Tutor moves and battle point costs

### How do I save my changes?

1. Make your edits in the interface
2. Click the "Preview" button to review changes
3. Review the file modifications in the preview dialog
4. Click "Save" to apply changes to your project files

HG Engine Editor automatically creates backup files (.bak) before making changes.

### What does the "Preview" function do?

The Preview function shows you exactly what changes will be made to your files before applying them. This includes:

- **File path** and **line numbers** of changes
- **Before/after** comparison of modified content
- **Syntax highlighting** for assembly code
- **Validation** of your changes

### Can I undo changes?

Yes! HG Engine Editor provides several ways to undo changes:

1. **Manual Revert**: Use the Change Log to restore from backup files
2. **File Restore**: Replace modified files with .bak versions
3. **External Tools**: Use Git or other version control to revert changes

## 🐾 Pokemon Editing

### How many Pokemon does HG Engine Editor support?

HG Engine Editor supports editing all Pokemon species defined in your project. This typically includes:

- **Generation 1-5 Pokemon** (649 species)
- **Extra forms and variants** (total varies by project)
- **Custom Pokemon** added to your project

### What Pokemon stats can I edit?

In the Overview tab, you can edit:

- **Base Stats**: HP, Attack, Defense, Sp. Attack, Sp. Defense, Speed (0-255)
- **Types**: Primary and secondary elemental types
- **Abilities**: Regular abilities and hidden ability
- **Experience Data**: Base EXP yield and growth rate
- **Capture Data**: Catch rate (0-255)
- **Breeding**: Gender ratio, egg cycles, egg groups
- **Pokedex**: Classification, entry text, height, weight
- **Items**: Wild encounter held items

### How do TM/HM learnsets work?

TM/HM compatibility is managed through the `tmlearnset.txt` file:

- **TM001-TM092**: Technical Machines (single use)
- **HM001-HM008**: Hidden Machines (reusable, often for field moves)
- **Compatibility**: Toggle which Pokemon can learn which moves
- **Multi-select**: Hold Ctrl to modify multiple Pokemon at once

### What are move tutors?

Move tutors are NPCs who teach Pokemon special moves for Battle Points (BP):

- **Tutor Moves**: Special moves not available through leveling or TMs
- **Cost System**: Each tutor charges a different BP cost
- **Compatibility**: Pokemon-specific availability
- **Cost Editing**: BP costs are project-defined

### Can I edit level-up moves?

**Current Version**: Level-up moves are read-only and displayed as summaries.

**Future Plans**: Full GUI editing for level-up moves is planned for future versions. For now, you can:
- View existing learnsets
- Preview changes (when available)
- Edit manually in `levelupdata.s`

### How do evolutions work?

Evolution data includes:

- **Evolution Methods**: 20+ different triggers (level, item, trade, etc.)
- **Parameters**: Method-specific requirements
- **Multiple Paths**: Some Pokemon have multiple evolution options
- **Chain Display**: Shows complete evolution lines

**Note**: Full GUI editing for evolution data is planned for future versions.

### What are egg moves?

Egg moves are special moves that Pokemon can inherit from their parents:

- **Breeding Inheritance**: Passed down through compatible breeding
- **Compatibility Rules**: Determined by egg groups and species
- **Generation Limits**: Cannot inherit moves from different generations
- **List Management**: View which moves can be inherited

## 👥 Trainer Editing

### What trainer data can I edit?

Trainer configuration includes:

- **Basic Info**: Name, class, party size
- **Battle Settings**: Battle type, AI flags, items
- **Pokemon Party**: Up to 6 Pokemon with detailed configurations
- **Advanced Options**: IVs, natures, items, moves, and more

### What are AI flags?

AI flags control how trainers behave in battle:

- **Offensive**: `TRY_TO_FAINT`, `CHECK_BAD_MOVE`
- **Defensive**: `SETUP_FIRST_TURN`, `HP_AWARE`
- **Strategic**: `SMART_SWITCHING`, `ACE_POKEMON`
- **Item Usage**: `CONSERVATIVE_ITEMS`, `RISKY`

Each trainer class uses different AI flag combinations for varied difficulty.

### What battle types are available?

- **Single Battle**: Standard 1v1 format
- **Double Battle**: 2v2 simultaneous battle
- **Multi Battle**: 2v2 with partner Pokemon
- **Tag Battle**: Partner battle system

### Can I edit Pokemon in trainer parties?

Yes! Each Pokemon in a trainer's party has extensive configuration:

- **Basic**: Species, level, form, held item, nature
- **Stats**: IVs (0-31 or 255 for random), EVs
- **Moves**: Up to 4 moves with PP settings
- **Special**: Shiny lock, nickname, ball type
- **Advanced**: Status conditions, ability slots, ball seals

### What's the difference between IVs and EVs?

- **IVs (Individual Values)**: Hidden stats (0-31) or 255 for random
  - Set to 255 for random IVs assigned at battle start
  - Set to 0-31 for fixed IV values

- **EVs (Effort Values)**: Stats gained from battle (0-252 total)
  - Distributed across HP, Attack, Defense, etc.
  - Maximum 63 per stat for 252 total

## 🔧 Advanced Features

### How does file watching work?

HG Engine Editor monitors your project files for external changes:

- **Detection**: Automatically detects modifications to `armips/data` files
- **Notification**: Shows dialog asking if you want to reload data
- **Options**: Reload to get latest changes or ignore them
- **Safety**: Preserves unsaved edits when reloading

### What are the backup files for?

HG Engine Editor creates `.bak` backup files before making changes:

- **Location**: Next to original files (e.g., `mondata.s.bak`)
- **Purpose**: Recovery if changes cause problems
- **Management**: Can be used to restore previous working state
- **Cleanup**: Safe to delete old backups when no longer needed

### How do I use the Change Log?

The Change Log tracks all file modifications:

- **Recent Changes**: Shows files modified in current session
- **Timestamps**: When changes were made
- **File Sizes**: Size information for change tracking
- **Actions**: Open files or restore from backup

### Can I use keyboard shortcuts?

Currently available shortcuts:

- `Ctrl+S`: Save changes in editor pages
- `Ctrl+F`: Focus search/filter boxes (when available)

More shortcuts are planned for future versions.

## 🔍 Troubleshooting

### Why do I see "No Species Found"?

Common causes:

1. **Wrong Project Path**: Selected wrong folder
2. **Missing Files**: `include/constants/species.h` not found
3. **File Format**: Species definitions malformed
4. **Project Structure**: Not a valid HG Engine project

**Solution**: Verify project structure and file existence.

### Why can't I save changes?

Possible issues:

1. **File Permissions**: No write access to project files
2. **File Locks**: Files open in another program
3. **Disk Space**: Insufficient space for backup files
4. **Antivirus**: Security software blocking file operations

**Solution**: Check permissions, close other programs, and verify disk space.

### Why is the app slow?

Performance issues can be caused by:

1. **Large Projects**: Many species/trainers to load
2. **Slow Storage**: Network drives or slow disks
3. **Memory**: Insufficient RAM for large datasets
4. **Background Processes**: System resource contention

**Solution**: Use local SSD storage and close unnecessary applications.

### How do I report bugs?

To report bugs effectively:

1. **Gather Information**:
   - Windows version and HG Engine Editor version
   - Exact steps to reproduce the issue
   - Error messages and crash logs

2. **Check Existing Issues**: Search GitHub issues first

3. **Report**: Create detailed issue with all relevant information

## 🔄 Updates and Support

### How do I update HG Engine Editor?

**MSIX Package**:
1. Download latest release from GitHub
2. Install new package (it will update existing installation)
3. Restart the application

**Built from Source**:
1. Pull latest changes from repository
2. Rebuild the solution
3. Run updated version

### Where can I get help?

- **GitHub Issues**: Report bugs and request features
- **GitHub Discussions**: Ask questions and share solutions
- **Troubleshooting Guide**: Check for common solutions
- **This FAQ**: Review for similar questions

### Is there a community?

Yes! The HG Engine and ROM hacking community is active on:

- **GitHub**: Project repositories and issue tracking
- **Discord**: Real-time chat and support
- **Forums**: ROM hacking communities
- **Social Media**: Follow project updates

## 🚧 Future Plans

### What's coming in future versions?

- **Enhanced Editors**: Full GUI for level-up moves and evolutions
- **Batch Operations**: Bulk editing capabilities
- **Export/Import**: Data exchange with other tools
- **Plugin System**: Extensible architecture
- **More Data Types**: Additional HG Engine features

### Can I request features?

Absolutely! Feature requests are welcome:

1. **GitHub Issues**: Create feature request with detailed description
2. **Use Cases**: Explain how the feature would be useful
3. **Community Voting**: Other users can upvote popular requests

### How can I contribute?

Contributions are welcome in many forms:

- **Code**: Bug fixes and new features
- **Documentation**: Improve guides and tutorials
- **Testing**: Report bugs and test new features
- **Feedback**: Share your experience and suggestions

---

**Still have questions?** Check the [Troubleshooting](Troubleshooting.md) guide, search GitHub issues, or start a GitHub Discussion. The community is here to help!
