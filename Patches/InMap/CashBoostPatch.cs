using Il2CppAssets.Scripts.Models.Powers;
using Il2CppAssets.Scripts.Unity;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using MelonLoader;
using System;
using UnityEngine;

namespace BloonsArchipelago.Patches.InMap
{
    internal static class CashDropManager
    {
        public static volatile int PendingCashDropCount = 0;

        private static PowerModel _cachedModel = null;

        public static void Update()
        {
            while (PendingCashDropCount > 0)
            {
                PendingCashDropCount--;
                ActivateCashDrop();
            }
        }

        private static void ActivateCashDrop()
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
                            if (p?.name != null && p.name.Contains("CashDrop"))
                            {
                                _cachedModel = p;
                                break;
                            }
                        }
                    }
                }

                if (_cachedModel == null)
                {
                    MelonLogger.Warning("[CashDrop] CashDrop power model not found in game.model.powers");
                    return;
                }

                inGame.bridge.ActivatePower(new Vector2(0f, 0f), _cachedModel);
                MelonLogger.Msg("[CashDrop] Activated CashDrop via bridge.ActivatePower");
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[CashDrop] ActivateCashDrop error: {ex.Message}");
            }
        }

        public static void CleanupAll()
        {
            PendingCashDropCount = 0;
            _cachedModel = null;
        }
    }
}
