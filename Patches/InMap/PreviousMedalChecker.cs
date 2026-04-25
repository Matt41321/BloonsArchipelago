using BTD_Mod_Helper;
using HarmonyLib;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using System.Linq;

namespace BloonsArchipelago.Patches.InMap
{
    [HarmonyPatch(typeof(InGame), nameof(InGame.RoundEnd))]
    internal class PreviousMedalChecker
    {
        [HarmonyPostfix]
        private static void Postfix(InGame __instance, int completedRound, int highestCompletedRound)
        {
            if (BloonsArchipelago.sessionHandler.ready)
            {
                if (BloonsArchipelago.sessionHandler.currentMode == "Standard")
                {
                    BloonsArchipelago.sessionHandler.currentMode = __instance.SelectedDifficulty;
                }

                string apMap = Utils.SessionHandler.GameIdToApId(BloonsArchipelago.sessionHandler.currentMap);

                if (completedRound == 39 && new[] { "Medium", "Hard", "Impoppable", "Clicks" }.Contains(BloonsArchipelago.sessionHandler.currentMode))
                {
                    BloonsArchipelago.sessionHandler.CompleteCheck(apMap + "-Easy");
                    ModHelper.Msg<BloonsArchipelago>(apMap + "-Easy");
                }
                else if (completedRound == 59 && new[] { "Hard", "Impoppable", "Clicks" }.Contains(BloonsArchipelago.sessionHandler.currentMode))
                {
                    BloonsArchipelago.sessionHandler.CompleteCheck(apMap + "-Medium");
                }
                else if (completedRound == 79 && new[] { "Impoppable", "Clicks" }.Contains(BloonsArchipelago.sessionHandler.currentMode))
                {
                    BloonsArchipelago.sessionHandler.CompleteCheck(apMap + "-Hard");
                }
                else if (completedRound == 99 && BloonsArchipelago.sessionHandler.currentMode == "Clicks")
                {
                    BloonsArchipelago.sessionHandler.CompleteCheck(apMap + "-Impoppable");
                }

                int interval = BloonsArchipelago.sessionHandler.RoundSanityInterval;
                if (interval > 0)
                {
                    int round = completedRound + 1; // completedRound is 0-indexed
                    for (int r = interval; r <= round && r <= 100; r += interval)
                    {
                        BloonsArchipelago.sessionHandler.CompleteCheck($"{apMap}-Round {r}");
                        ModHelper.Msg<BloonsArchipelago>($"Round Sanity check: {apMap}-Round {r}");
                    }
                }
            }
        }
    }
}
