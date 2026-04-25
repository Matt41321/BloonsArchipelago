using HarmonyLib;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.TowerSelectionMenu;

namespace BloonsArchipelago.Patches.InMap
{
    [HarmonyPatch(typeof(UpgradeButton), nameof(UpgradeButton.GetUpgradeStatus))]
    internal class UpgradeStatusPatch
    {
        [HarmonyPostfix]
        public static void Postfix(UpgradeButton __instance, int path, ref UpgradeButton.UpgradeStatus __result)
        {
            try
            {
                if (__result == UpgradeButton.UpgradeStatus.Locked ||
                    __result == UpgradeButton.UpgradeStatus.NoTiersLeft)
                    return;

                var sh = BloonsArchipelago.sessionHandler;
                if (sh == null || !sh.ready) return;

                var upgrade = __instance.upgrade;
                if (upgrade == null) return;
                int tier = upgrade.tier;

                var baseId = __instance.tts?.tower?.towerModel?.baseId;
                if (string.IsNullOrEmpty(baseId)) return;

                if (sh.UpgradeSanityEnabled && tier >= 3)
                {
                    string pathName = path switch
                    {
                        0 => "TopPath",
                        1 => "MiddlePath",
                        2 => "BottomPath",
                        _ => null,
                    };
                    if (pathName != null && !sh.PathsUnlocked.Contains($"{baseId}-{pathName}"))
                    {
                        __result = UpgradeButton.UpgradeStatus.Locked;
                        return;
                    }
                }

                if (sh.PopTierChecksEnabled && tier >= 2 && baseId != "MonkeyVillage")
                {
                    string unlockKey = $"{baseId}-Tier{tier + 1}";
                    if (!sh.PermanentlyUnlockedTiers.Contains(unlockKey))
                    {
                        long total = PopTierLockPatch.GetAggregateProgress(baseId);
                        long required = PopTierLockPatch.GetRequired(sh, tier);
                        if (total < required)
                            __result = UpgradeButton.UpgradeStatus.Locked;
                    }
                }
            }
            catch { }
        }
    }
}
