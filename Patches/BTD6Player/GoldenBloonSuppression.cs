using HarmonyLib;
using Il2CppAssets.Scripts.Unity.Player;

namespace BloonsArchipelago.Patches.BTD6Player
{
    [HarmonyPatch(typeof(Btd6Player), nameof(Btd6Player.UpdateGoldenBloonMaps))]
    internal class GoldenBloonSuppression
    {
        [HarmonyPrefix]
        private static bool Prefix()
        {
            return false;
        }
    }

    [HarmonyPatch(typeof(Btd6Player), nameof(Btd6Player.CompleteGoldenBloon))]
    internal class CompleteGoldenBloonSuppression
    {
        [HarmonyPrefix]
        private static bool Prefix()
        {
            return false;
        }
    }

    [HarmonyPatch(typeof(Btd6Player), nameof(Btd6Player.GenerateNewGoldenBloonMap))]
    internal class GenerateGoldenBloonSuppression
    {
        [HarmonyPrefix]
        private static bool Prefix()
        {
            return false;
        }
    }

    [HarmonyPatch(typeof(Btd6Player), nameof(Btd6Player.GetGoldenBloonMapDataForDifficulty))]
    internal class GetGoldenBloonMapDataSuppression
    {
        [HarmonyFinalizer]
        private static System.Exception Finalizer(System.Exception __exception, ref Il2CppSystem.Object __result)
        {
            if (__exception != null)
            {
                __result = null;
                return null;
            }
            return null;
        }
    }
}
