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
                PowerModel stormModel = null;
                var powers = Game.instance.model.powers;
                if (powers != null)
                {
                    foreach (var p in powers)
                    {
                        if (p == null) continue;
                        if (p.name != null && p.name.Contains("SuperMonkeyStorm"))
                        {
                            stormModel = p;
                            break;
                        }
                    }
                }

                if (stormModel == null)
                {
                    MelonLogger.Warning("[MonkeyStorm] SuperMonkeyStorm power model not found in game.model.powers");
                    return;
                }

                inGame.bridge.ActivatePower(new Vector2(0f, 0f), stormModel);
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
        }
    }
}
