using HarmonyLib;
using Il2CppAssets.Scripts.Data;
using Il2CppAssets.Scripts.Unity.UI_New.Main.MapSelect;
using BloonsArchipelago.Utils;
using MelonLoader;

namespace BloonsArchipelago.Patches.MapMenu
{
    [HarmonyPatch(typeof(MapSelectScreen), nameof(MapSelectScreen.Open))]
    internal class ReplaceMapList
    {
        [HarmonyPrefix]
        private static void Prefix()
        {
            var allMaps = GameData._instance.mapSet.Maps.items;

            if (SessionHandler.defaultMapList == null && allMaps != null && allMaps.Length > 0)
            {
                SessionHandler.defaultMapList = allMaps;
                SessionHandler.RebuildValidMapIds();
            }

            if (!BloonsArchipelago.sessionHandler.ready)
            {
                if (SessionHandler.defaultMapList != null)
                {
                    GameData._instance.mapSet.Maps.items = SessionHandler.defaultMapList;
                }
                return;
            }

            if (allMaps != null && SessionHandler.defaultMapList != null
                && allMaps.Length > SessionHandler.defaultMapList.Length)
            {
                MelonLogger.Msg($"[BloonsArchipelago] Refreshing map snapshot from GameData ({allMaps.Length} maps, was {SessionHandler.defaultMapList.Length}).");
                SessionHandler.defaultMapList = allMaps;
                SessionHandler.RebuildValidMapIds();
            }

            GameData._instance.mapSet.Maps.items = BloonsArchipelago.sessionHandler.GetMapDetails();
        }
    }
}
