using BTD_Mod_Helper;
using HarmonyLib;
using Il2CppAssets.Scripts.Unity.UI_New.Main.ModeSelect;
using MelonLoader;
using System.Collections.Generic;

namespace BloonsArchipelago.Patches.MapMenu
{
    [HarmonyPatch(typeof(ModeButton), nameof(ModeButton.Update))]
    internal class AllModesUnlocked
    {
        private static readonly HashSet<string> _loggedButtons = new();

        [HarmonyPrefix]
        public static bool Prefix(ModeButton __instance)
        {
            var sh = BloonsArchipelago.sessionHandler;
            if (!sh.ready) return true;

            string modeType = __instance.modeType ?? __instance.name;
            bool shouldUnlock;

            if (!sh.HasModePool)
            {
                if (modeType == "Standard")
                {
                    string tier = DifficultyLockPatch.CurrentTier;
                    shouldUnlock = !string.IsNullOrEmpty(tier)
                        ? sh.LegacyModes.Contains(tier)
                        : sh.LegacyModes.Contains("Easy") || sh.LegacyModes.Contains("Medium") || sh.LegacyModes.Contains("Hard");
                }
                else
                {
                    string apMode = Utils.SessionHandler.GameModeToApMode(modeType);
                    shouldUnlock = sh.LegacyModes.Contains(apMode);
                }
            }
            else if (sh.GoalType == 0 && !string.IsNullOrEmpty(sh.VictoryMap) && sh.currentMap == sh.VictoryMap)
            {
                string logKey = $"{sh.currentMap}|{modeType}";
                if (_loggedButtons.Add(logKey))
                    MelonLogger.Msg($"[VictoryMapLock] currentMap={sh.currentMap} VictoryMap={sh.VictoryMap} GoalMode={sh.GoalMode} GoalType={sh.GoalType} modeType={modeType} CurrentTier={DifficultyLockPatch.CurrentTier}");

                string apMode = modeType == "Standard"
                    ? DifficultyLockPatch.CurrentTier
                    : Utils.SessionHandler.GameModeToApMode(modeType);

                shouldUnlock = !string.IsNullOrEmpty(apMode) && apMode == sh.GoalMode;
            }
            else
            {
                string apMapId = Utils.SessionHandler.GameIdToApId(sh.currentMap);
                sh.MapModes.TryGetValue(apMapId, out var modes);
                modes ??= new List<string>();

                if (modeType == "Standard")
                {
                    string tier = DifficultyLockPatch.CurrentTier;
                    shouldUnlock = !string.IsNullOrEmpty(tier)
                        ? modes.Contains(tier)
                        : modes.Contains("Easy") || modes.Contains("Medium") || modes.Contains("Hard");
                }
                else
                {
                    string apMode = Utils.SessionHandler.GameModeToApMode(modeType);
                    shouldUnlock = modes.Contains(apMode);
                }
            }

            if (shouldUnlock)
            {
                if (__instance.currentState != "Unlock")
                {
                    __instance.Unlock();
                    __instance.DisplayUnlockAnimation();
                    __instance.currentState = "Unlock";
                }
            }
            else
            {
                if (__instance.currentState != "Lock")
                {
                    __instance.Lock();
                    __instance.currentState = "Lock";
                }
            }

            return false;
        }
    }
}
