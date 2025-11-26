# KK_Plugins

Plugins for Koikatu, Koikatsu Sunshine, EmotionCreators, AI Girl, HoneySelect2, and some other games.

## Installation

1. Install the latest versions of: 
    - [BepInEx v5](https://github.com/BepInEx/BepInEx/releases)
    - [BepisPlugins](https://gitgoon.dev/IllusionMods/BepisPlugins/releases)
    - [IllusionModdingAPI](https://gitgoon.dev/IllusionMods/IllusionModdingAPI)
2. Download the plugin release you want from the links below. Make sure it's a version for your game.
3. Extract the plugin .zip file to your game folder (where the BepInEx folder and game .exe is).

## If you are a modder

If you'd like to create a mod that is compatible with various plugins in this repo, check [the Guides folder](https://gitgoon.dev/IllusionMods/KK_Plugins/tree/master/Guides) and [the wiki](https://gitgoon.dev/IllusionMods/KK_Plugins/wiki).

If you'd like to contribute code fixes and improvements: fork this repository, create a new branch, push your changes, and open a new PR.

To build this repository you will need VisualStudio2022+ with the `.NET desktop development` and `Game development for Unity` workloads, and `.NET Framework 3.5 development tools` + targetting packs and SDKs for at least `.NET Framework 4.6` (best to just install them all).
All dependencies are downloaded via nuget on first build of the solution. Check the wiki if you are having issues with build steps failing.

You can discuss modding on the Koikatsu Discord server in the modding channels. There are also various modding guides linked in the pins of these channels you may want to check out.

## Plugin descriptions and downloads

Make sure you download the version for your game (the first part before _ is the initials of the game, e.g. HS2 = HoneySelect2).

If a plugin is listed but it's not a link, then it's either experimental or obsolete. You will need to compile it from source yourself, and you will not get any support.

You can get the latest nightly builds of all plugins from the [CI workflow](https://gitgoon.dev/IllusionMods/KK_Plugins/actions/workflows/ci.yaml). Open the latest successful run and download the build from the Artifacts section.

#### CharaMakerLoadedSound

Plays a sound when the Chara Maker finishes loading. Useful if you spend the load time alt-tabbed.

#### StudioSceneLoadedSound

Plays a sound when a Studio scene finishes loading or importing. Useful if you spend the load time for large scenes alt-tabbed.

#### ForceHighPoly

Forces all characters to load in high poly mode, even in the school exploration mode.

#### ReloadCharaListOnChange

Reloads the list of characters and coordinates in the character maker when any card is added or removed from the folders. Supports adding and removing large numbers of cards at once.

#### InvisibleBody

Set the Invisible Body toggle for a character in the character maker to hide the body. Any worn clothes or accessories will remain visible.

Select characters in the Studio workspace and Anim->Current State->Invisible Body to toggle them between invisible and visible. Any worn clothes or accessories and any attached studio items will remain visible. Invisible state saves and loads with the scene. 

#### UncensorSelector

Allows you to specify which uncensors individual characters use and removes the mosaic censor. Select an uncensor for your character in the character maker in the Body/General tab or specify a default uncensor to use in the plugin settings. The default uncensor will apply to any character that does not have one selected.

Requirements:
* Marco's [KKAPI](https://gitgoon.dev/IllusionMods/IllusionModdingAPI/releases)
* Marco's [Overlay Mods](https://github.com/ManlyMarco/Koikatu-Overlay-Mods/releases)
* [BepisPlugins](https://gitgoon.dev/IllusionMods/BepisPlugins/releases) ExtensibleSaveFormat and Sideloader.

For makers of uncensors, see the [template](https://gitgoon.dev/IllusionMods/KK_Plugins/blob/master/Guides/UncensorSelector%20Guide/uncensor_manifest_template.xml) for how to configure your uncensor for UncensorSelector compatibility.

Make sure to remove any sideloader uncensors and replace your oo_base with a clean, unmodified one to prevent incompatibilities!

#### Subtitles

For Koikatsu, adds subtitles for H scenes, spoken text in dialogues, and character maker.

For AI Girl trial version, adds subtitles for the character maker.

#### ClothingUnlocker

Allows gender restricted clothing to be used on all characters. Also allows you to unlock bras or skirts with any top on a per-character, per-outfit basis. This setting saves and loads with the character card or coordinate card to ensure compatibility.

#### EyeShaking

Virgins in H scenes will appear to have slightly shaking eye highlights.

#### RandomCharacterGenerator

Generates random characters in the character maker.

#### PoseTools

This plugin is aimed at increasing the usability of poses. You can create new folders in userdata/studio/pose and place the pose data inside them and those folders will show up in your list of poses in Studio. It also saves poses as .png files instead of .dat so you see can see what the content of the pose is. The list of poses is ordered by filename and the pose name is added to the file name so the list will be ordered alphabetically. It also saves skirt FK and facial expressions, though these can be disabled in plugin settings if you prefer.

Ported from Essu's NEOpose List Folders plugin for Honey Select.

#### ListOverride

Allows you to override vanilla list files. Comes with some overrides that enable half off state for some vanilla pantyhose.

Overriding list files can allow you to do things like enable bras with some shirts which don't normally allow it, or skirts with some tops, etc. Any part of of the list can be changed except for ID.

#### HairAccessoryCustomizer

Adds configuration options for hair accessories to the character maker. Hair accessories can be set to match color with the hair, enable hair gloss, modify outline color, and has a separate color picker for the hair tie part. Hairs that support a length slider can also have their length adjusted, just like vanilla front hairs. Saves and loads to cards and coordinates.

**Note for modders**: These options will only show up for hair accessories that are properly configured. For accessories to work the accessory must have a		`ChaCustomHairComponent` MonoBehavior in addition to the `ChaAccessoryComponent` MonoBehavior. Hair accessory color will display if the ChaCustomHairComponent rendAccessory array has meshes configured. The length slider will appear if the `ChaCustomHairComponent.trfLength` array has bones configured. Hair color will only match to meshes configured in the `ChaCustomHairComponent.rendHair` array. Also check out [this guide](https://gitgoon.dev/IllusionMods/KK_Plugins/wiki/Hair-Accessory-Guide) for how to create hair accessories.

#### FreeHRandom

Adds buttons to Free H selection screen to get random characters for your H session.

#### Colliders

Adds floor, breast, hand, and skirt colliders. Colliders can be toggled on and off in Studio and their state saves with the scene.

#### MaterialEditor

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

For makers of shaders, see the [template](https://gitgoon.dev/IllusionMods/KK_Plugins/blob/master/Guides/Material%20Editor%20Guide/shader_manifest_template.xml) for how to configure your shader zipmod for MaterialEditor compatibility.

#### MaleJuice

Enables juice textures for males in H scenes and Studio.

#### StudioObjectMoveHotkeys

Allows you to move objects in studio using hotkeys. Press Y/U/I to move along the X/Y/Z axes. You can also use these keys for rotating and scaling, and when scaling you can also press T to scale all axes at once. Hotkeys can be configured in plugin settings.

#### FKIK

Enables FK and IK at the same time. Pose characters in IK mode while still being able to adjust skirts, hair, and hands as if they were in FK mode.

#### AnimationOverdrive

Type in to the animation speed box in Studio for gimmicks and character animations to go past the normal limit of 3.

#### CharacterExport

Press Ctrl+E (configurable) to export all loaded character. Used for exporting characters from Studio scenes and such.

#### HCharaAdjustment

Adjust the position of the female character in H scene by pressing some hotkeys, which are configurable in the plugin settings.

#### StudioSceneSettings

Allows you to adjust a few more settings for scenes. Changes save and load with the scene data.

#### Pushup

Provides sliders and setting to shape the breasts of characters when bras or tops are worn. The basic set of sliders will modify the shape of the breasts if the breast sliders are below the specified threshhold. Advanced mode lets you fully customize the shape of the breasts.

#### PoseQuickLoad

A plugin that lets you load saved poses in Studio just by clicking on the pose. Vanilla behavior requires you to select the pose and then press the load button which can be pretty tedious if you have a lot of poses, especially since saved poses have no preview image.

Note: You MUST enable this option in the plugin settings (press F1 and search the plugin). This plugin is disabled by default so people don't accidentally load poses when they don't intend to, overwriting all their posing work. Use with caution.

#### StudioImageEmbed

This plugin will save .png files from your userdata folder to the scene data so anyone else can load the scene properly without needing the same .png file.

#### MakerDefaults

Allows you to set default settings of the character maker so you don't have to set the same values manually every time.

#### StudioCustomMasking

Allows you to add map masking functionality for maps made out of items in Studio.<br/>

#### ItemBlacklist

Right click an item in the character maker to hide it from your lists.

#### FadeAdjuster

Allows you to adjust fade color or disable the fade in and out effect.

#### Profile

A big textbox in the character creator where you can write a character description.

#### Autosave

Automatically saves cards in the character maker and scenes in Studio every few minutes.

#### EyeControl

Allows you to set a max eye openness, setting it to zero would let you create a character with permanently closed eyes. Can also disable a character's blinking.

#### AccessoryQuickRemove

Quickly remove accessories by pressing the delete key in the character maker.

#### DynamicBoneEditor

Edit properties of Dynamic Bones for accessories in the character maker.

#### AccessoryClothes

Allows clothes to function in accessory slots.

#### LightingTweaks

Increase shadow resolution for better quality and fix a shadow strength mismatch between main game and Studio.

#### MoreOutfits

Allows characters to have more than the default number of outfit slots.

#### TwoLut

Allows you to freely mix two studio shades (luts), instead of one always being set to Midday (based on plugin by essu). Also adds next/previous lut buttons next to the dropdown.

#### AccessoriesToStudioItems

Plugin for studio that makes normal character accessories available as items.
They are visible in the Item list and in QAB just like normal items.
To see all accessories in QAB, search for `ao_`.


#### HairShadowColorControl

Convenient controls for changing the shadow color of character hair in maker. Uses ME underneath.

#### TimelineFlowControl

Adds simple logic to Timeline that allows for controlling playback, mostly to create limited animation loops.
Requires the latest versions of BepInEx, Timeline and ModdingAPI.

#### StudioWindowResize

Makes studio selection windows (e.g. item and animation lists) larger so more items are visible. The size is configurable in plugin settings.

#### ClothesToAccessories

Allows using normal clothes and hair as accessories. New accessory types are added to the Type dropdown list. Body masks from normal top clothes will be used if available, otherwise masks from top clothes added as accessories will be used.

#### Boop

Boop the character by moving mouse over parts of their body, hair and clothes. Upgraded version of the original Boop by essu.

#### ShaderSwapper

By default, swap all shaders to the equivalent Vanilla Plus shader in the character maker or studio by pressing right ctrl + P.

Custom rules for swapping shaders can be provided in xml files. Check the "Mapping" category in plugin's settings. Optional premade XML configurations are included for swapping to Az Standard, USS, and UTS shaders.

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

## Experimental and obsolete plugins
Generally you don't want to install these unless you have a very good reason to do so.

#### AnimationController

*Mostly obsolete: [NodeConstraints](https://gitgoon.dev/IllusionMods/HSPlugins) does what this plugin does but better.*

Allows attaching IK nodes to objects to create custom animations. Press the Minus (-) hotkey to bring up the menu. This hotkey can be  configured in the F1 plugin settings.

Requires Marco's [KKAPI](https://gitgoon.dev/IllusionMods/IllusionModdingAPI/releases) and [BepisPlugins](https://gitgoon.dev/IllusionMods/BepisPlugins/releases) ExtensibleSaveFormat.

Inspired by [AttachAnimationLib](http://www.hongfire.com/forum/forum/hentai-lair/hf-modding-translation/honey-select-mods/6388508-vn-game-engine-ready-games-and-utils?p=6766050#post6766050) by Keitaro

#### Demosaic

*Obsolete: This is replaced by UncensorSelector and not needed.*

Removes mosaics from the character models.

#### FreeHStudioSceneLoader

*Experimental! Expect issues, no support will be given.*

Allows you to use studio scenes as H mode maps in main game.

#### PoseUnlocker

*Obsolete: Replaced by PoseTools.*

Removes the gender restriction on saved Studio poses.
