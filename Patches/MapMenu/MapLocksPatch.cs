using HarmonyLib;
using Il2CppAssets.Scripts.Unity.UI_New.Main.MapSelect;
using UnityEngine;

namespace BloonsArchipelago.Patches.MapMenu
{
    [HarmonyPatch(typeof(MapButton), nameof(MapButton.RefreshLockState))]
    internal class MapLocksPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(MapButton __instance)
        {
            if (!BloonsArchipelago.sessionHandler.ready) return true;

            var sh = BloonsArchipelago.sessionHandler;

            if (__instance.mapId == sh.VictoryMap && sh.MedalRequirement > sh.Medals)
            {
                __instance.isLocked = true;
                __instance.gameObject.transform.GetChild(6).gameObject.SetActive(true);
            }
            else
            {
                __instance.isLocked = false;

                __instance.gameObject.transform.GetChild(6).gameObject.SetActive(false);

                var t = __instance.gameObject.transform;
                for (int i = 0; i < t.childCount; i++)
                {
                    var child = t.GetChild(i).gameObject;
                    if (child.name.Contains("MonkeyTeam") || child.name.Contains("Team"))
                        child.SetActive(false);
                }
            }
            return false;
        }
    }
}
