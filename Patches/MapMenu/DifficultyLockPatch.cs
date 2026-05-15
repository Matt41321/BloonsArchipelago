using HarmonyLib;
using Il2CppAssets.Scripts.Unity.UI_New.Main.DifficultySelect;

namespace BloonsArchipelago.Patches.MapMenu
{
    [HarmonyPatch(typeof(DifficultySelectScreen), "OpenModeSelectUi")]
    internal class DifficultyLockPatch
    {
        internal static string CurrentTier = "";

        [HarmonyPrefix]
        private static bool Prefix(string difficulty)
        {
            if (difficulty == "Easy" || difficulty == "Medium" || difficulty == "Hard")
                CurrentTier = difficulty;
            return true;
        }
    }
}
