using HarmonyLib;
using Il2CppAssets.Scripts.Unity.Player;

namespace BloonsArchipelago.Patches.BTD6Player
{
    [HarmonyPatch(typeof(Btd6Player), nameof(Btd6Player.CanUnlockMap))]
    internal class VictoryMapUnlock
    {
        [HarmonyPrefix]
        private static bool Prefix(string map)
        {
            if (BloonsArchipelago.sessionHandler.ready && map == BloonsArchipelago.sessionHandler.VictoryMap) return false;
            return true;
        }
    }
}
