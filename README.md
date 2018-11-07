# KK_Plugins
Plugins for Koikatsu

## Installation
1. Install [BepInEx](https://github.com/BepInEx/BepInEx/releases) and [BepisPlugins](https://github.com/bbepis/BepisPlugins/releases)
2. Extract the plugin .zip file to your Koikatu folder

#### KK_CharaMakerLoadedSound v1.0 - [Download](https://github.com/DeathWeasel1337/KK_Plugins/releases/download/v1/KK_CharaMakerLoadedSound.v1.0.zip)
Plays a sound when the Chara Maker finishes loading. Useful if you spend the load time alt-tabbed.

#### KK_StudioSceneLoadedSound v1.0 - [Download](https://github.com/DeathWeasel1337/KK_Plugins/releases/download/v1/KK_StudioSceneLoadedSound.v1.0.zip)
Plays a sound when a Studio scene finishes loading or importing. Useful if you spend the load time for large scenes alt-tabbed.

#### KK_ForceHighPoly v1.1 - [Download](https://github.com/DeathWeasel1337/KK_Plugins/releases/download/v10/KK_ForceHighPoly.v1.1.zip)
Forces all characters to load in high poly mode, even in the school exploration mode.

<details><summary>Change Log</summary>
  
v1.1 Fixed locking up the game after special H scenes. Added config option to disable high poly mode.
</details>

#### KK_GUIDMigration v1.1 - [Download](https://github.com/DeathWeasel1337/KK_Plugins/releases/download/v9/KK_GUIDMigration.v1.1.zip)
Migrates your character cards in cases where a mod's GUID or IDs changed so you don't have to manually reselect everything. Will not attempt migration if you have the old mod but not the new.

Also attempts to fix cards saved with a blank GUID (Missing Mod []) by stripping the GUID and forcing sideloader to treat it as a hard mod. May not work 100%, so check your cards.

<details><summary>Change Log</summary>
  
v1.1 Added character name for blank GUID messages
</details>
<details><summary>Configuration</summary>
  
Comes preconfigured with a whole bunch of migration info. Unless I stopped maintaining it you shouldn't need to mess with this stuff.  
KK_GUIDMigration.csv is a comma separated file in the form Category,Old GUID,Old ID,New GUID,New ID.  
Category is the internal one used by sideloader, not the numeric category.  
When the category is * only GUID migration will be attempted and whatever you put for Old/New ID will be ignored. Use only in cases where a GUID changed and the IDs stay the same.
</details>

#### KK_CutsceneLockupFix v1.0 - [Download](https://github.com/DeathWeasel1337/KK_Plugins/releases/download/v2/KK_CutsceneLockupFix.v1.0.zip)
Adds some extra error handling to the game so certain hair mods wont lock up the whole game when they appear in a cutscene.

#### KK_ReloadCharaListOnChange v1.2 - [Download](https://github.com/DeathWeasel1337/KK_Plugins/releases/download/v7/KK_ReloadCharaListOnChange.v1.2.zip)
Reloads the list of characters and coordinates in the character maker when any card is added or removed from the folders. Supports adding and removing large numbers of cards at once.

<details><summary>Change Log</summary>
  
v1.1 Fixed new coordinates saved from within the game not being handled correctly  
v1.2 Fixed error when exiting the chara maker
</details>

#### KK_InvisibleBody v1.1 - [Download](https://github.com/DeathWeasel1337/KK_Plugins/releases/download/v7/KK_InvisibleBody.v1.1.zip)
Select characters in the Studio workspace and press numpad+ (configurable) to toggle them between invisible and visible. Any worn clothes or accessories and any attached studio items will remain visible. Invisible state saves and loads with the scene. Can also be used to make girls invisible in H scenes but cannot be disabled except by exiting the scene.

<details><summary>Change Log</summary>
  
v1.1 Fixed studio items becoming visible when they were toggled off in the workspace
</details>

#### KK_InputHotkeyBlock v1.0 - [Download](https://github.com/DeathWeasel1337/KK_Plugins/releases/download/v8/KK_InputHotkeyBlock.v1.0.zip)
Blocks mod hotkeys from triggering while typing in input fields. Based on kisama.dll by Essu.
