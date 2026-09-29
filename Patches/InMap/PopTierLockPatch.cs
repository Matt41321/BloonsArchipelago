using HarmonyLib;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.TowerSelectionMenu;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using BTD_Mod_Helper.Extensions;
using BloonsArchipelago.Utils;
using UnityEngine;
using System.Collections.Generic;
using TSM = Il2CppAssets.Scripts.Unity.UI_New.InGame.TowerSelectionMenu.TowerSelectionMenu;

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
                    sh.SaveProgress();
                }
            }
            catch { }
        }

        public static long GetAggregateProgress(string baseId)
        {
            var sh = BloonsArchipelago.sessionHandler;
            if (sh == null) return 0;

            sh.CumulativePops.TryGetValue(baseId, out long cumulative);
            sh.SessionEndLivePops.TryGetValue(baseId, out long sessionEnd);

            long liveTotal = 0;
            bool isBanana = baseId == BANANA;
            try
            {
                var inGame = InGame.instance;
                if (inGame != null)
                {
                    foreach (var t in inGame.GetTowers())
                    {
                        try
                        {
                            if (t?.towerModel?.baseId != baseId) continue;
                            liveTotal += isBanana ? (long)t.cashEarned : t.damageDealt;
                        }
                        catch { }
                    }
                }
            }
            catch { }

            return cumulative + System.Math.Max(0, liveTotal - sessionEnd);
        }

        public static long GetRequired(SessionHandler sh, int tier) =>
            tier == 2 ? sh.Tier3PopRequirement
          : tier == 3 ? sh.Tier4PopRequirement
          : sh.Tier5PopRequirement;

        public static void OnTowerUpgraded(string baseId, int[] tiers)
        {
            try
            {
                var sh = BloonsArchipelago.sessionHandler;
                if (sh == null || !sh.ready || !sh.PopTierChecksEnabled) return;
                if (string.IsNullOrEmpty(baseId) || baseId == VILLAGE || tiers == null) return;

                bool changed = false;
                foreach (int pathTier in tiers)
                {
                    for (int tier = 3; tier <= System.Math.Min(pathTier, 5); tier++)
                    {
                        string checkKey = $"{baseId}-Tier{tier}";
                        if (sh.PurchasedTiers.Add(checkKey)) changed = true;
                        if (!sh.LocationChecked(checkKey))
                            sh.CompleteCheck(checkKey);
                    }
                }
                if (changed) sh.SaveProgress();
            }
            catch { }
        }

        public static void ResyncChecks()
        {
            try
            {
                var sh = BloonsArchipelago.sessionHandler;
                if (sh == null || !sh.ready || !sh.PopTierChecksEnabled) return;

                var baseIds = new HashSet<string>(sh.CumulativePops.Keys);
                try
                {
                    var inGame = InGame.instance;
                    if (inGame != null)
                    {
                        foreach (var t in inGame.GetTowers())
                        {
                            try
                            {
                                string id = t?.towerModel?.baseId;
                                if (!string.IsNullOrEmpty(id)) baseIds.Add(id);
                            }
                            catch { }
                        }
                    }
                }
                catch { }

                bool changed = false;
                foreach (string baseId in baseIds)
                {
                    if (baseId == VILLAGE) continue;
                    long total = GetAggregateProgress(baseId);
                    for (int tier = 2; tier <= 4; tier++)
                    {
                        string unlockKey = $"{baseId}-Tier{tier + 1}";
                        if (sh.PermanentlyUnlockedTiers.Contains(unlockKey)) continue;
                        if (total < GetRequired(sh, tier)) continue;
                        sh.PermanentlyUnlockedTiers.Add(unlockKey);
                        changed = true;
                    }
                }

                int sent = 0;
                foreach (string checkKey in sh.PurchasedTiers)
                {
                    if (sh.LocationChecked(checkKey)) continue;
                    sh.CompleteCheck(checkKey);
                    sent++;
                }

                if (changed) sh.SaveProgress();
            }
            catch { }
        }

        public static void UpdateButtonDisplays()
        {
            try
            {
                var sh = BloonsArchipelago.sessionHandler;
                if (sh == null || !sh.ready) return;
                if (!sh.PopTierChecksEnabled && !sh.UpgradeSanityEnabled) return;

                bool needsRefresh = false;
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

                        // Upgrade-sanity "Locked" text (T4/T5 path missing).
                        // Pop-tier progress text takes priority when it would also write.
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
                            {
                                bool popWillWrite = false;
                                if (sh.PopTierChecksEnabled)
                                {
                                    string popKey = $"{baseId}-Tier{tier + 1}";
                                    if (!sh.PermanentlyUnlockedTiers.Contains(popKey))
                                    {
                                        long t2 = GetAggregateProgress(baseId);
                                        long r2 = GetRequired(sh, tier);
                                        popWillWrite = t2 < r2;
                                    }
                                }
                                if (!popWillWrite && btn.cost != null)
                                {
                                    btn.cost.SetText("Locked");
                                    continue;
                                }
                            }
                        }

                        if (!sh.PopTierChecksEnabled) continue;

                        string unlockKey = $"{baseId}-Tier{tier + 1}";
                        if (sh.PermanentlyUnlockedTiers.Contains(unlockKey)) continue;

                        long total = GetAggregateProgress(baseId);
                        long required = GetRequired(sh, tier);

                        if (total < required && btn.cost != null)
                        {
                            string unit = baseId == BANANA ? "cash" : "dmg";
                            btn.cost.SetText($"{total:N0} / {required:N0} {unit}");
                        }
                        else if (total >= required)
                        {
                            sh.PermanentlyUnlockedTiers.Add(unlockKey);
                            sh.SaveProgress();
                            needsRefresh = true;
                        }
                    }
                    catch { }
                }

                if (needsRefresh)
                {
                    try
                    {
                        Object.FindObjectOfType<TSM>()?.RefreshUpgrades();
                    }
                    catch { }
                }
            }
            catch { }
        }
    }
}
