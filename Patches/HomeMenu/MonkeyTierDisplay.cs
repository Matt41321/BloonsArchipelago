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

                        int unlocked = 0;
                        if (sh.PermanentlyUnlockedTiers.Contains($"{towerId}-Tier3")) unlocked++;
                        if (sh.PermanentlyUnlockedTiers.Contains($"{towerId}-Tier4")) unlocked++;
                        if (sh.PermanentlyUnlockedTiers.Contains($"{towerId}-Tier5")) unlocked++;

                        btn.xpAmount.text = $"Tier {unlocked}/3";
                    }
                    catch { }
                }
            }
            catch { }
        }
    }
}
