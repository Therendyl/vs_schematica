using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Schematica.GUI
{
    public class SchematicaUIBuilder
    {
        private GuiComposer composer;
        private ICoreClientAPI capi;

        public SchematicaUIBuilder(GuiComposer composer, ICoreClientAPI api)
        {
            this.composer = composer;
            this.capi = api;
        }

        /// <summary>
        /// Adds coordinate input controls with +/- buttons
        /// </summary>
        public SchematicaUIBuilder AddCoordinateInputs(
            string label,
            int yOffset,
            Action<string> onXChanged,
            Action<string> onYChanged,
            Action<string> onZChanged,
            ActionConsumable onMinusX,
            ActionConsumable onPlusX,
            ActionConsumable onMinusY,
            ActionConsumable onPlusY,
            ActionConsumable onMinusZ,
            ActionConsumable onPlusZ,
            string xInputKey,
            string yInputKey,
            string zInputKey)
        {
            // Label
            composer.AddStaticText(label, CairoFont.WhiteDetailText().WithFontSize(16),
                ElementBounds.Fixed(20, yOffset, 150, 25));

            // X coordinate
            composer.AddStaticText("X:", CairoFont.WhiteDetailText(),
                ElementBounds.Fixed(20, yOffset + 30, 30, 20));
            composer.AddSmallButton("-", onMinusX,
                ElementBounds.Fixed(20, yOffset + 50, 20, 25), EnumButtonStyle.Small);
            composer.AddTextInput(
                ElementBounds.Fixed(45, yOffset + 50, 80, 25),
                onXChanged, CairoFont.TextInput(), xInputKey);
            composer.AddSmallButton("+", onPlusX,
                ElementBounds.Fixed(130, yOffset + 50, 20, 25), EnumButtonStyle.Small);

            // Y coordinate
            composer.AddStaticText("Y:", CairoFont.WhiteDetailText(),
                ElementBounds.Fixed(165, yOffset + 30, 30, 20));
            composer.AddSmallButton("-", onMinusY,
                ElementBounds.Fixed(165, yOffset + 50, 20, 25), EnumButtonStyle.Small);
            composer.AddTextInput(
                ElementBounds.Fixed(190, yOffset + 50, 80, 25),
                onYChanged, CairoFont.TextInput(), yInputKey);
            composer.AddSmallButton("+", onPlusY,
                ElementBounds.Fixed(275, yOffset + 50, 20, 25), EnumButtonStyle.Small);

            // Z coordinate
            composer.AddStaticText("Z:", CairoFont.WhiteDetailText(),
                ElementBounds.Fixed(310, yOffset + 30, 30, 20));
            composer.AddSmallButton("-", onMinusZ,
                ElementBounds.Fixed(310, yOffset + 50, 20, 25), EnumButtonStyle.Small);
            composer.AddTextInput(
                ElementBounds.Fixed(335, yOffset + 50, 80, 25),
                onZChanged, CairoFont.TextInput(), zInputKey);
            composer.AddSmallButton("+", onPlusZ,
                ElementBounds.Fixed(420, yOffset + 50, 20, 25), EnumButtonStyle.Small);

            return this;
        }

        /// <summary>
        /// Adds layer control elements (slider, toggle, text)
        /// </summary>
        public SchematicaUIBuilder AddLayerControls(
            int yOffset,
            Action<bool> onToggle,
            ActionConsumable<int> onSliderChange,
            string toggleKey,
            string sliderKey,
            string textKey)
        {
            composer.AddStaticText("One Layer", CairoFont.WhiteDetailText(),
                ElementBounds.Fixed(20, yOffset + 5, 70, 20));

            composer.AddSwitch(onToggle,
                ElementBounds.Fixed(100, yOffset, 60, 30), toggleKey, 30);

            composer.AddStaticText("All Layers", CairoFont.WhiteDetailText(),
                ElementBounds.Fixed(170, yOffset + 5, 70, 20));

            composer.AddSlider(onSliderChange,
                ElementBounds.Fixed(0, yOffset + 40, 460, 30), sliderKey);

            composer.AddDynamicText("", CairoFont.WhiteSmallText(),
                ElementBounds.Fixed(0, yOffset + 75, 460, 20), textKey);

            return this;
        }

        /// <summary>
        /// Adds transform control buttons (rotate, mirror)
        /// </summary>
        public SchematicaUIBuilder AddTransformControls(
            int yOffset,
            ActionConsumable onRotateLeft,
            ActionConsumable onRotateRight,
            ActionConsumable onMirrorX,
            ActionConsumable onMirrorY,
            ActionConsumable onMirrorZ)
        {
            composer.AddStaticText("Transform:", CairoFont.WhiteDetailText().WithFontSize(16),
                ElementBounds.Fixed(0, yOffset, 460, 25));

            composer.AddSmallButton("Rotate L", onRotateLeft,
                ElementBounds.Fixed(0, yOffset + 30, 60, 40), EnumButtonStyle.Normal);

            composer.AddSmallButton("Rotate R", onRotateRight,
                ElementBounds.Fixed(70, yOffset + 30, 60, 40), EnumButtonStyle.Normal);

            composer.AddSmallButton("Mirror X", onMirrorX,
                ElementBounds.Fixed(150, yOffset + 30, 80, 40), EnumButtonStyle.Normal);

            composer.AddSmallButton("Mirror Y", onMirrorY,
                ElementBounds.Fixed(240, yOffset + 30, 80, 40), EnumButtonStyle.Normal);

            composer.AddSmallButton("Mirror Z", onMirrorZ,
                ElementBounds.Fixed(330, yOffset + 30, 80, 40), EnumButtonStyle.Normal);

            return this;
        }

        /// <summary>
        /// Adds direction buttons (N, S, E, W, U, D)
        /// </summary>
        public SchematicaUIBuilder AddDirectionButtons(
            int yOffset,
            ActionConsumable onNorth,
            ActionConsumable onSouth,
            ActionConsumable onEast,
            ActionConsumable onWest,
            ActionConsumable onUp,
            ActionConsumable onDown)
        {
            int btnSize = 40;
            int btnSpacing = 45;
            int startX = 50;

            composer.AddSmallButton("N", onNorth,
                ElementBounds.Fixed(startX + btnSpacing * 0, yOffset, btnSize, btnSize), EnumButtonStyle.Normal);

            composer.AddSmallButton("S", onSouth,
                ElementBounds.Fixed(startX + btnSpacing * 1, yOffset, btnSize, btnSize), EnumButtonStyle.Normal);

            composer.AddSmallButton("W", onWest,
                ElementBounds.Fixed(startX + btnSpacing * 2, yOffset, btnSize, btnSize), EnumButtonStyle.Normal);

            composer.AddSmallButton("E", onEast,
                ElementBounds.Fixed(startX + btnSpacing * 3, yOffset, btnSize, btnSize), EnumButtonStyle.Normal);

            composer.AddSmallButton("U", onUp,
                ElementBounds.Fixed(startX + btnSpacing * 4, yOffset, btnSize, btnSize), EnumButtonStyle.Normal);

            composer.AddSmallButton("D", onDown,
                ElementBounds.Fixed(startX + btnSpacing * 5, yOffset, btnSize, btnSize), EnumButtonStyle.Normal);

            return this;
        }

        /// <summary>
        /// Adds position input fields with relative coordinate support
        /// </summary>
        public SchematicaUIBuilder AddPositionInputs(
            string label,
            int yOffset,
            BlockPos currentPos,
            BlockPos worldSpawn,
            Action<string> onXChanged,
            Action<string> onYChanged,
            Action<string> onZChanged,
            string xKey,
            string yKey,
            string zKey)
        {
            ArgumentNullException.ThrowIfNull(currentPos);
            ArgumentNullException.ThrowIfNull(worldSpawn);

            composer.AddStaticText(label, CairoFont.WhiteDetailText().WithFontSize(16),
                ElementBounds.Fixed(0, yOffset, 460, 25));

            // Calculate relative positions
            int relX = currentPos.X - worldSpawn.X;
            int relZ = currentPos.Z - worldSpawn.Z;

            composer.AddTextInput(
                ElementBounds.Fixed(25, yOffset + 30, 80, 25),
                onXChanged, CairoFont.TextInput(), xKey);

            composer.AddTextInput(
                ElementBounds.Fixed(190, yOffset + 30, 80, 25),
                onYChanged, CairoFont.TextInput(), yKey);

            composer.AddTextInput(
                ElementBounds.Fixed(355, yOffset + 30, 80, 25),
                onZChanged, CairoFont.TextInput(), zKey);

            return this;
        }
    }
}



