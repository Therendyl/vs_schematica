using Schematica.Core;
using System;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Schematica.GUI
{
    public class SchematicaLoadDialog : GuiDialog
    {
        private SchematicaModSystem modSystem;
        private string selectedSchematic;
        private BlockPos renderPos;
        private bool showAllLayers = false;
        private bool updatingFromCode = false;
        private BlockPos worldSpawn;

        public override string ToggleKeyCombinationCode => null;

        public SchematicaLoadDialog(ICoreClientAPI capi, SchematicaModSystem modSystem) : base(capi)
        {
            this.modSystem = modSystem;
            this.worldSpawn = capi.World.DefaultSpawnPosition.AsBlockPos;

            // Load saved state
            renderPos = modSystem.GuiState.RenderPos?.Copy() ?? capi.World.Player.Entity.Pos.AsBlockPos.Copy();
            selectedSchematic = modSystem.GuiState.SelectedSchematic;
            showAllLayers = modSystem.GuiState.ShowAllLayers;

            SetupDialog();
        }

        private void SetupDialog()
        {
            ElementBounds dialogBounds = ElementBounds
                .Fixed(0, 0, 500, 600)
                .WithAlignment(EnumDialogArea.CenterMiddle);

            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            bgBounds.BothSizing = ElementSizing.FitToChildren;

            var composer = capi.Gui
                .CreateCompo("schematica_load_dialog", dialogBounds)
                .AddShadedDialogBG(bgBounds)
                .AddDialogTitleBar(Lang.Get("schematica:gui-load-title"), OnTitleBarClose)
                .BeginChildElements(bgBounds);

            // Schematic selection
            composer.AddStaticText(Lang.Get("schematica:gui-select-schematic"), CairoFont.WhiteDetailText(),
                ElementBounds.Fixed(0, 5, 460, 20));
            composer.AddDropDown(GetSchematicsList(), GetSchematicsList(), 0, OnSchematicSelected,
                ElementBounds.Fixed(0, 30, 460, 30), "schematicsList");
            composer.AddRichtext("", CairoFont.WhiteDetailText(),
                ElementBounds.Fixed(0, 70, 460, 80), "infoText");

            // Control buttons
            composer.AddSmallButton(Lang.Get("schematica:gui-load"), OnLoadClick,
                ElementBounds.Fixed(0, 160, 80, 30), EnumButtonStyle.Normal);
            composer.AddSmallButton(Lang.Get("schematica:gui-refresh"), OnRefreshClick,
                ElementBounds.Fixed(90, 160, 80, 30), EnumButtonStyle.Normal);
            composer.AddSmallButton(Lang.Get("schematica:gui-clear"), OnClearClick,
                ElementBounds.Fixed(180, 160, 80, 30), EnumButtonStyle.Normal);
            composer.AddSmallButton(Lang.Get("schematica:gui-set-here"), OnSetHereClick,
                ElementBounds.Fixed(270, 160, 100, 30), EnumButtonStyle.Normal);
            composer.AddSmallButton(Lang.Get("schematica:gui-info"), OnInfoClick,
                ElementBounds.Fixed(380, 160, 80, 30), EnumButtonStyle.Normal);

            var builder = new SchematicaUIBuilder(composer, capi);

            // Position controls with direction buttons
            builder.AddPositionInputs(
                Lang.Get("schematica:gui-render-position"), 210,
                renderPos, worldSpawn,
                OnPosXChanged, OnPosYChanged, OnPosZChanged,
                "posX", "posY", "posZ"
            );

            builder.AddDirectionButtons(280,
                () => { ModifyPosition('z', -1); return true; },
                () => { ModifyPosition('z', 1); return true; },
                () => { ModifyPosition('x', 1); return true; },
                () => { ModifyPosition('x', -1); return true; },
                () => { ModifyPosition('y', 1); return true; },
                () => { ModifyPosition('y', -1); return true; }
            );

            // Transform controls
            builder.AddTransformControls(330,
                OnRotateLeft, OnRotateRight,
                OnMirrorX, OnMirrorY, OnMirrorZ
            );

            // Layer controls
            builder.AddLayerControls(420,
                OnLayerToggle,
                (int value) => { OnLayerChanged(value); return true; },
                "layerToggle", "layerSlider", "layerText"
            );

            SingleComposer = composer.EndChildElements().Compose();

            if (renderPos == null)
            {
                renderPos = capi.World.Player.Entity.Pos.AsBlockPos.Copy();
                modSystem.GuiState.RenderPos = renderPos.Copy();
            }

            // Restore selected schematic
            if (!string.IsNullOrEmpty(selectedSchematic))
            {
                var dropdown = SingleComposer.GetDropDown("schematicsList");
                var schematics = GetSchematicsList();
                var index = Array.IndexOf(schematics, selectedSchematic);
                if (index >= 0)
                {
                    dropdown.SetSelectedIndex(index);
                }
            }

            UpdateInfo(selectedSchematic);
            UpdateLayerControls();
            UpdatePositionInputs();
        }

        private string[] GetSchematicsList()
        {
            try
            {
                return BlockSchematicStructure.GetAvailableSchematics(capi).OrderBy(x => x).ToArray();
            }
            catch
            {
                return new string[0];
            }
        }

        private void OnSchematicSelected(string code, bool selected)
        {
            selectedSchematic = code;
            modSystem.GuiState.SelectedSchematic = selectedSchematic;
            UpdateInfo(selectedSchematic);
        }

        private bool OnRotateLeft()
        {
            if (modSystem.CurrentSchematic == null) return true;
            modSystem.CurrentSchematic.TransformWhilePacked(capi.World, EnumOrigin.BottomCenter, -90, null);
            modSystem.CurrentSchematic.Unpack(capi);
            modSystem.Renderer.UpdateRender();
            return true;
        }

        private bool OnRotateRight()
        {
            if (modSystem.CurrentSchematic == null) return true;
            modSystem.CurrentSchematic.TransformWhilePacked(capi.World, EnumOrigin.BottomCenter, 90, null);
            modSystem.CurrentSchematic.Unpack(capi);
            modSystem.Renderer.UpdateRender();
            return true;
        }

        private bool OnMirrorX()
        {
            if (modSystem.CurrentSchematic == null) return true;
            modSystem.CurrentSchematic.TransformWhilePacked(capi.World, EnumOrigin.BottomCenter, 0, EnumAxis.X);
            modSystem.CurrentSchematic.Unpack(capi);
            modSystem.Renderer.UpdateRender();
            return true;
        }

        private bool OnMirrorY()
        {
            if (modSystem.CurrentSchematic == null) return true;
            modSystem.CurrentSchematic.TransformWhilePacked(capi.World, EnumOrigin.BottomCenter, 0, EnumAxis.Y);
            modSystem.CurrentSchematic.Unpack(capi);
            modSystem.Renderer.UpdateRender();
            return true;
        }

        private bool OnMirrorZ()
        {
            if (modSystem.CurrentSchematic == null) return true;
            modSystem.CurrentSchematic.TransformWhilePacked(capi.World, EnumOrigin.BottomCenter, 0, EnumAxis.Z);
            modSystem.CurrentSchematic.Unpack(capi);
            modSystem.Renderer.UpdateRender();
            return true;
        }

        private bool OnInfoClick()
        {
            if (modSystem.CurrentSchematic == null)
            {
                capi.ShowChatMessage(Lang.Get("schematica:msg-please-select"));
                return true;
            }

            try
            {
                var infoDialog = new SchematicaInfoDialog(capi);
                infoDialog.SetSchematic(modSystem.CurrentSchematic);
                infoDialog.TryOpen();
            }
            catch (Exception ex)
            {
                capi.ShowChatMessage($"Error opening info dialog: {ex.Message}");
            }

            return true;
        }

        private void OnPosXChanged(string value)
        {
            if (updatingFromCode) return;
            if (int.TryParse(value, out int relX))
            {
                renderPos.X = relX + worldSpawn.X;
                modSystem.GuiState.RenderPos = renderPos.Copy();
                UpdateRenderPosition();
            }
        }

        private void OnPosYChanged(string value)
        {
            if (updatingFromCode) return;
            if (int.TryParse(value, out int y))
            {
                renderPos.Y = y;
                modSystem.GuiState.RenderPos = renderPos.Copy();
                UpdateRenderPosition();
            }
        }

        private void OnPosZChanged(string value)
        {
            if (updatingFromCode) return;
            if (int.TryParse(value, out int relZ))
            {
                renderPos.Z = relZ + worldSpawn.Z;
                modSystem.GuiState.RenderPos = renderPos.Copy();
                UpdateRenderPosition();
            }
        }

        private bool ModifyPosition(char axis, int delta)
        {
            switch (axis)
            {
                case 'x': renderPos.X += delta; break;
                case 'y': renderPos.Y += delta; break;
                case 'z': renderPos.Z += delta; break;
            }

            modSystem.GuiState.RenderPos = renderPos.Copy();
            UpdatePositionInputs();
            UpdateRenderPosition();
            return true;
        }

        private void UpdateRenderPosition()
        {
            if (modSystem.CurrentSchematic != null)
            {
                modSystem.Renderer.SetRenderOrigin(renderPos);
            }
        }

        private void UpdatePositionInputs()
        {
            updatingFromCode = true;
            var relX = renderPos.X - worldSpawn.X;
            var relZ = renderPos.Z - worldSpawn.Z;

            capi.Logger.Debug($"[Schematica] UpdatePositionInputs: renderPos={renderPos}, worldSpawn={worldSpawn}, relative=({relX}, {renderPos.Y}, {relZ})");

            SingleComposer.GetTextInput("posX").SetValue(relX.ToString());
            SingleComposer.GetTextInput("posY").SetValue(renderPos.Y.ToString());
            SingleComposer.GetTextInput("posZ").SetValue(relZ.ToString());
            updatingFromCode = false;
        }

        private bool OnSetHereClick()
        {
            renderPos = capi.World.Player.Entity.Pos.AsBlockPos.Copy();
            modSystem.GuiState.RenderPos = renderPos.Copy();
            UpdatePositionInputs();
            UpdateRenderPosition();
            return true;
        }

        private void OnLayerToggle(bool on)
        {
            showAllLayers = on;
            modSystem.GuiState.ShowAllLayers = on;
            if (modSystem.CurrentSchematic != null)
            {
                modSystem.Renderer.SetShowAllLayers(on);
            }
            UpdateLayerControls();
        }

        private bool OnLoadClick()
        {
            if (string.IsNullOrEmpty(selectedSchematic))
            {
                capi.ShowChatMessage(Lang.Get("schematica:msg-please-select"));
                return true;
            }

            try
            {
                var schematic = BlockSchematicStructure.LoadFromFile(capi, selectedSchematic);
                modSystem.LoadSchematic(schematic);

                if (renderPos == null)
                {
                    renderPos = capi.World.Player.Entity.Pos.AsBlockPos.Copy();
                }

                modSystem.Renderer.SetRenderOrigin(renderPos);
                modSystem.Renderer.SetShowAllLayers(showAllLayers);
                capi.ShowChatMessage(Lang.Get("schematica:msg-schematic-loaded", selectedSchematic, schematic.TotalBlocks, schematic.MaxY + 1));
                UpdateLayerControls();
            }
            catch (Exception e)
            {
                capi.ShowChatMessage(Lang.Get("schematica:msg-failed-load", e.Message));
            }

            return true;
        }

        private bool OnRefreshClick()
        {
            RefreshSchematicsList();
            return true;
        }

        private bool OnClearClick()
        {
            modSystem.ClearSchematic();
            UpdateLayerControls();
            capi.ShowChatMessage(Lang.Get("schematica:msg-cleared"));
            return true;
        }

        private void OnLayerChanged(int value)
        {
            if (modSystem.CurrentSchematic == null || showAllLayers) return;

            int layer = (int)((float)value / 100f * modSystem.CurrentSchematic.MaxY);
            modSystem.Renderer.SetLayer(layer);
            UpdateLayerText();
        }

        private void UpdateInfo(string selectedSchematic)
        {
            var infoLabel = SingleComposer?.GetRichtext("infoText");
            if (infoLabel == null) return;

            if (string.IsNullOrEmpty(selectedSchematic))
            {
                infoLabel.SetNewText("", CairoFont.WhiteDetailText());
                return;
            }

            try
            {
                var schematic = BlockSchematicStructure.LoadFromFile(capi, selectedSchematic);

                var infoText = new StringBuilder();
                infoText.AppendLine($"Size: {schematic.SizeX}x{schematic.SizeY}x{schematic.SizeZ}");
                infoText.AppendLine($"Total blocks: {schematic.TotalBlocks}");

                infoLabel.SetNewText(infoText.ToString(), CairoFont.WhiteDetailText());
            }
            catch (Exception e)
            {
                infoLabel?.SetNewText($"Error loading info: {e.Message}", CairoFont.WhiteDetailText());
            }
        }

        private void UpdateLayerControls()
        {
            var slider = SingleComposer.GetSlider("layerSlider");
            var toggle = SingleComposer.GetSwitch("layerToggle");

            bool hasSchematic = modSystem.CurrentSchematic != null;
            slider.Enabled = hasSchematic && !showAllLayers;
            toggle.Enabled = hasSchematic;

            if (hasSchematic)
            {
                slider.SetValue((int)((float)modSystem.Renderer.CurrentLayer / modSystem.CurrentSchematic.MaxY * 100));
                toggle.SetValue(showAllLayers);
            }
            else
            {
                slider.SetValue(0);
                toggle.SetValue(false);
            }

            UpdateLayerText();
        }

        private void UpdateLayerText()
        {
            var layerText = SingleComposer.GetDynamicText("layerText");

            if (modSystem.CurrentSchematic != null && !showAllLayers)
            {
                layerText.SetNewText(Lang.Get("schematica:gui-current-layer",
                    modSystem.Renderer.CurrentLayer, modSystem.CurrentSchematic.MaxY));
            }
            else if (showAllLayers)
            {
                layerText.SetNewText(Lang.Get("schematica:gui-showing-all-layers"));
            }
            else
            {
                layerText.SetNewText(Lang.Get("schematica:gui-no-schematic"));
            }
        }

        private void RefreshSchematicsList()
        {
            var newList = GetSchematicsList();
            var dropdown = SingleComposer.GetDropDown("schematicsList");
            dropdown.SetList(newList, newList);
            selectedSchematic = null;
            UpdateInfo(selectedSchematic);
        }

        private void OnTitleBarClose()
        {
            // Save state on close
            modSystem.GuiState.RenderPos = renderPos.Copy();
            modSystem.GuiState.SelectedSchematic = selectedSchematic;
            modSystem.GuiState.ShowAllLayers = showAllLayers;

            TryClose();
        }

        public override void OnGuiOpened()
        {
            base.OnGuiOpened();
            RefreshSchematicsList();
            UpdateLayerControls();
        }
    }
}