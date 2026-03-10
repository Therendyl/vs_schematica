using System;
using System.Globalization;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Schematica.Core;

namespace Schematica.GUI
{
    public class SchematicaInfoDialog : GuiDialog
    {
        private BlockSchematicStructure? schematic;

        public override string? ToggleKeyCombinationCode => null;

        public SchematicaInfoDialog(ICoreClientAPI capi) : base(capi)
        {
        }

        public void SetSchematic(BlockSchematicStructure schematic)
        {
            this.schematic = schematic;
        }

        public override void OnGuiOpened()
        {
            base.OnGuiOpened();
            SetupDialog();
        }

        private void SetupDialog()
        {
            var bounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);
            var dialogBounds = ElementStdBounds.DialogBackground();

            try
            {
                SingleComposer = capi.Gui.CreateCompo("schematicainfo", dialogBounds)
                    .AddShadedDialogBG(bounds)
                    .AddDialogTitleBar(Lang.Get("schematica:gui-info-title"), OnTitleBarCloseClicked)
                    .BeginChildElements(bounds);

                var font = CairoFont.WhiteDetailText();
                var infoText = BuildInfoText();

                var textBounds = ElementBounds.Fixed(0, 30, 400, 300);
                SingleComposer.AddRichtext(infoText, font, textBounds);

                var buttonBounds = ElementBounds.Fixed(150, 340, 100, 30);
                SingleComposer.AddSmallButton(Lang.Get("schematica:gui-close"), OnCloseClick, buttonBounds);

                SingleComposer.EndChildElements().Compose();
            }
            catch (InvalidOperationException ex)
            {
                capi.Logger.Error($"[Schematica] Error creating info dialog: {ex.Message}");
                TryClose();
            }
        }

        private string BuildInfoText()
        {
            if (schematic == null)
            {
                return Lang.Get("schematica:gui-no-schematic-data");
            }

            var sb = new StringBuilder();
            var culture = CultureInfo.InvariantCulture;
            sb.AppendLine(string.Format(culture, "{0}: {1} x {2} x {3}", Lang.Get("schematica:gui-size"), schematic.SizeX, schematic.SizeY, schematic.SizeZ));
            sb.AppendLine(string.Format(culture, "{0}: {1}", Lang.Get("schematica:gui-total-blocks"), schematic.TotalBlocks));
            sb.AppendLine();

            if (schematic.BlockCounts.Count > 0)
            {
                sb.AppendLine(Lang.Get("schematica:gui-required-blocks"));

                var sortedBlocks = schematic.BlockCounts
                    .OrderByDescending(x => x.Value)
                    .Take(15);

                foreach (var block in sortedBlocks)
                {
                    var cleanName = CleanBlockName(block.Key);
                    sb.AppendLine(string.Format(culture, "  - {0}: {1}", cleanName, block.Value));
                }

                if (schematic.BlockCounts.Count > 15)
                {
                    sb.AppendLine(string.Format(culture, "  {0}", Lang.Get("schematica:gui-and-more", schematic.BlockCounts.Count - 15)));
                }
            }
            else
            {
                sb.AppendLine(Lang.Get("schematica:gui-no-block-info"));
            }

            return sb.ToString();
        }

        private static string CleanBlockName(string blockCode)
        {
            if (string.IsNullOrEmpty(blockCode)) return Lang.Get("schematica:gui-unknown");

            // Remove game: prefix
            string clean = blockCode.Replace("game:", string.Empty, StringComparison.Ordinal);

            // Replace underscores with spaces
            clean = clean.Replace("_", " ", StringComparison.Ordinal);

            // Capitalize first letter of each word
            var words = clean.Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length > 0)
                {
                    words[i] = char.ToUpperInvariant(words[i][0]) + words[i].Substring(1);
                }
            }

            return string.Join(" ", words);
        }

        private bool OnCloseClick()
        {
            TryClose();
            return true;
        }

        private void OnTitleBarCloseClicked()
        {
            TryClose();
        }
    }
}
