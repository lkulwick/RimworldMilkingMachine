# Rimworld Milking Machine

A RimWorld mod that automates milk collection using a dedicated extractor pad. Built on top of a Visual Studio Code template, the project compiles a .NET Framework 4.8 assembly and mirrors the packaged mod directly into the local RimWorld `Mods` directory.

## Features
- Automated milking pad — Milkable colony animals walk themselves to the extractor once they are full enough and finish a short session on the pad.
- Internal storage with capacity — The extractor stores milk up to a per‑building capacity; overflow drops near the pad. Animals won’t use a full pad until it’s emptied.
- Emptying job — A Basic work job “Empty milk extractor” lets colonists dump all stored milk onto the ground. You can right‑click a pawn to prioritize it. No Harmony patches are used.
- Refuel‑style gizmo — A white storage bar shows current milk (X/Y) with a vertical marker for the auto‑empty threshold. Drag on the bar to change the map‑wide threshold interactively.
- Research gating — Unlock the pad after researching “Automated milking systems.” Advanced pads also require “Advanced fabrication.”

## Variants
- Milk extractor pad (3x1)
  - Capacity 75; power ~30W idle / ~60W during sessions
  - Cost 120 steel, 20 silver, 3 components
- Heavy milk extractor pad (3x2)
  - Capacity 300; power ~60W idle / ~100W during sessions
  - Cost 300 steel, 50 silver, 3 components
- Advanced milk extractor pad (3x1)
  - 2× faster sessions; capacity 75; power ~30W idle / ~60W during sessions
  - Cost 150 steel, 20 silver, 3 components, 1 advanced component (requires Advanced fabrication)
- Advanced heavy milk extractor pad (3x2)
  - 2× faster sessions; capacity 300; power ~60W idle / ~100W during sessions
  - Cost 360 steel, 50 silver, 3 components, 1 advanced component (requires Advanced fabrication)

## Repository Layout
- `Source/` - C# source files (`MilkExtractor.cs`, `ModEntry.cs`).
- `RimworldMilkingMachine/` - Packaged mod assets (Defs, About, Languages, Patches).
- `.vscode/` - Build scripts, project file, and launch configuration used by VS Code.

## Building
1. Ensure the .NET SDK capable of targeting `net48` is installed (Visual Studio Build Tools or Visual Studio).
2. Run the VS Code build task (`Terminal -> Run Build Task... -> BuildWindowsDLL`) or execute `.vscode\build.bat`.
3. The script compiles `RimworldMilkingMachine.dll` to `RimworldMilkingMachine/1.6/Assemblies` and mirrors the entire mod folder into the Steam RimWorld `Mods` directory.
4. If RimWorld is running, the script looks for `RimworldMilkingMachine\.rmm_lock` in the target folder and aborts with a warning so you can close the game before copying.

## Debugging
- Enable RimWorld dev mode to view the `RMM debug:` log messages that trace the animal job decisions.
- Use the debug action **Fill milk fullness** (RMM category) to force-test the extractor behaviour.

## Status
The extractor automates milking and storage with an intuitive, refuel‑style UI. Auto‑emptying is threshold‑based and map‑wide configurable. No Harmony is required in the current version.

## License
BSD 2-Clause License
