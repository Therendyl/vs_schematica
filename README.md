# Schematica

Schematica is a client-side Vintage Story mod for capturing structures as schematics, loading them back into the game, and previewing placement as a ghost projection.

The projection is comparison-based: it highlights blocks that are missing or incorrect at the target position instead of drawing a full solid blueprint over blocks that already match.

## Features

- Save a selected area to a schematic JSON file.
- Load saved schematics from disk.
- Preview placement as ghost chunks in the world.
- Show a single layer or all layers of a schematic.
- Move the render origin interactively.
- Rotate and mirror loaded schematics from the load dialog.
- Inspect schematic size and block count from the GUI.
- Track renderer behavior with built-in profiling commands and runtime configuration.
- Use expanded localization support for many game languages.

## How It Works

Schematica compares the loaded schematic against the current world at the chosen render origin.

- Missing blocks are rendered as white ghost blocks.
- Wrong blocks are rendered as red ghost blocks.
- Matching blocks are not rendered.

This means a preview can appear empty when the target area already matches the schematic.

## In-Game Usage

### GUI

- Press `L` to open the main Schematica menu.
- Open the save dialog to capture a selected area.
- Open the load dialog to:
  - choose a schematic
  - inspect basic schematic info
  - set the render position
  - rotate or mirror the loaded schematic
  - switch between one layer and all layers

### Hotkeys

- `L`: open or close the main Schematica GUI
- `PageUp`: next layer
- `PageDown`: previous layer

### Commands

Schematica registers the `.schem` command group.

- `.schem start`
  Sets the first selection point from the currently targeted block.
- `.schem end`
  Sets the second selection point from the currently targeted block.
- `.schem save <filename>`
  Saves the selected area as a schematic.
- `.schem load <filename>`
  Loads a schematic into memory.
- `.schem here`
  Sets the render origin to the currently targeted block.
- `.schem clear`
  Clears the active schematic and projection.
- `.schem layer set <index>`
  Sets the current preview layer.
- `.schem layer next`
  Advances to the next layer.
- `.schem layer prev`
  Goes to the previous layer.
- `.schem layer all`
  Toggles full-layer rendering.
- `.schem list`
  Lists available schematics.
- `.schem gui`
  Opens the Schematica GUI.

### Profiling Commands

Profiling support is built in for renderer and projection analysis.

- `.schem profile start`
- `.schem profile stop`
- `.schem profile status`
- `.schem profile baseline`
- `.schem profile reload`
- `.schem profile runtime`
- `.schem profile burst <seconds>`

## Data Locations

- Schematics: `VintagestoryData/ModData/Schematics`
- Renderer runtime config: `VintagestoryData/ModData/Schematica/schematica.runtime.json`
- Profiling config and outputs: `VintagestoryData/ModData/Schematica`

## Build From Source

### Requirements

- .NET SDK 8.0
- A valid Vintage Story installation path

The project resolves the game directory from either:

- `Properties/localSettings.props`
- `VINTAGE_STORY` environment variable

### Build

Debug build:

```powershell
dotnet build .\schematica.csproj -c Debug
```

Release build:

```powershell
dotnet build .\schematica.csproj -c Release
```

Release packages are written to:

```text
Releases/
```

## Development Notes

- This is a client-side mod.
- Generated build outputs and local machine settings are intentionally excluded from version control.
- The renderer includes chunk indexing, queued chunk rebuilds, adaptive safe mode, and optional runtime diagnostics to keep larger projections manageable.

## Troubleshooting

### The preview is not visible

Check the following:

- A schematic was actually loaded.
- A render origin was set.
- The target area does not already fully match the schematic.
- The loaded schematic file exists under `VintagestoryData/ModData/Schematics`.

If needed, use `.schem profile burst <seconds>` and inspect the game logs for Schematica debug output.
