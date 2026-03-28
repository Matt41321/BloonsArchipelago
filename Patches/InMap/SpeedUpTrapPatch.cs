using HarmonyLib;
using Il2CppAssets.Scripts.Simulation.Bloons;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using MelonLoader;
using System;
using System.Collections.Generic;

namespace BloonsArchipelago.Patches.InMap
{
    internal static class SpeedUpTrapManager
    {
        public const float SPEED_MULTIPLIER = 1.5f;

        public static volatile int PendingSpeedUpCount = 0;

        public static void Update()
        {
            while (PendingSpeedUpCount > 0)
            {
                PendingSpeedUpCount--;
                var sh = BloonsArchipelago.sessionHandler;
                if (sh == null) continue;
                sh.SpeedUpRoundsRemaining += 2;
                MelonLogger.Msg($"[SpeedUp] Activated — SpeedUpRoundsRemaining={sh.SpeedUpRoundsRemaining}");
            }
        }

        public static void CleanupAll()
        {
            var sh = BloonsArchipelago.sessionHandler;
            if (sh != null)
                sh.SpeedUpRoundsRemaining = 0;
        }
    }

    [HarmonyPatch(typeof(Bloon), nameof(Bloon.Process))]
    internal class SpeedUpTrapProcessPatch
    {
        private static readonly Dictionary<IntPtr, float> _preDistances = new();

        [HarmonyPrefix]
        private static void Prefix(Bloon __instance)
        {
            try
            {
                var sh = BloonsArchipelago.sessionHandler;
                if (sh == null || sh.SpeedUpRoundsRemaining <= 0) return;
                _preDistances[__instance.Pointer] = __instance.DistanceTraveled;
            }
            catch { }
        }

        [HarmonyPostfix]
        private static void Postfix(Bloon __instance)
        {
            try
            {
                var sh = BloonsArchipelago.sessionHandler;
                if (sh == null || sh.SpeedUpRoundsRemaining <= 0) return;

                if (!_preDistances.TryGetValue(__instance.Pointer, out float preDist)) return;
                _preDistances.Remove(__instance.Pointer);

                float delta = __instance.DistanceTraveled - preDist;
                if (delta <= 0) return;

                __instance.SetDistanceTravelled(preDist + delta * SpeedUpTrapManager.SPEED_MULTIPLIER, false);
            }
            catch { }
        }
    }

    [HarmonyPatch(typeof(InGame), nameof(InGame.RoundEnd))]
    internal class SpeedUpTrapRoundEndPatch
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            try
            {
                var sh = BloonsArchipelago.sessionHandler;
                if (sh == null || sh.SpeedUpRoundsRemaining <= 0) return;

                sh.SpeedUpRoundsRemaining--;
                MelonLogger.Msg($"[SpeedUp] RoundEnd — SpeedUpRoundsRemaining={sh.SpeedUpRoundsRemaining}");
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[BloonsArchipelago] SpeedUp RoundEnd error: {ex.Message}");
            }
        }
    }
}
