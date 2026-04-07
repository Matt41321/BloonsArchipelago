using HarmonyLib;
using Il2CppAssets.Scripts.Unity.Player;

namespace BloonsArchipelago.Patches.BTD6Player
{
    [HarmonyPatch(typeof(Btd6Player), nameof(Btd6Player.RotateMonkeyTeamsMap))]
    internal class RotateMonkeyTeamsSuppression
    {
        [HarmonyPrefix]
        private static bool Prefix() => !BloonsArchipelago.sessionHandler.ready;
    }

    [HarmonyPatch(typeof(Btd6Player), nameof(Btd6Player.GenerateNewMonkeyTeamsMap))]
    internal class GenerateMonkeyTeamsSuppression
    {
        [HarmonyPrefix]
        private static bool Prefix() => !BloonsArchipelago.sessionHandler.ready;
    }

    [HarmonyPatch(typeof(Btd6Player), nameof(Btd6Player.HasMapGotMonkeyTeams))]
    internal class IsMonkeyTeamsMapSuppression
    {
        [HarmonyPrefix]
        private static bool Prefix(ref bool __result)
        {
            if (!BloonsArchipelago.sessionHandler.ready) return true;
            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(Btd6Player), nameof(Btd6Player.UpdateMonkeyTeamsMaps))]
    internal class UpdateMonkeyTeamsSuppression
    {
        [HarmonyPrefix]
        private static bool Prefix() => !BloonsArchipelago.sessionHandler.ready;
    }

    [HarmonyPatch(typeof(Btd6Player), nameof(Btd6Player.GetMonkeyTeamsMapDataForDifficulty))]
    internal class GetMonkeyTeamsMapDataSuppression
    {
        [HarmonyFinalizer]
        private static System.Exception Finalizer(System.Exception __exception, ref Il2CppSystem.Object __result)
        {
            if (__exception != null)
            {
                __result = null;
                return null; // Suppress the exception
            }
            return null; // Return null to not override any unhandled exceptions
        }
    }
}
