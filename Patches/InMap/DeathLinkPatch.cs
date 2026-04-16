using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using BTD_Mod_Helper;
using HarmonyLib;
using Il2CppAssets.Scripts.Unity.UI_New.GameOver;
using System;

namespace BloonsArchipelago.Patches.InMap
{
    [HarmonyPatch(typeof(DefeatScreen), nameof(DefeatScreen.Open))]
    internal class DeathLinkOnDefeatPatch
    {
        private static readonly Random _rng = new();

        private static string GetDeathMessage(string player)
        {
            string[] messages =
            {
                $"{player} was mauled by a MOAB.",
                $"{player} couldn't hold the line.",
                $"{player} was overrun by Bloons.",
                $"{player} let too many Bloons through.",
                $"{player} was popped.",
                $"{player} forgot about camos."
            };
            return messages[_rng.Next(messages.Length)];
        }

        [HarmonyPostfix]
        private static void Postfix(DefeatScreen __instance)
        {
            var sh = BloonsArchipelago.sessionHandler;
            if (sh == null || !sh.ready) return;

            if (sh._receivingRemoteDeath)
            {
                sh._receivingRemoteDeath = false;
                return;
            }

            if (!sh.deathLinkEnabled || sh.deathLinkService == null) return;

            try
            {
                string cause = GetDeathMessage(sh.PlayerSlotName());
                sh.deathLinkService.SendDeathLink(new DeathLink(sh.PlayerSlotName(), cause));
                ModHelper.Msg<BloonsArchipelago>("[DeathLink] Sent death: " + cause);
            }
            catch (Exception ex)
            {
                MelonLoader.MelonLogger.Warning("[BloonsArchipelago] DeathLink send failed: " + ex.Message);
            }
        }
    }
}
