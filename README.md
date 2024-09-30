# KK_Plugins

Plugins for Koikatu, Koikatsu Sunshine, EmotionCreators, AI Girl, HoneySelect2, and some other games.

## Installation

1. Install the latest versions of: 
    - [BepInEx v5](https://github.com/BepInEx/BepInEx/releases)
    - [BepisPlugins](https://github.com/bbepis/BepisPlugins/releases)
    - [IllusionModdingAPI](https://github.com/IllusionMods/IllusionModdingAPI)
2. Download the plugin release you want from the links below. Make sure it's a version for your game.
3. Extract the plugin .zip file to your game folder (where the BepInEx folder and game .exe is).

## Plugin descriptions and downloads

Make sure you download the version for your game (the first part before _ is the initials of the game, e.g. HS2 = HoneySelect2).

If a plugin is listed but it's not a link, then it's either experimental or obsolete. You will need to compile it from source yourself, and you will not get any support.

#### CharaMakerLoadedSound

- [KK_CharaMakerLoadedSound]
- [KKS_CharaMakerLoadedSound]

Plays a sound when the Chara Maker finishes loading. Useful if you spend the load time alt-tabbed.

#### StudioSceneLoadedSound

- [AI_StudioSceneLoadedSound]
- [HS2_StudioSceneLoadedSound]
- [KK_StudioSceneLoadedSound]
- [KKS_StudioSceneLoadedSound]

Plays a sound when a Studio scene finishes loading or importing. Useful if you spend the load time for large scenes alt-tabbed.

<details><summary>Change Log</summary>
v1.1 Config options, AI version (thanks GeBo!)<br/>
</details>

#### ForceHighPoly

- [KK_ForceHighPoly]
- [KKS_ForceHighPoly]

Forces all characters to load in high poly mode, even in the school exploration mode.

<details><summary>Change Log</summary>
v1.1 Fixed locking up the game after special H scenes. Added config option to disable high poly mode.<br/>
v1.2 Fixed hair physics not working (Thanks Rau/Marco/Essu)<br/>
v1.2.1 Removed the hair physics fix due to being obsolete, changed default enabled state to depend on RAM<br/>
v1.2.2 Fixed a patch not working<br/>
</details>

#### ReloadCharaListOnChange

- [EC_ReloadCharaListOnChange]
- [KK_ReloadCharaListOnChange]
- [KKS_ReloadCharaListOnChange]

Reloads the list of characters and coordinates in the character maker when any card is added or removed from the folders. Supports adding and removing large numbers of cards at once.

<details><summary>Change Log</summary>
v1.1 Fixed new coordinates saved from within the game not being handled correctly<br/>
v1.2 Fixed error when exiting the chara maker<br/>
v1.3 Updated for plugin compatibility<br/>
v1.4 Studio support<br/>
v1.4.1 Compatibility with BepisPlugins versions higher than r8<br/>
v1.5 Koikatsu Party compatibility<br/>
v1.5.1 Create card folders if missing to prevent errors<br/>
v1.5.2 Prevent reloading when cards in the _autosave folder are changed<br/>
</details>

#### InvisibleBody

- [AI_InvisibleBody]
- [EC_InvisibleBody]
- [HS2_InvisibleBody]
- [KK_InvisibleBody]
- [KKS_InvisibleBody]

Set the Invisible Body toggle for a character in the character maker to hide the body. Any worn clothes or accessories will remain visible.

Select characters in the Studio workspace and Anim->Current State->Invisible Body to toggle them between invisible and visible. Any worn clothes or accessories and any attached studio items will remain visible. Invisible state saves and loads with the scene. 

<details><summary>Change Log</summary>
v1.1 Fixed studio items becoming visible when they were toggled off in the workspace<br/>
v1.2 Added a character maker toggle, EmotionCreators port<br/>
v1.2.1 Fixed an incompatibility with UncensorSelector<br/>
v1.2.2 Updated for KK Darkness<br/>
v1.3 Added a toggle button for Studio, removed hotkey<br/>
v1.3.1 Fixed accessories and items attached by animations from turning invisible in AI version<br/>
v1.3.2 Fixed Studio items turning invisible in AI version<br/>
v1.4 Changes made in Studio apply to all selected characters, keep visible state when replacing characters<br/>
</details>

#### UncensorSelector

- [AI_UncensorSelector]
- [EC_UncensorSelector]
- [HS2_UncensorSelector]
- [KK_UncensorSelector]
- [KKS_UncensorSelector]

Allows you to specify which uncensors individual characters use and removes the mosaic censor. Select an uncensor for your character in the character maker in the Body/General tab or specify a default uncensor to use in the plugin settings. The default uncensor will apply to any character that does not have one selected.

Requirements:
* Marco's [KKAPI](https://github.com/ManlyMarco/KKAPI/releases)
* Marco's [Overlay Mods](https://github.com/ManlyMarco/Koikatu-Overlay-Mods/releases)
* [BepisPlugins](https://github.com/bbepis/BepisPlugins/releases) ExtensibleSaveFormat and Sideloader.

For makers of uncensors, see the [template](https://github.com/IllusionMods/KK_Plugins/blob/master/src/UncensorSelector.Core/Template.xml) for how to configure your uncensor for UncensorSelector compatibility.

Make sure to remove any sideloader uncensors and replace your oo_base with a clean, unmodified one to prevent incompatibilities!

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
v3.7 Fix shadows on male parts and reduce error spam<br/>
v3.8 AI Girl version<br/>
v3.8.1 Fix broken config stuff (thanks Keelhauled)<br/>
v3.8.3 Fix uncensors not working in AI Girl main game<br/>
v3.9 Fix crash with duplicate uncensor GUIDs, implement dick/balls support for AI Girl<br/>
v3.9.1 Fix error in Studio resulting from having no uncensors<br/>
v3.9.2 Fix glossiness being lost on uncensor change, fix monochrome body showing mosaic censor<br/>
v3.10 Add ability to exclude uncensors from random selection (Thanks Gebo)<br/>
v3.11 Changes made in Studio apply to all selected characters<br/>
v3.11.2 Compatibility with BetterPenetration<br/>
v3.11.3 Compatibility with MaterialEditor<br/>
</details>

#### Subtitles

- [AI_Subtitles]
- [HS_Subtitles]
- [HS2_Subtitles]
- [KK_Subtitles]
- [KKS_Subtitles]
- [PC_Subtitles]

For Koikatsu, adds subtitles for H scenes, spoken text in dialogues, and character maker.

For AI Girl trial version, adds subtitles for the character maker.

<details><summary>Change Log</summary>
v1.1 Fixed H subs not working for some people<br/>
v1.2 Subtitles for idle lines in dialogue<br/>
v1.3 Subtitles for character maker<br/>
v1.4 Fixed subtitles in the character maker being under the UI<br/>
v1.5 AI Girl port<br/>
v1.5.1 Fixed text wrapping, clarified config description<br/>
v1.6 HS2 port, removed FontName setting<br/>
v1.6.1 Fixed text going off screen in HS, fixed the TextAlign config setting not working properly<br/>
v2.0 Implemented subtitles for VR mode in KK and HS2<br/>
v2.0.1 Fixed using the wrong object type causing HSceneInstance to bypass null checks<br/>
v2.0.2 Subtitles are now part of the scene so they can be scoped to XUA translations<br/>
v2.1 Subtitles for character maker, Fur, Sitri in HS2<br/>
v2.2 Play Club version
</details>

#### AnimationController

- [AI_AnimationController]
- [HS2_AnimationController]
- [KK_AnimationController]
- [KKS_AnimationController]

*Koikatsu version: Mostly obsolete. [NodeConstraints](https://www.patreon.com/posts/26357789) does what this plugin does but better.*

Allows attaching IK nodes to objects to create custom animations. Press the Minus (-) hotkey to bring up the menu. This hotkey can be  configured in the F1 plugin settings.

Requires Marco's [KKAPI](https://github.com/ManlyMarco/KKAPI/releases) and [BepisPlugins](https://github.com/bbepis/BepisPlugins/releases) ExtensibleSaveFormat.

Inspired by [AttachAnimationLib](http://www.hongfire.com/forum/forum/hentai-lair/hf-modding-translation/honey-select-mods/6388508-vn-game-engine-ready-games-and-utils?p=6766050#post6766050) by Keitaro

<details><summary>Change Log</summary>
v1.1 Gimmicks can now rotate hands and feet properly<br/>
v1.2 Rotating characters doesn't break everything anymore<br/>
v2.0 Significant rewrite with KKAPI integration. Can now link eyes and neck to objects, scene import support, Drag and Drop plugin support<br/>
v2.1 Fix neck link not working, fix linking after unlinking not working<br/>
v2.2 AI version, window position adjustment<br/>
</details>

#### ClothingUnlocker

- [EC_ClothingUnlocker]
- [KK_ClothingUnlocker]
- [KKS_ClothingUnlocker]

Allows gender restricted clothing to be used on all characters. Also allows you to unlock bras or skirts with any top on a per-character, per-outfit basis. This setting saves and loads with the character card or coordinate card to ensure compatibility.

<details><summary>Change Log</summary>
v1.1 Added clothing unlocking for bras/skirts with any top<br/>
v2.0 Unlocking bra/skirt per character<br/>
v2.0.1 Fixed unlock state not saving and loading to coordinates properly<br/>
</details>

#### EyeShaking

- [KK_EyeShaking]
- [KKS_EyeShaking]

Virgins in H scenes will appear to have slightly shaking eye highlights.

<details><summary>Change Log</summary>
v1.1 VR support, added toggle to Studio<br/>
v1.2 Changes made in Studio apply to all selected characters<br/>
</details>

#### RandomCharacterGenerator

- [KK_RandomCharacterGenerator]
- [KKS_RandomCharacterGenerator]

Generates random characters in the character maker.

<details><summary>Change Log</summary>
v2.0 Merged changes from https://github.com/AUTOMATIC1111/KoikatsuMods<br/>
</details>

#### PoseTools

- [AI_PoseTools]
- [HS2_PoseTools]
- [KK_PoseTools]
- [KKS_PoseTools]
- [PH_PoseTools]

This plugin is aimed at increasing the usability of poses. You can create new folders in userdata/studio/pose and place the pose data inside them and those folders will show up in your list of poses in Studio. It also saves poses as .png files instead of .dat so you see can see what the content of the pose is. The list of poses is ordered by filename and the pose name is added to the file name so the list will be ordered alphabetically. It also saves skirt FK and facial expressions, though these can be disabled in plugin settings if you prefer.

Ported from Essu's NEOpose List Folders plugin for Honey Select.

#### ListOverride

- [KK_ListOverride]
- [KKS_ListOverride]

Allows you to override vanilla list files. Comes with some overrides that enable half off state for some vanilla pantyhose.

Overriding list files can allow you to do things like enable bras with some shirts which don't normally allow it, or skirts with some tops, etc. Any part of of the list can be changed except for ID.

#### HairAccessoryCustomizer

- [EC_HairAccessoryCustomizer]
- [KK_HairAccessoryCustomizer]
- [KKS_HairAccessoryCustomizer]

Adds configuration options for hair accessories to the character maker. Hair accessories can be set to match color with the hair, enable hair gloss, modify outline color, and has a separate color picker for the hair tie part. Hairs that support a length slider can also have their length adjusted, just like vanilla front hairs. Saves and loads to cards and coordinates.

**Note for modders**: These options will only show up for hair accessories that are properly configured. For accessories to work the accessory must have a		`ChaCustomHairComponent` MonoBehavior in addition to the `ChaAccessoryComponent` MonoBehavior. Hair accessory color will display if the ChaCustomHairComponent rendAccessory array has meshes configured. The length slider will appear if the `ChaCustomHairComponent.trfLength` array has bones configured. Hair color will only match to meshes configured in the `ChaCustomHairComponent.rendHair` array. Also check out [this guide](https://github.com/IllusionMods/KK_Plugins/wiki/Hair-Accessory-Guide) for how to create hair accessories.

<details><summary>Change Log</summary>
v1.1 Fixed a bug with changing coordinates outside of Studio not applying color matching. Fixed a bug where changing hair color in the maker would not apply color matching to other outfit slots.<br/>
v1.1.1 Fixed hair accessories matching color when they shouldn't.<br/>
v1.1.2 Fixed hair accessories matching color when they shouldn't, again.<br/>
v1.1.3 Fixed an error when starting the classroom character maker.<br/>
v1.1.4 Fixed hair accessories turning invisible with an edited MainTex.<br/>
v1.1.5 Support for coordinate load flags<br/>
</details>

#### FreeHRandom

- [KK_FreeHRandom]
- [KKS_FreeHRandom]

Adds buttons to Free H selection screen to get random characters for your H session.

<details><summary>Change Log</summary>
v1.1 Added UI, KK Party support<br/>
v1.1.1 Create card folders if missing to prevent errors<br/>
v1.2 VR support<br/>
</details>

#### Colliders

- [AI_Colliders]
- [HS2_Colliders]
- [KK_Colliders]
- [KKS_Colliders]

Adds floor, breast, hand, and skirt colliders. Colliders can be toggled on and off in Studio and their state saves with the scene.

<details><summary>Change Log</summary>
v1.1 Major rewrite, many new features<br/>
v1.1.1 ModBoneImplantor compatibility<br/>
v1.2 Changes made in Studio apply to all selected characters<br/>
</details>

#### MaterialEditor

- [AI_MaterialEditor]
- [EC_MaterialEditor]
- [HS2_MaterialEditor]
- [KK_MaterialEditor]
- [KKS_MaterialEditor]
- [PH_MaterialEditor]

MaterialEditor is a plugin that allows you to edit many properties of objects that aren't usually accessible in game. Much like [Marco's clothing overlays](https://github.com/ManlyMarco/Koikatu-Overlay-Mods) you can replace the texture of an item, however with MaterialEditor you can edit much more than clothes. Edit clothes, accessories, hair, and even Studio items.

Features:
* Export UV maps of a mesh to help with drawing textures
* Replace nearly any texture with custom textures
* Change properties of materials to control things like shininess or outline thickness
* Change properties of the mesh to affect whether it casts shadows or disable a mesh completely
* Change the shader of a material
* All changes save and load with the card or Studio scene
* Duplicate textures are saved to the card once. 100 accessories with the same texture have the same file size as one accessory with a texture

Access the Material Editor by pressing the "Open Material Editor" button on clothes, hair, or accessories in the character maker. Access it in Studio by pressing the "Mat. Editor" button on the Workspace with a studio item selected.

<details><summary>Change Log</summary>
v1.1 Fixed errors loading coordinates, errors loading scenes with multiple characters<br/>
v1.2 Added the ability to change body and face materials<br/>
v1.3 Copied studio items now copy Material Editor settings, changed filename format<br/>
v1.3.1 Fixed error on importing studio objects with textures<br/>
v1.4 Add ability to change shaders, change skin and face material<br/>
v1.4.1 .jpg loading support, scroll speed increase, color and texture default values for custom shaders<br/>
v1.5 Added main_skin shader, removed alpha_a and alpha_b properties for character skin<br/>
v1.6 AI version<br/>
v1.7 Add sliders<br/>
v1.8 AI Studio support<br/>
v1.9 Fixed data lost on changing clothing color and copying accessories, add texture offset and scale, add indicator mark for changed properties, add support for MainTex replacement<br/>
v1.9.1 Fixed mipmaps not being generated, fixed error on object copy in Studio, added ability to resize the UI<br/>
v1.9.2 Fixed an error on importing scenes<br/>
v1.9.3 Added several missing properties<br/>
v1.9.4 Added more shaders<br/>
v1.9.4.1 Fixed a missing shader<br/>
v1.9.5 Fixed a memory leak involving textures<br/>
v1.9.5.1 Fixed missing toon_glasses shader<br/>
v1.10 Fixed Material Editor (All) button breaking sliders in AI, TextureWrapMode is preserved when replacing textures<br/>
v1.10.1 Fixed an error on saving scenes<br/>
v2.0 Major rewrite. Virtual lists, results filtering, maker load flags, more character maker buttons, support for UncensorSelector added parts<br/>
v2.0.1 Fixed UV and obj export buttons, fixed missing hook for refreshing changes on outfit change<br/>
v2.0.2 Support for copying clothes, coordinate load flags, fixed textures not loading for coordiantes<br/>
v2.0.3 Fixed UV and obj export buttons again<br/>
v2.0.4 Fixed loading coordinates in Studio<br/>
v2.0.5 Fixed wrong coordinate index for character and hair edits<br/>
v2.0.6 Exposed the AlphaMask texture for main_skin shader, blacklisted it for the body<br/>
v2.0.7 Fixed wrong tongue materials after character replacement in KK, fixed wrong face materials after replacing in Studio, fixed shaders not loading in HS2<br/>
v2.1 Add copy/paste for material edits, shader optimization<br/>
v2.1.1 Fix paste button being disabled, blacklist Standard shader from optimization<br/>
v2.1.2 Fixed meka shader in AI/HS2, added option for exclusion from shader optimization to shader xml<br/>
v2.1.3 Fixed items being toggled on in the workspace overriding renderer disabled changes<br/>
v2.1.4 Blacklist AIT/eyelashes shader from optimization<br/>
v2.1.5 Fix .obj export<br/>
v2.2 Added hotkeys for disabling or enabling ShadowCastingMode and ReceiveShadows for all selected items in Studio<br/>
v2.3 Fix for tongue edits applying to other characters<br/>
v2.3.1 Fix nullref<br/>
v2.4 Dropdown for editing clothes, hair, and accessories in Studio<br/>
v2.4.1 Various bug fixes<br/>
v2.5 PH version<br/>
v2.6 Accessory and Studio support for PH<br/>
v2.6.1 Fix EC card import bug<br/>
v3.0 Added material copying<br/>
v3.0.5 Better UV Map export<br/>
v3.1 Color picker thanks to ame225, bulk paste edits in Studio<br/>
v3.1.2 Convert grey normal maps to red for compatibility across Unity versions<br/>
v3.1.3 Fix using wrong coordinate index in KKS, convert NormalMapDetail normal maps<br/>
v3.1.4 Compatibility with ExtSave changes<br/>
v3.1.5 Compatibility with UncensorSelector<br/>
</details>

#### MaleJuice

- [AI_MaleJuice]
- [HS2_MaleJuice]
- [KK_MaleJuice]
- [KKS_MaleJuice]

Enables juice textures for males in H scenes and Studio.

<details><summary>Change Log</summary>
v1.1 Fixed compatibility issues with UncensorSelector using male body type on female characters<br/>
v1.2 AI and HS2 version<br/>
v1.2.1 Fixed an error due to mishandled materials<br/>
v1.2.2 Fixed juice textures not loading in HS2 sometimes<br/>
v1.3 Better UncensorSelector compatibility<br/>
</details>

#### StudioObjectMoveHotkeys

- [AI_StudioObjectMoveHotkeys]
- [HS2_StudioObjectMoveHotkeys]
- [KK_StudioObjectMoveHotkeys]
- [KKS_StudioObjectMoveHotkeys]

Allows you to move objects in studio using hotkeys. Press Y/U/I to move along the X/Y/Z axes. You can also use these keys for rotating and scaling, and when scaling you can also press T to scale all axes at once. Hotkeys can be configured in plugin settings.

#### FKIK

- [AI_FKIK]
- [HS2_FKIK]
- [KK_FKIK]
- [KKS_FKIK]

Enables FK and IK at the same time. Pose characters in IK mode while still being able to adjust skirts, hair, and hands as if they were in FK mode.

<details><summary>Change Log</summary>
v1.1 Fix toggles going out of sync, FK being disabled when switching between characters<br/>
v1.1.1 Fix neck look pattern not loading properly<br/>
</details>

#### AnimationOverdrive

- [AI_AnimationOverdrive]
- [HS2_AnimationOverdrive]
- [KK_AnimationOverdrive]
- [KKS_AnimationOverdrive]

Type in to the animation speed box in Studio for gimmicks and character animations to go past the normal limit of 3.

<details><summary>Change Log</summary>
v1.1 AI Girl port, capped animation speed at 1000 to prevent animations breaking<br/>
</details>

#### CharacterExport

- [AI_CharacterExport]
- [EC_CharacterExport]
- [HS2_CharacterExport]
- [KK_CharacterExport]
- [KKS_CharacterExport]

Press Ctrl+E (configurable) to export all loaded character. Used for exporting characters from Studio scenes and such.

#### HCharaAdjustment

- [KK_HCharaAdjustment]
- [KKS_HCharaAdjustment]

Adjust the position of the female character in H scene by pressing some hotkeys, which are configurable in the plugin settings.

<details><summary>Change Log</summary>
v1.0.1 Made the hotkeys configurable<br/>
v2.0 Added a guide object instead of hotkeys for positioning<br/>
</details>

#### StudioSceneSettings

- [AI_StudioSceneSettings]
- [HS2_StudioSceneSettings]
- [KK_StudioSceneSettings]
- [KKS_StudioSceneSettings]

Allows you to adjust a few more settings for scenes. Changes save and load with the scene data.

<details><summary>Settings</summary>
Map Masking<br/>
Near Clip Plane<br/>
Far Clip Plane<br/>
</details>

<details><summary>Change Log</summary>
v1.2 Map masking for AI and HS2<br/>
v1.2.1 Fixed compatibility issues with StudioCustomMasking<br/>
v1.3 Changes made in Studio apply to all selected characters<br/>
v1.3.2 Increased FarClipPlane slider<br/>
</details>

#### Pushup

- [EC_Pushup]
- [KK_Pushup]
- [KKS_Pushup]

Provides sliders and setting to shape the breasts of characters when bras or tops are worn. The basic set of sliders will modify the shape of the breasts if the breast sliders are below the specified threshhold. Advanced mode lets you fully customize the shape of the breasts.

<details><summary>Change Log</summary>
v1.0.1 Fixed an incompatibility with a MakerOptimizations plugin setting<br/>
v1.1 Fixed nipple gloss not working in KK Party, fixed maker load flags<br/>
v1.1.1 Force ABMX update on clothing state change<br/>
v1.1.2 Unlock sliders when chest tab is clicked after a delay to prevent sliders being locked<br/>
v1.1.3.1 Fix Pushup not working properly in main game mode, fix Pushup not being applied on character maker load<br/>
v1.2 EmotionCreators version<br/>
v1.3 Changes made in Studio apply to all selected characters<br/>
</details>

#### PoseQuickLoad

- [AI_PoseQuickLoad]
- [HS2_PoseQuickLoad]
- [KK_PoseQuickLoad]
- [KKS_PoseQuickLoad]

A plugin that lets you load saved poses in Studio just by clicking on the pose. Vanilla behavior requires you to select the pose and then press the load button which can be pretty tedious if you have a lot of poses, especially since saved poses have no preview image.

Note: You MUST enable this option in the plugin settings (press F1 and search the plugin). This plugin is disabled by default so people don't accidentally load poses when they don't intend to, overwriting all their posing work. Use with caution.

#### StudioImageEmbed

- [AI_StudioImageEmbed]
- [HS2_StudioImageEmbed]
- [KK_StudioImageEmbed]
- [KKS_StudioImageEmbed]

This plugin will save .png files from your userdata folder to the scene data so anyone else can load the scene properly without needing the same .png file.

#### MakerDefaults

- [AI_MakerDefaults]
- [EC_MakerDefaults]
- [HS2_MakerDefaults]
- [KK_MakerDefaults]
- [KKS_MakerDefaults]

Allows you to set default settings of the character maker so you don't have to set the same values manually every time.

<details><summary>Change Log</summary>
v1.0.1 KKAPI compatibility<br/>
v1.1 Settings for preset cards and coordinates, personality in character list<br/>
</details>

#### StudioCustomMasking

- [AI_StudioCustomMasking]
- [HS2_StudioCustomMasking]
- [KK_StudioCustomMasking]
- [KKS_StudioCustomMasking]

Allows you to add map masking functionality for maps made out of items in Studio.<br/>

#### ItemBlacklist

- [EC_ItemBlacklist]
- [KK_ItemBlacklist]
- [KKS_ItemBlacklist]

Right click an item in the character maker to hide it from your lists.

<details><summary>Change Log</summary>
v1.1 Item info shows Asset and AssetBundle, better UI<br/>
</details>

#### FadeAdjuster

- [KK_FadeAdjuster]
- [KKS_FadeAdjuster]

Allows you to adjust fade color or disable the fade in and out effect.

#### Profile

- [EC_Profile]
- [KK_Profile]
- [KKS_Profile]

A big textbox in the character creator where you can write a character description.

#### Autosave

- [AI_Autosave]
- [EC_Autosave]
- [HS_Autosave]
- [HS2_Autosave]
- [KK_Autosave]
- [KKS_Autosave]
- [PC_Autosave]
- [PH_Autosave]
- [SBPR_Autosave]

Automatically saves cards in the character maker and scenes in Studio every few minutes.

#### EyeControl

- [AI_EyeControl]
- [EC_EyeControl]
- [HS2_EyeControl]
- [KK_EyeControl]
- [KKS_EyeControl]

Allows you to set a max eye openness, setting it to zero would let you create a character with permanently closed eyes. Can also disable a character's blinking.

#### AccessoryQuickRemove

- [EC_AccessoryQuickRemove]
- [KK_AccessoryQuickRemove]
- [KKS_AccessoryQuickRemove]

Quickly remove accessories by pressing the delete key in the character maker.

#### DynamicBoneEditor

- [AI_DynamicBoneEditor]
- [EC_DynamicBoneEditor]
- [HS2_DynamicBoneEditor]
- [KK_DynamicBoneEditor]
- [KKS_DynamicBoneEditor]
- [PH_DynamicBoneEditor]

Edit properties of Dynamic Bones for accessories in the character maker.

#### AccessoryClothes

- [EC_AccessoryClothes]
- [KK_AccessoryClothes]
- [KKS_AccessoryClothes]

Allows clothes to function in accessory slots.

#### PoseUnlocker

- [AI_PoseUnlocker]
- [HS2_PoseUnlocker]
- [KK_PoseUnlocker]
- [KKS_PoseUnlocker]

Removes the gender restriction on saved Studio poses.

#### LightingTweaks

- [KK_LightingTweaks]
- [KKS_LightingTweaks]

Increase shadow resolution for better quality and fix a shadow strength mismatch between main game and Studio.

#### MoreOutfits

- [KK_MoreOutfits]
- [KKS_MoreOutfits]

Allows characters to have more than the default number of outfit slots.

<details><summary>Change Log</summary>
v1.1.2 Fix loading outfit names when outfits aren't loaded, change outfit in Studio when replacing characters if the new character has less outfits<br/>
</details>

#### TwoLut

- [KK_TwoLut]
- [KKS_TwoLut]

Allows you to freely mix two studio shades (luts), instead of one always being set to Midday (based on plugin by essu). Also adds next/previous lut buttons next to the dropdown.

#### AccessoriesToStudioItems

- [AI_AccessoriesToStudioItems]
- [HS2_AccessoriesToStudioItems]
- [KK_AccessoriesToStudioItems]
- [KKS_AccessoriesToStudioItems]

Plugin for studio that makes normal character accessories available as items.
They are visible in the Item list and in QAB just like normal items.
To see all accessories in QAB, search for `ao_`.

Requires at least QuickAccessBox v3.1.1 and BepisPlugins r19.3.2 to work.

#### HairShadowColorControl

- [KK_HairShadowColorControl]
- [KKS_HairShadowColorControl]

Convenient controls for changing the shadow color of character hair in maker. Uses ME underneath.

#### TimelineFlowControl

- [AI_TimelineFlowControl]
- [HS2_TimelineFlowControl]
- [KK_TimelineFlowControl]
- [KKS_TimelineFlowControl]

Adds simple logic to Timeline that allows for controlling playback, mostly to create limited animation loops.
Requires the latest versions of BepInEx, Timeline and ModdingAPI.

#### Demosaic

- [AI_Demosaic]
- [EC_Demosaic]
- [HS2_Demosaic]
- [KK_Demosaic]
- [KKS_Demosaic]

Removes mosaics from the character models. **This is replaced by UncensorSelector and not needed** unless there is no UncensorSelector port available for a game.

#### StudioWindowResize

- [AI_StudioWindowResize]
- [HS2_StudioWindowResize]
- [KK_StudioWindowResize]
- [KKS_StudioWindowResize]

Makes studio selection windows (e.g. item and animation lists) larger so more items are visible. The size is configurable in plugin settings.

#### ClothesToAccessories

- [KK_ClothesToAccessories]
- [KKS_ClothesToAccessories]

Allows using normal clothes and hair as accessories. New accessory types are added to the Type dropdown list. Body masks from normal top clothes will be used if available, otherwise masks from top clothes added as accessories will be used.

#### Boop

- [KK_Boop]
- [KKS_Boop]

Boop the character by moving mouse over parts of their body, hair and clothes. Upgraded version of the original Boop by essu.

#### ShaderSwapper

- [KK_ShaderSwapper]
- [KKS_ShaderSwapper]

Swap all shaders to the equivalent Vanilla Plus shader in the character maker or studio by pressing right ctrl + P.

Custom rules for swapping shaders can be provided in xml files. Check the "Mapping" category in plugin's settings. 

<details>
<summary>Explanation of the xml file format</summary>

```xml
<ShaderSwapper>
  <!-- Old structure for "Mapping" element, still supported -->
  <Mapping From="Shader Forge/main_skin" To="xukmi/SkinPlus">

  <!-- New structure for "Mapping" element, keep the old "From" attribute... -->
  <Mapping From="Shader Forge/main_skin">
    <!-- ...but introduce a "Rule" element to replace the "To" attribute -->
    <Rule Name="xukmi/SkinPlus" />
  </Mapping>

  <!-- Advanced usage, with includes and excludes -->
  <Mapping From="original_shader">
    <!-- Change to shader_0 if the material name is material_0 or material_1... -->
    <Rule Name="shader_0">
      <Include>
        <Entry>material_0</Entry>
        <Entry>material_1</Entry>
      </Include>
    </Rule>

    <!-- ...else, change to shader_1 if the material name is NOT material_2 or material_3... -->
    <Rule Name="shader_1">
      <Exclude>
        <Entry>material_2</Entry>
        <Entry>material_3</Entry>
      </Exclude>
    </Rule>

    <!-- ...else, change to shader_2 -->
    <Rule Name="shader_2" />
  </Mapping>
</ShaderSwapper>
```

</details>

#### FreeHStudioSceneLoader

- [KK_FreeHStudioSceneLoader]

Allows you to use studio scenes as H mode maps in main game. **Experimental! Expect issues, no support will be given.**

[//]: # (## Latest Links)

[KKS_AnimationController]: https://github.com/IllusionMods/KK_Plugins/releases/download/v206/KKS_AnimationController.v2.3.zip "v2.3"
[KK_AnimationController]: https://github.com/IllusionMods/KK_Plugins/releases/download/v206/KK_AnimationController.v2.3.zip "v2.3"
[HS2_AnimationController]: https://github.com/IllusionMods/KK_Plugins/releases/download/v206/HS2_AnimationController.v2.3.zip "v2.3"
[AI_AnimationController]: https://github.com/IllusionMods/KK_Plugins/releases/download/v206/AI_AnimationController.v2.3.zip "v2.3"
[KKS_AnimationOverdrive]: https://github.com/IllusionMods/KK_Plugins/releases/download/v206/KKS_AnimationOverdrive.v1.1.zip "v1.1"
[KK_AnimationOverdrive]: https://github.com/IllusionMods/KK_Plugins/releases/download/v124/KK_AnimationOverdrive.v1.1.zip "v1.1"
[HS2_AnimationOverdrive]: https://github.com/IllusionMods/KK_Plugins/releases/download/v156/HS2_AnimationOverdrive.v1.1.zip "v1.1"
[AI_AnimationOverdrive]: https://github.com/IllusionMods/KK_Plugins/releases/download/v124/AI_AnimationOverdrive.v1.1.zip "v1.1"
[PH_Autosave]: https://github.com/IllusionMods/KK_Plugins/releases/download/v207/PH_Autosave.v1.1.1.zip "v1.1.1"
[PC_Autosave]: https://github.com/IllusionMods/KK_Plugins/releases/download/v207/PC_Autosave.v1.1.1.zip "v1.1.1"
[KKS_Autosave]: https://github.com/IllusionMods/KK_Plugins/releases/download/v207/KKS_Autosave.v1.1.1.zip "v1.1.1"
[KK_Autosave]: https://github.com/IllusionMods/KK_Plugins/releases/download/v207/KK_Autosave.v1.1.1.zip "v1.1.1"
[HS2_Autosave]: https://github.com/IllusionMods/KK_Plugins/releases/download/v207/HS2_Autosave.v1.1.1.zip "v1.1.1"
[HS_Autosave]: https://github.com/IllusionMods/KK_Plugins/releases/download/v207/HS_Autosave.v1.1.1.zip "v1.1.1"
[EC_Autosave]: https://github.com/IllusionMods/KK_Plugins/releases/download/v207/EC_Autosave.v1.1.1.zip "v1.1.1"
[AI_Autosave]: https://github.com/IllusionMods/KK_Plugins/releases/download/v207/AI_Autosave.v1.1.1.zip "v1.1.1"
[SBPR_Autosave]: https://github.com/IllusionMods/KK_Plugins/releases/download/v207/SBPR_Autosave.v1.1.1.zip "v1.1.1"
[KKS_CharacterExport]: https://github.com/IllusionMods/KK_Plugins/releases/download/v201/KKS_CharacterExport.v1.0.zip "v1.0"
[KK_CharacterExport]: https://github.com/IllusionMods/KK_Plugins/releases/download/v131/KK_CharacterExport.v1.0.zip "v1.0"
[HS2_CharacterExport]: https://github.com/IllusionMods/KK_Plugins/releases/download/v156/HS2_CharacterExport.v1.0.zip "v1.0"
[EC_CharacterExport]: https://github.com/IllusionMods/KK_Plugins/releases/download/v131/EC_CharacterExport.v1.0.zip "v1.0"
[AI_CharacterExport]: https://github.com/IllusionMods/KK_Plugins/releases/download/v131/AI_CharacterExport.v1.0.zip "v1.0"
[KKS_Colliders]: https://github.com/IllusionMods/KK_Plugins/releases/download/v257/KKS_Colliders.v1.3.1.zip "v1.3.1"
[KK_Colliders]: https://github.com/IllusionMods/KK_Plugins/releases/download/v257/KK_Colliders.v1.3.1.zip "v1.3.1"
[HS2_Colliders]: https://github.com/IllusionMods/KK_Plugins/releases/download/v257/HS2_Colliders.v1.3.1.zip "v1.3.1"
[AI_Colliders]: https://github.com/IllusionMods/KK_Plugins/releases/download/v257/AI_Colliders.v1.3.1.zip "v1.3.1"
[KKS_Demosaic]: https://github.com/IllusionMods/KK_Plugins/releases/download/v201/KKS_Demosaic.v1.1.zip "v1.1"
[HS2_Demosaic]: https://github.com/IllusionMods/KK_Plugins/releases/download/v156/HS2_Demosaic.v1.1.zip "v1.1"
[EC_Demosaic]: https://github.com/IllusionMods/KK_Plugins/releases/download/v73/EC_Demosaic.v1.1.zip "v1.1"
[AI_Demosaic]: https://github.com/IllusionMods/KK_Plugins/releases/download/v116/AI_Demosaic.v1.1.zip "v1.1"
[PH_DynamicBoneEditor]: https://github.com/IllusionMods/KK_Plugins/releases/download/v233/PH_DynamicBoneEditor.v1.0.5.zip "v1.0.5"
[KKS_DynamicBoneEditor]: https://github.com/IllusionMods/KK_Plugins/releases/download/v233/KKS_DynamicBoneEditor.v1.0.5.zip "v1.0.5"
[KK_DynamicBoneEditor]: https://github.com/IllusionMods/KK_Plugins/releases/download/v233/KK_DynamicBoneEditor.v1.0.5.zip "v1.0.5"
[HS2_DynamicBoneEditor]: https://github.com/IllusionMods/KK_Plugins/releases/download/v233/HS2_DynamicBoneEditor.v1.0.5.zip "v1.0.5"
[EC_DynamicBoneEditor]: https://github.com/IllusionMods/KK_Plugins/releases/download/v233/EC_DynamicBoneEditor.v1.0.5.zip "v1.0.5"
[AI_DynamicBoneEditor]: https://github.com/IllusionMods/KK_Plugins/releases/download/v233/AI_DynamicBoneEditor.v1.0.5.zip "v1.0.5"
[KKS_EyeControl]: https://github.com/IllusionMods/KK_Plugins/releases/download/v201/KKS_EyeControl.v1.0.1.zip "v1.0.1"
[KK_EyeControl]: https://github.com/IllusionMods/KK_Plugins/releases/download/v190/KK_EyeControl.v1.0.1.zip "v1.0.1"
[HS2_EyeControl]: https://github.com/IllusionMods/KK_Plugins/releases/download/v190/HS2_EyeControl.v1.0.1.zip "v1.0.1"
[EC_EyeControl]: https://github.com/IllusionMods/KK_Plugins/releases/download/v190/EC_EyeControl.v1.0.1.zip "v1.0.1"
[AI_EyeControl]: https://github.com/IllusionMods/KK_Plugins/releases/download/v190/AI_EyeControl.v1.0.1.zip "v1.0.1"
[KKS_FKIK]: https://github.com/IllusionMods/KK_Plugins/releases/download/v212/KKS_FKIK.v1.1.3.zip "v1.1.3"
[KK_FKIK]: https://github.com/IllusionMods/KK_Plugins/releases/download/v212/KK_FKIK.v1.1.3.zip "v1.1.3"
[HS2_FKIK]: https://github.com/IllusionMods/KK_Plugins/releases/download/v212/HS2_FKIK.v1.1.3.zip "v1.1.3"
[AI_FKIK]: https://github.com/IllusionMods/KK_Plugins/releases/download/v212/AI_FKIK.v1.1.3.zip "v1.1.3"
[KKS_InvisibleBody]: https://github.com/IllusionMods/KK_Plugins/releases/download/v201/KKS_InvisibleBody.v1.4.zip "v1.4"
[KK_InvisibleBody]: https://github.com/IllusionMods/KK_Plugins/releases/download/v184/KK_InvisibleBody.v1.4.zip "v1.4"
[HS2_InvisibleBody]: https://github.com/IllusionMods/KK_Plugins/releases/download/v184/HS2_InvisibleBody.v1.4.zip "v1.4"
[EC_InvisibleBody]: https://github.com/IllusionMods/KK_Plugins/releases/download/v184/EC_InvisibleBody.v1.4.zip "v1.4"
[AI_InvisibleBody]: https://github.com/IllusionMods/KK_Plugins/releases/download/v184/AI_InvisibleBody.v1.4.zip "v1.4"
[KKS_MakerDefaults]: https://github.com/IllusionMods/KK_Plugins/releases/download/v218/KKS_MakerDefaults.v1.1.zip "v1.1"
[KK_MakerDefaults]: https://github.com/IllusionMods/KK_Plugins/releases/download/v218/KK_MakerDefaults.v1.1.zip "v1.1"
[HS2_MakerDefaults]: https://github.com/IllusionMods/KK_Plugins/releases/download/v218/HS2_MakerDefaults.v1.1.zip "v1.1"
[EC_MakerDefaults]: https://github.com/IllusionMods/KK_Plugins/releases/download/v218/EC_MakerDefaults.v1.1.zip "v1.1"
[AI_MakerDefaults]: https://github.com/IllusionMods/KK_Plugins/releases/download/v218/AI_MakerDefaults.v1.1.zip "v1.1"
[KK_MaleJuice]: https://github.com/IllusionMods/KK_Plugins/releases/download/v200/KK_MaleJuice.v1.3.zip "v1.3"
[KKS_MaleJuice]: https://github.com/IllusionMods/KK_Plugins/releases/download/v230/KKS_MaleJuice.v1.3.zip "v1.3"
[HS2_MaleJuice]: https://github.com/IllusionMods/KK_Plugins/releases/download/v200/HS2_MaleJuice.v1.3.zip "v1.3"
[AI_MaleJuice]: https://github.com/IllusionMods/KK_Plugins/releases/download/v200/AI_MaleJuice.v1.3.zip "v1.3"
[PH_MaterialEditor]: https://github.com/IllusionMods/KK_Plugins/releases/download/v257/PH_MaterialEditor.v3.9.2.zip "v3.9.2"
[KKS_MaterialEditor]: https://github.com/IllusionMods/KK_Plugins/releases/download/v257/KKS_MaterialEditor.v3.9.2.zip "v3.9.2"
[KK_MaterialEditor]: https://github.com/IllusionMods/KK_Plugins/releases/download/v257/KK_MaterialEditor.v3.9.2.zip "v3.9.2"
[HS2_MaterialEditor]: https://github.com/IllusionMods/KK_Plugins/releases/download/v257/HS2_MaterialEditor.v3.9.2.zip "v3.9.2"
[EC_MaterialEditor]: https://github.com/IllusionMods/KK_Plugins/releases/download/v257/EC_MaterialEditor.v3.9.2.zip "v3.9.2"
[AI_MaterialEditor]: https://github.com/IllusionMods/KK_Plugins/releases/download/v257/AI_MaterialEditor.v3.9.2.zip "v3.9.2"
[KKS_PoseTools]: https://github.com/IllusionMods/KK_Plugins/releases/download/v248/KKS_PoseTools.v1.1.2.zip "v1.1.2"
[KK_PoseTools]: https://github.com/IllusionMods/KK_Plugins/releases/download/v248/KK_PoseTools.v1.1.2.zip "v1.1.2"
[HS2_PoseTools]: https://github.com/IllusionMods/KK_Plugins/releases/download/v248/HS2_PoseTools.v1.1.2.zip "v1.1.2"
[AI_PoseTools]: https://github.com/IllusionMods/KK_Plugins/releases/download/v248/AI_PoseTools.v1.1.2.zip "v1.1.2"
[PH_PoseTools]: https://github.com/IllusionMods/KK_Plugins/releases/download/v248/PH_PoseTools.v1.1.2.zip "v1.1.2"
[KKS_PoseQuickLoad]: https://github.com/IllusionMods/KK_Plugins/releases/download/v224/KKS_PoseQuickLoad.v1.1.zip "v1.1"
[KK_PoseQuickLoad]: https://github.com/IllusionMods/KK_Plugins/releases/download/v224/KK_PoseQuickLoad.v1.1.zip "v1.1"
[HS2_PoseQuickLoad]: https://github.com/IllusionMods/KK_Plugins/releases/download/v224/HS2_PoseQuickLoad.v1.1.zip "v1.1"
[AI_PoseQuickLoad]: https://github.com/IllusionMods/KK_Plugins/releases/download/v224/AI_PoseQuickLoad.v1.1.zip "v1.1"
[KKS_PoseUnlocker]: https://github.com/IllusionMods/KK_Plugins/releases/download/v206/KKS_PoseUnlocker.v1.0.zip "v1.0"
[KK_PoseUnlocker]: https://github.com/IllusionMods/KK_Plugins/releases/download/v197/KK_PoseUnlocker.v1.0.zip "v1.0"
[HS2_PoseUnlocker]: https://github.com/IllusionMods/KK_Plugins/releases/download/v197/HS2_PoseUnlocker.v1.0.zip "v1.0"
[AI_PoseUnlocker]: https://github.com/IllusionMods/KK_Plugins/releases/download/v197/AI_PoseUnlocker.v1.0.zip "v1.0"
[KKS_StudioCustomMasking]: https://github.com/IllusionMods/KK_Plugins/releases/download/v207/KKS_StudioCustomMasking.v1.1.1.zip "v1.1.1"
[KK_StudioCustomMasking]: https://github.com/IllusionMods/KK_Plugins/releases/download/v207/KK_StudioCustomMasking.v1.1.1.zip "v1.1.1"
[HS2_StudioCustomMasking]: https://github.com/IllusionMods/KK_Plugins/releases/download/v207/HS2_StudioCustomMasking.v1.1.1.zip "v1.1.1"
[AI_StudioCustomMasking]: https://github.com/IllusionMods/KK_Plugins/releases/download/v207/AI_StudioCustomMasking.v1.1.1.zip "v1.1.1"
[KKS_StudioImageEmbed]: https://github.com/IllusionMods/KK_Plugins/releases/download/v254/KKS_StudioImageEmbed.v1.0.3.zip "v1.0.3"
[KK_StudioImageEmbed]: https://github.com/IllusionMods/KK_Plugins/releases/download/v254/KK_StudioImageEmbed.v1.0.3.zip "v1.0.3"
[HS2_StudioImageEmbed]: https://github.com/IllusionMods/KK_Plugins/releases/download/v254/HS2_StudioImageEmbed.v1.0.3.zip "v1.0.3"
[AI_StudioImageEmbed]: https://github.com/IllusionMods/KK_Plugins/releases/download/v254/AI_StudioImageEmbed.v1.0.3.zip "v1.0.3"
[KKS_StudioObjectMoveHotkeys]: https://github.com/IllusionMods/KK_Plugins/releases/download/v206/KKS_StudioObjectMoveHotkeys.v1.0.zip "v1.0"
[KK_StudioObjectMoveHotkeys]: https://github.com/IllusionMods/KK_Plugins/releases/download/v122/KK_StudioObjectMoveHotkeys.v1.0.zip "v1.0"
[HS2_StudioObjectMoveHotkeys]: https://github.com/IllusionMods/KK_Plugins/releases/download/v156/HS2_StudioObjectMoveHotkeys.v1.0.zip "v1.0"
[AI_StudioObjectMoveHotkeys]: https://github.com/IllusionMods/KK_Plugins/releases/download/v122/AI_StudioObjectMoveHotkeys.v1.0.zip "v1.0"
[KKS_StudioSceneLoadedSound]: https://github.com/IllusionMods/KK_Plugins/releases/download/v206/KKS_StudioSceneLoadedSound.v1.1.zip "v1.1"
[KK_StudioSceneLoadedSound]: https://github.com/IllusionMods/KK_Plugins/releases/download/v132/KK_StudioSceneLoadedSound.v1.1.zip "v1.1"
[HS2_StudioSceneLoadedSound]: https://github.com/IllusionMods/KK_Plugins/releases/download/v156/HS2_StudioSceneLoadedSound.v1.1.zip "v1.1"
[AI_StudioSceneLoadedSound]: https://github.com/IllusionMods/KK_Plugins/releases/download/v132/AI_StudioSceneLoadedSound.v1.1.zip "v1.1"
[AI_AccessoriesToStudioItems]: https://github.com/IllusionMods/KK_Plugins/releases/download/v246/AI_AccessoriesToStudioItems.v1.0.1.zip "v1.0.1"
[HS2_AccessoriesToStudioItems]: https://github.com/IllusionMods/KK_Plugins/releases/download/v246/HS2_AccessoriesToStudioItems.v1.0.1.zip "v1.0.1"
[KK_AccessoriesToStudioItems]: https://github.com/IllusionMods/KK_Plugins/releases/download/v246/KK_AccessoriesToStudioItems.v1.0.1.zip "v1.0.1"
[KKS_AccessoriesToStudioItems]: https://github.com/IllusionMods/KK_Plugins/releases/download/v246/KKS_AccessoriesToStudioItems.v1.0.1.zip "v1.0.1"
[KKS_StudioSceneSettings]: https://github.com/IllusionMods/KK_Plugins/releases/download/v206/KKS_StudioSceneSettings.v1.3.2.zip "v1.3.2"
[KK_StudioSceneSettings]: https://github.com/IllusionMods/KK_Plugins/releases/download/v197/KK_StudioSceneSettings.v1.3.2.zip "v1.3.2"
[HS2_StudioSceneSettings]: https://github.com/IllusionMods/KK_Plugins/releases/download/v197/HS2_StudioSceneSettings.v1.3.2.zip "v1.3.2"
[AI_StudioSceneSettings]: https://github.com/IllusionMods/KK_Plugins/releases/download/v197/AI_StudioSceneSettings.v1.3.2.zip "v1.3.2"
[KKS_HairShadowColorControl]: https://github.com/IllusionMods/KK_Plugins/releases/download/v248/KKS_HairShadowColorControl.v1.0.0.0.zip "v1.0.0.0"
[KK_HairShadowColorControl]: https://github.com/IllusionMods/KK_Plugins/releases/download/v248/KK_HairShadowColorControl.v1.0.0.0.zip "v1.0.0.0"
[KKS_TimelineFlowControl]: https://github.com/IllusionMods/KK_Plugins/releases/download/v249/KKS_TimelineFlowControl.v1.0.0.0.zip "v1.0.0.0"
[KK_TimelineFlowControl]: https://github.com/IllusionMods/KK_Plugins/releases/download/v249/KK_TimelineFlowControl.v1.0.0.0.zip "v1.0.0.0"
[HS2_TimelineFlowControl]: https://github.com/IllusionMods/KK_Plugins/releases/download/v249/HS2_TimelineFlowControl.v1.0.0.0.zip "v1.0.0.0"
[AI_TimelineFlowControl]: https://github.com/IllusionMods/KK_Plugins/releases/download/v249/AI_TimelineFlowControl.v1.0.0.0.zip "v1.0.0.0"
[PC_Subtitles]: https://github.com/IllusionMods/KK_Plugins/releases/download/v226.1/PC_Subtitles.v2.3.2.zip "v2.3.2"
[KKS_Subtitles]: https://github.com/IllusionMods/KK_Plugins/releases/download/v226.1/KKS_Subtitles.v2.3.2.zip "v2.3.2"
[KK_Subtitles]: https://github.com/IllusionMods/KK_Plugins/releases/download/v226.1/KK_Subtitles.v2.3.2.zip "v2.3.2"
[HS2_Subtitles]: https://github.com/IllusionMods/KK_Plugins/releases/download/v226.1/HS2_Subtitles.v2.3.2.zip "v2.3.2"
[HS_Subtitles]: https://github.com/IllusionMods/KK_Plugins/releases/download/v226.1/HS_Subtitles.v2.3.2.zip "v2.3.2"
[AI_Subtitles]: https://github.com/IllusionMods/KK_Plugins/releases/download/v226.1/AI_Subtitles.v2.3.2.zip "v2.3.2"
[KKS_UncensorSelector]: https://github.com/IllusionMods/KK_Plugins/releases/download/v254/KKS_UncensorSelector.v3.12.1.zip "v3.12.1"
[KK_UncensorSelector]: https://github.com/IllusionMods/KK_Plugins/releases/download/v254/KK_UncensorSelector.v3.12.1.zip "v3.12.1"
[HS2_UncensorSelector]: https://github.com/IllusionMods/KK_Plugins/releases/download/v254/HS2_UncensorSelector.v3.12.1.zip "v3.12.1"
[EC_UncensorSelector]: https://github.com/IllusionMods/KK_Plugins/releases/download/v254/EC_UncensorSelector.v3.12.1.zip "v3.12.1"
[AI_UncensorSelector]: https://github.com/IllusionMods/KK_Plugins/releases/download/v254/AI_UncensorSelector.v3.12.1.zip "v3.12.1"
[KKS_AccessoryClothes]: https://github.com/IllusionMods/KK_Plugins/releases/download/v226.1/KKS_AccessoryClothes.v1.0.2.zip "v1.0.2"
[KK_AccessoryClothes]: https://github.com/IllusionMods/KK_Plugins/releases/download/v226.1/KK_AccessoryClothes.v1.0.2.zip "v1.0.2"
[EC_AccessoryClothes]: https://github.com/IllusionMods/KK_Plugins/releases/download/v226.1/EC_AccessoryClothes.v1.0.2.zip "v1.0.2"
[KKS_AccessoryQuickRemove]: https://github.com/IllusionMods/KK_Plugins/releases/download/v201/KKS_AccessoryQuickRemove.v1.0.zip "v1.0"
[KK_AccessoryQuickRemove]: https://github.com/IllusionMods/KK_Plugins/releases/download/v191/KK_AccessoryQuickRemove.v1.0.zip "v1.0"
[EC_AccessoryQuickRemove]: https://github.com/IllusionMods/KK_Plugins/releases/download/v191/EC_AccessoryQuickRemove.v1.0.zip "v1.0"
[KKS_ClothingUnlocker]: https://github.com/IllusionMods/KK_Plugins/releases/download/v203/KKS_ClothingUnlocker.v2.0.2.zip "v2.0.2"
[KK_ClothingUnlocker]: https://github.com/IllusionMods/KK_Plugins/releases/download/v203/KK_ClothingUnlocker.v2.0.2.zip "v2.0.2"
[EC_ClothingUnlocker]: https://github.com/IllusionMods/KK_Plugins/releases/download/v203/EC_ClothingUnlocker.v2.0.2.zip "v2.0.2"
[KKS_HairAccessoryCustomizer]: https://github.com/IllusionMods/KK_Plugins/releases/download/v223/KKS_HairAccessoryCustomizer.v1.1.7.zip "v1.1.7"
[KK_HairAccessoryCustomizer]: https://github.com/IllusionMods/KK_Plugins/releases/download/v223/KK_HairAccessoryCustomizer.v1.1.7.zip "v1.1.7"
[EC_HairAccessoryCustomizer]: https://github.com/IllusionMods/KK_Plugins/releases/download/v223/EC_HairAccessoryCustomizer.v1.1.7.zip "v1.1.7"
[KKS_ItemBlacklist]: https://github.com/IllusionMods/KK_Plugins/releases/download/v240/KKS_ItemBlacklist.v3.0.zip "v3.0"
[KK_ItemBlacklist]: https://github.com/IllusionMods/KK_Plugins/releases/download/v240/KK_ItemBlacklist.v3.0.zip "v3.0"
[EC_ItemBlacklist]: https://github.com/IllusionMods/KK_Plugins/releases/download/v240/EC_ItemBlacklist.v3.0.zip "v3.0"
[KKS_Profile]: https://github.com/IllusionMods/KK_Plugins/releases/download/v223/KKS_Profile.v1.0.3.zip "v1.0.3"
[KK_Profile]: https://github.com/IllusionMods/KK_Plugins/releases/download/v223/KK_Profile.v1.0.3.zip "v1.0.3"
[EC_Profile]: https://github.com/IllusionMods/KK_Plugins/releases/download/v223/EC_Profile.v1.0.3.zip "v1.0.3"
[KKS_Pushup]: https://github.com/IllusionMods/KK_Plugins/releases/download/v253/KKS_Pushup.v1.4.0.zip "v1.4.0"
[KK_Pushup]: https://github.com/IllusionMods/KK_Plugins/releases/download/v253/KK_Pushup.v1.4.0.zip "v1.4.0"
[EC_Pushup]: https://github.com/IllusionMods/KK_Plugins/releases/download/v253/EC_Pushup.v1.4.0.zip "v1.4.0"
[KKS_ReloadCharaListOnChange]: https://github.com/IllusionMods/KK_Plugins/releases/download/v201/KKS_ReloadCharaListOnChange.v1.5.2.zip "v1.5.2"
[KK_ReloadCharaListOnChange]: https://github.com/IllusionMods/KK_Plugins/releases/download/v189/KK_ReloadCharaListOnChange.v1.5.2.zip "v1.5.2"
[EC_ReloadCharaListOnChange]: https://github.com/IllusionMods/KK_Plugins/releases/download/v201/EC_ReloadCharaListOnChange.v1.5.2.zip "v1.5.2"
[KKS_EyeShaking]: https://github.com/IllusionMods/KK_Plugins/releases/download/v227/KKS_EyeShaking.v1.3.1.zip "v1.3.1"
[KK_EyeShaking]: https://github.com/IllusionMods/KK_Plugins/releases/download/v227/KK_EyeShaking.v1.3.1.zip "v1.3.1"
[KKS_ForceHighPoly]: https://github.com/IllusionMods/KK_Plugins/releases/download/v229/KKS_ForceHighPoly.v2.1.zip "v2.1"
[KK_ForceHighPoly]: https://github.com/IllusionMods/KK_Plugins/releases/download/v229/KK_ForceHighPoly.v2.1.zip "v2.1"
[KKS_FreeHRandom]: https://github.com/IllusionMods/KK_Plugins/releases/download/v233/KKS_FreeHRandom.v1.4.zip "v1.4"
[KK_FreeHRandom]: https://github.com/IllusionMods/KK_Plugins/releases/download/v233/KK_FreeHRandom.v1.4.zip "v1.4"
[KKS_HCharaAdjustment]: https://github.com/IllusionMods/KK_Plugins/releases/download/v243/KKS_HCharaAdjustment.v2.1.zip "v2.1"
[KK_HCharaAdjustment]: https://github.com/IllusionMods/KK_Plugins/releases/download/v243/KK_HCharaAdjustment.v2.1.zip "v2.1"
[KKS_LightingTweaks]: https://github.com/IllusionMods/KK_Plugins/releases/download/v238/KKS_LightingTweaks.v1.1.zip "v1.1"
[KK_LightingTweaks]: https://github.com/IllusionMods/KK_Plugins/releases/download/v238/KK_LightingTweaks.v1.1.zip "v1.1"
[KKS_MoreOutfits]: https://github.com/IllusionMods/KK_Plugins/releases/download/v231/KKS_MoreOutfits.v1.1.3.zip "v1.1.3"
[KK_MoreOutfits]: https://github.com/IllusionMods/KK_Plugins/releases/download/v231/KK_MoreOutfits.v1.1.3.zip "v1.1.3"
[KKS_TwoLut]: https://github.com/IllusionMods/KK_Plugins/releases/download/v205/KKS_TwoLut.v1.0.zip "v1.0"
[KK_TwoLut]: https://github.com/IllusionMods/KK_Plugins/releases/download/v205/KK_TwoLut.v1.0.zip "v1.0"
[KK_CharaMakerLoadedSound]: https://github.com/IllusionMods/KK_Plugins/releases/download/v210/KK_CharaMakerLoadedSound.v1.0.zip "v1.0"
[KKS_CharaMakerLoadedSound]: https://github.com/IllusionMods/KK_Plugins/releases/download/v210/KKS_CharaMakerLoadedSound.v1.0.zip "v1.0"
[KK_FadeAdjuster]: https://github.com/IllusionMods/KK_Plugins/releases/download/v226.1/KK_FadeAdjuster.v1.0.3.zip "v1.0.3"
[KKS_FadeAdjuster]: https://github.com/IllusionMods/KK_Plugins/releases/download/v226.1/KKS_FadeAdjuster.v1.0.3.zip "v1.0.3"
[KK_ListOverride]: https://github.com/IllusionMods/KK_Plugins/releases/download/v216/KK_ListOverride.v1.0.zip "v1.0"
[KKS_ListOverride]: https://github.com/IllusionMods/KK_Plugins/releases/download/v216/KKS_ListOverride.v1.0.zip "v1.0"
[KK_RandomCharacterGenerator]: https://github.com/IllusionMods/KK_Plugins/releases/download/v216/KK_RandomCharacterGenerator.v2.0.zip "v2.0"
[KKS_RandomCharacterGenerator]: https://github.com/IllusionMods/KK_Plugins/releases/download/v216/KKS_RandomCharacterGenerator.v2.0.zip "v2.0"
[KK_ShaderSwapper]: https://github.com/IllusionMods/KK_Plugins/releases/download/v247/KK_ShaderSwapper.v1.6.zip "v1.6"
[KKS_ShaderSwapper]: https://github.com/IllusionMods/KK_Plugins/releases/download/v247/KKS_ShaderSwapper.v1.6.zip "v1.6"
[KK_StudioWindowResize]: https://github.com/IllusionMods/KK_Plugins/releases/download/v231/KK_StudioWindowResize.v1.1.1.zip "v1.1.1"
[KKS_StudioWindowResize]: https://github.com/IllusionMods/KK_Plugins/releases/download/v231/KKS_StudioWindowResize.v1.1.1.zip "v1.1.1"
[AI_StudioWindowResize]: https://github.com/IllusionMods/KK_Plugins/releases/download/v231/AI_StudioWindowResize.v1.1.1.zip "v1.1.1"
[Hs2_StudioWindowResize]: https://github.com/IllusionMods/KK_Plugins/releases/download/v231/HS2_StudioWindowResize.v1.1.1.zip "v1.1.1"
[KK_ClothesToAccessories]: https://github.com/IllusionMods/KK_Plugins/releases/download/v251/KK_ClothesToAccessories.v1.1.1.zip "v1.1.1"
[KKS_ClothesToAccessories]: https://github.com/IllusionMods/KK_Plugins/releases/download/v251/KKS_ClothesToAccessories.v1.1.1.zip "v1.1.1"
[KK_Boop]: https://github.com/IllusionMods/KK_Plugins/releases/download/v250/KK_Boop.v2.1.zip "v2.1"
[KKS_Boop]: https://github.com/IllusionMods/KK_Plugins/releases/download/v250/KKS_Boop.v2.1.zip "v2.1"
