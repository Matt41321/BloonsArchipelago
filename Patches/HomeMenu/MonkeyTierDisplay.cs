using Il2CppAssets.Scripts.Unity.UI_New.Main.MonkeySelect;
using UnityEngine;

namespace BloonsArchipelago.Patches.HomeMenu
{
    internal static class MonkeyTierDisplay
    {
        public static void UpdateMonkeyTierDisplays()
        {
            try
            {
                var sh = BloonsArchipelago.sessionHandler;

                if (sh == null || !sh.ready || !sh.PopTierChecksEnabled) return;

                var btns = Object.FindObjectsOfType<MonkeyButton>();
                if (btns == null) return;

                foreach (var btn in btns)
                {
                    try
                    {
                        if (btn.xpAmount == null) continue;

                        string towerId = btn.towerId;
                        if (string.IsNullOrEmpty(towerId)) continue;

                        int score;
                        if (!sh.MonkeysUnlocked.Contains(towerId))
                        {
                            score = 0;
                        }
                        else
                        {
                            score = 2;
                            if (sh.PermanentlyUnlockedTiers.Contains($"{towerId}-Tier3")) score++;
                            if (sh.PermanentlyUnlockedTiers.Contains($"{towerId}-Tier4")) score++;
                            if (sh.PermanentlyUnlockedTiers.Contains($"{towerId}-Tier5")) score++;
                        }

                        btn.xpAmount.text = $"{score}/5";
                    }
                    catch { }
                }
            }
            catch { }
        }
    }
}
