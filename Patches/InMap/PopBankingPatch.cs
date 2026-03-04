using HarmonyLib;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using BTD_Mod_Helper.Extensions;
using System.Collections.Generic;

namespace BloonsArchipelago.Patches.InMap
{
    [HarmonyPatch(typeof(InGame), nameof(InGame.Quit))]
    internal class MapQuitBankingPatch
    {
        [HarmonyPrefix]
        public static void Prefix()
        {
            try
            {
                var sh = BloonsArchipelago.sessionHandler;
                if (sh == null || !sh.ready || !sh.PopTierChecksEnabled) return;

                var inGame = InGame.instance;
                if (inGame == null) return;

                var liveTotals = new Dictionary<string, long>();
                foreach (var tower in inGame.GetTowers())
                {
                    try
                    {
                        string baseId = tower?.towerModel?.baseId;
                        if (string.IsNullOrEmpty(baseId) || baseId == "MonkeyVillage") continue;
                        bool isBanana = baseId == "BananaFarm";
                        long pops = isBanana ? (long)tower.cashEarned : tower.damageDealt;
                        if (!liveTotals.ContainsKey(baseId)) liveTotals[baseId] = 0;
                        liveTotals[baseId] += pops;
                    }
                    catch { }
                }

                foreach (var kvp in liveTotals)
                {
                    string baseId = kvp.Key;
                    long liveTotal = kvp.Value;
                    sh.SessionEndLivePops.TryGetValue(baseId, out long prevEnd);
                    long gain = System.Math.Max(0, liveTotal - prevEnd);
                    sh.BankTowerPops(baseId, gain);
                    sh.SessionEndLivePops[baseId] = liveTotal;
                }

                sh.SaveProgress();
            }
            catch { }
        }
    }
}
