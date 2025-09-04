# HG Engine Editor Wiki

Welcome to the comprehensive wiki for **HG Engine Editor** - a modern Windows application for editing Pokemon ROM data in HG Engine projects.

## 🚀 Project Overview

HG Engine Editor is a sophisticated GUI editor designed specifically for the HG Engine project, which is a decompilation and enhancement of Pokemon HeartGold and SoulSilver games. This application provides Pokemon ROM hackers and developers with an intuitive, user-friendly interface for editing complex game data without manually modifying assembly files.

### Key Features

- **🎯 Modern Windows UI**: Built with WinUI 3 for a native Windows 11/10 experience
- **📁 Project Management**: Easy project folder selection with automatic file detection
- **👁️ Live File Monitoring**: Real-time detection of external file changes with reload prompts
- **🔍 Comprehensive Pokemon Editor**: Complete species data editing including:
  - Base stats, types, and abilities
  - EV yields, catch rates, and experience data
  - Pokédex information (classification, entries, height/weight)
  - TM/HM and move tutor compatibility
  - Evolution chains and egg moves
- **👥 Trainer Editor**: Full trainer party management with detailed Pokemon configuration
- **⚡ Safe Edit Workflow**: Preview/Save system with automatic backup creation
- **🔧 Developer-Friendly**: Open-source with clear data mapping documentation

## 🎮 Supported Game Data

This application is designed to work with HG Engine projects and supports editing:

- **649 Pokemon species** (Generation 1-5 + extras)
- **Trainer data** with complex Pokemon configurations
- **TM/HM compatibility** (92 technical machines + 8 hidden machines)
- **Move tutor systems** with cost configurations
- **Evolution data** with all evolution methods
- **Egg move inheritance** and breeding mechanics

## 🏗️ Architecture

The application is built using modern technologies:

- **Framework**: .NET 8 with WinUI 3
- **MVVM Pattern**: CommunityToolkit.Mvvm for clean separation of concerns
- **Data Parsing**: Custom regex-based parsers for assembly data files
- **File Operations**: Safe read/write operations with backup preservation
- **Cross-Platform**: Supports x86, x64, and ARM64 architectures

## 📚 Documentation Sections

- **[Getting Started](Getting-Started.md)** - Installation and initial setup
- **[User Interface Guide](User-Interface.md)** - Complete UI walkthrough
- **[Species Editing](Species-Editing.md)** - Pokemon data modification
- **[Trainer Editing](Trainer-Editing.md)** - Trainer party management
- **[Data Mapping](Data-Mapping.md)** - File structure and technical details
- **[Troubleshooting](Troubleshooting.md)** - Common issues and solutions
- **[Development](Development.md)** - Building from source
- **[FAQ](FAQ.md)** - Frequently asked questions

## 🔄 Workflow Integration

HG Engine Editor integrates seamlessly into the HG Engine development workflow:

1. **Project Setup**: Point to your HG Engine root folder
2. **Data Loading**: Automatic parsing of assembly data files
3. **Editing**: User-friendly forms for complex data structures
4. **Preview**: Review changes before applying them
5. **Backup**: Automatic `.bak` file creation for safety
6. **Build**: Modified files ready for compilation

## 📋 Requirements

- **Operating System**: Windows 10 version 2004 (19041) or newer
- **Runtime**: Windows App SDK Runtime (automatically handled via MSIX)
- **Project**: Valid HG Engine project structure
- **Storage**: Minimal disk space (application footprint ~50MB)

## 🤝 Contributing

This project welcomes contributions! Whether you're fixing bugs, adding features, or improving documentation, your help is appreciated. See the [Development](Development.md) section for setup instructions.

## 📄 License

HG Engine Editor is released under the MIT License, allowing free use, modification, and distribution for both personal and commercial purposes.

---

*For questions or support, please open an issue on the project's GitHub repository.*
