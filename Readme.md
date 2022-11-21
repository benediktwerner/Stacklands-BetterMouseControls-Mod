# Stacklands BetterMouseControls Mod

This adds two convenience features:
- Double click on any card to combine it with all nearby cards of the same type (any villager for villagers, any equipment for equipment, otherwise has to be exactly the same card). Never pulls cards out of stacks if the whole stack shouldn't be combined. Useful to combine villagers after combat or pickup loot.
- Rightclick drag to drag a whole stack of cards (as if you were holding down shift)
  - Pro tip: Bind selling to middle-mouse for an even better mouse-only experience.

Each feature can be disabled individually and there are some parameters that can be adjusted if you find you can't trigger double-clicks reliably (or it triggers way too easily for you).

## Manual Installation
This mod requires BepInEx to work. BepInEx is a modding framework which allows multiple mods to be loaded.

1. Download and install BepInEx from the [Thunderstore](https://stacklands.thunderstore.io/package/BepInEx/BepInExPack_Stacklands/).
4. Download this mod and extract it into `BepInEx/plugins/`
5. Launch the game

## Development
1. Install BepInEx
2. This mod uses publicized game DLLs to get private members without reflection
   - Use https://github.com/CabbageCrow/AssemblyPublicizer for example to publicize `Stacklands/Stacklands_Data/Managed/GameScripts.dll` (just drag the DLL onto the publicizer exe)
   - This outputs to `Stacklands_Data\Managed\publicized_assemblies\GameScripts_publicized.dll` (if you use another publicizer, place the result there)
3. Compile the project. This copies the resulting DLL into `<GAME_PATH>/BepInEx/plugins/`.
   - Your `GAME_PATH` should automatically be detected. If it isn't, you can manually set it in the `.csproj` file.
   - If you're using VSCode, the `.vscode/tasks.json` file should make it so that you can just do `Run Build`/`Ctrl+Shift+B` to build.

## Links
- Github: https://github.com/benediktwerner/Stacklands-BetterMouseControls-Mod
- Thunderstore: https://stacklands.thunderstore.io/package/benediktwerner/BetterMouseControls

## Changelog

- v1.1.1: Update for game version 1.2.6
- v1.1
  - Allow double-clicking stack root to pull onto stack
  - Double-clicking a combatant will now properly pull all similar cards into the fight
- v1.0.2
  - Fix right-click drag after game update v1.2.5
- v1.0.1
  - Fix equipped cards getting pulled on double click
  - Fix pulling into stacks larger than 30
- v1.0: Initial release
