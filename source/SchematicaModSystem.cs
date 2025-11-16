using Schematica.Commands;
using Schematica.Core;
using Schematica.GUI;
using Schematica.Rendering;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Schematica
{
    public class SchematicaModSystem : ModSystem
    {
        private ICoreClientAPI capi;
        private SchematicCommands commands;
        private GuiDialog currentDialog;
        private int updateCounter = 0;

        // Saved GUI data
        public SchematicaGuiState GuiState { get; private set; }
        public BlockSchematicStructure CurrentSchematic { get; private set; }
        public SchematicRenderer Renderer { get; private set; }

        public override void StartClientSide(ICoreClientAPI api)
        {
            this.capi = api;

            // Initialize GUI state
            GuiState = new SchematicaGuiState();

            // Initialize components
            Renderer = new SchematicRenderer(api, this);
            commands = new SchematicCommands(api, this);

            // Register renderer
            api.Event.RegisterRenderer(Renderer, EnumRenderStage.Opaque, "schematica_projection");

            // Register GUI hotkey
            api.Input.RegisterHotKey("schematica_gui", "Open Schematica GUI", GlKeys.L, HotkeyType.GUIOrOtherControls);
            api.Input.SetHotKeyHandler("schematica_gui", OnGuiHotkey);

            // Register layer hotkeys
            api.Input.RegisterHotKey("schematica_layer_up", "Schematica Layer Up", GlKeys.PageUp, HotkeyType.GUIOrOtherControls);
            api.Input.RegisterHotKey("schematica_layer_down", "Schematica Layer Down", GlKeys.PageDown, HotkeyType.GUIOrOtherControls);
            api.Input.SetHotKeyHandler("schematica_layer_up", (t) => { Renderer.NextLayer(); return true; });
            api.Input.SetHotKeyHandler("schematica_layer_down", (t) => { Renderer.PreviousLayer(); return true; });

            // Register tick listener for updating chunks
            api.Event.RegisterGameTickListener((dt) =>
            {
                if (CurrentSchematic != null && Renderer != null)
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

        private bool OnGuiHotkey(KeyCombination t)
        {
            if (currentDialog == null || !currentDialog.IsOpened())
            {
                currentDialog = new SchematicaMainDialog(capi, this);
                currentDialog.TryOpen();
            }
            else
            {
                currentDialog.TryClose();
            }
            return true;
        }

        public void LoadSchematic(BlockSchematicStructure schematic)
        {
            CurrentSchematic = schematic;
            Renderer.SetSchematic(schematic);
        }

        public void ClearSchematic()
        {
            CurrentSchematic = null;
            Renderer.Clear();
        }

        public override void Dispose()
        {
            Renderer?.ClearAllProjections();
            Renderer?.Dispose();
            currentDialog?.Dispose();
            base.Dispose();
        }
    }

    // Class for saving GUI state
    public class SchematicaGuiState
    {
        public BlockPos FirstPoint { get; set; } = new BlockPos(0, 0, 0);
        public BlockPos SecondPoint { get; set; } = new BlockPos(0, 0, 0);
        public BlockPos RenderPos { get; set; } = new BlockPos(0, 0, 0);
        public string SelectedSchematic { get; set; }
        public string LastFilename { get; set; }
        public bool ShowAllLayers { get; set; }
    }
}