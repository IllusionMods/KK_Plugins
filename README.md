# KK_Plugins
Plugins for Koikatsu and EmotionCreators

## Installation - Koikatsu
1. Install [BepInEx](https://github.com/BepInEx/BepInEx/releases)
2. Install [BepisPlugins](https://github.com/bbepis/BepisPlugins/releases)
3. Install [KKAPI](https://github.com/ManlyMarco/KKAPI/releases)
4. Extract the plugin .zip file to your Koikatu folder

## Installation - EmotionCreators
1. Install [Bepinex 5](https://builds.bepis.io/bepinex_be) BepInEx build for post-Unity 2018 game (x64) - Build 133
2. Install [EC_CorePlugins](https://builds.bepis.io/ec_coreplugins)
3. Install [ECAPI](https://github.com/ManlyMarco/KKAPI/releases)
4. Extract the plugin .zip file to your Koikatu folder

#### KK_CharaMakerLoadedSound
**v1.0 - [Download](https://github.com/DeathWeasel1337/KK_Plugins/releases/download/v1/KK_CharaMakerLoadedSound.v1.0.zip)**<br/>

Plays a sound when the Chara Maker finishes loading. Useful if you spend the load time alt-tabbed.

#### KK_StudioSceneLoadedSound
**v1.0 - [Download](https://github.com/DeathWeasel1337/KK_Plugins/releases/download/v1/KK_StudioSceneLoadedSound.v1.0.zip)**<br/>

Plays a sound when a Studio scene finishes loading or importing. Useful if you spend the load time for large scenes alt-tabbed.<br/>

#### KK_ForceHighPoly
**v1.2 - [Download](https://github.com/DeathWeasel1337/KK_Plugins/releases/download/v70/KK_ForceHighPoly.v1.2.zip)**<br/>

Forces all characters to load in high poly mode, even in the school exploration mode.<br/>

<details><summary>Change Log</summary>
v1.1 Fixed locking up the game after special H scenes. Added config option to disable high poly mode.<br/>
v1.2 Fixed hair physics not working (Thanks Rau/Marco/Essu)<br/>
</details>

#### KK_GUIDMigration
**v1.5 - [Download](https://github.com/DeathWeasel1337/KK_Plugins/releases/download/v72/KK_GUIDMigration.v1.5.zip)**<br/>
Note: Only to be used with BepisPlugins r10, earlier versions are incompatible!<br/>

Migrates information on your character cards in cases where a mod's GUID or IDs changed so you don't have to manually reselect everything. Will not attempt migration if you have the old mod but not the new.<br/>

Also attempts to fix cards saved with a blank GUID (Missing Mod []) by stripping the GUID and forcing sideloader to treat it as a hard mod. May not work 100%, so check your cards.<br/>

<details><summary>Change Log</summary>
v1.1 Added character name for blank GUID messages<br/>
v1.2 Fixed hard coded path<br/>
v1.3 Added support for stripping extended data, fix errors resulting from missing .csv<br/>
v1.4 Added support for coordinate cards (Thanks Kokaiinum), fix errors caused by wrong sideloader version<br/>
v1.4 BepisPlugins r10 support, MoreAccessories support<br/>
</details>

<details><summary>Configuration</summary>
Comes preconfigured with a whole bunch of migration info. Unless I stopped maintaining it you shouldn't need to mess with this stuff.<br/>
KK_GUIDMigration.csv is a comma separated file in the form Category,Old GUID,Old ID,New GUID,New ID.<br/>
Category is the internal one used by sideloader, not the numeric category.<br/>
When the category is * only GUID migration will be attempted and whatever you put for Old/New ID will be ignored. Use only in cases where a GUID changed and the IDs stay the same.<br/>
When the category is - the extended data will be stripped and will be treated as a hard mod
</details>

#### KK_CutsceneLockupFix
**v1.0 - [Download](https://github.com/DeathWeasel1337/KK_Plugins/releases/download/v2/KK_CutsceneLockupFix.v1.0.zip)**<br/>

Adds some extra error handling to the game so certain hair mods wont lock up the whole game when they appear in a cutscene.<br/>

#### KK_ReloadCharaListOnChange
**v1.4.1 - [Download](https://github.com/DeathWeasel1337/KK_Plugins/releases/download/v46/KK_ReloadCharaListOnChange.v1.4.1.zip)**<br/>

Reloads the list of characters and coordinates in the character maker when any card is added or removed from the folders. Supports adding and removing large numbers of cards at once.<br/>

<details><summary>Change Log</summary>
v1.1 Fixed new coordinates saved from within the game not being handled correctly<br/>
v1.2 Fixed error when exiting the chara maker<br/>
v1.3 Updated for plugin compatibility<br/>
v1.4 Studio support<br/>
v1.4.1 Compatibility with BepisPlugins versions higher than r8
</details>

#### KK_InvisibleBody EC_InvisibleBody
**v1.2.2 - [Download](https://github.com/DeathWeasel1337/KK_Plugins/releases/download/v76/KK_InvisibleBody.v1.2.2.zip)** - For Koikatsu<br/>
**v1.2.1 - [Download](https://github.com/DeathWeasel1337/KK_Plugins/releases/download/v75/EC_InvisibleBody.v1.2.1.zip)** - For EmotionCreators<br/>

Set the Invisible Body toggle for a character in the character maker to hide the body. Any worn clothes or accessories will remain visible.<br/>

Select characters in the Studio workspace and press numpad+ (configurable) to toggle them between invisible and visible. Any worn clothes or accessories and any attached studio items will remain visible. Invisible state saves and loads with the scene. <br/>

<details><summary>Change Log</summary> 
v1.1 Fixed studio items becoming visible when they were toggled off in the workspace<br/>
v1.2 Added a character maker toggle, EmotionCreators port<br/>
v1.2.1 Fixed an incompatibility with UncensorSelector<br/>
v1.2.2 Updated for KK Darkness<br/>
</details>

#### KK_InputHotkeyBlock
**v1.1 - [Download](https://github.com/DeathWeasel1337/KK_Plugins/releases/download/v28/KK_InputHotkeyBlock.v1.1.zip)**<br/>

Blocks mod hotkeys from triggering while typing in input fields. Based on kisama.dll by Essu.

<details><summary>Change Log</summary>
v1.1 Blocks hotkeys in studio coordinate fields
</details>

#### KK_PersonalityCorrector
**v1.3.1 - [Download](https://github.com/DeathWeasel1337/KK_Plugins/releases/download/v38/KK_PersonalityCorrector.v1.3.1.zip)**<br/>

Replaces any cards with the modded story character personalities with the default "Pure" personality when attempting to added them to the class to prevent the game from breaking. Also defaults to "Pure" for characters using paid DLC personalities if you don't have the paid DLC installed.

<details><summary>Change Log</summary>
v1.1 Updated to support missing DLC personalities<br/>
v1.2 Updated for 1221 DLC personalities<br/>
v1.3 Now corrects personalities when using the random button<br/>
v1.3.1 Removed log messages. Oops.
</details>

#### KK_UncensorSelector EC_UncensorSelector
**v3.6.4 - [Download](https://github.com/DeathWeasel1337/KK_Plugins/releases/download/v81/KK_UncensorSelector.v3.6.4.zip)** - For Koikatsu<br/>
**v3.6 - [Download](https://github.com/DeathWeasel1337/KK_Plugins/releases/download/v75/EC_UncensorSelector.v3.6.zip)** - For EmotionCreators<br/>

3.0 update note: Uncensors from previous versions are not compatible, download new versions [here.](https://mega.nz/#F!upYGBTAZ!S1lMalC33LYEditk7GwzgQ!n54h1KZS)<br/>

Allows you to specify which uncensors individual characters use and removes the mosaic censor. Select an uncensor for your character in the character maker in the Body/General tab or specify a default uncensor to use in the plugin settings. The default uncensor will apply to any character that does not have one selected.<br/>

Requires Marco's [KKAPI](https://github.com/ManlyMarco/KKAPI/releases) and [BepisPlugins](https://github.com/bbepis/BepisPlugins/releases) ConfigurationManager, ExtensibleSaveFormat, and Sideloader.<br/>

UncensorSelector compatible uncensors can be found [here.](https://mega.nz/#F!upYGBTAZ!S1lMalC33LYEditk7GwzgQ!n54h1KZS) For makers of uncensors, see the [template](https://github.com/DeathWeasel1337/KK_Plugins/blob/master/Core_UncensorSelector/Template.xml) for how to configure your uncensor for UncensorSelector compatibility.<br/>

Make sure to remove any sideloader uncensors and replace your oo_base with a clean, unmodified one to prevent incompatibilities!<br/>

<details><summary>Change Log</summary>
v2.0 Complete rewrite, now supports changing uncensors inside the character maker, configuring uncensor metadata in manifest.xml, demosaic, etc.<br/>
v2.1 Reduce reliance on KK_UncensorSelector Base.zipmod<br/>
v2.2 Removed the ability to specify _low assets. A matching _low asset is expected to exist for everything that requires one.<br/>
v2.3 Added some warning labels<br/>
v2.4 Fixed demosaic not working sometimes<br/>
v2.5 ConfigManager dropdown for GUID selection, fixed color matching bug in chara maker<br/>
v2.6 Uncensors now change much more quickly without causing lag in the character maker. Random can be selected as an option for the default uncensor, any character with no uncensor selected will use a random one (Thanks @ManlyMarco). Uncensors can be exluded from random selection with a modification to the manifest.xml.<br/>
v2.6.1 Fix for the new uncensor loading code breaking in low poly<br/>
v2.7 Names in ConfigManager instead of GUIDs, uncensor lists are ordered (Thanks @ManlyMarco), slightly faster uncensor switching<br/>
v3.0 All uncensors load correctly in the character maker, default uncensors display in character maker, body parts can be selected independently from the body, new format for uncensors, new bugs<br/>
v3.1 Fixed uncensors not loading in the character maker accessed through the class menu, fixed some low poly uncensors not display correctly for the main character, fixed low poly uncensors not working at all for female characters<br/>
v3.2 Random uncensors are now more evenly distributed, gender bender config option simplified, fixed a problem with clothes that have the same mesh name as body part meshes causing problems<br/>
v3.3 Fix wrong normals after loading a character sometimes, fix default values when loading a character in to the character maker from class menu, fix balls dropdown not reloading the uncensor on change<br/>
v3.4 Fix liquid textures being removed on changing characters<br/>
v3.5 Added a message that displays if the skin texture has become corrupt and attempts a fix (Thanks @ManlyMarco)<br/>
v3.5.1 Reduce false positives for the above change<br/>
v3.6 EmotionCreators port, removed "none" as a default config option<br/>
v3.6.1 Updated for KK Darkness<br/>
v3.6.2 Fix replacing janitor's body when it shouldn't<br/>
v3.6.3 Fix janitor's uncensor<br/>
v3.6.4 Fix compatibility issues for non Darkness game versions<br/>
</details>

#### KK_Subtitles
**v1.2 - [Download](https://github.com/DeathWeasel1337/KK_Plugins/releases/download/v50/KK_Subtitles.v1.2.zip)**<br/>

Subtitles for H scenes and spoken text in dialogues<br/>

<details><summary>Change Log</summary>
v1.1 Fixed H subs not working for some people<br/>
v1.2 Subtitles for idle lines in dialogue
</details>

#### KK_AnimationController
**v2.1 - [Download](https://github.com/DeathWeasel1337/KK_Plugins/releases/download/v57/KK_AnimationController.v2.1.zip)**<br/>
*Note: Mostly obsolete. [NodeConstraints](https://www.patreon.com/posts/26357789) does what this plugin does but better.*

Allows attaching IK nodes to objects to create custom animations. Press the Minus (-) hotkey to bring up the menu. This hotkey can be  configured in the F1 plugin settings.<br/>

Requires Marco's [KKAPI](https://github.com/ManlyMarco/KKAPI/releases) 1.2 or higher and [BepisPlugins](https://github.com/bbepis/BepisPlugins/releases) ConfigurationManager and ExtensibleSaveFormat.<br/>

Inspired by [AttachAnimationLib](http://www.hongfire.com/forum/forum/hentai-lair/hf-modding-translation/honey-select-mods/6388508-vn-game-engine-ready-games-and-utils?p=6766050#post6766050) by Keitaro  

<details><summary>Change Log</summary>
v1.1 Gimmicks can now rotate hands and feet properly<br/>
v1.2 Rotating characters doesn't break everything anymore<br/>
v2.0 Significant rewrite with KKAPI integration. Can now link eyes and neck to objects, scene import support, Drag and Drop plugin support<br/>
v2.1 Fix neck link not working, fix linking after unlinking not working<br/>
</details>

#### KK_ClothingUnlocker EC_ClothingUnlocker
**v1.1 - [Download](https://github.com/DeathWeasel1337/KK_Plugins/releases/download/v62/KK_ClothingUnlocker.v1.1.zip)** - For Koikatsu<br/>
**v1.1 - [Download](https://github.com/DeathWeasel1337/KK_Plugins/releases/download/v77/EC_ClothingUnlocker.v1.1.zip)** - For EmotionCreators<br/>

Allows gender restricted clothing to be used on all characters.<br/>

<details><summary>Advanced mode</summary>
KK_ClothingUnlocker can unlock bras/skirts with any top. Go to plugin settings and enable advanced mode to see the options for them. These settings are not recommended because they will require updating many characters for compatibility.<br/>
</details>

<details><summary>Change Log</summary>
v1.1 Added clothing unlocking for bras/skirts with any top<br/>
</details>

#### KK_EyeShaking
**v1.0 - [Download](https://github.com/DeathWeasel1337/KK_Plugins/releases/download/v38/KK_EyeShaking.v1.0.zip)**<br/>

Virgins in H scenes will appear to have slightly shaking eye highlights.<br/>

Requires Marco's [KKAPI](https://github.com/ManlyMarco/KKAPI/releases)<br/>

#### KK_MiscFixes
**v1.1 - [Download](https://github.com/DeathWeasel1337/KK_Plugins/releases/download/v43/KK_MiscFixes.v1.1.zip)**<br/>

Miscellaneous fixes aimed at improving the performance of the game.<br/>

* Improves load time of the list of characters in Free H<br/>
* Improves load time when opening the class roster menu<br/>

<details><summary>Change Log</summary>
v1.1 Now uses full path instead of file name for compatibility with Marco's KK_BrowserFolders
</details>

#### KK_RandomCharacterGenerator
**v1.0 - [Download](https://github.com/DeathWeasel1337/KK_Plugins/releases/download/v47/KK_RandomCharacterGenerator.v1.0.zip)**<br/>

Generates random characters in the character maker.<br/>

Requires Marco's [KKAPI](https://github.com/ManlyMarco/KKAPI/releases)<br/>

#### KK_PoseFolders
**v1.0 - [Download](https://github.com/DeathWeasel1337/KK_Plugins/releases/download/v60/KK_PoseFolders.v1.0.zip)**<br/>

Create new folders in userdata/studio/pose and place the pose data inside them. Folders will show up in your list of poses in Studio.<br/>

Ported to Koikatsu from Essu's NEOpose List Folders plugin for Honey Select.<br/>

#### KK_TranslationSync
**v1.2 - [Download](https://github.com/DeathWeasel1337/KK_Plugins/releases/download/v61/KK_TranslationSync.v1.2.zip)**<br/>

A plugin for correctly formatting translation files. Corrects formatting and copies translations from one file to another for the same personality in case of duplicate entries. Used by translators working on the [Koikatsu Translation](https://github.com/DeathWeasel1337/Koikatsu-Translations) project. No need to download unless you're working on translations.<br/>

To use, open the plugin settings and set a personality, press the hotkey (default 0) to sync translations. Read your bepinex console or output_log.txt to see the changes made or any warnings and errors. Press alt+hotkey to force sync translation files in case of differing translations (warning: make backups first. It may not be obvious which translations are treated as the primary source). Press ctrl+hotkey to sync translations for all personalities (warning: very slow).<br/>

#### KK_ListOverride
**v1.0 - [Download](https://github.com/DeathWeasel1337/KK_Plugins/releases/download/v65/KK_ListOverride.v1.0.zip)**<br/>

Allows you to override vanilla list files. Comes with some overrides that enable half off state for some vanilla pantyhose.<br/>

Overriding list files can allow you to do things like enable bras with some shirts which don't normally allow it, or skirts with some tops, etc. Any part of of the list can be changed except for ID.<br/>

#### KK_HairAccessoryCustomizer EC_HairAccessoryCustomizer
**v1.0 - [Download](https://github.com/DeathWeasel1337/KK_Plugins/releases/download/v71/KK_HairAccessoryCustomizer.v1.0.zip)** - For Koikatsu<br/>
**v1.0 - [Download](https://github.com/DeathWeasel1337/KK_Plugins/releases/download/v73/EC_HairAccessoryCustomizer.v1.0.zip)** - For EmotionCreators<br/>

Adds configuration options for hair accessories to the character maker. Hair accessories can be set to match color with the hair, enable hair gloss, modify outline color, and has a separate color picker for the hair tie part. Hairs that support a length slider can also hair their length adjusted, just like vanilla front hairs. Saves and loads to cards and coordinates.<br/>

Configuration options will work only on properly configured hair accessories. All of the hair accessories from <https://mega.nz/#F!upYGBTAZ!S1lMalC33LYEditk7GwzgQ!GpJEiLwK> will work.<br/>

Requires Marco's [KKAPI](https://github.com/ManlyMarco/KKAPI/releases) v1.3 or higher, previous versions will NOT work.<br/>

Note for modders: These options will only show up for hair accessories that are properly configured. For accessories to work the accessory must have a ChaCustomHairComponent MonoBehavior in addition to the ChaAccessoryComponent MonoBehavior. Hair accessory color will display if the ChaCustomHairComponent rendAccessory array has meshes configured. The length slider will appear if the ChaCustomHairComponent trfLength array has bones configured. Hair color will only match to meshes configured in the ChaCustomHairComponent rendHair array. Also check out [this guide](https://github.com/DeathWeasel1337/KK_Plugins/wiki/Hair-Accessory-Guide) for how to create hair accessories.<br/>

#### EC_Demosaic
**v1.1 - [Download](https://github.com/DeathWeasel1337/KK_Plugins/releases/download/v73/EC_Demosaic.v1.1.zip)** - For EmotionCreators<br/>
Note: Not required when using UncensorSelector<br/>

Removes the mosaic from female characters. Based on the demosaic for Koikatsu by [AUTOMATIC1111](https://github.com/AUTOMATIC1111/KoikatsuMods), compiled for EC and BepInEx 5.<br/>

<details><summary>Change Log</summary>
v1.1 Added a config option to disable the plugin<br/>
</details>

#### KK_HeadFix
**v1.0 - [Download](https://github.com/DeathWeasel1337/KK_Plugins/releases/download/v78/KK_HeadFix.v1.0.zip)**<br/>
Fixes a bug where alternate heads cannot load modded eyeliners and modded heads cannot load any eyeliners.<br/>


# Experimental plugins
Experimental or unfinished plugins. No support will be given and most likely no fixes will be made. Feel free to report bugs that aren't already listed but don't expect a fix. Anyone who wants to improve these plugins is welcome to do so, all the source code is available.<br/>


#### KK_BodyShaders
**Beta - [Download](https://github.com/DeathWeasel1337/KK_Plugins/releases/download/v63/KK_BodyShaders.beta.zip)**<br/>

Applies shaders to a character's body and hair. Currently only has a shader for making goo girls. Or boys, if you're in to that kind of thing. Also has bugs. Not a shader for making bug girls, the kind of bugs that might make your game act strange.<br/>

Shaders by Essu. Requires Marco's [KKAPI](https://github.com/ManlyMarco/KKAPI/releases)<br/>

#### KK_Colliders
**v1.0 - [Download](https://github.com/DeathWeasel1337/KK_Plugins/releases/download/v37/KK_Colliders.v1.0.zip)**<br/>

Adds floor, breast, hand, and skirt colliders. Ported from Patchwork.<br/>

Requires Marco's [KKAPI](https://github.com/ManlyMarco/KKAPI/releases)<br/>

#### KK_FreeHRandom
**v1.0 - [Download](https://github.com/DeathWeasel1337/KK_Plugins/releases/download/v38/KK_FreeHRandom.v1.0.zip)**<br/>

Press F5 at Free H selection screen to get random characters for your H session.<br/>

#### KK_ANIMATIONOVERDRIVE
**v1.0 - [Download](https://github.com/DeathWeasel1337/KK_Plugins/releases/download/v42/KK_ANIMATIONOVERDRIVE.zip)**<br/>

Type a value in to a gimmick's speed text box to use speeds higher than normally allowed.<br/>

#### KK_HCharaAdjustment
**v1.0 - [Download](https://github.com/DeathWeasel1337/KK_Plugins/releases/download/v46/KK_HCharaAdjustment.v1.0.zip)**<br/>
Adjust the position of the female character in H scene by pressing some hotkeys, listed [here](https://github.com/DeathWeasel1337/KK_Plugins/blob/master/KK_HCharaAdjustment/KK_HCharaAdjustment.cs#L40).<br/>
