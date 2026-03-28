using BTD_Mod_Helper.Extensions;
using HarmonyLib;
using Il2CppAssets.Scripts.Data.Boss;
using Il2CppAssets.Scripts.Models.Profile;
using Il2CppAssets.Scripts.Models.ServerEvents;
using Il2CppAssets.Scripts.Unity;
using Il2CppAssets.Scripts.Unity.Menu;
using Il2CppAssets.Scripts.Unity.Player;
using Il2CppAssets.Scripts.Unity.UI_New.GameOver;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using Il2CppAssets.Scripts.Unity.UI_New.Main.DifficultySelect;
using Il2CppAssets.Scripts.Unity.UI_New.Main.ModeSelect;
using Il2CppSystem.Collections.Generic;
using MelonLoader;
using System;
using System.Collections;
using System.Reflection;

namespace BloonsArchipelago.Patches.InMap
{
    internal static class VictoryMapBossManager
    {
        public const string EventId = "BloonsArchipelagoBoss";

        private static readonly BossType[] BossTypes = (BossType[])Enum.GetValues(typeof(BossType));

        // deterministic from seed so the same session always picks the same boss
        public static BossType GetBossForSession(string sessionSeed)
        {
            var rng = new Random(StableHash(sessionSeed));
            return BossTypes[rng.Next(BossTypes.Length)];
        }

        private static int StableHash(string s)
        {
            unchecked
            {
                int hash = 17;
                foreach (char c in s)
                    hash = hash * 31 + c;
                return hash;
            }
        }

        public static bool IsActive =>
            InGameData.CurrentGame != null && InGameData.CurrentGame.gameEventId == EventId;

        // set when player clicks victory map; OnUpdate uses it to auto-select Standard in ModeScreen
        public static bool AutoStartPending = false;

        // set when our boss game launches; cleared on victory or back-out
        public static bool BossGameStarted = false;

        // true only when a boss tier was actually killed; false if player ran out of lives
        public static bool BossActuallyKilled = false;

    }

    [HarmonyPatch(typeof(ModeScreen), nameof(ModeScreen.OnModeSelected))]
    internal static class VictoryMapModeSelectedPatch
    {
        [HarmonyPrefix]
        private static void Prefix(string modeType)
        {
            var sh = BloonsArchipelago.sessionHandler;
            if (sh == null || !sh.ready || !sh.BossGoal || string.IsNullOrEmpty(sh.VictoryMap)) return;

            var inGameData = InGameData.Editable;
            if (inGameData.selectedMap != sh.VictoryMap) return;

            var bossType = VictoryMapBossManager.GetBossForSession(sh.APID);
            bool isElite = sh.GoalType == 2;
            MelonLogger.Msg($"[VictoryMapBoss] Starting boss event: {bossType} (elite={isElite}) on {inGameData.selectedMap} (seed: {sh.APID})");

            var spawnRounds = BossGameData.DefaultSpawnRounds;
            MelonLogger.Msg($"[VictoryMapBoss] DefaultSpawnRounds={spawnRounds?.Count.ToString() ?? "NULL"}");

            VictoryMapBossManager.BossGameStarted = true;
            VictoryMapBossManager.BossActuallyKilled = false;
            VictoryMapBossStartingCashPatch.Reset();

            var challengeModel = new DailyChallengeModel
            {
                difficulty = "Medium",
                map = inGameData.selectedMap,
                mode = "Standard",
                towers = new Il2CppAssets.Scripts.Models.ServerEvents.TowerData[]
                {
                    new() { isHero = true, tower = DailyChallengeModel.CHOSENPRIMARYHERO, max = 1 }
                }.ToIl2CppList()
            };

            inGameData.SetupBoss(VictoryMapBossManager.EventId, bossType, isElite, false,
                spawnRounds, challengeModel, LeaderboardScoringType.GameTime);
        }
    }

    [HarmonyPatch]
    internal static class VictoryMapSkuSettingsPatch
    {
        private static System.Collections.Generic.IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(SkuSettings), nameof(SkuSettings.GetEvent),
                new[] { typeof(string), typeof(bool) }, new[] { typeof(BossEvent) });
        }

        [HarmonyPrefix]
        private static bool Prefix(ref object __result)
        {
            if (!VictoryMapBossManager.IsActive) return true;

            var bossData = InGameData.CurrentGame.bossData;
            __result = new BossEvent
            {
                id = VictoryMapBossManager.EventId,
                eventData = new BossDataModel
                {
                    bossType = bossData.bossBloon
                }
            };
            return false;
        }
    }

    // game sometimes shows VictoryScreen instead of BossDefeatScreen; handle that path here
    [HarmonyPatch(typeof(InGame), nameof(InGame.OnVictory))]
    internal static class VictoryMapBossOnVictoryPatch
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            if (!VictoryMapBossManager.IsActive) return;
            if (!VictoryMapBossManager.BossActuallyKilled) return;

            var sh = BloonsArchipelago.sessionHandler;
            if (sh == null || !sh.ready) return;

            VictoryMapBossManager.BossGameStarted = false;
            VictoryMapBossManager.BossActuallyKilled = false;
            MelonLogger.Msg("[VictoryMapBoss] Boss defeated (victory screen path) — completing rando!");
            sh.CompleteRando();
        }
    }

    // save progress to map, not boss event leaderboard
    [HarmonyPatch(typeof(InGame), nameof(InGame.MapDataSaveId), MethodType.Getter)]
    internal static class VictoryMapBossMapDataSaveIdPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(ref string __result)
        {
            if (!VictoryMapBossManager.IsActive) return true;
            __result = InGameData.CurrentGame.selectedMap;
            return false;
        }
    }

    [HarmonyPatch(typeof(Il2CppAssets.Scripts.Simulation.Track.BossBloonManager), nameof(Il2CppAssets.Scripts.Simulation.Track.BossBloonManager.BloonDestroyed))]
    internal static class VictoryMapBossDefeatedFlagPatch
    {
        [HarmonyPrefix]
        private static void Prefix()
        {
            if (VictoryMapBossManager.IsActive && InGame.instance?.bridge?.GetCurrentRound() >= 119)
            {
                VictoryMapBossManager.BossActuallyKilled = true;
                MelonLogger.Msg("[VictoryMapBoss] Final boss tier killed — will trigger victory after BossDefeated.");
            }
        }
    }

    // boss events never fire InGame.OnVictory; they go to BossDefeatScreen directly
    [HarmonyPatch(typeof(BossDefeatScreen), nameof(BossDefeatScreen.Open))]
    internal static class VictoryMapBossDefeatScreenPatch
    {
        [HarmonyPrefix]
        private static void Prefix(BossDefeatScreen __instance, ref BossEventData __state)
        {
            if (!VictoryMapBossManager.IsActive) return;

            var sh = BloonsArchipelago.sessionHandler;
            if (sh != null && sh.ready && VictoryMapBossManager.BossActuallyKilled)
            {
                VictoryMapBossManager.BossGameStarted = false;
                VictoryMapBossManager.BossActuallyKilled = false;
                MelonLogger.Msg("[VictoryMapBoss] Boss defeated — completing rando!");
                sh.CompleteRando();
            }
            else
            {
                VictoryMapBossManager.BossActuallyKilled = false;
                MelonLogger.Msg("[VictoryMapBoss] BossDefeatScreen opened but player was defeated — not completing rando.");
            }

            __state = Game.Player.Data._CurrentBossEventData_k__BackingField;
            Game.Player.Data._CurrentBossEventData_k__BackingField = new BossEventData
            {
                elite = new BossModeData { highestCompletedRound = 69, hasCompleted = true, seenCompletion = true, tierBeaten = 5, newBestRound = false },
                normal = new BossModeData { highestCompletedRound = 69, hasCompleted = true, seenCompletion = true, tierBeaten = 5, newBestRound = false },
                eventId = VictoryMapBossManager.EventId,
                leaderboardStandings = new List<BossLeaderboardStanding>(),
                hasClaimedRewards = true
            };

            var lastRound = InGame.instance.GetLastRoundMapSaveData();
            if (lastRound != null) { __instance.retryMapSave = lastRound; __instance.canRetry = true; }
        }

        [HarmonyPostfix]
        private static void Postfix(BossDefeatScreen __instance, ref BossEventData __state)
        {
            if (!VictoryMapBossManager.IsActive) return;
            Game.Player.Data._CurrentBossEventData_k__BackingField = __state;
            __instance.bestRoundTxt.SetText("n/a");
        }
    }

    // block InGame.BossDefeated — it NullRefs on our fake event ID; trigger OnVictory manually instead
    [HarmonyPatch]
    internal static class VictoryMapBossInGameBossDefeatedPatch
    {
        private static System.Collections.Generic.IEnumerable<MethodBase> TargetMethods()
        {
            MethodInfo found = null;
            foreach (var mi in typeof(InGame).GetMethods(
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            {
                if (mi.Name == "BossDefeated") { found = mi; break; }
            }
            if (found != null)
                yield return found;
            else
                MelonLogger.Warning("[VictoryMapBoss] Could not find InGame.BossDefeated — crash prevention inactive.");
        }

        [HarmonyPrefix]
        private static bool Prefix()
        {
            if (!VictoryMapBossManager.IsActive) return true;

            if (VictoryMapBossManager.BossActuallyKilled)
            {
                MelonLogger.Msg("[VictoryMapBoss] Blocking InGame.BossDefeated (final tier) — triggering OnVictory next frame.");
                MelonCoroutines.Start(TriggerVictoryNextFrame());
            }
            else
            {
                MelonLogger.Msg("[VictoryMapBoss] Blocking InGame.BossDefeated (non-final tier) — no victory yet.");
            }
            return false;
        }

        private static IEnumerator TriggerVictoryNextFrame()
        {
            yield return null;
            if (InGame.instance != null)
            {
                MelonLogger.Msg("[VictoryMapBoss] Calling InGame.OnVictory after final boss tier.");
                InGame.instance.OnVictory();
            }
        }
    }

    // top up starting cash; called from OnRoundStart since InGame.RoundStart doesn't fire in boss mode
    internal static class VictoryMapBossStartingCashPatch
    {
        private static bool _cashGiven = false;

        public static void TryGiveCash()
        {
            if (_cashGiven) return;
            var inGame = InGame.instance;
            if (inGame?.bridge == null) return;
            _cashGiven = true;
            try
            {
                var sh = BloonsArchipelago.sessionHandler;
                double bonus = (sh?.GoalType == 2 ? 5000.0 : 1500.0) - 850.0;
                MelonLogger.Msg($"[VictoryMapBoss] Calling AddCash({bonus}, 0) GoalType={sh?.GoalType}");
                inGame.bridge.AddCash(bonus, 0);
                MelonLogger.Msg($"[VictoryMapBoss] AddCash completed successfully");
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[VictoryMapBoss] StartingCash error: {ex.Message}");
            }
        }

        public static void Reset() => _cashGiven = false;
    }

    [HarmonyPatch(typeof(BossDefeatScreen), nameof(BossDefeatScreen.CanPlayAgain), MethodType.Getter)]
    internal static class VictoryMapBossCanPlayAgainPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(ref bool __result)
        {
            if (!VictoryMapBossManager.IsActive) return true;
            __result = true;
            return false;
        }
    }

    // suppress player stat methods that crash on our fake event ID
    [HarmonyPatch]
    internal static class VictoryMapBossPreventMethodsPatch
    {
        private static System.Collections.Generic.IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(Btd6Player), nameof(Btd6Player.BossEventBestRound));
            yield return AccessTools.Method(typeof(Btd6Player), nameof(Btd6Player.CompleteBossEvent));
            yield return AccessTools.Method(typeof(Btd6Player), nameof(Btd6Player.HasDefeatedBossTier));
            yield return AccessTools.Method(typeof(InGame), nameof(InGame.RetrieveTopScoreAndPostAnalytics));
        }

        [HarmonyPrefix]
        private static bool Prefix() => !VictoryMapBossManager.IsActive;
    }

    // block ModeScreen on back-out so no intermediate screen is shown
    [HarmonyPatch(typeof(ModeScreen), "Open")]
    internal static class VictoryMapBossModeScreenBackOutPatch
    {
        [HarmonyPrefix]
        private static bool Prefix()
        {
            if (!VictoryMapBossManager.BossGameStarted || VictoryMapBossManager.AutoStartPending) return true;

            VictoryMapBossManager.BossGameStarted = false;
            MelonLogger.Msg("[VictoryMapBoss] ModeScreen back-out detected — going straight to main menu.");
            MenuManager.instance?.GoToMainMenu();
            return false;
        }
    }

    // prefix required — must block before ContinueGamePanel.Initialise() looks up our fake event ID
    [HarmonyPatch(typeof(DifficultySelectScreen), nameof(DifficultySelectScreen.Open))]
    internal static class VictoryMapBossDifficultyNavigatePatch
    {
        [HarmonyPrefix]
        private static bool Prefix(DifficultySelectScreen __instance)
        {
            // back-out: gameEventId on InGameData.Editable is already null by now, which is what
            // causes ContinueGamePanel.Initialise to crash — block and go home instead
            if (VictoryMapBossManager.BossGameStarted && !VictoryMapBossManager.AutoStartPending)
            {
                VictoryMapBossManager.BossGameStarted = false;
                MelonLogger.Msg("[VictoryMapBoss] Back-out detected — going to main menu.");
                MenuManager.instance?.GoToMainMenu();
                return false;
            }

            var sh = BloonsArchipelago.sessionHandler;
            if (sh == null || !sh.BossGoal || !sh.ready || string.IsNullOrEmpty(sh.VictoryMap)) return true;
            if (InGameData.Editable?.selectedMap != sh.VictoryMap) return true;

            MelonLogger.Msg("[VictoryMapBoss] Victory map selected — auto-navigating to game.");
            VictoryMapBossManager.AutoStartPending = true;
            AccessTools.Method(typeof(DifficultySelectScreen), "OpenModeSelectUi")
                ?.Invoke(__instance, new object[] { "Medium" });
            return false;
        }
    }

}
