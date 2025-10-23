# Rimworld Milking Machine

An experimental RimWorld mod that automates milk collection using a dedicated extractor pad. Built on top of a Visual Studio Code template, the project compiles a .NET Framework 4.8 assembly and mirrors the packaged mod directly into the local RimWorld `Mods` directory.

## Features
- **Automated milking pad** - Milkable colony animals walk themselves to the extractor once they are full enough.
- **Milk extraction comp** - Drains the animal's `CompMilkable` fullness, spawns milk beside the pad, and releases the animal on completion.
- **Research gating** - Unlock the pad after researching *Automated milking systems*.
- **Debug helpers** - Dev-mode actions let you instantly fill an animal's milk fullness for testing.
- **VS Code build flow** - `Ctrl+Shift+B` invokes `.vscode/build.bat`, which compiles and mirrors the mod into the Steam install (`C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\RimworldMilkingMachine`).

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
The automated extractor is functional: animals reserve the pad, colonist milking jobs back off, and milk drops beside the pad. Further balancing (session length, thresholds) and art polish are still to-do items.

## License
BSD 2-Clause License
