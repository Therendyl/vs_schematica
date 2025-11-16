using ImGuiNET;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Schematica.GUI
{
    public class SchematicaMainDialog : GuiDialog
    {
        private SchematicaModSystem modSystem;

        public override string ToggleKeyCombinationCode => "schematica_gui";

        public SchematicaMainDialog(ICoreClientAPI capi, SchematicaModSystem modSystem) : base(capi)
        {
            this.modSystem = modSystem;
            SetupDialog();
        }

        private void SetupDialog()
        {
            ElementBounds dialogBounds = ElementBounds
                .Fixed(0, 0, 300, 200)
                .WithAlignment(EnumDialogArea.CenterMiddle);

            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            bgBounds.BothSizing = ElementSizing.FitToChildren;

            ElementBounds saveButtonBounds = ElementBounds.Fixed(50, 30, 200, 40);
            ElementBounds loadButtonBounds = ElementBounds.Fixed(50, 80, 200, 40);

            SingleComposer = capi.Gui
                .CreateCompo("schematica_main_dialog", dialogBounds)
                .AddShadedDialogBG(bgBounds)
                .AddDialogTitleBar(Lang.Get("schematica:gui-title"), OnTitleBarClose, CairoFont.WhiteDetailText())
                .BeginChildElements(bgBounds)
                    .AddButton(Lang.Get("schematica:gui-save-menu"), OnSaveMenuClick, saveButtonBounds, EnumButtonStyle.Normal)
                    .AddButton(Lang.Get("schematica:gui-load-menu"), OnLoadMenuClick, loadButtonBounds, EnumButtonStyle.Normal)
                .EndChildElements()
                .Compose();
        }

        private bool OnSaveMenuClick()
        {
            TryClose();
            var saveDialog = new SchematicaSaveDialog(capi, modSystem);
            saveDialog.TryOpen();
            return true;
        }

        private bool OnLoadMenuClick()
        {
            TryClose();
            var loadDialog = new SchematicaLoadDialog(capi, modSystem);
            loadDialog.TryOpen();
            return true;
        }

        private void OnTitleBarClose()
        {
            TryClose();
        }
    }
}