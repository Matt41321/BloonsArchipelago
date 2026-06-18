using BTD_Mod_Helper.Extensions;
using Il2CppAssets.Scripts.Simulation.Towers;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using MelonLoader;
using System;
using System.Collections.Generic;
using SimVector2 = Il2CppAssets.Scripts.Simulation.SMath.Vector2;

namespace BloonsArchipelago.Patches.InMap
{
    internal static class SwapTrapManager
    {
        public static volatile int PendingSwapCount = 0;

        private static readonly System.Random _rng = new();

        public static void Update()
        {
            while (PendingSwapCount > 0)
            {
                PendingSwapCount--;
                try { PerformSwap(); }
                catch (Exception ex) { MelonLogger.Warning($"[SwapTrap] Error: {ex}"); }
            }
        }

        public static void CleanupAll()
        {
            PendingSwapCount = 0;
        }

        private static void PerformSwap()
        {
            var inGame = InGame.instance;
            if (inGame == null) return;

            var towers = new List<Tower>();
            foreach (var tower in inGame.GetTowers())
            {
                if (tower == null) continue;
                towers.Add(tower);
            }

            if (towers.Count < 2)
            {
                MelonLogger.Msg("[SwapTrap] Not enough towers to swap.");
                return;
            }

            int i = _rng.Next(towers.Count);
            int j;
            do { j = _rng.Next(towers.Count); } while (j == i);

            var towerA = towers[i];
            var towerB = towers[j];

            string nameA = towerA.towerModel?.baseId ?? "?";
            string nameB = towerB.towerModel?.baseId ?? "?";

            float ax = towerA.Position.X, ay = towerA.Position.Y;
            float bx = towerB.Position.X, by = towerB.Position.Y;

            towerA.PositionTower(new SimVector2(bx, by));
            towerB.PositionTower(new SimVector2(ax, ay));

            MelonLogger.Msg($"[SwapTrap] Swapped {nameA} ({ax:F1},{ay:F1}) <-> {nameB} ({bx:F1},{by:F1})");
        }
    }
}
