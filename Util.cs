using UnityEngine;
using HarmonyLib;
using System;
using System.Linq;

internal static class Util
{
#if USE_VRCFURY
    public static GameObject GetSelectedAvatarGameObject()
    {
        var VFavatar = GetSelectedAvatar();
        var avatar = VFavatar.GetType()
            .GetField("_gameObject", AccessTools.all).GetValue(VFavatar) as GameObject;
        
        return avatar;
    }
    
    public static object GetSelectedAvatar()
    {
        var menuUtils = AppDomain.CurrentDomain.GetAssemblies()
            .First(o => o.GetName().Name == "VRCFury-Editor")
            .GetTypes()
            .First(o => o.Namespace == "VF.Menu" && o.Name == "MenuUtils");
        
        var getSelected = menuUtils.GetMethod("GetSelectedAvatar", AccessTools.all);
        var avatar = getSelected.Invoke(null, Array.Empty<object>());

        return avatar;
    }
#endif
}