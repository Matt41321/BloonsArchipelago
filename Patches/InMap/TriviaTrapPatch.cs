using MelonLoader;
using System;
using UnityEngine;

namespace BloonsArchipelago.Patches.InMap
{
    internal static class TriviaTrapManager
    {
        public static volatile int PendingTriviaCount = 0;

        private static readonly QuizTrapRunner _runner = new("TriviaTrap");
        private static readonly System.Random _rng = new();

        // (question, correct answer, three wrong answers)
        private static readonly (string Q, string A, string[] Wrong)[] _questions =
        {
            ("What was the first boss to be added?", "Bloonarius", new[] { "Lych", "Dreadbloon", "Vortex" }),
            ("Which of these towers cannot pop lead bloons?", "Glaive Lord", new[] { "Sentry Expert", "Velociraptor", "Crossbow Master" }),
            ("Which of these Heroes cannot see camo?", "Rosalia", new[] { "Silas", "Sauda", "Ezili" }),
            ("Which of these is not a real upgrade?", "The One Above All", new[] { "Avatar Of Wrath", "Legend of the Night", "Archmage" }),
            ("What round does the first lead bloon appear?", "28", new[] { "29", "24", "21" }),
            ("How many monkeys are there excluding Heroes?", "25", new[] { "23", "24", "26" }),
            ("How much HP does a MOAB have?", "200", new[] { "400", "150", "100" }),
            ("Which Paragon is the most expensive?", "Goliath Doomship", new[] { "Magus Perfectus", "Ascended Shadow", "Ballistic Obliteration Missile Bunker" }),
            ("Which of these is the real name of the Bomb Shooter Paragon?", "Ballistic Obliteration Missile Bunker", new[] { "Ballistic Obliteration Monkey Bomber", "Bombastic Obliterated Mega Bomb", "Ballistic Obliterating Missile Bomber" }),
            ("How many projectiles does a 0-0-0 tack shooter shoot?", "8", new[] { "6", "4", "12" }),
            ("How many lives do you lose to a rainbow bloon?", "47", new[] { "52", "38", "67" }),
            ("What was the old name for the Monarch of Storms?", "Superstorm", new[] { "Storm Master", "God of Storms", "Bringer of Storms" }),
            ("What year did BTD6 release?", "2018", new[] { "2020", "2009", "2015" }),
            ("Which of these is not an expert map?", "X-Factor", new[] { "Tricky Tracks", "Infernal", "Quad" }),
            ("Which Monkey Knowledge Tree has the most nodes?", "Primary", new[] { "Military", "Magic", "Heroes" }),
            ("Which of these Heroes cost the most to place?", "Captain Churchill", new[] { "Corvus", "Benjamin", "Adora" }),
            ("Which of these is not a real Hero skin?", "Doctor Jones", new[] { "Joan of Arc Adora", "Viking Sauda", "Psimbals" }),
            ("Which of these bosses is the fastest?", "Vortex", new[] { "Phayze", "Lych", "Bloonarius" }),
            ("Which of these maps has a secret variant?", "Skull Tweak", new[] { "Town Center", "The Cabin", "Candy Falls" }),
            ("Which of these towers was introduced in BTD6?", "Alchemist", new[] { "Engineer", "Sniper", "Spike Factory" }),
            ("Which tower has the upgrade MOAB Shove", "Heli Pilot", new[] { "Boomerang", "Sniper", "Bomb Shooter" }),
            ("Which developer made BTD6?", "Ninja Kiwi", new[] { "PopCap", "Blizzard", "Valve" }),
        };

        public static void Update()
        {
            if (_runner.Idle && PendingTriviaCount > 0)
            {
                PendingTriviaCount--;
                try { _runner.Begin(NextQuestion()); }
                catch (Exception ex) { MelonLogger.Warning($"[TriviaTrap] Start error: {ex}"); }
            }
            _runner.Update();
        }

        public static void CleanupAll()
        {
            PendingTriviaCount = 0;
            _runner.Cleanup();
        }

        private static QuizQuestion NextQuestion()
        {
            var (q, a, wrong) = _questions[_rng.Next(_questions.Length)];

            var options = new string[4];
            int correctIndex = _rng.Next(4);
            int w = 0;
            for (int i = 0; i < 4; i++)
                options[i] = i == correctIndex ? a : wrong[w++];

            return new QuizQuestion
            {
                Header = "TRIVIA TRAP!",
                HeaderColor = new Color(0.30f, 0.75f, 1.00f),
                Prompt = q,
                Options = options,
                CorrectIndex = correctIndex,
                CorrectAnswerText = a,
                TimeLimit = 12f,
            };
        }
    }
}
