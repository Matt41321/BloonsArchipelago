using HarmonyLib;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.TowerSelectionMenu;

namespace BloonsArchipelago.Patches.InMap
{
    [HarmonyPatch(typeof(UpgradeButton), nameof(UpgradeButton.HasUnlockedUpgrade))]
    internal class UpgradePathLockPatch
    {
        [HarmonyPostfix]
        public static void Postfix(UpgradeButton __instance, ref bool __result)
        {
            try
            {
                if (!__result) return;
                var sh = BloonsArchipelago.sessionHandler;
                if (sh == null || !sh.ready || !sh.UpgradeSanityEnabled) return;

                var upgrade = __instance.upgrade;
                if (upgrade == null) return;

                int tier = upgrade.tier;    // 0-indexed: 3=T4, 4=T5
                if (tier < 3) return;       // T1-T3 stay freely purchasable

                int path = upgrade.path;
                string pathName = path switch
                {
                    0 => "TopPath",
                    1 => "MiddlePath",
                    2 => "BottomPath",
                    _ => null,
                };
                if (pathName == null) return;

                var baseId = __instance.tts?.tower?.towerModel?.baseId;
                if (string.IsNullOrEmpty(baseId)) return;

                if (!sh.PathsUnlocked.Contains($"{baseId}-{pathName}"))
                    __result = false;
            }
            catch { }
        }
    }
}
