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

#### KK_GUIDMigration v1.2.2 - [Download](https://github.com/DeathWeasel1337/KK_Plugins/releases/download/v20/KK_GUIDMigration.v1.2.2.zip)
Migrates information on your character cards in cases where a mod's GUID or IDs changed so you don't have to manually reselect everything. Will not attempt migration if you have the old mod but not the new.

Also attempts to fix cards saved with a blank GUID (Missing Mod []) by stripping the GUID and forcing sideloader to treat it as a hard mod. May not work 100%, so check your cards.

<details><summary>Change Log</summary>
  
v1.1 Added character name for blank GUID messages  
v1.2 Fixed hard coded path
</details>
<details><summary>Configuration</summary>
  
Comes preconfigured with a whole bunch of migration info. Unless I stopped maintaining it you shouldn't need to mess with this stuff.  
KK_GUIDMigration.csv is a comma separated file in the form Category,Old GUID,Old ID,New GUID,New ID.  
Category is the internal one used by sideloader, not the numeric category.  
When the category is * only GUID migration will be attempted and whatever you put for Old/New ID will be ignored. Use only in cases where a GUID changed and the IDs stay the same.
</details>

#### KK_CutsceneLockupFix v1.0 - [Download](https://github.com/DeathWeasel1337/KK_Plugins/releases/download/v2/KK_CutsceneLockupFix.v1.0.zip)
Adds some extra error handling to the game so certain hair mods wont lock up the whole game when they appear in a cutscene.

#### KK_ReloadCharaListOnChange v1.4 - [Download](https://github.com/DeathWeasel1337/KK_Plugins/releases/download/v18/KK_ReloadCharaListOnChange.v1.4.zip)
Reloads the list of characters and coordinates in the character maker when any card is added or removed from the folders. Supports adding and removing large numbers of cards at once.

<details><summary>Change Log</summary>
  
v1.1 Fixed new coordinates saved from within the game not being handled correctly  
v1.2 Fixed error when exiting the chara maker  
v1.3 Updated for plugin compatibility  
v1.4 Studio support
</details>

#### KK_InvisibleBody v1.1 - [Download](https://github.com/DeathWeasel1337/KK_Plugins/releases/download/v7/KK_InvisibleBody.v1.1.zip)
Select characters in the Studio workspace and press numpad+ (configurable) to toggle them between invisible and visible. Any worn clothes or accessories and any attached studio items will remain visible. Invisible state saves and loads with the scene. Can also be used to make girls invisible in H scenes but cannot be disabled except by exiting the scene.

<details><summary>Change Log</summary>
  
v1.1 Fixed studio items becoming visible when they were toggled off in the workspace
</details>

#### KK_InputHotkeyBlock v1.0 - [Download](https://github.com/DeathWeasel1337/KK_Plugins/releases/download/v8/KK_InputHotkeyBlock.v1.0.zip)
Blocks mod hotkeys from triggering while typing in input fields. Based on kisama.dll by Essu.

#### KK_PersonalityCorrector v1.2 - [Download](https://github.com/DeathWeasel1337/KK_Plugins/releases/download/v18/KK_PersonalityCorrector.v1.2.zip)
Replaces any cards with the modded story character personalities with the default "Pure" personality when attempting to added them to the class to prevent the game from breaking. Also defaults to "Pure" for characters using paid DLC personalities if you don't have the paid DLC installed.

<details><summary>Change Log</summary>
  
v1.1 Updated to support missing DLC personalities  
v1.2 Updated for 1221 DLC personalities
</details>

#### KK_UncensorSelector v1.1 - [Download](https://github.com/DeathWeasel1337/KK_Plugins/releases/download/v24/KK_UncensorSelector.v1.1.zip)
Allows you to specify which uncensors individual characters use.  

Usage: See Example.txt in the BepInEx/KK_UncensorSelector folder for instructions.  
Create a new .txt file in the BepInEx/KK_UncensorSelector folder and add your entries there.  

Note: Only what you have set as the default for the character's gender or the wild card (*) will load in the character maker. If you need a specific one loaded (for example when working with more extreme body mods) make sure to change the default. Set it back after you're done.  

<details><summary>Change Log</summary>
  
v1.1 Now reads .txt files. Reads all .txt and .csv files in BepInEx/KK_UncensorSelector folder. Loads body textures correctly when loading a scene with multiple characters. Can specify uncensors to apply only to one gender or the other.
</details>

#### KK_Subtitles v1.1 - [Download](https://github.com/DeathWeasel1337/KK_Plugins/releases/download/v23/KK_Subtitles.v1.1.zip)
Subtitles for H scenes and spoken text in dialogues

<details><summary>Change Log</summary>
  
v1.1 Fixed H subs not working for some people  
</details>

#### KK_AnimationController v1.1 - [Download](https://github.com/DeathWeasel1337/KK_Plugins/releases/download/v26/KK_AnimationController.v1.1.zip)
Allows attaching IK nodes to objects to create custom animations  

<details><summary>Change Log</summary>
  
v1.1 Gimmicks can now rotate hands and feet properly  
</details>

Inspired by [AttachAnimationLib](http://www.hongfire.com/forum/forum/hentai-lair/hf-modding-translation/honey-select-mods/6388508-vn-game-engine-ready-games-and-utils?p=6766050#post6766050) by Keitaro 

#### KK_FutaMod BETA 2 - [Download](https://github.com/DeathWeasel1337/KK_Plugins/releases/download/v15/KK_FutaMod.v0.2.zip)
Adds dicks to girls. Requires MakerAPI, currently available as part of [ABMX](https://github.com/ManlyMarco/KKABMX#readme).

More features soon, hopefully.

<details><summary>Change Log</summary>
  
BETA 2 Updated for MakerAPI compatibility
</details>
