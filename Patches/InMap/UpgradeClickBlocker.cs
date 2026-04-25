using HarmonyLib;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.TowerSelectionMenu;

namespace BloonsArchipelago.Patches.InMap
{
    [HarmonyPatch(typeof(UpgradeButton), nameof(UpgradeButton.OnPointerDown))]
    internal class UpgradeClickBlocker
    {
        [HarmonyPrefix]
        public static bool Prefix(UpgradeButton __instance)
        {
            try
            {
                var sh = BloonsArchipelago.sessionHandler;
                if (sh == null || !sh.ready) return true;

                var upgrade = __instance.upgrade;
                if (upgrade == null) return true;

                int tier = upgrade.tier;
                var baseId = __instance.tts?.tower?.towerModel?.baseId;
                if (string.IsNullOrEmpty(baseId)) return true;

                if (sh.UpgradeSanityEnabled && tier >= 3)
                {
                    string pathName = upgrade.path switch
                    {
                        0 => "TopPath",
                        1 => "MiddlePath",
                        2 => "BottomPath",
                        _ => null,
                    };
                    if (pathName != null && !sh.PathsUnlocked.Contains($"{baseId}-{pathName}"))
                        return false;
                }

                if (sh.PopTierChecksEnabled && tier >= 2 && baseId != "MonkeyVillage")
                {
                    string unlockKey = $"{baseId}-Tier{tier + 1}";
                    if (!sh.PermanentlyUnlockedTiers.Contains(unlockKey))
                    {
                        long total = PopTierLockPatch.GetAggregateProgress(baseId);
                        long required = PopTierLockPatch.GetRequired(sh, tier);
                        if (total < required)
                            return false;
                    }
                }
            }
            catch { }
            return true;
        }
    }
}
