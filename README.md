# Rimworld Milking Machine

A RimWorld mod that automates milk collection using a dedicated extractor pad. The project builds a .NET 10 assembly for RimWorld 1.7 from Visual Studio 2022 and copies the packaged mod into the local RimWorld `Mods` directory.

## Features
- Automated milking pad - Milkable colony animals walk themselves to the extractor once they are full enough and finish a short session on the pad.
- Internal storage with capacity - The extractor stores milk up to a per‑building capacity; overflow drops near the pad. Animals won’t use a full pad until it’s emptied.
- Emptying job - A Basic work job "Empty milk extractor" lets colonists dump all stored milk onto the ground. You can right‑click a pawn to prioritize it. No Harmony patches are used.
- Refuel‑style gizmo - A white storage bar shows current milk (X/Y) with a vertical marker for the auto‑empty threshold. Drag on the bar to change the map‑wide threshold interactively.
- Research gating - Unlock the pad after researching "Automated milking systems". Advanced pads also require "Advanced fabrication".

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

## Building and testing locally (RimWorld 1.7)
Open `RimworldMilkingMachine.sln` in Visual Studio 2022 and use **Build Solution** (Ctrl+Shift+B),
**Rebuild Solution**, or **Clean Solution**. The IDE project delegates the actual
.NET 10 compilation to `.vscode/mod.Build.csproj`; its .NET Framework dropdown
belongs only to the VS 2022 editor adapter.

Copy `Build.local.props.example` to `Build.local.props` and adjust the game and
SDK paths. This machine is already configured. The local props file is ignored
by Git. The .NET 10 SDK is shared with the DrillUntil setup.

Debug and Release write `RimworldMilkingMachine/1.7/Assemblies/RimworldMilkingMachine.dll`
and automatically copy the mod to `RimWorldDir/Mods/RimworldMilkingMachine`.
Override `ModDeployDir` in the local props file to change the destination.
The optional `./build.ps1 -NoDeploy` command builds without deploying.
The existing 1.6 DLL is preserved and is not rebuilt by this project.
Close RimWorld before rebuilding; restart the game to load a new DLL.
Compilation and deployment do not replace an in-game behavior test.

No Harmony dependency is needed. Shared definitions are copied from `1.6/Defs` to `1.7/Defs` during each build; edit the source definitions in `1.6/Defs`.

## Debugging
- Enable RimWorld dev mode to view the `RMM debug:` log messages that trace the animal job decisions.
- Use the debug action **Fill milk fullness** (RMM category) to force-test the extractor behaviour.

## Status
The extractor automates milking and storage with an intuitive, refuel‑style UI. Auto‑emptying is threshold‑based and map‑wide configurable. No Harmony is required in the current version.

## License
BSD 2-Clause License
