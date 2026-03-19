using Schematica.Core;
using Schematica.GUI;
using System;
using System.IO;
using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.CommandAbbr;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Schematica.Commands
{
    public class SchematicCommands
    {
        private readonly ICoreClientAPI capi;
        private readonly SchematicaModSystem modSystem;
        private BlockPos? startPos, endPos;

        public SchematicCommands(ICoreClientAPI api, SchematicaModSystem modSystem)
        {
            this.capi = api;
            this.modSystem = modSystem;
            RegisterCommands();
        }

        private void RegisterCommands()
        {
            capi.ChatCommands.GetOrCreate("schem")
                .WithDescription("Schematica Plus commands")
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
                .EndSub()
                .BeginSubCommand("profile")
                    .WithDescription("Profiling commands")
                    .BeginSubCommand("start")
                        .WithDescription("Start automated profiling run")
                        .HandleWith(OnCmdProfileStart)
                    .EndSub()
                    .BeginSubCommand("stop")
                        .WithDescription("Stop profiling run")
                        .HandleWith(OnCmdProfileStop)
                    .EndSub()
                    .BeginSubCommand("status")
                        .WithDescription("Show profiling status")
                        .HandleWith(OnCmdProfileStatus)
                    .EndSub()
                    .BeginSubCommand("baseline")
                        .WithDescription("Promote latest summary as baseline")
                        .HandleWith(OnCmdProfileBaseline)
                    .EndSub()
                    .BeginSubCommand("reload")
                        .WithDescription("Reload profiling config from disk")
                        .HandleWith(OnCmdProfileReload)
                    .EndSub()
                    .BeginSubCommand("runtime")
                        .WithDescription("Reload renderer runtime optimization config")
                        .HandleWith(OnCmdProfileRuntimeReload)
                    .EndSub()
                    .BeginSubCommand("burst")
                        .WithDescription("Enable temporary renderer debug burst (seconds)")
                        .WithArgs(capi.ChatCommands.Parsers.Int("seconds"))
                        .HandleWith(OnCmdProfileBurst)
                    .EndSub()
                .EndSub();
        }

        private TextCommandResult OnCmdStart(TextCommandCallingArgs args)
        {
            var sel = capi.World.Player.CurrentBlockSelection;
            if (sel == null) return TextCommandResult.Error("You must be looking at a block!");

            startPos = sel.Position.Copy();
            capi.ShowChatMessage(Lang.Get("schematicaplus:msg-set-start", startPos));
            return TextCommandResult.Success();
        }

        private TextCommandResult OnCmdEnd(TextCommandCallingArgs args)
        {
            var sel = capi.World.Player.CurrentBlockSelection;
            if (sel == null) return TextCommandResult.Error("You must be looking at a block!");

            endPos = sel.Position.Copy();
            capi.ShowChatMessage(Lang.Get("schematicaplus:msg-set-end", endPos));
            return TextCommandResult.Success();
        }

        private TextCommandResult OnCmdSave(TextCommandCallingArgs args)
        {
            var begin = startPos;
            var end = endPos;

            if (begin == null || end == null)
                return TextCommandResult.Error("You must set both start and end positions first!");

            string filename = (string)args[0];

            try
            {
                var schematic = BlockSchematicStructure.CreateFromSelection(capi, begin, end);
                BlockSchematicStructure.SaveToFile(capi, schematic, filename);

                capi.ShowChatMessage(Lang.Get("schematicaplus:msg-schematic-saved", filename, schematic.TotalBlocks));
                return TextCommandResult.Success();
            }
            catch (ArgumentNullException ex)
            {
                return TextCommandResult.Error(ex.Message);
            }
            catch (JsonException ex)
            {
                return TextCommandResult.Error(Lang.Get("schematicaplus:msg-failed-save", ex.Message));
            }
            catch (IOException ex)
            {
                return TextCommandResult.Error(Lang.Get("schematicaplus:msg-failed-save", ex.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                return TextCommandResult.Error(Lang.Get("schematicaplus:msg-failed-save", ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return TextCommandResult.Error(Lang.Get("schematicaplus:msg-failed-save", ex.Message));
            }
        }

        private TextCommandResult OnCmdLoad(TextCommandCallingArgs args)
        {
            string filename = (string)args[0];

            try
            {
                var schematic = BlockSchematicStructure.LoadFromFile(capi, filename);
                modSystem.LoadSchematic(schematic);

                capi.ShowChatMessage(Lang.Get("schematicaplus:msg-schematic-loaded", filename, schematic.TotalBlocks, schematic.MaxY + 1));
                return TextCommandResult.Success();
            }
            catch (ArgumentNullException ex)
            {
                return TextCommandResult.Error(ex.Message);
            }
            catch (JsonException ex)
            {
                return TextCommandResult.Error(Lang.Get("schematicaplus:msg-failed-load", ex.Message));
            }
            catch (IOException ex)
            {
                return TextCommandResult.Error(Lang.Get("schematicaplus:msg-failed-load", ex.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                return TextCommandResult.Error(Lang.Get("schematicaplus:msg-failed-load", ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return TextCommandResult.Error(Lang.Get("schematicaplus:msg-failed-load", ex.Message));
            }
        }

        private TextCommandResult OnCmdHere(TextCommandCallingArgs args)
        {
            if (modSystem.CurrentSchematic == null)
                return TextCommandResult.Error(Lang.Get("schematicaplus:msg-please-select"));

            var sel = capi.World.Player.CurrentBlockSelection;
            if (sel == null)
                return TextCommandResult.Error("You must be looking at a block!");

            modSystem.Renderer.SetRenderOrigin(sel.Position);
            capi.ShowChatMessage(Lang.Get("schematicaplus:msg-set-render", modSystem.Renderer.CurrentLayer, modSystem.CurrentSchematic.MaxY));
            return TextCommandResult.Success();
        }

        private TextCommandResult OnCmdClear(TextCommandCallingArgs args)
        {
            modSystem.ClearSchematic();
            capi.ShowChatMessage(Lang.Get("schematicaplus:msg-cleared"));
            return TextCommandResult.Success();
        }

        private TextCommandResult OnCmdLayerSet(TextCommandCallingArgs args)
        {
            if (modSystem.CurrentSchematic == null)
                return TextCommandResult.Error(Lang.Get("schematicaplus:msg-please-select"));

            int layer = (int)args[0];
            modSystem.Renderer.SetLayer(layer);
            return TextCommandResult.Success();
        }

        private TextCommandResult OnCmdLayerNext(TextCommandCallingArgs args)
        {
            if (modSystem.CurrentSchematic == null)
                return TextCommandResult.Error(Lang.Get("schematicaplus:msg-please-select"));

            modSystem.Renderer.NextLayer();
            return TextCommandResult.Success();
        }

        private TextCommandResult OnCmdLayerPrev(TextCommandCallingArgs args)
        {
            if (modSystem.CurrentSchematic == null)
                return TextCommandResult.Error(Lang.Get("schematicaplus:msg-please-select"));

            modSystem.Renderer.PreviousLayer();
            return TextCommandResult.Success();
        }

        private TextCommandResult OnCmdLayerAll(TextCommandCallingArgs args)
        {
            if (modSystem.CurrentSchematic == null)
                return TextCommandResult.Error(Lang.Get("schematicaplus:msg-please-select"));

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
                    capi.ShowChatMessage(Lang.Get("schematicaplus:msg-no-schematics"));
                }
                else
                {
                    capi.ShowChatMessage(Lang.Get("schematicaplus:msg-available-schematics", schematics.Count));
                    foreach (var schematic in schematics)
                    {
                        capi.ShowChatMessage($"  - {schematic}");
                    }
                }
                return TextCommandResult.Success();
            }
            catch (JsonException ex)
            {
                return TextCommandResult.Error($"Failed to list schematics: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                return TextCommandResult.Error($"Failed to list schematics: {ex.Message}");
            }
            catch (IOException ex)
            {
                return TextCommandResult.Error($"Failed to list schematics: {ex.Message}");
            }
        }

        private TextCommandResult OnCmdGui(TextCommandCallingArgs args)
        {
            modSystem.ShowMainDialog();
            return TextCommandResult.Success();
        }

        private TextCommandResult OnCmdProfileStart(TextCommandCallingArgs args)
        {
            var profiler = modSystem.ProfilingManager;
            if (profiler == null)
            {
                return TextCommandResult.Error("Profiling subsystem is not initialized.");
            }

            bool started = profiler.TryStart(out string message);
            capi.ShowChatMessage(message);
            return started ? TextCommandResult.Success() : TextCommandResult.Error(message);
        }

        private TextCommandResult OnCmdProfileStop(TextCommandCallingArgs args)
        {
            var profiler = modSystem.ProfilingManager;
            if (profiler == null)
            {
                return TextCommandResult.Error("Profiling subsystem is not initialized.");
            }

            bool stopped = profiler.Stop(out string message);
            capi.ShowChatMessage(message);
            return stopped ? TextCommandResult.Success() : TextCommandResult.Error(message);
        }

        private TextCommandResult OnCmdProfileStatus(TextCommandCallingArgs args)
        {
            var profiler = modSystem.ProfilingManager;
            if (profiler == null)
            {
                return TextCommandResult.Error("Profiling subsystem is not initialized.");
            }

            foreach (string line in profiler.GetStatusLines())
            {
                capi.ShowChatMessage(line);
            }
            capi.ShowChatMessage($"Renderer runtime config: {modSystem.Renderer.RuntimeConfigPath}");

            return TextCommandResult.Success();
        }

        private TextCommandResult OnCmdProfileBaseline(TextCommandCallingArgs args)
        {
            var profiler = modSystem.ProfilingManager;
            if (profiler == null)
            {
                return TextCommandResult.Error("Profiling subsystem is not initialized.");
            }

            bool updated = profiler.TryPromoteLatestSummaryToBaseline(baselineName: null, out string message);
            capi.ShowChatMessage(message);
            return updated ? TextCommandResult.Success() : TextCommandResult.Error(message);
        }

        private TextCommandResult OnCmdProfileReload(TextCommandCallingArgs args)
        {
            var profiler = modSystem.ProfilingManager;
            if (profiler == null)
            {
                return TextCommandResult.Error("Profiling subsystem is not initialized.");
            }

            profiler.ReloadConfig();
            capi.ShowChatMessage($"Profiling config reloaded from: {profiler.ConfigPath}");
            return TextCommandResult.Success();
        }

        private TextCommandResult OnCmdProfileRuntimeReload(TextCommandCallingArgs args)
        {
            modSystem.ReloadRuntimeConfig();
            capi.ShowChatMessage("Schematica Plus runtime optimization config reloaded.");
            return TextCommandResult.Success();
        }

        private TextCommandResult OnCmdProfileBurst(TextCommandCallingArgs args)
        {
            int seconds = (int)args[0];
            int clamped = Math.Clamp(seconds, 1, 300);
            modSystem.EnableRendererDebugBurst(clamped);
            capi.ShowChatMessage($"Schematica Plus renderer debug burst enabled for {clamped}s.");
            return TextCommandResult.Success();
        }
    }
}



