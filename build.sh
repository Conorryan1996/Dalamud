#!/bin/bash

# Build script for SamplePlugin
# This script requires Dalamud to be installed

echo "SamplePlugin Build Script"
echo "========================"

# Check for .NET SDK
if ! command -v dotnet &> /dev/null; then
    echo "Error: .NET SDK not found. Please install .NET 8.0 or later."
    exit 1
fi

echo "Found .NET SDK: $(dotnet --version)"

# Check DALAMUD_HOME
if [ -z "$DALAMUD_HOME" ]; then
    DEFAULT_DALAMUD="$HOME/.xlcore/dalamud/Hooks/dev"
    if [ -d "$DEFAULT_DALAMUD" ]; then
        export DALAMUD_HOME="$HOME/.xlcore/dalamud/Hooks"
        echo "Using default Dalamud location: $DALAMUD_HOME"
    else
        echo "Error: Dalamud not found. Please install XIVLauncher and Dalamud first."
        echo "Or set DALAMUD_HOME environment variable to your Dalamud installation."
        exit 1
    fi
fi

# Build the project
echo "Building SamplePlugin..."
dotnet build SamplePlugin.sln --configuration Release

if [ $? -eq 0 ]; then
    echo "Build succeeded!"
    echo "Plugin DLL location: SamplePlugin/bin/x64/Release/SamplePlugin.dll"
else
    echo "Build failed. Please check the error messages above."
    exit 1
fi