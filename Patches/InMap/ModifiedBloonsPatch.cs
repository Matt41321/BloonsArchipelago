using BTD_Mod_Helper;
using HarmonyLib;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using MelonLoader;
using System;
using System.Collections.Generic;

namespace BloonsArchipelago.Patches.InMap
{
    [HarmonyPatch(typeof(InGame), nameof(InGame.RoundEnd))]
    internal class ModifiedBloonsPatch
    {
        private static readonly string[] MoabClassBloons = { "Moab", "Bfb", "Zomg", "Bad", "Ddt" };
        private static readonly string[] FortifiableBloons = { "Moab", "Bfb", "Zomg", "Bad", "Ddt", "Ceramic", "Lead" };

        private static HashSet<string> _validBloonIds = null;

        private static void BuildValidBloonIds()
        {
            _validBloonIds = new HashSet<string>();
            var gameModel = BloonsArchipelago.currentGameModel;
            if (gameModel?.bloons == null) return;
            foreach (var bloon in gameModel.bloons)
            {
                if (bloon?.name != null)
                    _validBloonIds.Add(bloon.name);
            }
        }

        private static string RandomizeBloon(string bloon, Random rng)
        {
            bool hasCamo = bloon.Contains("Camo");
            bool hasRegrow = bloon.Contains("Regrow");
            bool hasFortified = bloon.Contains("Fortified");

            string baseName = bloon
                .Replace("Regrow", "")
                .Replace("Fortified", "")
                .Replace("Camo", "");

            bool isMoabClass = Array.Exists(MoabClassBloons, b => baseName.Equals(b, StringComparison.OrdinalIgnoreCase));
            bool canFortify = Array.Exists(FortifiableBloons, b => baseName.Equals(b, StringComparison.OrdinalIgnoreCase));

            if (rng.Next(2) == 0)
                hasCamo = true;

            if (rng.Next(2) == 0 && !isMoabClass)
                hasRegrow = true;

            if (rng.Next(2) == 0 && canFortify)
                hasFortified = true;

            string candidate = baseName
                + (hasRegrow ? "Regrow" : "")
                + (hasFortified ? "Fortified" : "")
                + (hasCamo ? "Camo" : "");

            if (_validBloonIds != null && _validBloonIds.Contains(candidate))
                return candidate;

            return bloon;
        }

        [HarmonyPostfix]
        private static void Postfix(int completedRound)
        {
            var sh = BloonsArchipelago.sessionHandler;
            if (sh == null || !sh.ready) return;
            if (sh.ModifiedBloonsRoundsRemaining <= 0) return;

            var inGame = InGame.instance;
            if (inGame == null) return;

            var gameModel = BloonsArchipelago.currentGameModel;
            if (gameModel?.roundSet?.rounds == null) return;

            try
            {
                BuildValidBloonIds();

                int nextRound = completedRound + 1;
                if (nextRound < 0 || nextRound >= gameModel.roundSet.rounds.Length) return;

                var round = gameModel.roundSet.rounds[nextRound];
                if (round?.groups == null) return;

                var rng = new Random();
                var newGroups = new List<Il2CppAssets.Scripts.Models.Rounds.BloonGroupModel>();

                foreach (var group in round.groups)
                {
                    if (group == null || string.IsNullOrEmpty(group.bloon)) continue;

                    int count = group.count;
                    if (count <= 1)
                    {
                        group.bloon = RandomizeBloon(group.bloon, rng);
                        newGroups.Add(group);
                        continue;
                    }

                    // split group so each bloon can be randomized independently
                    float start = group.start;
                    float end = group.end;
                    float interval = count > 1 ? (end - start) / (count - 1) : 0;

                    for (int i = 0; i < count; i++)
                    {
                        float t = start + interval * i;
                        string newBloon = RandomizeBloon(group.bloon, rng);
                        var single = new Il2CppAssets.Scripts.Models.Rounds.BloonGroupModel(newBloon, newBloon, t, t, 1);
                        newGroups.Add(single);
                    }
                }

                round.groups = newGroups.ToArray();

                sh.ModifiedBloonsRoundsRemaining--;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[BloonsArchipelago] Error applying Modified Bloons trap: {ex.Message}");
            }
        }
    }
}
