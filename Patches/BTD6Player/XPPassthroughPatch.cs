using HarmonyLib;
using Il2CppAssets.Scripts.Unity;
using Il2CppAssets.Scripts.Unity.Player;

namespace BloonsArchipelago.Patches.BTD6Player
{
    [HarmonyPatch(typeof(Btd6Player), nameof(Btd6Player.GainPlayerXP))]
    internal class XPPassthroughPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(float amount)
        {
            if (BloonsArchipelago.sessionHandler.ready)
            {
                BloonsArchipelago.sessionHandler.XPTracker.PassXP(amount);
                int playerRank = Game.Player?.Data?.rank?.ValueInt ?? 0;
                if (playerRank < 21)
                    return false;
            }
            return true;
        }
    }
}
