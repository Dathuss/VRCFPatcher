# VRCFPatcher
This tools aims to create an avatar ready for encryption with KannaProtecc, for people who use VRCFury, which is not natively supported by KannaProtecc

This tool uses the Harmony library to hook into Unity's API and redirect all calls to AddObjectToAsset to create new assets instead of sub-assets, which are not really compatible with KannaProtecc.
It also renames these assets to prevent conflicts between assets with the same name in different controllers, and renames controllers to their layer name, and some other stuff I think.

But you're probably not here to hear me rumble nonsense garbage so let's start !

# How to use

1. [Ensure VRCFury is installed as a package.](https://vrcfury.com/download) Not sure if it will be detected otherwise.
2. [Ensure KannaProtecc is installed as well.](https://github.com/PlagueVRC/AntiRip) That's... the whole point of it all !
3. Download this repository and extract it into your Assets folder.
4. Select your avatar in your scene and [setup KannaProtecc on it](https://github.com/PlagueVRC/AntiRip#setup-kanna-protecc-component). You'll have to manually add the layers you don't want to be obfuscated (they might be automatically added in the future). If using a locomotion controller like GoGoLoco, at least add 'Base' and 'Sitting'.
5. Go to Tools -> VRCFury -> Build avatar for KannaProtecc and wait until it finishes building

![f](https://github.com/Dathuss/VRCFPatcher/assets/34245959/779c4ac1-7b1e-4870-bb60-42d1f8fc7921)

6. A new object will be created called 'VRCF clone'. You can [Protecc it](https://github.com/PlagueVRC/AntiRip#encrypting-and-uploading) now !
