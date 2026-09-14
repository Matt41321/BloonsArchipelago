using HarmonyLib;
using Il2CppAssets.Scripts.Data.MapSets;
using Il2CppAssets.Scripts.Unity.UI_New.Transitions;

namespace BloonsArchipelago.Patches.MapMenu
{
    [HarmonyPatch(typeof(MapSelectTransition), nameof(MapSelectTransition.CheckAnyNewMapDifficultyUnlock))]
    internal class MapDifficultyUnlockSuppression
    {
        [HarmonyPrefix]
        private static bool Prefix(ref MapDifficulty unlockedDifficulty, ref bool __result)
        {
            if (BloonsArchipelago.sessionHandler.ready)
            {
                unlockedDifficulty = default;
                __result = false;
                return false;
            }
            return true;
        }
    }
}
