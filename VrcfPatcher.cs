using UnityEngine;
using VRC;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;

#if USE_VRCFURY
using System.Reflection;
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
    private static string _targetFolderPath = null;

    private static Dictionary<VRCExpressionsMenu, List<VRCExpressionsMenu.Control>> _submenuControlsKV = new();

    public static bool AssetDBPrefix(Object objectToAdd, Object assetObject)
    {
        if (_isBuilding && _typePrefixExtensionKV.ContainsKey(objectToAdd.GetType()))
        {
            var folderPath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(assetObject));
            if (_targetFolderPath == null)
                _targetFolderPath = folderPath;
            (var prefix, var extension) = _typePrefixExtensionKV[objectToAdd.GetType()];

            if (objectToAdd is VRCExpressionsMenu menu)
                _submenuControlsKV.Add(menu, menu.controls.ToList());

            // We don't keep the original asset names because sometimes they contain characters that cannot be
            // contained in a file name. Instead we give them a simple name with an automatically assigned index
            var filePath = AssetDatabase.GenerateUniqueAssetPath($"{folderPath}/{prefix}_0.{extension}");
            AssetDatabase.CreateAsset(objectToAdd, filePath);
            objectToAdd.MarkDirty();

            return false;
        }

        return true;
    }
    
    [MenuItem(menuPrefix + "Build avatar", validate = true)]
    private static bool CheckAvatarBuild() => GetSelectedAvatar() != null;

    private static GameObject GetSelectedAvatarGameObject()
    {
        var VFavatar = GetSelectedAvatar();
        var avatar = VFavatar.GetType().GetField("_gameObject", AccessTools.all).GetValue(VFavatar) as GameObject;

        Debug.Log($"Found selected avatar GameObject: {avatar?.name ?? "Null"}");
        
        return avatar;
    }
    
    private static object GetSelectedAvatar()
    {
        var getSelected = GetMenuUtils().GetMethod("GetSelectedAvatar", AccessTools.all);
        
        Debug.Log($"Found GetSelectedAvatar: {getSelected.Name}");
        
        var avatar = getSelected.Invoke(null, Array.Empty<object>());
        
        Debug.Log($"Found selected avatar: {avatar?.GetType()?.Name ?? "Null"}");
        
        return avatar;
    }

    private static Type GetMenuUtils()
    {
        var menuUtils = AppDomain.CurrentDomain.GetAssemblies().First(o => o.GetName().Name == "VRCFury-Editor").GetTypes().First(o => o.Namespace == "VF.Menu" && o.Name == "MenuUtils");
        
        Debug.Log($"Found MenuUtils: {menuUtils.Namespace}.{menuUtils.Name}");
        
        return menuUtils;
    }
    //private static bool CheckAvatarBuilds() => MenuUtils.GetSelectedAvatar() != null;

    [MenuItem(menuPrefix + "Build avatar")]
    private static void StartAvatarBuild()
    {
        GameObject originalObject;
        object originalObjectVF;
        try
        {
            _isBuilding = true;
            _targetFolderPath = null;
            _submenuControlsKV.Clear();
            originalObjectVF = GetSelectedAvatar();
            originalObject = GetSelectedAvatarGameObject();
            AppDomain.CurrentDomain.GetAssemblies().First(o => o.GetName().Name == "VRCFury-Editor").GetTypes().First(o => o.Namespace == "VF.Menu" && o.Name == "VRCFuryTestCopyMenuItem").GetMethod("BuildTestCopy", AccessTools.all).Invoke(null, new []{ originalObjectVF });
        }
        catch (Exception e)
        {
            Debug.LogError("An exception occured when building avatar. This shouldn't happen but oh well. Exception :");
            Debug.LogException(e);
            _isBuilding = false;
            _targetFolderPath = null;
            return;
        }
        _isBuilding = false;

        try
        {
            PostProcessAvatar(originalObject.name);
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

    static void PostProcessAvatar(string originalObjectName)
    {
        var clone = GameObject.Find("VRCF Test Copy for " + originalObjectName);
        if (clone == null) return;

        if (clone.GetComponents<Component>().FirstOrDefault(o => o.GetType().Name == "VRCFuryTest") is var test && test != null)
        {
            Object.DestroyImmediate(test);
        }

        VRCAvatarDescriptor.CustomAnimLayer[] cloneLayers;
        var cloneDescriptor = clone.GetComponent<VRCAvatarDescriptor>();
        cloneLayers = cloneDescriptor.baseAnimationLayers.Concat(cloneDescriptor.specialAnimationLayers).ToArray();

        AssetDatabase.Refresh();

        string sourcePath = _targetFolderPath;
        if (sourcePath == null)
            throw new Exception("_targetFolderPath is null");
        Debug.Log("Temp folder path is " + sourcePath);

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
        }

        var exprParamPath = AssetDatabase.GetAssetPath(cloneDescriptor.expressionParameters);
        if (exprParamPath != null)
            AssetDatabase.MoveAsset(exprParamPath, $"{sourcePath}/parameters.asset");
        else
            Debug.LogError("expressions parameters of avatar descriptor not in an asset file");

        var exprMenuPath = AssetDatabase.GetAssetPath(cloneDescriptor.expressionsMenu);
        if (exprMenuPath != null)
        {
            // We have to make a clone of the top menu because its submenu assets won't survive
            // a reload (don't ask why)
            var menu = AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(exprMenuPath);
            var menuClone = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
            menuClone.controls = menu.controls.ToList();
            AssetDatabase.DeleteAsset(exprMenuPath);
            AssetDatabase.CreateAsset(menuClone, $"{sourcePath}/main_menu.asset");
            cloneDescriptor.expressionsMenu = menuClone;

            // in addition the submenus sometimes get their own submenus set to null
            // like i really don't know why, that's why we copy their control properties
            // when their respective assets are created and give them back here
            // and it does the trick
            void RecursiveEmptySubmenuFix(VRCExpressionsMenu cur)
            {
                if (cur != menuClone && !_submenuControlsKV.ContainsKey(cur))
                    Debug.LogWarning(cur.name + " not in expected menu list");
                else
                {
                    Debug.Log($"Fixing menu {cur.name}");
                    if (cur != menuClone)
                    {
                        var count = cur.controls.Count(c => c.type == VRCExpressionsMenu.Control.ControlType.SubMenu && c.subMenu);
                        if (count > 0)
                            Debug.Log($"Fixing was indeed needed for this menu as it had {count} links broken");
                        cur.controls = _submenuControlsKV[cur];
                    }
                    foreach (var control in cur.controls)
                    {
                        if (control.type == VRCExpressionsMenu.Control.ControlType.SubMenu)
                        {
                            if (control.subMenu == null)
                                Debug.LogWarning($"submenu {control.name} in {cur.name} is null");
                            else
                                RecursiveEmptySubmenuFix(control.subMenu);
                        }
                    }
                }
                cur.MarkDirty();
            }
            RecursiveEmptySubmenuFix(menuClone);
        }
        else
            Debug.LogError("expressions menu of avatar descriptor not in an asset file");

        AssetDatabase.Refresh();
        clone.name = $"VRCF clone ({originalObjectName})";

        Debug.Log("Avatar cloned and VRCFPatcher did its thing !");
    }
    
    [MenuItem(menuPrefix + "Fix Missing Parameters", validate = true)]
    private static bool CheckFixMissingParameters()
    {
        if (GetSelectedAvatar() != null)
        {
            var obj = Selection.activeGameObject;
            return obj.name.StartsWith("VRCF clone (") && obj.name.EndsWith(")_KannaProteccted");
        }
        return false;
    }
    
    [MenuItem(menuPrefix + "Fix Missing Parameters")]
    private static void FixMissingParameters()
    {
        var avatar = GetSelectedAvatarGameObject().GetComponent<VRCAvatarDescriptor>();
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