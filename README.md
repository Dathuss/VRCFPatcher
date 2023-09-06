# VRCFPatcher
This tools aims to create an avatar ready for encryption with KannaProtecc, for people who use VRCFury. KannaProtecc <b>WILL</b> encrypt all your models even without this tool, but won't obfuscate toggles and full controllers, or reparent objects to non-humanoid bones with armature links. So this tool is aimed at people who want <b>more compatibility</b> between VRCFury and KannaProtecc, although it's not complicated to use, just requires a bit of manual labor. For simple things like the GogGoLoco prefab, or simple toggles like a Marker, you might just use KannaProtecc as-is, and VRCF will handle things post-encryption.

This tool uses the Harmony library to hook into Unity's API and redirect all calls to AddObjectToAsset to create new assets instead of sub-assets, which are not really compatible with KannaProtecc.
It also renames these assets to prevent conflicts between assets with the same name in different controllers, and renames controllers to their layer name, and some other stuff I think.

This tool was made because I don't have the time nor the energy to maintain a fork or make a PR of VRCFury.

But you're probably not here to hear me rumble nonsense garbage so let's start !

# How to use

1. [Ensure VRCFury is installed as a package.](https://vrcfury.com/download) Not sure if it will be detected otherwise.
2. [Ensure KannaProtecc is installed as well.](https://github.com/PlagueVRC/AntiRip) That's... the whole point of it after all !
3. [Download this repository](https://github.com/Dathuss/VRCFPatcher/archive/refs/heads/main.zip) and extract it into your Assets folder.
4. Select your avatar in your scene and [setup KannaProtecc on it](https://github.com/PlagueVRC/AntiRip#setup-kanna-protecc-component) (<b>do not encrypt yet ! just set it up.</b>).
5. You'll have to manually add the animator layers you don't want to be obfuscated, excluding them by name <b>won't work</b>. (they might be automatically added in the future). If using a locomotion controller like GoGoLoco, at least add 'Base' and 'Sitting'.

![r](https://github.com/Dathuss/VRCFPatcher/assets/34245959/0cd907f9-da81-4b4c-8018-9e5b9a315fb9)

6. Go to Tools -> VRCFury -> Build avatar for KannaProtecc and wait until it finishes building

![f](https://github.com/Dathuss/VRCFPatcher/assets/34245959/779c4ac1-7b1e-4870-bb60-42d1f8fc7921)

7. A new object will be created called 'VRCF clone'. You can [Protecc it](https://github.com/PlagueVRC/AntiRip#encrypting-and-uploading) now !
