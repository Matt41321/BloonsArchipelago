using HarmonyLib;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.TowerSelectionMenu;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using BTD_Mod_Helper.Extensions;
using BloonsArchipelago.Utils;
using UnityEngine;

namespace BloonsArchipelago.Patches.InMap
{
    [HarmonyPatch(typeof(UpgradeButton), nameof(UpgradeButton.HasUnlockedUpgrade))]
    internal class PopTierLockPatch
    {
        private const string VILLAGE = "MonkeyVillage";
        private const string BANANA = "BananaFarm";

        [HarmonyPostfix]
        public static void Postfix(UpgradeButton __instance, ref bool __result)
        {
            try
            {
                if (!__result) return;

                var sh = BloonsArchipelago.sessionHandler;
                if (sh == null || !sh.ready || !sh.PopTierChecksEnabled) return;

                var upgrade = __instance.upgrade;
                if (upgrade == null) return;
                int tier = upgrade.tier;
                if (tier < 2) return;

                var tts = __instance.tts;
                if (tts == null) return;
                var tower = tts.tower;
                if (tower == null) return;

                string baseId = tower.towerModel?.baseId;
                if (string.IsNullOrEmpty(baseId)) return;

                if (baseId == VILLAGE) return;

                string unlockKey = $"{baseId}-Tier{tier + 1}";
                if (sh.PermanentlyUnlockedTiers.Contains(unlockKey)) return;

                long total = GetAggregateProgress(baseId);
                long required = GetRequired(sh, tier);

                if (total < required)
                {
                    __result = false;
                }
                else
                {
                    sh.PermanentlyUnlockedTiers.Add(unlockKey);
                    if (!sh.LocationChecked(unlockKey))
                        sh.CompleteCheck(unlockKey);
                    sh.SaveProgress();
                }
            }
            catch { }
        }

        public static long GetAggregateProgress(string baseId)
        {
            long total = 0;
            bool isBanana = baseId == BANANA;
            try
            {
                var sh = BloonsArchipelago.sessionHandler;
                if (sh != null && sh.CumulativePops.ContainsKey(baseId))
                    total += sh.CumulativePops[baseId];

                var inGame = InGame.instance;
                if (inGame != null)
                {
                    foreach (var t in inGame.GetTowers())
                    {
                        try
                        {
                            if (t?.towerModel?.baseId != baseId) continue;
                            long pops = isBanana ? (long)t.cashEarned : t.pops;
                            long id = t.Pointer.ToInt64();
                            if (!sh.TowerInstanceStartPops.ContainsKey(id))
                                sh.TowerInstanceStartPops[id] = pops;
                            total += System.Math.Max(0, pops - sh.TowerInstanceStartPops[id]);
                        }
                        catch { }
                    }
                }
            }
            catch { }
            return total;
        }

        public static long GetRequired(SessionHandler sh, int tier) =>
            tier == 2 ? sh.Tier3PopRequirement
          : tier == 3 ? sh.Tier4PopRequirement
          : sh.Tier5PopRequirement;

        public static void UpdateButtonDisplays()
        {
            try
            {
                var sh = BloonsArchipelago.sessionHandler;
                if (sh == null || !sh.ready || !sh.PopTierChecksEnabled) return;

                var buttons = Object.FindObjectsOfType<UpgradeButton>();
                foreach (var btn in buttons)
                {
                    try
                    {
                        var upgrade = btn.upgrade;
                        if (upgrade == null) continue;
                        int tier = upgrade.tier;
                        if (tier < 2) continue;

                        var tts = btn.tts;
                        if (tts == null) continue;
                        var tower = tts.tower;
                        if (tower == null) continue;

                        string baseId = tower.towerModel?.baseId;
                        if (string.IsNullOrEmpty(baseId)) continue;

                        if (baseId == VILLAGE) continue;

                        string unlockKey = $"{baseId}-Tier{tier + 1}";
                        if (sh.PermanentlyUnlockedTiers.Contains(unlockKey)) continue;

                        long total = GetAggregateProgress(baseId);
                        long required = GetRequired(sh, tier);

                        if (total < required && btn.cost != null)
                        {
                            string unit = baseId == BANANA ? "cash" : "pops";
                            btn.cost.SetText($"{total:N0} / {required:N0} {unit}");
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }
    }
}
