using BTD_Mod_Helper;
using HarmonyLib;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using System.Linq;

namespace BloonsArchipelago.Patches.InMap
{
    [HarmonyPatch(typeof(InGame), nameof(InGame.RoundEnd))]
    internal class PreviousMedalChecker
    {
        // Each array lists game-mode currentMode values that clear the target difficulty.
        private static readonly string[] _clearsEasy = {
            "Medium", "Hard", "Impoppable", "Clicks",
            "HalfCash", "DoubleMoabHealth", "AlternateBloonsRounds", "MagicOnly",
            "MilitaryOnly", "Reverse", "PrimaryOnly"
        };

        private static readonly string[] _clearsMedium = {
            "Hard", "Impoppable", "Clicks",
            "HalfCash", "DoubleMoabHealth", "AlternateBloonsRounds", "MagicOnly",
            "Apopalypse", "MilitaryOnly", "Reverse"
        };

        private static readonly string[] _clearsHard = {
            "Impoppable", "Clicks",
            "HalfCash", "DoubleMoabHealth", "AlternateBloonsRounds", "MagicOnly"
        };

        [HarmonyPostfix]
        private static void Postfix(InGame __instance, int completedRound, int highestCompletedRound)
        {
            var sh = BloonsArchipelago.sessionHandler;
            if (!sh.ready) return;

            if (sh.currentMode == "Standard")
                sh.currentMode = __instance.SelectedDifficulty;

            string apMap = Utils.SessionHandler.GameIdToApId(sh.currentMap);
            sh.MapModes.TryGetValue(apMap, out var mapModes);
            mapModes ??= new System.Collections.Generic.List<string>();

            string mode = sh.currentMode;

            if (completedRound == 39 && _clearsEasy.Contains(mode) && mapModes.Contains("Easy"))
            {
                sh.CompleteCheck(apMap + "-Easy");
                ModHelper.Msg<BloonsArchipelago>(apMap + "-Easy");
            }

            if (completedRound == 59 && _clearsMedium.Contains(mode) && mapModes.Contains("Medium"))
                sh.CompleteCheck(apMap + "-Medium");

            if (completedRound == 79 && _clearsHard.Contains(mode) && mapModes.Contains("Hard"))
                sh.CompleteCheck(apMap + "-Hard");

            if (completedRound == 99 && mode == "Clicks" && mapModes.Contains("Impoppable"))
                sh.CompleteCheck(apMap + "-Impoppable");

            int interval = sh.RoundSanityInterval;
            var customChecks = sh.CustomRoundChecks;
            int round = completedRound + 1;
            if (interval > 0)
            {
                for (int r = interval; r <= round && r <= 100; r += interval)
                {
                    sh.CompleteCheck($"{apMap}-Round {r}");
                    ModHelper.Msg<BloonsArchipelago>($"Round Sanity check: {apMap}-Round {r}");
                }
            }
            foreach (int r in customChecks)
            {
                if (r <= round && r <= 100)
                {
                    sh.CompleteCheck($"{apMap}-Round {r}");
                    ModHelper.Msg<BloonsArchipelago>($"Custom Round check: {apMap}-Round {r}");
                }
            }
        }
    }
}
