using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;

#if USE_VRCFURY 
using VF.Menu;
using VF.Model;
#endif

#endif

using HarmonyLib;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

// This class aims to hook to AssetDatabase.AddObjectToAsset to prevent VRCFury from making sub-assets
// which disrupts the way KannaProtecc works. Extracting the assets on KannaProtecc's side could
// have worked if only unity's asset database system wasn't fucked up when working with sub assets.
// And it's also faster this way

#if UNITY_EDITOR
[InitializeOnLoad]
#endif
internal static class VrcfPatcher
{
    private const string _patchName = "vrcfpatcher";
    private const string menuPrefix = "Tools/VRCFury/KannaProtecc/";
    private static readonly Harmony _harmony = new Harmony(_patchName);

#if UNITY_EDITOR && USE_VRCFURY
    static VrcfPatcher()
    {
        Debug.Log("Patching AssetDatabase...");

        var assetDBmethod = typeof(AssetDatabase).GetMethod("AddObjectToAsset", new Type[] { typeof(Object), typeof(Object) });
        var assetDBPrefix = new HarmonyMethod(typeof(VrcfPatcher).GetMethod(nameof(AssetDBPrefix)));

        _harmony.UnpatchAll(_patchName);
        _harmony.Patch(assetDBmethod, prefix: assetDBPrefix);

        Debug.Log("Patched !");
    }

    // Types that we don't want to be added as sub-assets
    private static readonly Dictionary<Type, (string, string)> _typePrefixExtensionKV = new Dictionary<Type, (string, string)>()
    {
        { typeof(VRCExpressionsMenu), ("menu", "asset") },
        { typeof(AnimationClip), ("anim", "anim") },
        { typeof(BlendTree), ("blend_tree", "asset") }
    };

    private static bool _isBuilding = false;

    public static bool AssetDBPrefix(Object objectToAdd, Object assetObject)
    {
        if (_isBuilding && _typePrefixExtensionKV.ContainsKey(objectToAdd.GetType()))
        {
            var folderPath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(assetObject));
            (var prefix, var extension) = _typePrefixExtensionKV[objectToAdd.GetType()];

            // We don't keep the original asset names because sometimes they contain characters that cannot be
            // contained in a file name. Instead we give them a simple name with an automatically assigned index
            var filePath = AssetDatabase.GenerateUniqueAssetPath($"{folderPath}/{prefix}_0.{extension}");
            AssetDatabase.CreateAsset(objectToAdd, filePath);
            return false;
        }

        return true;
    }
    
    [MenuItem(menuPrefix + "Build avatar", validate = true)]
    private static bool CheckAvatarBuild() => MenuUtils.GetSelectedAvatar() != null;

    [MenuItem(menuPrefix + "Build avatar")]
    private static void StartAvatarBuild()
    {
        GameObject originalObject;
        try
        {
            _isBuilding = true;
            originalObject = MenuUtils.GetSelectedAvatar();
            VRCFuryTestCopyMenuItem.BuildTestCopy(originalObject);
        }
        catch (Exception e)
        {
            Debug.LogError("An exception occured when building avatar. This shouldn't happen but oh well. Exception :");
            Debug.LogException(e);
            _isBuilding = false;
            return;
        }
        _isBuilding = false;

        try
        {
            var clone = GameObject.Find("VRCF Test Copy for " + originalObject.name);
            if (clone == null) return;

            if (clone.TryGetComponent<VRCFuryTest>(out var test)) { Object.DestroyImmediate(test); }

            VRCAvatarDescriptor.CustomAnimLayer[] cloneLayers;
            {
                var cloneDescriptor = clone.GetComponent<VRCAvatarDescriptor>();
                cloneLayers = cloneDescriptor.baseAnimationLayers.Concat(cloneDescriptor.specialAnimationLayers).ToArray();
            }

            var sourcePath = $"Packages/com.vrcfury.temp/" + clone.name;
            sourcePath = AssetDatabase.GetSubFolders(sourcePath)[0];

            foreach (var guid in AssetDatabase.FindAssets("VRCFury *", new string[] { sourcePath }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var fileName = Path.GetFileName(path);

                if (fileName.EndsWith(".controller"))
                {
                    var layerType = cloneLayers
                        .Where(l => l.animatorController == AssetDatabase.LoadAssetAtPath<AnimatorController>(path))
                        .First().type;

                    AssetDatabase.MoveAsset(path, $"{sourcePath}/{layerType}_layer.controller");
                }
                else if (fileName.StartsWith("VRCFury Params"))
                {
                    AssetDatabase.MoveAsset(path, $"{sourcePath}/parameters.asset");
                }
                else if (fileName.StartsWith("VRCFury Menu"))
                {
                    AssetDatabase.MoveAsset(path, $"{sourcePath}/main_menu.asset");
                }
            }

            clone.name = $"VRCF clone ({originalObject.name})";

            Debug.Log("Avatar cloned and VRCFPatcher did its thing !");
        }
        catch (Exception e)
        {
            Debug.LogError("Post processing the clone avatar failed. This will probably not impact "
            + "build, although you may have to open an issue on github. Exception :");
            Debug.LogException(e);
        }
        finally
        {
            AssetDatabase.SaveAssets();
            originalObject.SetActive(false);
        }
    }
    
    [MenuItem(menuPrefix + "Fix Missing Parameters", validate = true)]
    private static bool CheckFixMissingParameters()
    {
        if (MenuUtils.GetSelectedAvatar() != null)
        {
            var obj = Selection.activeGameObject;
            return obj.name.StartsWith("VRCF clone (") && obj.name.EndsWith(")_KannaProteccted");
        }
        return false;
    }
    
    [MenuItem(menuPrefix + "Fix Missing Parameters")]
    private static void FixMissingParameters()
    {
        var avatar = MenuUtils.GetSelectedAvatar().GetComponent<VRCAvatarDescriptor>();
        var originalAvatarName = avatar.name.Remove(avatar.name.IndexOf("_KannaProteccted"));
        var originalAvatar = Resources.FindObjectsOfTypeAll<VRCAvatarDescriptor>().Where(o => o.gameObject.name == originalAvatarName).First();

        var parameters = avatar.expressionParameters.parameters.ToList();

        void RecursiveCheckMenuParams(VRCExpressionsMenu currentMenu, VRCExpressionsMenu originalMenu)
        {
            for (int i = 0; i < currentMenu.controls.Count; i++)
            {
                var control = currentMenu.controls[i];

                if (control.parameter != null 
                    && !string.IsNullOrEmpty(control.parameter.name)
                    && !avatar.expressionParameters.parameters.Any(p => p.name == control.parameter.name))
                {
                    var originalParameter = originalAvatar.expressionParameters.parameters.Where(p => p.name == originalMenu.controls[i].parameter.name).First();

                    var newParam = new VRCExpressionParameters.Parameter()
                    {
                        name = control.parameter.name,
                        valueType = originalParameter.valueType,
                        saved = originalParameter.saved,
                        defaultValue = originalParameter.defaultValue,
                        networkSynced = originalParameter.networkSynced
                    };
                    Debug.Log($"Adding '{newParam.name}' to the expression parameters");
                    parameters.Insert(Random.Range(0, parameters.Count), newParam);
                }

                for (int j = 0; j < control.subParameters.Length; j++)
                {
                    var subParam = control.subParameters[j];
                    if (subParam != null
                        && !string.IsNullOrEmpty(subParam.name)
                        && !avatar.expressionParameters.parameters.Any(p => p.name == subParam.name))
                    {
                        var originalParameter = originalAvatar.expressionParameters.parameters.Where(p => p.name == originalMenu.controls[i].subParameters[j].name).First();

                        var newParam = new VRCExpressionParameters.Parameter()
                        {
                            name = control.subParameters[j].name,
                            valueType = originalParameter.valueType,
                            saved = originalParameter.saved,
                            defaultValue = originalParameter.defaultValue,
                            networkSynced = originalParameter.networkSynced
                        };
                        Debug.Log($"Adding '{newParam.name}' to the expression parameters");
                        parameters.Insert(Random.Range(0, parameters.Count), newParam);
                    }
                }

                if (control.type == VRCExpressionsMenu.Control.ControlType.SubMenu && control.subMenu != null)
                {
                    RecursiveCheckMenuParams(control.subMenu, originalMenu.controls[i].subMenu);
                }
            }
        }

        RecursiveCheckMenuParams(avatar.expressionsMenu, originalAvatar.expressionsMenu);
        avatar.expressionParameters.parameters = parameters.ToArray();
        AssetDatabase.SaveAssets();
    }

#else
    static VrcfPatcher()
    {
        _harmony.UnpatchAll(_patchName);
    }
#endif
}