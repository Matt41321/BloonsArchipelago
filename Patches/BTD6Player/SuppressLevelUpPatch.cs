using HarmonyLib;
using Il2CppAssets.Scripts.Unity.UI_New.LevelUp;

namespace BloonsArchipelago.Patches.BTD6Player
{
    [HarmonyPatch(typeof(LevelUpScreen), nameof(LevelUpScreen.Open))]
    internal class SuppressLevelUpPatch
    {
        [HarmonyPrefix]
        private static bool Prefix()
        {
            return !BloonsArchipelago.sessionHandler.ready;
        }
    }
}
