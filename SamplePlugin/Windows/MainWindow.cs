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
using Dalamud.Interface.Textures;

namespace SamplePlugin.Windows;

public class MainWindow : Window, IDisposable
{
    private string GoatImagePath;
    private Plugin Plugin;
    private string? customImagePath = null;
    private IDalamudTextureWrap? customImage = null;
    private string inputPath = "";
    private ISharedImmediateTexture? loadingTexture = null;
    
    // Decal system
    private bool decalEnabled = false;
    private float decalSize = 2.0f;
    private float decalOpacity = 0.7f;
    private Vector3 lastPlayerPosition = Vector3.Zero;

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
        // Don't dispose customImage - it's managed by Dalamud's ISharedImmediateTexture
        loadingTexture = null;
        customImage = null;
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
            // Clear previous image (don't dispose - managed by Dalamud)
            customImage = null;
            loadingTexture = null;
            customImagePath = inputPath;
            
            Plugin.Log.Info($"Attempting to load: {customImagePath}");
            Plugin.Log.Info($"File size: {new FileInfo(customImagePath).Length} bytes");
            
            // Store the texture for async loading
            loadingTexture = Plugin.TextureProvider.GetFromFile(customImagePath);
            Plugin.Log.Info($"Texture loading started: {loadingTexture != null}");
            
            // Clear the current image since we're loading a new one
            customImage = null;
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Error starting image load: {ex.Message}");
            Plugin.Log.Error($"Stack trace: {ex.StackTrace}");
        }
    }

    private void DrawGroundDecal()
    {
        if (customImage == null || !decalEnabled)
            return;

        var localPlayer = Plugin.ClientState.LocalPlayer;
        if (localPlayer == null)
            return;

        try
        {
            // Get player's world position
            var playerPos = localPlayer.Position;
            
            // Convert world position to screen coordinates
            if (Plugin.GameGui.WorldToScreen(playerPos, out var screenPos))
            {
                // Create a separate window for the decal overlay
                ImGui.SetNextWindowPos(new Vector2(screenPos.X - (decalSize * 50), screenPos.Y - (decalSize * 50)));
                ImGui.SetNextWindowSize(new Vector2(decalSize * 100, decalSize * 100));
                
                var flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | 
                           ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollbar | 
                           ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoInputs |
                           ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.NoSavedSettings;

                if (ImGui.Begin("GroundDecal##DecalOverlay", flags))
                {
                    // Draw the image with opacity
                    var drawList = ImGui.GetWindowDrawList();
                    var windowPos = ImGui.GetWindowPos();
                    var windowSize = ImGui.GetWindowSize();
                    
                    // Apply opacity by modulating the color
                    var color = ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 1.0f, 1.0f, decalOpacity));
                    
                    drawList.AddImage(
                        customImage.ImGuiHandle,
                        windowPos,
                        new Vector2(windowPos.X + windowSize.X, windowPos.Y + windowSize.Y),
                        Vector2.Zero,
                        Vector2.One,
                        color
                    );
                }
                ImGui.End();
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Error drawing ground decal: {ex.Message}");
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
        
        // Check if async texture has loaded and display it directly
        if (loadingTexture != null)
        {
            var currentTexture = loadingTexture.GetWrapOrEmpty();
            if (currentTexture.Width > 1 && currentTexture.Height > 1) // Not empty texture
            {
                if (customImage == null) // First time loaded
                {
                    Plugin.Log.Info($"Async texture loaded successfully: {Path.GetFileName(customImagePath)} ({currentTexture.Width}x{currentTexture.Height})");
                }
                customImage = currentTexture;
            }
            else
            {
                // Still loading or failed - check for errors
                if (loadingTexture.TryGetWrap(out _, out var exception) && exception != null)
                {
                    Plugin.Log.Error($"Async texture load failed: {exception.Message}");
                    Plugin.Log.Error($"Exception type: {exception.GetType().Name}");
                    loadingTexture = null; // Clear the failed reference
                }
            }
        }

        if (customImage != null)
        {
            try
            {
                // Check if the texture handle is still valid
                if (customImage.ImGuiHandle != IntPtr.Zero)
                {
                    ImGui.TextUnformatted($"Loaded: {Path.GetFileName(customImagePath)} ({customImage.Width}x{customImage.Height})");
                    ImGui.Image(customImage.ImGuiHandle, new Vector2(customImage.Width, customImage.Height));
                }
                else
                {
                    ImGui.TextUnformatted("Image handle invalid - texture was disposed");
                    customImage = null; // Clear invalid texture
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"Error displaying image: {ex.Message}");
                customImage = null; // Clear problematic texture
            }
        }
        else if (loadingTexture != null)
        {
            ImGui.TextUnformatted("Loading image...");
        }
        
        // Decal System Controls
        if (customImage != null)
        {
            ImGui.Separator();
            ImGui.TextUnformatted("Ground Decal System:");
            
            if (ImGui.Checkbox("Enable Decal Under Character", ref decalEnabled))
            {
                if (decalEnabled)
                {
                    Plugin.Log.Info("Ground decal enabled");
                }
                else
                {
                    Plugin.Log.Info("Ground decal disabled");
                }
            }
            
            if (decalEnabled)
            {
                ImGui.SliderFloat("Decal Size", ref decalSize, 0.5f, 10.0f);
                ImGui.SliderFloat("Decal Opacity", ref decalOpacity, 0.1f, 1.0f);
                
                // Show current player position for debugging
                var localPlayer = Plugin.ClientState.LocalPlayer;
                if (localPlayer != null)
                {
                    var pos = localPlayer.Position;
                    ImGui.TextUnformatted($"Player Position: X={pos.X:F2}, Y={pos.Y:F2}, Z={pos.Z:F2}");
                    
                    // Update position tracking
                    lastPlayerPosition = pos;
                }
                else
                {
                    ImGui.TextUnformatted("Player not found");
                }
                
                // Draw ground decal if enabled
                if (decalEnabled && customImage != null)
                {
                    DrawGroundDecal();
                }
            }
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
