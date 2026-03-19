using ImGuiNET;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Schematica.GUI
{
    public class SchematicaMainDialog : GuiDialog
    {
        private readonly SchematicaModSystem modSystem;

        public override string ToggleKeyCombinationCode => "schematicaplus_gui";

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

            // Dialog width is fixed to 300. With padding, x=40 centers 200-wide buttons.
            ElementBounds saveButtonBounds = ElementBounds.Fixed(40, 30, 200, 40);
            ElementBounds loadButtonBounds = ElementBounds.Fixed(40, 80, 200, 40);

            SingleComposer = capi.Gui
                .CreateCompo("schematicaplus_main_dialog", dialogBounds)
                .AddShadedDialogBG(bgBounds)
                .AddDialogTitleBar(Lang.Get("schematicaplus:gui-title"), OnTitleBarClose, CairoFont.WhiteDetailText())
                .BeginChildElements(bgBounds)
                    .AddButton(Lang.Get("schematicaplus:gui-save-menu"), OnSaveMenuClick, saveButtonBounds, EnumButtonStyle.Normal)
                    .AddButton(Lang.Get("schematicaplus:gui-load-menu"), OnLoadMenuClick, loadButtonBounds, EnumButtonStyle.Normal)
                .EndChildElements()
                .Compose();
        }

        private bool OnSaveMenuClick()
        {
            TryClose();
            modSystem.ShowSaveDialog();
            return true;
        }

        private bool OnLoadMenuClick()
        {
            TryClose();
            modSystem.ShowLoadDialog();
            return true;
        }

        private void OnTitleBarClose()
        {
            TryClose();
        }
    }
}



