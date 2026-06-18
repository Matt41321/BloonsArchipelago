using BTD_Mod_Helper.Extensions;
using Il2CppAssets.Scripts.Simulation.Towers;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using MelonLoader;
using System;
using System.Collections.Generic;
using SimVector2 = Il2CppAssets.Scripts.Simulation.SMath.Vector2;

namespace BloonsArchipelago.Patches.InMap
{
    internal static class ShuffleTrapManager
    {
        public static volatile int PendingShuffleCount = 0;

        private static readonly System.Random _rng = new();

        public static void Update()
        {
            while (PendingShuffleCount > 0)
            {
                PendingShuffleCount--;
                try { PerformShuffle(); }
                catch (Exception ex) { MelonLogger.Warning($"[ShuffleTrap] Error: {ex}"); }
            }
        }

        public static void CleanupAll()
        {
            PendingShuffleCount = 0;
        }

        private static void PerformShuffle()
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
                MelonLogger.Msg("[ShuffleTrap] Not enough towers to shuffle.");
                return;
            }

            var positions = new List<SimVector2>(towers.Count);
            foreach (var tower in towers)
                positions.Add(new SimVector2(tower.Position.X, tower.Position.Y));

            var order = new List<int>(towers.Count);
            for (int i = 0; i < towers.Count; i++) order.Add(i);
            for (int i = order.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }

            for (int k = 0; k < order.Count; k++)
            {
                var tower = towers[order[k]];
                var dest = positions[order[(k + 1) % order.Count]];
                try { tower.PositionTower(dest); }
                catch (Exception ex) { MelonLogger.Warning($"[ShuffleTrap] Failed to move tower: {ex.Message}"); }
            }

            MelonLogger.Msg($"[ShuffleTrap] Shuffled {towers.Count} towers.");
        }
    }
}
