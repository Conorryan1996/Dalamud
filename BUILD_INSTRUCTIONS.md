# Build Instructions for Modified SamplePlugin

## Prerequisites
1. XIVLauncher and Dalamud must be installed
2. .NET 8.0 SDK or later (the project uses Dalamud.NET.Sdk which targets .NET 9.0)
3. Visual Studio 2022 or JetBrains Rider (recommended)

## Changes Made
- Modified `SamplePlugin/Windows/MainWindow.cs` to add PNG loading functionality:
  - Added text input field for file paths
  - Added "Load PNG" button
  - Added image display for loaded PNGs
  - Proper resource disposal for loaded images

## Building the Plugin

### Option 1: Using Visual Studio/Rider
1. Open `SamplePlugin.sln` in your IDE
2. Ensure Dalamud is installed at the default location or set `DALAMUD_HOME` environment variable
3. Build → Build Solution (or press Ctrl+Shift+B)
4. The built plugin will be at `SamplePlugin/bin/x64/Release/SamplePlugin.dll`

### Option 2: Command Line
```bash
# Set DALAMUD_HOME if not using default location
export DALAMUD_HOME=/path/to/dalamud

# Build in Release mode
dotnet build SamplePlugin.sln --configuration Release
```

## Build Requirements
The project uses Dalamud.NET.Sdk v12.0.2 which automatically handles:
- Dalamud API references
- ImGui.NET bindings
- FFXIVClientStructs
- Lumina data framework

## Note
Building requires a working Dalamud installation as the SDK references Dalamud assemblies during compilation. Without Dalamud installed, the build will fail with missing reference errors.

## Using the Modified Plugin
1. Build the plugin following the instructions above
2. Add the built DLL to Dalamud's Dev Plugin Locations
3. Enable in Plugin Installer
4. Use `/pmycommand` to open the window
5. Enter a PNG file path and click "Load PNG" to display custom images