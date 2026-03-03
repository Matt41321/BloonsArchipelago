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
            if (!BloonsArchipelago.sessionHandler.ready) return;

            var allMaps = GameData._instance.mapSet.Maps.items;
            if (SessionHandler.defaultMapList == null)
            {
                MelonLogger.Warning("[BloonsArchipelago] defaultMapList was null on map screen open — re-capturing from GameData.");
                SessionHandler.defaultMapList = allMaps;
                SessionHandler.RebuildValidMapIds();
            }
            else if (allMaps != null && allMaps.Length > SessionHandler.defaultMapList.Length)
            {
                MelonLogger.Msg($"[BloonsArchipelago] GameData map count ({allMaps.Length}) exceeds snapshot ({SessionHandler.defaultMapList.Length}) — BTD6 update added maps. Re-capturing.");
                SessionHandler.defaultMapList = allMaps;
                SessionHandler.RebuildValidMapIds();
            }

            GameData._instance.mapSet.Maps.items = BloonsArchipelago.sessionHandler.GetMapDetails();
        }
    }
}
