using System;
using Schematica.Commands;
using Schematica.Core;
using Schematica.GUI;
using Schematica.Profiling;
using Schematica.Rendering;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Schematica
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "Disposal is handled by the Vintage Story ModSystem lifecycle via override Dispose().")]
    public sealed class SchematicaModSystem : ModSystem
    {
        private ICoreClientAPI? capi;
        private SchematicCommands? commands;
        private GuiDialog? currentDialog;
        private int updateCounter;
        private bool disposed;
        private long updateTickListenerId = -1;
        private SchematicaProfilingManager? profilingManager;
        private Action? reloadTexturesHandler;

        // Saved GUI data
        public SchematicaGuiState GuiState { get; private set; } = new SchematicaGuiState();
        public BlockSchematicStructure? CurrentSchematic { get; private set; }
        public SchematicRenderer Renderer { get; private set; } = null!;
        public SchematicaProfilingManager? ProfilingManager => profilingManager;

        public override void StartClientSide(ICoreClientAPI api)
        {
            ArgumentNullException.ThrowIfNull(api);

            this.capi = api;

            // Initialize GUI state
            GuiState = new SchematicaGuiState();

            // Initialize components
            Renderer = new SchematicRenderer(api, this);
            profilingManager = new SchematicaProfilingManager(api, this, Renderer);
            Renderer.ProfilingSink = profilingManager;
            commands = new SchematicCommands(api, this);

            // Register renderer
            api.Event.RegisterRenderer(Renderer, EnumRenderStage.Opaque, "schematica_projection");
            reloadTexturesHandler = () =>
            {
                Renderer.ClearRuntimeCaches();
                Renderer.ReloadRuntimeConfig();
            };
            api.Event.ReloadTextures += reloadTexturesHandler;

            // Register GUI hotkey
            api.Input.RegisterHotKey("schematica_gui", "Open Schematica GUI", GlKeys.L, HotkeyType.GUIOrOtherControls);
            api.Input.SetHotKeyHandler("schematica_gui", OnGuiHotkey);

            // Register layer hotkeys
            api.Input.RegisterHotKey("schematica_layer_up", "Schematica Layer Up", GlKeys.PageUp, HotkeyType.GUIOrOtherControls);
            api.Input.RegisterHotKey("schematica_layer_down", "Schematica Layer Down", GlKeys.PageDown, HotkeyType.GUIOrOtherControls);
            api.Input.SetHotKeyHandler("schematica_layer_up", (t) => { Renderer.NextLayer(); return true; });
            api.Input.SetHotKeyHandler("schematica_layer_down", (t) => { Renderer.PreviousLayer(); return true; });

            // Register tick listener for updating chunks
            updateTickListenerId = api.Event.RegisterGameTickListener((dt) =>
            {
                profilingManager?.OnTick(dt);
                if (profilingManager?.IsRunning == true)
                {
                    return;
                }

                if (CurrentSchematic != null)
                {
                    updateCounter++;
                    if (updateCounter >= 20) // Every 20 ticks (1 second)
                    {
                        updateCounter = 0;
                        var playerPos = api.World.Player.Entity.Pos.AsBlockPos;
                        Renderer.UpdateChunksNearPlayer(playerPos, 10);
                    }
                }
            }, 50); // Every 50ms
        }

        public bool ShowDialog(GuiDialog dialog)
        {
            ArgumentNullException.ThrowIfNull(dialog);

            currentDialog?.TryClose();
            currentDialog = dialog;
            return currentDialog.TryOpen();
        }

        public bool HasOpenDialog => currentDialog?.IsOpened() == true;

        private bool OnGuiHotkey(KeyCombination t)
        {
            if (capi == null)
            {
                return false;
            }

            if (!HasOpenDialog)
            {
                return ShowDialog(new SchematicaMainDialog(capi, this));
            }

            currentDialog?.TryClose();
            currentDialog = null;
            return true;
        }

        public void LoadSchematic(BlockSchematicStructure schematic)
        {
            ArgumentNullException.ThrowIfNull(schematic);

            CurrentSchematic = schematic;
            Renderer.SetSchematic(schematic);
        }

        public void ClearSchematic()
        {
            CurrentSchematic = null;
            Renderer.Clear();
        }

        public void ShowSaveDialog()
        {
            if (capi == null) throw new InvalidOperationException("Client API is not initialized");
            ShowDialog(new SchematicaSaveDialog(capi, this));
        }

        public void ShowLoadDialog()
        {
            if (capi == null) throw new InvalidOperationException("Client API is not initialized");
            ShowDialog(new SchematicaLoadDialog(capi, this));
        }

        public bool ShowMainDialog()
        {
            if (capi == null) throw new InvalidOperationException("Client API is not initialized");
            return ShowDialog(new SchematicaMainDialog(capi, this));
        }

        public override void Dispose()
        {
            if (disposed)
            {
                base.Dispose();
                return;
            }

            if (capi != null)
            {
                if (updateTickListenerId >= 0)
                {
                    capi.Event.UnregisterGameTickListener(updateTickListenerId);
                    updateTickListenerId = -1;
                }

                if (reloadTexturesHandler != null)
                {
                    capi.Event.ReloadTextures -= reloadTexturesHandler;
                    reloadTexturesHandler = null;
                }

                capi.Event.UnregisterRenderer(Renderer, EnumRenderStage.Opaque);
            }

            profilingManager?.Dispose();
            profilingManager = null;
            Renderer.ProfilingSink = null;
            Renderer?.ClearAllProjections();
            Renderer?.Dispose();
            currentDialog?.Dispose();
            currentDialog = null;
            disposed = true;

            base.Dispose();
        }

        public void ReloadRuntimeConfig()
        {
            Renderer.ReloadRuntimeConfig();
        }

        public void EnableRendererDebugBurst(int seconds)
        {
            Renderer.EnableDebugBurst(seconds);
        }
    }

    // Class for saving GUI state
    public class SchematicaGuiState
    {
        public BlockPos FirstPoint { get; set; } = new BlockPos(0, 0, 0);
        public BlockPos SecondPoint { get; set; } = new BlockPos(0, 0, 0);
        public BlockPos RenderPos { get; set; } = new BlockPos(0, 0, 0);
        public string SelectedSchematic { get; set; } = string.Empty;
        public string LastFilename { get; set; } = string.Empty;
        public bool ShowAllLayers { get; set; }
    }
}
