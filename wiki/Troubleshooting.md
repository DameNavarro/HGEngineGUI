# Troubleshooting Guide

This comprehensive troubleshooting guide covers common issues, error messages, and solutions for HG Engine Editor. Whether you're encountering crashes, data loading problems, or unexpected behavior, you'll find solutions here.

## 🚨 Crash Handling and Diagnostics

### Understanding Crashes

HG Engine Editor includes comprehensive crash handling that automatically saves diagnostic information:

- **Crash Dialog**: Appears when the application encounters an error
- **Log Files**: Automatically saved to app LocalState directory
- **Error Details**: Technical information for debugging

### Locating Crash Logs

**Location**: `%LOCALAPPDATA%\Packages\HG Engine Editor_*\LocalState\`

**Files**:
- `last_crash.txt` - Most recent crash information
- `first_chance.log` - Detailed exception logs
- `change_log.txt` - Recent file modification history

### Crash Log Contents

Each crash log contains:
- **Timestamp**: When the crash occurred
- **Exception Type**: Technical error classification
- **Stack Trace**: Code execution path leading to crash
- **System Information**: Windows version, .NET version
- **User Actions**: What you were doing when it crashed

## 🔍 Common Issues and Solutions

### "No Species Found" Error

**Symptoms**:
- Empty species list in the Species page
- "No Pokemon species detected" message
- Unable to select any Pokemon

**Possible Causes**:
1. **Missing Species Header**: `include/constants/species.h` not found
2. **Incorrect Project Path**: Wrong folder selected
3. **File Format Issues**: Species definitions malformed
4. **Encoding Problems**: File not in UTF-8 encoding

**Solutions**:
1. **Verify File Existence**:
   ```
   Your-Project/include/constants/species.h
   ```
   Should contain `#define SPECIES_BULBASAUR 1` etc.

2. **Check Project Structure**:
   - Ensure correct HG Engine project layout
   - Verify all required directories exist

3. **Validate File Format**:
   - Open `species.h` in a text editor
   - Check for proper `#define` syntax
   - Ensure no syntax errors

4. **Test File Encoding**:
   - Save file as UTF-8 without BOM
   - Avoid special characters in species names

### "Data Files Not Found" Errors

**Symptoms**:
- "Could not find mondata.s" or similar messages
- Missing TM/HM or tutor data
- Empty lists in various tabs

**Possible Causes**:
1. **Missing Data Files**: Required `.s` or `.txt` files absent
2. **Wrong File Names**: Files named differently than expected
3. **Incorrect Directory Structure**: Files in wrong locations

**Solutions**:
1. **Verify Required Files**:
   ```
   armips/data/mondata.s
   armips/data/levelupdata.s
   armips/data/evodata.s
   armips/data/eggmoves.s
   armips/data/tmlearnset.txt
   armips/data/tutordata.txt
   armips/data/trainers/trainers.s
   ```

2. **Check File Names**:
   - Ensure exact filename matching
   - Case sensitivity matters on some systems

3. **Alternative Locations**:
   - HG Engine Editor searches recursively
   - Files can be in subdirectories
   - But standard locations are preferred

### "Invalid Data Format" Errors

**Symptoms**:
- "Parse error in file X at line Y"
- "Unexpected token" messages
- Data not loading correctly
- "No routes found" or "Empty encounter list" in Encounters tab

**Common Issues**:
1. **Syntax Errors**: Missing commas, brackets, or quotes
2. **Wrong Constants**: Using undefined constants
3. **Format Changes**: Modified file structure
4. **Missing Encounter Data**: `armips/data/encounters.s` not found or malformed
5. **Config File Issues**: `include/config.h` not found or corrupted

**Solutions**:
1. **Check File Syntax**:
   - Open the problematic file in a text editor
   - Look for missing commas, quotes, or brackets
   - Validate macro structure

2. **Verify Constants**:
   - Ensure all referenced constants are defined
   - Check `#define` statements in header files
   - Validate constant names match exactly

3. **Restore from Backup**:
   - Use `.bak` files to restore previous working state
   - Check change log for recent modifications

4. **Encounter Data Issues**:
   - Verify `armips/data/encounters.s` exists and is properly formatted
   - Check for `encounterdata <id> // Route Name` structure
   - Ensure proper `.close` endings for each encounter block

5. **Config File Issues**:
   - Verify `include/config.h` exists
   - Check for proper `#define` statements
   - Look for missing `LEVEL_CAP_VARIABLE` when level cap is enabled

### "Permission Denied" Errors

**Symptoms**:
- "Cannot write to file" messages
- "Access denied" when saving changes
- Files not updating after save operations

**Possible Causes**:
1. **File Locks**: Files open in another program
2. **Permission Issues**: Insufficient write permissions
3. **Read-only Files**: Files marked as read-only
4. **Antivirus Interference**: Security software blocking access

**Solutions**:
1. **Close Other Programs**:
   - Close any text editors with project files open
   - Exit other ROM editing tools
   - Ensure no build processes are running

2. **Check File Permissions**:
   - Right-click file → Properties → Security tab
   - Ensure user has write permissions
   - Remove read-only attribute if set

3. **Run as Administrator**:
   - Try running HG Engine Editor as administrator
   - May be needed for system-protected directories

4. **Antivirus Exclusions**:
   - Add project directory to antivirus exclusions
   - Whitelist HG Engine Editor executable

## 🔧 Performance Issues

### Slow Loading Times

**Symptoms**:
- Long startup times
- Delayed data loading
- Unresponsive interface

**Possible Causes**:
1. **Large Projects**: Many species/trainers to parse
2. **File System Issues**: Slow disk access
3. **Memory Constraints**: Insufficient RAM
4. **Background Processes**: System resource contention

**Solutions**:
1. **Optimize Project Size**:
   - Consider working with smaller test projects
   - Remove unnecessary files from project directory

2. **System Resources**:
   - Close unnecessary background applications
   - Ensure at least 4GB RAM available
   - Use SSD for better performance

3. **HG Engine Editor Settings**:
   - Wait for complete loading before use
   - Avoid rapid navigation during loading

### Memory Usage Problems

**Symptoms**:
- "Out of memory" errors
- Application becoming unresponsive
- Frequent crashes

**Solutions**:
1. **Close Other Applications**:
   - Free up system memory
   - Close memory-intensive programs

2. **Project Optimization**:
   - Work with smaller project subsets
   - Avoid loading unnecessary data

3. **System Upgrade**:
   - Consider upgrading to more RAM
   - Use 64-bit Windows for better memory management

## 📁 File System Issues

### External Changes Not Detected

**Symptoms**:
- File changes not prompting reload dialog
- Manual refresh required
- Outdated data displayed

**Possible Causes**:
1. **File Watcher Issues**: System file monitoring not working
2. **Network Drives**: Project on network storage
3. **File System Filters**: Antivirus or backup software interference

**Solutions**:
1. **Restart Application**:
   - Restart HG Engine Editor to reset file watchers
   - Ensure project path is local

2. **Check File System**:
   - Move project to local drive
   - Disable real-time antivirus scanning temporarily

3. **Manual Reload**:
   - Use Ctrl+R or restart to refresh data
   - Check timestamps on modified files

### File Corruption Issues

**Symptoms**:
- Garbled data in files
- Parse errors after saves
- Missing data sections

**Possible Causes**:
1. **Interrupted Saves**: Power loss during file operations
2. **Disk Errors**: Hard drive corruption
3. **Encoding Issues**: Wrong character encoding

**Solutions**:
1. **Restore Backups**:
   - Use `.bak` files to restore previous versions
   - Check change log for recent modifications

2. **File Repair**:
   - Open corrupted files in text editor
   - Look for obvious syntax errors
   - Fix encoding issues

3. **System Check**:
   - Run CHKDSK to check disk integrity
   - Scan for malware or system issues

## 🔒 Installation and Setup Issues

### MSIX Installation Problems

**Symptoms**:
- Installation fails
- Certificate errors
- "Package not supported" messages

**Solutions**:
1. **Certificate Installation**:
   - Install `.cer` file to Trusted People store
   - Use administrator privileges
   - Restart system after certificate installation

2. **System Requirements**:
   - Ensure Windows 10 version 2004 (19041) or newer
   - Install Windows App SDK Runtime
   - Enable Developer Mode in Windows settings

3. **PowerShell Installation**:
   - Run `install.ps1` with administrator privileges
   - Follow script prompts carefully

### Build from Source Issues

**Symptoms**:
- Compilation errors
- Missing dependencies
- Runtime errors

**Solutions**:
1. **Prerequisites Check**:
   - Install Visual Studio 2022 with UWP workloads
   - Install .NET 8 SDK
   - Update Windows SDK to latest version

2. **NuGet Packages**:
   - Restore NuGet packages in solution
   - Check for package version conflicts
   - Clear NuGet cache if needed

3. **Build Configuration**:
   - Set correct target platform (x86/x64/ARM64)
   - Ensure proper signing certificate
   - For AnyCPU packaging error, build with: `dotnet build HGEngineGUI.csproj -c Debug -p:Platform=x64 -r win-x64`
   - Check build output for specific errors

### Mart Items save inserts instead of replacing

**Symptoms**:
- Saving General Poké Mart Table adds a new block instead of editing the existing one

**Solutions**:
1. Confirm the header comment exists exactly above the general table:
   ```
   /* General Poké Mart Table */
   .org 0x020FBF22
   ```
2. Ensure there are no stray `.org` lines inside the general table body
3. The editor replaces only the general table body (items/badges) between the header and the next comment/.org/.close
4. If the header text differs, update it to match or report the exact lines for support

### Mart Items editor disabled

**Symptoms**:
- Mart Items expander is greyed out

**Solutions**:
1. Verify `armips/asm/custom/mart_items.s` exists under your project root
2. Restart the app or use Project → Reload to refresh detection
3. Check file permissions for write access

## 🌐 Network and Connectivity Issues

### Remote Project Access

**Symptoms**:
- Issues accessing projects on network drives
- Slow performance with remote files
- File watcher not working

**Solutions**:
1. **Local Copy**:
   - Copy project to local drive for editing
   - Sync changes back when done

2. **Network Optimization**:
   - Use wired connection instead of WiFi
   - Ensure stable network connection
   - Disable offline file caching

3. **Permission Issues**:
   - Ensure proper network share permissions
   - Check firewall settings

## 🎯 Data-Specific Issues

### Pokemon Data Problems

**Symptoms**:
- Stats not saving correctly
- Types not updating
- Ability changes not applied

**Solutions**:
1. **File Format Check**:
   - Verify `mondata.s` syntax
   - Check for missing commas or brackets
   - Validate constant names

2. **Preview Changes**:
   - Use Preview function to verify changes
   - Compare before/after in preview dialog
   - Look for syntax errors in output

3. **Restore Defaults**:
   - Use backup files to restore working state
   - Check recent changes in change log

### Trainer Data Issues

**Symptoms**:
- Trainer parties not saving
- AI flags not working
- Pokemon configuration problems

**Solutions**:
1. **File Structure**:
   - Verify `trainers.s` format
   - Check party block structure
   - Validate trainer header syntax

2. **Data Validation**:
   - Ensure Pokemon species exist
   - Check move compatibility
   - Verify level ranges (1-100)

3. **AI Configuration**:
   - Check AI flag definitions
   - Ensure flag constants are defined
   - Validate flag combinations

### Config Tab Issues

**Symptoms**:
- Config tab not loading or showing empty
- Toggle switches not working
- Configuration changes not saving
- "Level cap" and "Fairy type" options not appearing

**Possible Causes**:
1. **Missing Config File**: `include/config.h` not found
2. **File Format Issues**: Config file not following expected format
3. **Permission Issues**: Cannot write to config file
4. **Parse Errors**: Invalid `#define` statements in config file

**Solutions**:
1. **Verify Config File**:
   - Ensure `include/config.h` exists in your project
   - Check for proper `#define` syntax
   - Look for `FAIRY_TYPE_IMPLEMENTED` and `IMPLEMENT_LEVEL_CAP` definitions

2. **Check File Permissions**:
   - Ensure write access to `include/config.h`
   - Close any other programs editing the file

3. **Restore Default Config**:
   - Use backup files if config becomes corrupted
   - Check change log for recent modifications

### Encounters Tab Issues

**Symptoms**:
- "No routes found" or empty encounter list
- Encounter data not loading
- Changes not saving to encounter files
- Missing route names (showing "Area 1" instead of "Route 31")

**Possible Causes**:
1. **Missing Encounter Files**: `armips/data/encounters.s` not found
2. **File Format Issues**: Encounter data not properly structured
3. **Parse Errors**: Invalid encounter data format
4. **Route Name Parsing**: Comments not properly formatted

**Solutions**:
1. **Verify Encounter Files**:
   - Check `armips/data/encounters.s` exists
   - Ensure proper `encounterdata <id> // Route Name` format
   - Verify each block ends with `.close`

2. **Check File Structure**:
   - Validate encounter data macro structure
   - Ensure proper `walklevels`, `pokemon`, `encounter` statements
   - Check for missing or malformed blocks

3. **Route Name Display**:
   - Verify comments are on the same line as `encounterdata`
   - Format: `encounterdata 4 // Route 31`
   - Use Refresh button to reload data after file changes

4. **Save Issues**:
   - Check write permissions for encounter files
   - Verify file is not open in another editor
   - Use Preview to check for syntax errors before saving

## 📞 Getting Additional Help

### Before Reporting Issues

1. **Gather Information**:
   - Windows version and build number
   - HG Engine Editor version
   - Project structure details
   - Crash logs and error messages

2. **Reproduce the Issue**:
   - Document exact steps to reproduce
   - Note any specific data or configurations
   - Test with minimal project if possible

3. **Check Known Issues**:
   - Review this troubleshooting guide
   - Check FAQ for similar problems
   - Search existing issues

### Reporting Bugs

When reporting issues, include:

- **System Information**:
  - Windows version: `winver` command
  - HG Engine Editor version: Help → About
  - .NET version: `dotnet --version`

- **Problem Details**:
  - Exact error messages
  - Steps to reproduce
  - Expected vs actual behavior

- **Files and Logs**:
  - Crash logs from LocalState directory
  - Change log entries
  - Sample problematic files

- **Environment**:
  - Project structure (anonymized)
  - Any modifications to default setup
  - Antivirus or security software

### Community Support

- **GitHub Issues**: Report bugs and feature requests
- **Discussions**: Ask questions and share solutions
- **Pull Requests**: Contribute fixes and improvements

---

**Remember**: Most issues can be resolved by ensuring proper project structure, checking file permissions, and following the installation instructions carefully. When in doubt, restore from backup files and proceed step by step.
