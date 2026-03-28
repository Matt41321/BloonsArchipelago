using HarmonyLib;
using Il2CppAssets.Scripts.Data;
using Il2CppAssets.Scripts.Unity.UI_New.Transitions;
using BloonsArchipelago.Utils;
using MelonLoader;
using System;

namespace BloonsArchipelago.Patches.MapMenu
{
    // guards crash from map asset data not yet cached (common on new accounts)
    [HarmonyPatch(typeof(MapSelectTransition), nameof(MapSelectTransition.Initialise))]
    internal class MapSelectInitialiseCrashGuard
    {
        [HarmonyFinalizer]
        private static Exception Finalizer(Exception __exception)
        {
            if (__exception == null) return null;

            MelonLogger.Warning($"[BloonsArchipelago] MapSelectTransition.Initialise crashed: {__exception.Message}");
            MelonLogger.Warning("[BloonsArchipelago] This usually means map asset data is not yet fully cached by the game (common on new accounts). Restoring map list and suppressing crash.");

            try
            {
                if (SessionHandler.defaultMapList != null)
                    GameData._instance.mapSet.Maps.items = SessionHandler.defaultMapList;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[BloonsArchipelago] Could not restore map list after crash: {ex.Message}");
            }

            try
            {
                SessionHandler.RefreshDefaultMapList();
            }
            catch { }

            return null;
        }
    }
}
