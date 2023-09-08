# VRCFPatcher
This tools aims to create an avatar ready for encryption with KannaProtecc, for people who use VRCFury. KannaProtecc <b>WILL</b> encrypt all your models even without this tool, but won't obfuscate toggles and full controllers, or reparent objects to non-humanoid bones with armature links. So this tool is aimed at people who want <b>more compatibility</b> between VRCFury and KannaProtecc, although it's not complicated to use, just requires a bit of manual labor. For simple things like the GogGoLoco prefab, or simple toggles like a Marker, you might just use KannaProtecc as-is, and VRCF will handle things post-encryption.

This tool uses the Harmony library to hook into Unity's API and redirect all calls to AddObjectToAsset to create new assets instead of sub-assets, which are not really compatible with KannaProtecc.
It also renames these assets to prevent conflicts between assets with the same name in different controllers, and renames controllers to their layer name, and some other stuff I think.

This tool was made because I don't have the time nor the energy to maintain a fork or make a PR of VRCFury.

But you're probably not here to hear me rumble nonsense garbage so let's start !

# How to use

Make sure to read <b>EVERYTHING</b> before asking for help !

1. [Ensure VRCFury is installed as a package.](https://vrcfury.com/download) Not sure if it will be detected otherwise.
2. [Ensure KannaProtecc is installed as well.](https://github.com/PlagueVRC/AntiRip) That's... the whole point of it after all !
3. [Download this repository](https://github.com/Dathuss/VRCFPatcher/archive/refs/heads/main.zip) and extract it into your Assets folder.
4. Select your avatar in your scene and [setup KannaProtecc on it](https://github.com/PlagueVRC/AntiRip#setup-kanna-protecc-component) (<b>do not encrypt yet ! just set it up.</b>).
5. You'll have to manually add the animator layers you don't want to be obfuscated, excluding them by name <b>won't work</b>. (they might be automatically added in the future). If using a locomotion controller like GoGoLoco, at least add 'Base' and 'Sitting'.

![r](https://github.com/Dathuss/VRCFPatcher/assets/34245959/0cd907f9-da81-4b4c-8018-9e5b9a315fb9)

6. Go to <i>Tools -> VRCFury -> KannaProtecc -> Build avatar</i> and wait until it finishes building

![1](https://github.com/Dathuss/VRCFPatcher/assets/34245959/1d541c29-ed7c-4147-b261-066f1cc4c06d)

7. A new object will be created called 'VRCF clone'. You can [Protecc it](https://github.com/PlagueVRC/AntiRip#encrypting-and-uploading) now !
8. <b>IMPORTANT</b>: Under certain circumstances, some parameters will not be copied to the expression parameters, so if you get any errors like these :

![3](https://github.com/Dathuss/VRCFPatcher/assets/34245959/3792ba09-b795-4cfa-9c8e-32bc0706b014)
![2](https://github.com/Dathuss/VRCFPatcher/assets/34245959/23ba2313-bd61-468b-bcee-1ae997a0ff81)

Select your Proteccted avatar, then go to <i>Tools -> VRCFury -> KannaProtecc -> Fix Missing Parameters</i> and the issue should be fixed !

![4](https://github.com/Dathuss/VRCFPatcher/assets/34245959/a8fdf33e-8e50-4392-aa15-c53169c48492)

