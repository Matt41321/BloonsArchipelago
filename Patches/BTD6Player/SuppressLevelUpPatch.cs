using HarmonyLib;
using Il2CppAssets.Scripts.Unity;
using Il2CppAssets.Scripts.Unity.UI_New.LevelUp;

namespace BloonsArchipelago.Patches.BTD6Player
{
    [HarmonyPatch(typeof(LevelUpScreen), nameof(LevelUpScreen.Open))]
    internal class SuppressLevelUpPatch
    {
        [HarmonyPrefix]
        private static bool Prefix()
        {
            if (!BloonsArchipelago.sessionHandler.ready) return true;
            int playerRank = Game.Player?.Data?.rank?.ValueInt ?? 0;
            return playerRank >= 21;
        }
    }
}
