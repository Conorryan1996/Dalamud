using System;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using Lumina.Excel.Sheets;
using Dalamud.Interface.Textures.TextureWraps;

namespace SamplePlugin.Windows;

public class MainWindow : Window, IDisposable
{
    private string GoatImagePath;
    private Plugin Plugin;
    private string? customImagePath = null;
    private IDalamudTextureWrap? customImage = null;
    private string inputPath = "";

    // We give this window a hidden ID using ##
    // So that the user will see "My Amazing Window" as window title,
    // but for ImGui the ID is "My Amazing Window##With a hidden ID"
    public MainWindow(Plugin plugin, string goatImagePath)
        : base("My Amazing Window##With a hidden ID", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        GoatImagePath = goatImagePath;
        Plugin = plugin;
    }

    public void Dispose() 
    { 
        customImage?.Dispose();
    }

    private void LoadImageFromPath()
    {
        if (string.IsNullOrEmpty(inputPath))
        {
            Plugin.Log.Warning("Please enter a file path");
            return;
        }

        if (!File.Exists(inputPath))
        {
            Plugin.Log.Warning($"File not found: {inputPath}");
            return;
        }

        if (!inputPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
        {
            Plugin.Log.Warning("Please select a PNG file");
            return;
        }

        try
        {
            customImage?.Dispose();
            customImagePath = inputPath;
            
            Plugin.Log.Info($"Attempting to load: {customImagePath}");
            Plugin.Log.Info($"File size: {new FileInfo(customImagePath).Length} bytes");
            
            var textureWrap = Plugin.TextureProvider.GetFromFile(customImagePath);
            Plugin.Log.Info($"TextureWrap created: {textureWrap != null}");
            
            if (textureWrap != null)
            {
                // Try to get the actual texture and any error
                if (textureWrap.TryGetWrap(out var texture, out var exception))
                {
                    customImage = texture;
                    Plugin.Log.Info($"Successfully loaded: {Path.GetFileName(customImagePath)} ({customImage.Width}x{customImage.Height})");
                }
                else
                {
                    Plugin.Log.Error($"TryGetWrap failed for: {customImagePath}");
                    if (exception != null)
                    {
                        Plugin.Log.Error($"Load exception: {exception.Message}");
                        Plugin.Log.Error($"Exception type: {exception.GetType().Name}");
                    }
                    else
                    {
                        Plugin.Log.Error("No exception - texture may still be loading");
                    }
                }
            }
            else
            {
                Plugin.Log.Error($"TextureProvider.GetFromFile returned null for: {customImagePath}");
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Error loading image: {ex.Message}");
            Plugin.Log.Error($"Stack trace: {ex.StackTrace}");
        }
    }

    public override void Draw()
    {
        // Do not use .Text() or any other formatted function like TextWrapped(), or SetTooltip().
        // These expect formatting parameter if any part of the text contains a "%", which we can't
        // provide through our bindings, leading to a Crash to Desktop.
        // Replacements can be found in the ImGuiHelpers Class
        ImGui.TextUnformatted($"The random config bool is {Plugin.Configuration.SomePropertyToBeSavedAndWithADefault}");

        if (ImGui.Button("Show Settings"))
        {
            Plugin.ToggleConfigUI();
        }

        ImGui.Spacing();
        
        ImGui.Separator();
        ImGui.TextUnformatted("Custom Image Loader:");
        ImGui.TextUnformatted("Enter full path to PNG file:");
        ImGui.SetNextItemWidth(400);
        if (ImGui.InputText("##imagepath", ref inputPath, 500, ImGuiInputTextFlags.EnterReturnsTrue))
        {
            LoadImageFromPath();
        }
        ImGui.SameLine();
        if (ImGui.Button("Load PNG"))
        {
            LoadImageFromPath();
        }
        
        if (ImGui.Button("Browse..."))
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                inputPath = @"C:\Users\YourName\Pictures\image.png";
            }
            else
            {
                inputPath = "/home/user/Pictures/image.png";
            }
        }
        ImGui.SameLine();
        ImGui.TextUnformatted("(Click Browse for example path)");
        
        if (customImage != null)
        {
            ImGui.TextUnformatted($"Loaded: {Path.GetFileName(customImagePath)}");
            ImGui.Image(customImage.ImGuiHandle, new Vector2(customImage.Width, customImage.Height));
        }
        
        ImGui.Separator();
        ImGui.Spacing();

        // Normally a BeginChild() would have to be followed by an unconditional EndChild(),
        // ImRaii takes care of this after the scope ends.
        // This works for all ImGui functions that require specific handling, examples are BeginTable() or Indent().
        using (var child = ImRaii.Child("SomeChildWithAScrollbar", Vector2.Zero, true))
        {
            // Check if this child is drawing
            if (child.Success)
            {
                ImGui.TextUnformatted("Have a goat:");
                var goatImage = Plugin.TextureProvider.GetFromFile(GoatImagePath).GetWrapOrDefault();
                if (goatImage != null)
                {
                    using (ImRaii.PushIndent(55f))
                    {
                        ImGui.Image(goatImage.ImGuiHandle, new Vector2(goatImage.Width, goatImage.Height));
                    }
                }
                else
                {
                    ImGui.TextUnformatted("Image not found.");
                }

                ImGuiHelpers.ScaledDummy(20.0f);

                // Example for other services that Dalamud provides.
                // ClientState provides a wrapper filled with information about the local player object and client.

                var localPlayer = Plugin.ClientState.LocalPlayer;
                if (localPlayer == null)
                {
                    ImGui.TextUnformatted("Our local player is currently not loaded.");
                    return;
                }

                if (!localPlayer.ClassJob.IsValid)
                {
                    ImGui.TextUnformatted("Our current job is currently not valid.");
                    return;
                }

                // ExtractText() should be the preferred method to read Lumina SeStrings,
                // as ToString does not provide the actual text values, instead gives an encoded macro string.
                ImGui.TextUnformatted($"Our current job is ({localPlayer.ClassJob.RowId}) \"{localPlayer.ClassJob.Value.Abbreviation.ExtractText()}\"");

                // Example for quarrying Lumina directly, getting the name of our current area.
                var territoryId = Plugin.ClientState.TerritoryType;
                if (Plugin.DataManager.GetExcelSheet<TerritoryType>().TryGetRow(territoryId, out var territoryRow))
                {
                    ImGui.TextUnformatted($"We are currently in ({territoryId}) \"{territoryRow.PlaceName.Value.Name.ExtractText()}\"");
                }
                else
                {
                    ImGui.TextUnformatted("Invalid territory.");
                }
            }
        }
    }
}
