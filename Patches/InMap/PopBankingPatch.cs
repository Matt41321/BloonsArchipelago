using HarmonyLib;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using BTD_Mod_Helper.Extensions;

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

                foreach (var tower in inGame.GetTowers())
                {
                    try
                    {
                        string baseId = tower?.towerModel?.baseId;
                        if (string.IsNullOrEmpty(baseId)) continue;
                        if (baseId == "MonkeyVillage") continue;

                        bool isBanana = baseId == "BananaFarm";
                        long value = isBanana ? (long)tower.cashEarned : tower.pops;
                        long id = tower.Pointer.ToInt64();
                        sh.TowerInstanceStartPops.TryGetValue(id, out long startPops);
                        long delta = System.Math.Max(0, value - startPops);
                        sh.BankTowerPops(baseId, delta);
                    }
                    catch { }
                }

                sh.TowerInstanceStartPops.Clear();

                sh.SaveProgress();
            }
            catch { }
        }
    }
}
