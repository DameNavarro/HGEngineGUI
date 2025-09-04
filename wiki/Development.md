# Development Guide

This comprehensive guide covers building HG Engine Editor from source, contributing to the project, and extending its functionality. Whether you're fixing bugs, adding features, or customizing the application, this guide provides the information you need.

## 🏗️ Building from Source

### Prerequisites

#### Required Software

- **Operating System**: Windows 10 version 2004 (19041) or newer
- **Development Environment**: Visual Studio 2022
- **.NET SDK**: .NET 8.0 SDK or newer
- **Windows SDK**: Latest Windows SDK (10.0.26100.0 or newer)
- **Git**: For source code management

#### Visual Studio Workloads

Install these workloads in Visual Studio 2022:

1. **Universal Windows Platform (UWP) development**
2. **.NET desktop development**
3. **Desktop development with C++** (for Windows App SDK)
4. **MSVC v143 build tools**

#### Optional Tools

- **Windows App SDK**: Latest version for runtime
- **MSIX Packaging Tool**: For package creation
- **Windows Application Packaging Project**: For store distribution

### Clone and Setup

```bash
# Clone the repository
git clone https://github.com/your-repo/HG Engine Editor.git
cd HG Engine Editor

# Open the solution
start HG Engine Editor.sln
```

### Build Configurations

HG Engine Editor supports multiple target platforms:

- **x86**: 32-bit Windows applications
- **x64**: 64-bit Windows applications
- **ARM64**: ARM-based Windows devices

#### Build Process

1. **Open Solution**: Double-click `HG Engine Editor.sln`
2. **Select Configuration**:
   - Debug: Development builds with debugging symbols
   - Release: Optimized production builds
3. **Choose Platform**: x86, x64, or ARM64
4. **Build**: Project → Build Solution (Ctrl+Shift+B)

#### Packaging

For distribution:

1. **Create Package**:
   - Right-click project → Publish → Create App Packages
   - Choose MSIX or MSIX Bundle
   - Select target platforms

2. **Signing**:
   - Use test certificate for development
   - Use code signing certificate for distribution

### Running Unpackaged

For development and testing:

1. **Install Windows App SDK Runtime**
2. **Set as Startup Project**: Right-click HG Engine Editor project
3. **Run**: F5 or Debug → Start Debugging

## 🏛️ Project Architecture

### Solution Structure

```
HG Engine Editor/
├── HG Engine Editor/              # Main application project
│   ├── App.xaml/cs          # Application entry point
│   ├── MainWindow.xaml/cs   # Main window and navigation
│   ├── Pages/               # UI pages
│   │   ├── StartPage.xaml/cs    # Project selection
│   │   ├── SpeciesListPage.xaml/cs  # Pokemon list
│   │   ├── SpeciesDetailPage.xaml/cs # Pokemon editor
│   │   ├── TrainersPage.xaml/cs     # Trainer editor
│   │   ├── ConfigPage.xaml/cs       # Game configuration editor
│   │   └── EncountersPage.xaml/cs   # Wild encounter data editor
│   ├── Data/                # Data parsing and management
│   │   ├── HGParsers.cs     # Main data parsing logic
│   │   └── HGSerializers.cs # Data serialization
│   ├── Services/            # Application services
│   │   ├── FileWatcher.cs   # File monitoring
│   │   └── ChangeLog.cs     # Change tracking
│   ├── Properties/          # Application properties
│   └── Assets/              # Application assets
├── HG Engine Editor.Package/     # Packaging project
└── docs/                   # Documentation
```

### Key Components

#### Data Layer (`Data/`)

- **HGParsers.cs**: Core parsing logic for HG Engine data files
- **HGSerializers.cs**: Data writing and modification logic

#### Service Layer (`Services/`)

- **FileWatcher.cs**: Monitors project files for external changes
- **ChangeLog.cs**: Tracks file modifications and creates backups

#### UI Layer (`Pages/`)

- **StartPage**: Project selection and configuration
- **SpeciesListPage**: Pokemon species browser
- **SpeciesDetailPage**: Comprehensive Pokemon editor
- **TrainersPage**: Trainer party management
- **ConfigPage**: Game configuration toggle editor
- **EncountersPage**: Wild Pokemon encounter data editor

### MVVM Architecture

HG Engine Editor uses the **CommunityToolkit.Mvvm** library for clean separation of concerns:

- **Models**: Data structures representing Pokemon, trainers, etc.
- **ViewModels**: UI logic and data binding
- **Views**: XAML UI definitions with code-behind

### Dependency Injection

Services are registered and resolved through the MVVM toolkit:

```csharp
// Service registration
private void ConfigureServices()
{
    // Register services here
}
```

## 🔧 Extending Functionality

### Adding New Pokemon Data Fields

1. **Update Data Model**:
   ```csharp
   public class SpeciesOverview
   {
       // Add new property
       public string NewField { get; set; }
   }
   ```

2. **Extend Parser**:
   ```csharp
   // In HGParsers.cs
   var newField = Regex.Match(block, @"newfield\s+(?<v>[A-Z0-9_]+)");
   if (newField.Success)
   {
       overview.NewField = newField.Groups["v"].Value;
   }
   ```

3. **Update UI**:
   ```xaml
   <!-- In SpeciesDetailPage.xaml -->
   <TextBox Text="{Binding NewField}" Header="New Field" />
   ```

4. **Add Serialization**:
   ```csharp
   // In HGSerializers.cs
   writer.WriteLine($"    newfield {speciesData.NewField}");
   ```

### Adding New Evolution Methods

1. **Define Constants**:
   ```csharp
   // Add to evolution method enums
   EVO_NEW_METHOD = 99
   ```

2. **Update Parser**:
   ```csharp
   // In HGParsers.cs evolution parsing
   case "EVO_NEW_METHOD":
       // Handle new evolution logic
       break;
   ```

3. **Extend UI**:
   - Add new evolution method to dropdown
   - Add parameter fields as needed

### Adding New Trainer AI Flags

1. **Define Flag Constants**:
   ```csharp
   // Add to AI flag definitions
   AI_FLAG_NEW_BEHAVIOR = 1 << 15
   ```

2. **Update UI**:
   ```xaml
   <CheckBox Content="New Behavior"
             IsChecked="{Binding IsNewBehaviorEnabled}" />
   ```

3. **Implement Logic**:
   ```csharp
   // In trainer data handling
   if (newBehaviorEnabled)
   {
       aiFlags |= AI_FLAG_NEW_BEHAVIOR;
   }
   ```

### Adding New Config Options

1. **Add Config Model Property**:
   ```csharp
   // In ConfigPage.xaml.cs
   public bool NewFeatureEnabled { get; set; }
   ```

2. **Update Parser**:
   ```csharp
   // In HGParsers.cs Config parsing
   var newFeatureMatch = Regex.Match(content, @"#define\s+NEW_FEATURE\s+(0|1)");
   if (newFeatureMatch.Success)
   {
       config.NewFeatureEnabled = newFeatureMatch.Groups[1].Value == "1";
   }
   ```

3. **Add UI Control**:
   ```xaml
   <!-- In ConfigPage.xaml -->
   <ToggleSwitch Header="New Feature"
                 IsOn="{Binding NewFeatureEnabled, Mode=TwoWay}" />
   ```

4. **Update Serializer**:
   ```csharp
   // In HGSerializers.cs
   if (config.NewFeatureEnabled)
       writer.WriteLine("#define NEW_FEATURE 1");
   else
       writer.WriteLine("#define NEW_FEATURE 0");
   ```

### Adding New Encounter Types

1. **Extend EncounterArea Model**:
   ```csharp
   // In HGParsers.cs
   public record EncounterArea
   {
       // Add new properties
       public List<EncounterSlot> NewEncounterType { get; set; } = new();
   }
   ```

2. **Update Parser**:
   ```csharp
   // In HGParsers.cs encounter parsing
   var newTypeRx = new Regex(@"\.halfword\s+(?<species>SPECIES_[A-Z0-9_]+|0x[0-9A-Fa-f]+)");
   // Parse new encounter type blocks
   ```

3. **Add UI Section**:
   ```xaml
   <!-- In EncountersPage.xaml -->
   <Expander Header="New Encounter Type" IsExpanded="False">
       <ItemsControl ItemsSource="{Binding SelectedArea.NewEncounterType}">
           <!-- Encounter slot template -->
       </ItemsControl>
   </Expander>
   ```

4. **Update Serializer**:
   ```csharp
   // In HGSerializers.cs
   writer.WriteLine("// New encounter type");
   foreach (var slot in area.NewEncounterType)
   {
       writer.WriteLine($".halfword {slot.SpeciesMacro}");
   }
   ```

### Adding Headbutt Encounter Support

1. **Create HeadbuttArea Model**:
   ```csharp
   // In HGParsers.cs
   public record HeadbuttArea
   {
       public int Id { get; set; }
       public string Label { get; set; } = string.Empty;
       public List<EncounterSlot> NormalSlots { get; set; } = new();
       public List<EncounterSlot> SpecialSlots { get; set; } = new();
       public List<HeadbuttTree> Trees { get; set; } = new();
   }
   ```

2. **Implement Parser**:
   ```csharp
   // In HGParsers.cs
   public static async Task RefreshHeadbuttAsync()
   {
       // Parse armips/data/headbutt.s
       // Extract headbuttheader, headbuttencounter, treecoords data
   }
   ```

3. **Add Headbutt Tab**:
   ```xaml
   <!-- In EncountersPage.xaml -->
   <TabViewItem Header="Headbutt" Icon="MapPin">
       <!-- Headbutt encounter editor -->
   </TabViewItem>
   ```

## 🧪 Testing

### Unit Testing

```csharp
// Example test structure
[TestClass]
public class DataParsingTests
{
    [TestMethod]
    public void TestSpeciesParsing()
    {
        // Test species data parsing
    }

    [TestMethod]
    public void TestEvolutionParsing()
    {
        // Test evolution data parsing
    }
}
```

### Integration Testing

- **Data File Parsing**: Test with various HG Engine project structures
- **File Modification**: Verify changes are written correctly
- **Backup Creation**: Ensure `.bak` files are created properly
- **Error Handling**: Test behavior with malformed data

### UI Testing

- **Navigation**: Test page transitions and data loading
- **Data Binding**: Verify UI updates with data changes
- **Validation**: Test input validation and error display
- **Performance**: Monitor loading times and memory usage

## 📦 Packaging and Distribution

### MSIX Package Creation

1. **Configure Package**:
   ```xml
   <!-- In Package.appxmanifest -->
   <Identity Name="HG Engine Editor"
             Publisher="CN=YourPublisher"
             Version="1.0.0.0" />
   ```

2. **Build Package**:
   - Project → Publish → Create App Packages
   - Choose distribution method
   - Configure signing certificate

3. **Test Package**:
   - Install locally for testing
   - Verify functionality in packaged environment

### Store Submission

1. **Partner Center**:
   - Register as Microsoft developer
   - Create app submission
   - Upload MSIX bundle

2. **Store Requirements**:
   - Valid code signing certificate
   - Complete app metadata
   - Screenshots and descriptions

## 🤝 Contributing Guidelines

### Code Style

- **Language**: C# 10.0+ with nullable reference types
- **Naming**: PascalCase for classes, camelCase for variables
- **Formatting**: Use Visual Studio's default formatting
- **Documentation**: XML documentation comments for public APIs

### Git Workflow

1. **Fork Repository**: Create your own fork
2. **Create Branch**: `git checkout -b feature/your-feature`
3. **Make Changes**: Implement your feature or fix
4. **Write Tests**: Add tests for new functionality
5. **Commit**: `git commit -m "Description of changes"`
6. **Push**: `git push origin feature/your-feature`
7. **Pull Request**: Create PR with detailed description

### Pull Request Requirements

- **Title**: Clear, descriptive title
- **Description**: Detailed explanation of changes
- **Tests**: Include tests for new features
- **Documentation**: Update docs for significant changes
- **Breaking Changes**: Clearly marked and explained

### Code Review Process

- **Automated Checks**: CI/CD pipeline runs tests and builds
- **Peer Review**: At least one maintainer review required
- **Testing**: Manual testing of significant changes
- **Approval**: Maintainers approve and merge

## 🔧 Debugging

### Common Debugging Techniques

#### Data Parsing Issues

```csharp
// Add debug output to parsers
Debug.WriteLine($"Parsing species: {speciesName}");
Debug.WriteLine($"Block content: {block}");
```

#### UI Binding Problems

```csharp
// In XAML
<TextBox Text="{Binding Path=PropertyName, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"
         IsEnabled="{Binding CanEditProperty}" />
```

#### File System Operations

```csharp
// Add logging to file operations
try
{
    // File operation
    Logger.Log($"Successfully wrote {filePath}");
}
catch (Exception ex)
{
    Logger.Error($"Failed to write {filePath}: {ex.Message}");
}
```

### Performance Profiling

- **Memory Usage**: Monitor for memory leaks in data loading
- **File I/O**: Profile file reading/writing operations
- **UI Responsiveness**: Check for blocking operations on UI thread

### Remote Debugging

1. **Enable Remote Debugging**:
   - Install remote debugging tools on target machine
   - Configure firewall and network settings

2. **Attach Debugger**:
   - Debug → Attach to Process
   - Select remote connection
   - Choose HG Engine Editor process

## 🚀 Future Development

### Implemented Features

- **Config Editor**: Game configuration toggles (Fairy Type, Level Cap)
- **Encounters Editor**: Wild Pokemon encounter data management
- **Mart Items Editor**: Poké Mart inventory configuration
- **Enhanced UI**: Improved layouts and user experience

### Planned Features

- **Headbutt Encounters**: Headbutt tree encounter data editor
- **Enhanced Evolution Editor**: Full GUI for evolution configuration
- **Move Editor**: Comprehensive move data editing
- **Ability Editor**: Ability configuration interface
- **Batch Operations**: Bulk editing capabilities
- **Export/Import**: Data exchange with other tools
- **Form Support**: Extended Pokemon form editing
- **Probability Editor**: Advanced encounter probability management

### Architecture Improvements

- **Plugin System**: Extensible architecture for custom editors
- **Database Backend**: Optional database storage for large projects
- **Cloud Integration**: Project synchronization and backup
- **Multi-language Support**: Localization for different languages

### Community Contributions

Areas needing contribution:

- **Additional Data Types**: Support for new HG Engine features
- **UI Enhancements**: Improved user experience and accessibility
- **Documentation**: Comprehensive user guides and tutorials
- **Testing**: Expanded test coverage and automated testing
- **Performance**: Optimization for large projects

## 📚 Learning Resources

### Recommended Reading

- **WinUI 3 Documentation**: Microsoft official documentation
- **CommunityToolkit.Mvvm**: MVVM toolkit documentation
- **HG Engine Documentation**: ROM hacking resources
- **C# Best Practices**: Language and framework guidelines

### Development Tools

- **Visual Studio Extensions**:
  - XAML Styler: Code formatting
  - CodeMaid: Code organization
  - ReSharper: Code analysis and refactoring

- **Debugging Tools**:
  - WinDbg: Advanced debugging
  - Process Monitor: System call monitoring
  - Performance Profiler: Performance analysis

### Community Resources

- **GitHub Discussions**: Community questions and answers
- **Discord Servers**: Real-time development chat
- **ROM Hacking Forums**: Domain-specific knowledge
- **Microsoft Developer Community**: Platform-specific help

---

**Happy developing!** 🎮 This guide should provide everything you need to contribute to HG Engine Editor and extend its capabilities for the ROM hacking community.
