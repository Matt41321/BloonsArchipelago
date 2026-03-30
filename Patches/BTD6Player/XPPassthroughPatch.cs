using HarmonyLib;
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
                return false; // suppress vanilla XP gain to prevent level-ups during AP runs
                }
                return true;
            }
    }
}
