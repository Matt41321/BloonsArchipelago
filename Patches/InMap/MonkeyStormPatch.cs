using Il2CppAssets.Scripts.Models.Powers;
using Il2CppAssets.Scripts.Unity;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using MelonLoader;
using System;
using UnityEngine;

namespace BloonsArchipelago.Patches.InMap
{
    internal static class MonkeyStormManager
    {
        public static volatile int PendingStormCount = 0;

        private static PowerModel _cachedModel = null;

        public static void Update()
        {
            while (PendingStormCount > 0)
            {
                PendingStormCount--;
                ActivateStorm();
            }
        }

        private static void ActivateStorm()
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
                            if (p?.name != null && p.name.Contains("SuperMonkeyStorm"))
                            {
                                _cachedModel = p;
                                break;
                            }
                        }
                    }
                }

                if (_cachedModel == null)
                {
                    MelonLogger.Warning("[MonkeyStorm] SuperMonkeyStorm power model not found in game.model.powers");
                    return;
                }

                inGame.bridge.ActivatePower(new Vector2(0f, 0f), _cachedModel);
                MelonLogger.Msg("[MonkeyStorm] Activated SuperMonkeyStorm via bridge.ActivatePower");
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[MonkeyStorm] ActivateStorm error: {ex.Message}");
            }
        }

        public static void CleanupAll()
        {
            PendingStormCount = 0;
            _cachedModel = null;
        }
    }
}
