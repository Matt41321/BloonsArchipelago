using MelonLoader;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BloonsArchipelago.Patches.InMap
{
    internal static class NumberSequenceTrapManager
    {
        public static volatile int PendingNumberSequenceCount = 0;

        private const int   SET_SIZE      = 5;
        private const float MEMORIZE_TIME = 5f;
        private const float ANSWER_TIME   = 10f;

        private static readonly QuizTrapRunner _runner = new("NumberSeq");
        private static readonly System.Random _rng = new();

        public static void Update()
        {
            if (_runner.Idle && PendingNumberSequenceCount > 0)
            {
                PendingNumberSequenceCount--;
                try { _runner.Begin(NextQuestion()); }
                catch (Exception ex) { MelonLogger.Warning($"[NumberSeq] Start error: {ex}"); }
            }
            _runner.Update();
        }

        public static void CleanupAll()
        {
            PendingNumberSequenceCount = 0;
            _runner.Cleanup();
        }

        private static QuizQuestion NextQuestion()
        {
            // The set
            var set = new int[SET_SIZE];
            var used = new HashSet<int>();
            for (int i = 0; i < SET_SIZE; i++)
            {
                int n;
                do { n = _rng.Next(10, 100); } while (!used.Add(n));
                set[i] = n;
            }

            string correct = FormatSet(set);

            // Wrong options
            var options = new List<string> { correct };
            var seen = new HashSet<string> { correct };
            int attempts = 0;
            while (options.Count < 4 && attempts < 300)
            {
                attempts++;
                string variant = _rng.Next(2) == 0 ? MakeChangedVariant(set, used) : MakeSwappedVariant(set);
                if (variant != null && seen.Add(variant))
                    options.Add(variant);
            }

            while (options.Count < 4)
            {
                var filler = new int[SET_SIZE];
                for (int i = 0; i < SET_SIZE; i++) filler[i] = _rng.Next(10, 100);
                string s = FormatSet(filler);
                if (seen.Add(s)) options.Add(s);
            }

            for (int i = options.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                (options[i], options[j]) = (options[j], options[i]);
            }

            return new QuizQuestion
            {
                Header = "NUMBER SEQUENCE TRAP!",
                HeaderColor = new Color(0.55f, 1.00f, 0.40f),
                Instruction = "Memorize the numbers!",
                MemorizePrompt = correct,
                MemorizeTime = MEMORIZE_TIME,
                Prompt = "Which set of numbers did you just see?",
                Options = options.ToArray(),
                CorrectIndex = options.IndexOf(correct),
                CorrectAnswerText = correct,
                TimeLimit = ANSWER_TIME,
            };
        }

        private static string FormatSet(int[] set)
            => string.Join("   ", set);
        private static string MakeChangedVariant(int[] set, HashSet<int> used)
        {
            var copy = (int[])set.Clone();
            int idx = _rng.Next(SET_SIZE);
            int original = copy[idx];

            for (int tries = 0; tries < 30; tries++)
            {
                int changed = (_rng.Next(3)) switch
                {
                    0 => (original / 10) * 10 + _rng.Next(10),
                    1 => _rng.Next(1, 10) * 10 + original % 10,
                    _ => (original % 10) * 10 + original / 10,
                };
                if (changed != original && changed >= 10 && !used.Contains(changed))
                {
                    copy[idx] = changed;
                    return FormatSet(copy);
                }
            }
            return null;
        }
        private static string MakeSwappedVariant(int[] set)
        {
            var copy = (int[])set.Clone();
            int i = _rng.Next(SET_SIZE);
            int j;
            do { j = _rng.Next(SET_SIZE); } while (j == i);
            (copy[i], copy[j]) = (copy[j], copy[i]);
            return FormatSet(copy);
        }
    }
}
