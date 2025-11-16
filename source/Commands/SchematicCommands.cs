using Schematica.Core;
using Schematica.GUI;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.CommandAbbr;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Schematica.Commands
{
    public class SchematicCommands
    {
        private ICoreClientAPI capi;
        private SchematicaModSystem modSystem;
        private BlockPos startPos, endPos;

        public SchematicCommands(ICoreClientAPI api, SchematicaModSystem modSystem)
        {
            this.capi = api;
            this.modSystem = modSystem;
            RegisterCommands();
        }

        private void RegisterCommands()
        {
            capi.ChatCommands.GetOrCreate("schem")
                .WithDescription("Schematica commands")
                .RequiresPlayer()
                .BeginSubCommand("start")
                    .WithDescription("Set start position")
                    .HandleWith(OnCmdStart)
                .EndSub()
                .BeginSubCommand("end")
                    .WithDescription("Set end position")
                    .HandleWith(OnCmdEnd)
                .EndSub()
                .BeginSubCommand("save")
                    .WithDescription("Save schematic")
                    .WithArgs(capi.ChatCommands.Parsers.Word("filename"))
                    .HandleWith(OnCmdSave)
                .EndSub()
                .BeginSubCommand("load")
                    .WithDescription("Load schematic")
                    .WithArgs(capi.ChatCommands.Parsers.Word("filename"))
                    .HandleWith(OnCmdLoad)
                .EndSub()
                .BeginSubCommand("here")
                    .WithDescription("Set render position")
                    .HandleWith(OnCmdHere)
                .EndSub()
                .BeginSubCommand("clear")
                    .WithDescription("Clear projections")
                    .HandleWith(OnCmdClear)
                .EndSub()
                .BeginSubCommand("layer")
                    .WithDescription("Layer commands")
                    .BeginSubCommand("set")
                        .WithArgs(capi.ChatCommands.Parsers.Int("layer"))
                        .HandleWith(OnCmdLayerSet)
                    .EndSub()
                    .BeginSubCommand("next")
                        .HandleWith(OnCmdLayerNext)
                    .EndSub()
                    .BeginSubCommand("prev")
                        .HandleWith(OnCmdLayerPrev)
                    .EndSub()
                    .BeginSubCommand("all")
                        .HandleWith(OnCmdLayerAll)
                    .EndSub()
                .EndSub()
                .BeginSubCommand("list")
                    .WithDescription("List saved schematics")
                    .HandleWith(OnCmdList)
                .EndSub()
                .BeginSubCommand("gui")
                    .WithDescription("Open GUI")
                    .HandleWith(OnCmdGui)
                .EndSub();
        }

        private TextCommandResult OnCmdStart(TextCommandCallingArgs args)
        {
            var sel = capi.World.Player.CurrentBlockSelection;
            if (sel == null) return TextCommandResult.Error("You must be looking at a block!");

            startPos = sel.Position.Copy();
            capi.ShowChatMessage(Lang.Get("schematica:msg-set-start", startPos));
            return TextCommandResult.Success();
        }

        private TextCommandResult OnCmdEnd(TextCommandCallingArgs args)
        {
            var sel = capi.World.Player.CurrentBlockSelection;
            if (sel == null) return TextCommandResult.Error("You must be looking at a block!");

            endPos = sel.Position.Copy();
            capi.ShowChatMessage(Lang.Get("schematica:msg-set-end", endPos));
            return TextCommandResult.Success();
        }

        private TextCommandResult OnCmdSave(TextCommandCallingArgs args)
        {
            if (startPos == null || endPos == null)
                return TextCommandResult.Error("You must set both start and end positions first!");

            string filename = (string)args[0];

            try
            {
                var schematic = BlockSchematicStructure.CreateFromSelection(capi, startPos, endPos);
                BlockSchematicStructure.SaveToFile(capi, schematic, filename);

                capi.ShowChatMessage(Lang.Get("schematica:msg-schematic-saved", filename, schematic.TotalBlocks));
                return TextCommandResult.Success();
            }
            catch (Exception e)
            {
                return TextCommandResult.Error(Lang.Get("schematica:msg-failed-save", e.Message));
            }
        }

        private TextCommandResult OnCmdLoad(TextCommandCallingArgs args)
        {
            string filename = (string)args[0];

            try
            {
                var schematic = BlockSchematicStructure.LoadFromFile(capi, filename);
                modSystem.LoadSchematic(schematic);

                capi.ShowChatMessage(Lang.Get("schematica:msg-schematic-loaded", filename, schematic.TotalBlocks, schematic.MaxY + 1));
                return TextCommandResult.Success();
            }
            catch (Exception e)
            {
                return TextCommandResult.Error(Lang.Get("schematica:msg-failed-load", e.Message));
            }
        }

        private TextCommandResult OnCmdHere(TextCommandCallingArgs args)
        {
            if (modSystem.CurrentSchematic == null)
                return TextCommandResult.Error(Lang.Get("schematica:msg-please-select"));

            var sel = capi.World.Player.CurrentBlockSelection;
            if (sel == null)
                return TextCommandResult.Error("You must be looking at a block!");

            modSystem.Renderer.SetRenderOrigin(sel.Position);
            capi.ShowChatMessage(Lang.Get("schematica:msg-set-render", modSystem.Renderer.CurrentLayer, modSystem.CurrentSchematic.MaxY));
            return TextCommandResult.Success();
        }

        private TextCommandResult OnCmdClear(TextCommandCallingArgs args)
        {
            modSystem.ClearSchematic();
            capi.ShowChatMessage(Lang.Get("schematica:msg-cleared"));
            return TextCommandResult.Success();
        }

        private TextCommandResult OnCmdLayerSet(TextCommandCallingArgs args)
        {
            if (modSystem.CurrentSchematic == null)
                return TextCommandResult.Error(Lang.Get("schematica:msg-please-select"));

            int layer = (int)args[0];
            modSystem.Renderer.SetLayer(layer);
            return TextCommandResult.Success();
        }

        private TextCommandResult OnCmdLayerNext(TextCommandCallingArgs args)
        {
            if (modSystem.CurrentSchematic == null)
                return TextCommandResult.Error(Lang.Get("schematica:msg-please-select"));

            modSystem.Renderer.NextLayer();
            return TextCommandResult.Success();
        }

        private TextCommandResult OnCmdLayerPrev(TextCommandCallingArgs args)
        {
            if (modSystem.CurrentSchematic == null)
                return TextCommandResult.Error(Lang.Get("schematica:msg-please-select"));

            modSystem.Renderer.PreviousLayer();
            return TextCommandResult.Success();
        }

        private TextCommandResult OnCmdLayerAll(TextCommandCallingArgs args)
        {
            if (modSystem.CurrentSchematic == null)
                return TextCommandResult.Error(Lang.Get("schematica:msg-please-select"));

            modSystem.Renderer.ToggleAllLayers();
            return TextCommandResult.Success();
        }

        private TextCommandResult OnCmdList(TextCommandCallingArgs args)
        {
            try
            {
                var schematics = BlockSchematicStructure.GetAvailableSchematics(capi);
                if (schematics.Count == 0)
                {
                    capi.ShowChatMessage(Lang.Get("schematica:msg-no-schematics"));
                }
                else
                {
                    capi.ShowChatMessage(Lang.Get("schematica:msg-available-schematics", schematics.Count));
                    foreach (var schematic in schematics)
                    {
                        capi.ShowChatMessage($"  - {schematic}");
                    }
                }
                return TextCommandResult.Success();
            }
            catch (Exception e)
            {
                return TextCommandResult.Error($"Failed to list schematics: {e.Message}");
            }
        }

        private TextCommandResult OnCmdGui(TextCommandCallingArgs args)
        {
            var dialog = new SchematicaMainDialog(capi, modSystem);
            dialog.TryOpen();
            return TextCommandResult.Success();
        }
    }
}