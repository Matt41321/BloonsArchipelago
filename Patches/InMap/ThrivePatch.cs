using Il2CppAssets.Scripts.Models.Powers;
using Il2CppAssets.Scripts.Unity;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using MelonLoader;
using System;
using UnityEngine;

namespace BloonsArchipelago.Patches.InMap
{
    internal static class ThriveManager
    {
        public static volatile int PendingThriveCount = 0;

        private static PowerModel _cachedModel = null;

        public static void Update()
        {
            while (PendingThriveCount > 0)
            {
                PendingThriveCount--;
                ActivateThrive();
            }
        }

        private static void ActivateThrive()
        {
            var inGame = InGame.instance;
            if (inGame == null) return;

            try
            {
                if (_cachedModel == null)
                {
                    var powers = Game.instance.model.powers;
                    if (powers != null)
                    {
                        foreach (var p in powers)
                        {
                            if (p?.name != null && p.name.Contains("Thrive"))
                            {
                                _cachedModel = p;
                                break;
                            }
                        }
                    }
                }

                if (_cachedModel == null)
                {
                    MelonLogger.Warning("[Thrive] Thrive power model not found in game.model.powers");
                    return;
                }

                inGame.bridge.ActivatePower(new Vector2(0f, 0f), _cachedModel);
                MelonLogger.Msg("[Thrive] Activated Thrive via bridge.ActivatePower");
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[Thrive] ActivateThrive error: {ex.Message}");
            }
        }

        public static void CleanupAll()
        {
            PendingThriveCount = 0;
            _cachedModel = null;
        }
    }
}
