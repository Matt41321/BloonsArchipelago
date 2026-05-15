using BTD_Mod_Helper;
using HarmonyLib;
using Il2CppAssets.Scripts.Unity.UI_New.GameOver;

namespace BloonsArchipelago.Patches
{
    [HarmonyPatch(typeof(VictoryScreen), nameof(VictoryScreen.Open))]
    internal class MapCompletePatch
    {
        [HarmonyPostfix]
        private static void Postfix(VictoryScreen __instance)
        {
            if (BloonsArchipelago.sessionHandler.currentMode == "Standard")
            {
                BloonsArchipelago.sessionHandler.currentMode = __instance.difficulty.text.Substring(__instance.difficulty.text.IndexOf(":") + 2);
            }

            string apMap = Utils.SessionHandler.GameIdToApId(BloonsArchipelago.sessionHandler.currentMap);
            string apMode = Utils.SessionHandler.GameModeToApMode(BloonsArchipelago.sessionHandler.currentMode);
            string checkstring = apMap + "-" + apMode;
            ModHelper.Msg<BloonsArchipelago>("Checked: " + checkstring);
            if (BloonsArchipelago.sessionHandler.ready)
            {
                if (BloonsArchipelago.sessionHandler.currentMap == BloonsArchipelago.sessionHandler.VictoryMap)
                {
                    string currentApMode = Utils.SessionHandler.GameModeToApMode(BloonsArchipelago.sessionHandler.currentMode);
                    if (currentApMode == BloonsArchipelago.sessionHandler.GoalMode)
                    {
                        BloonsArchipelago.sessionHandler.CompleteRando();
                        __instance.difficulty.text = "You have just beaten the Randomizer! Congragulations!";
                    }
                    return;
                }
                BloonsArchipelago.sessionHandler.CompleteCheck(checkstring);
            }
        }
    }
}
