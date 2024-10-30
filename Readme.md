# Stacklands BetterMouseControls Mod

This adds two convenience features:

- Double click on any card to combine it with all nearby cards of the same type (any villager for villagers, any equipment for equipment, otherwise has to be exactly the same card). Never pulls cards out of stacks if the whole stack shouldn't be combined. Useful to combine villagers after combat or pickup loot.
- Rightclick drag to drag a whole stack of cards (as if you were holding down shift)
  - Pro tip: Bind selling to middle-mouse for an even better mouse-only experience.

Each feature can be disabled individually and there are some parameters that can be adjusted if you find you can't trigger double-clicks reliably (or it triggers way too easily for you).

## Development

- Build using `dotnet build`
- For release builds, add `-c Release`
- If you're using VSCode, the `.vscode/tasks.json` file allows building via `Run Build`/`Ctrl+Shift+B`

## Links

- Github: https://github.com/benediktwerner/Stacklands-BetterMouseControls-Mod
- Steam Workshop: https://steamcommunity.com/sharedfiles/filedetails/?id=3012078700

## Changelog

- v1.2.5: Fix compatibility with the _Stacklands 2000_ DLC
- v1.2.4: Fix double click being able to stack unstackable cards
- v1.2.3: Disable double click to stack on resource chests
- v1.2.2: Fix disabling one of the features leading to all getting disabled
- v1.2.1: Don't unpatch when exiting the game
- v1.2.0: Steam Workshop Support
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
