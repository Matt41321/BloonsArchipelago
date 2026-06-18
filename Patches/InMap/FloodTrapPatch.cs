using BTD_Mod_Helper.Api.Testing;
using Il2CppAssets.Scripts.Models.Powers;
using Il2CppAssets.Scripts.Unity;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using MelonLoader;
using System;
using UnityEngine;

namespace BloonsArchipelago.Patches.InMap
{
    internal static class FloodTrapManager
    {
        public static volatile int PendingFloodCount = 0;

        private const int LAKE_COUNT = 5;
        private const float MAX_X = 110f;
        private const float MAX_Y = 85f;

        private static readonly System.Random _rng = new();

        public static void Update()
        {
            while (PendingFloodCount > 0)
            {
                PendingFloodCount--;
                try { PerformFlood(); }
                catch (Exception ex) { MelonLogger.Warning($"[FloodTrap] Error: {ex}"); }
            }
        }

        public static void CleanupAll()
        {
            PendingFloodCount = 0;
        }

        private static void PerformFlood()
        {
            var inGame = InGame.instance;
            if (inGame == null) return;

            PowerModel lakePower = null;
            var powers = Game.instance.model.powers;
            if (powers != null)
            {
                foreach (var p in powers)
                {
                    if (p?.name != null && p.name.Contains("PortableLake"))
                    {
                        lakePower = p;
                        break;
                    }
                }
            }

            var lakeModel = lakePower?.tower;
            if (lakeModel == null)
            {
                MelonLogger.Warning("[FloodTrap] PortableLake power/tower model not found.");
                return;
            }

            int placed = 0;
            for (int i = 0; i < LAKE_COUNT; i++)
            {
                float x = (float)(_rng.NextDouble() * 2.0 - 1.0) * MAX_X;
                float y = (float)(_rng.NextDouble() * 2.0 - 1.0) * MAX_Y;
                try
                {
                    ModTest.CreateTowerAt(inGame.bridge, new Vector2(x, y), lakeModel, true, true, 0);
                    placed++;
                }
                catch (Exception ex)
                {
                    MelonLogger.Warning($"[FloodTrap] Lake placement at ({x:F0},{y:F0}) failed: {ex.Message}");
                }
            }

            MelonLogger.Msg($"[FloodTrap] FLOOD! Dropped {placed} Portable Lakes on the map.");
        }
    }
}
