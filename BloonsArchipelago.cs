using MelonLoader;
using BTD_Mod_Helper;

using BloonsArchipelago;
using BloonsArchipelago.Utils;

using BTD_Mod_Helper.Api.ModOptions;
using BTD_Mod_Helper.Api;
using BTD_Mod_Helper.Extensions;

using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using System.Collections;

using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using Il2CppAssets.Scripts.Unity;
using Il2CppAssets.Scripts.Unity.Menu;
using Il2CppAssets.Scripts.Unity.UI_New.Main.ModeSelect;
using Il2CppAssets.Scripts.Models;
using Il2CppAssets.Scripts.Simulation.Towers;
using UnityEngine;

[assembly: MelonInfo(typeof(BloonsArchipelago.BloonsArchipelago), ModHelperData.Name, ModHelperData.Version, ModHelperData.RepoOwner)]
[assembly: MelonGame("Ninja Kiwi", "BloonsTD6")]

namespace BloonsArchipelago;

public class BloonsArchipelago : BloonsTD6Mod
{
    public static NotificationJSON notifJson = new() { APWorlds = new Dictionary<string, string[]>() };

    public static SessionHandler sessionHandler = new();

    public static List<GameObject> vMapIndicators = new();
    public static GameModel currentGameModel = null;

    private static bool hasSnappedThisSession = false;
    private static float lastNotificationTime = 0f;
    private static Queue<APNotification> pendingNotifications = new Queue<APNotification>();

    static readonly ModSettingString url = "archipelago.gg";
    static readonly ModSettingInt port = 25565;
    static readonly ModSettingString slot = "Player";
    static readonly ModSettingString password = "";
    static readonly ModSettingButton archipelagoConnect = new(() =>
    {
        ModHelper.Msg<BloonsArchipelago>("Connecting...");

        sessionHandler = new SessionHandler(url, port, slot, password);
    });
    static readonly ModSettingButton archipelagoDisconnect = new(() =>
    {
        if (!sessionHandler.ready)
        {
            ModHelper.Msg<BloonsArchipelago>("Not connected.");
            return;
        }
        sessionHandler.Disconnect();
        sessionHandler = new SessionHandler();
        ModHelper.Msg<BloonsArchipelago>("Disconnected.");
    });
    static readonly ModSettingButton refreshData = new(() =>
    {
        if (!sessionHandler.ready)
        {
            ModHelper.Msg<BloonsArchipelago>("Not connected — connect first.");
            return;
        }
        sessionHandler.RefreshAllData();
        ModHelper.Msg<BloonsArchipelago>("Data refreshed!");
    });
    static readonly ModSettingBool showNotifications = true;
    static readonly ModSettingBool deathLink = new(false)
    {
        displayName = "DeathLink",
        description = "Share deaths with all DeathLink-enabled players in the MultiWorld.",
        onValueChanged = new System.Action<bool>(v => sessionHandler?.ApplyDeathLinkToggle(v)),
    };
    public static bool DeathLinkSetting => deathLink;

    public override void OnApplicationStart()
    {
        string modPath = ModContent.GetInstance<BloonsArchipelago>().GetModDirectory();

        if (!Directory.Exists(modPath))
        {
            Directory.CreateDirectory(modPath);
        }

        string filepath = Path.Combine(modPath, "Notifications.json");
        if (File.Exists(filepath))
        {
            var JSONString = File.ReadAllText(filepath);
            var NotifObject = JsonSerializer.Deserialize<NotificationJSON>(JSONString);

            notifJson = NotifObject;
        }
        else
        {
            notifJson = new NotificationJSON { APWorlds = new Dictionary<string, string[]>() };
        }
        ModHelper.Msg<BloonsArchipelago>("BloonsArchipelago loaded!");
    }

    public override void OnRoundStart()
    {
        if (hasSnappedThisSession) return;
        hasSnappedThisSession = true;

        try
        {
            var sh = sessionHandler;
            if (sh == null || !sh.ready || !sh.PopTierChecksEnabled) return;

            var inGame = InGame.instance;
            if (inGame == null) return;

            sh.SessionEndLivePops.Clear();
            foreach (var tower in inGame.GetTowers())
            {
                try
                {
                    string baseId = tower?.towerModel?.baseId;
                    if (string.IsNullOrEmpty(baseId) || baseId == "MonkeyVillage") continue;
                    bool isBanana = baseId == "BananaFarm";
                    long pops = isBanana ? (long)tower.cashEarned : tower.damageDealt;
                    if (!sh.SessionEndLivePops.ContainsKey(baseId))
                        sh.SessionEndLivePops[baseId] = 0;
                    sh.SessionEndLivePops[baseId] += pops;
                }
                catch { }
            }
        }
        catch { }
    }

    public override void OnTowerSold(Tower tower, float cashGained)
    {
        try
        {
            var sh = sessionHandler;
            if (sh == null || !sh.ready || !sh.PopTierChecksEnabled) return;
            string baseId = tower?.towerModel?.baseId;
            if (string.IsNullOrEmpty(baseId) || baseId == "MonkeyVillage") return;

            bool isBanana = baseId == "BananaFarm";
            long soldPops = isBanana ? (long)tower.cashEarned : tower.damageDealt;

            long liveRemaining = 0;
            bool soldFound = false;
            var inGame = InGame.instance;
            if (inGame != null)
            {
                foreach (var t in inGame.GetTowers())
                {
                    try
                    {
                        if (t?.towerModel?.baseId != baseId) continue;
                        if (!soldFound && t.Pointer == tower.Pointer) { soldFound = true; continue; }
                        liveRemaining += isBanana ? (long)t.cashEarned : t.damageDealt;
                    }
                    catch { }
                }
            }

            sh.CumulativePops.TryGetValue(baseId, out long prevCum);
            sh.SessionEndLivePops.TryGetValue(baseId, out long prevEnd);
            long liveTotalIncludingSold = liveRemaining + soldPops;
            long aggregate = prevCum + System.Math.Max(0, liveTotalIncludingSold - prevEnd);
            sh.CumulativePops[baseId] = aggregate;
            sh.SessionEndLivePops[baseId] = liveRemaining;
            sh.SaveProgress();
        }
        catch { }
    }

    private static float GetDifficultyMultiplier(string difficulty, string mode)
    {
        switch (mode)
        {
            // Easy tier (0.85x)
            case "Easy":
            case "PrimaryOnly":
            case "Deflation":
                return 0.85f;
            // Medium tier (1.0x)
            case "Medium":
            case "Standard":
            case "MilitaryOnly":
            case "Apopalypse":
            case "Reverse":
                return 1.00f;
            // Hard tier (1.08x)
            case "Hard":
            case "MagicOnly":
            case "DoubleMoabHealth":
            case "HalfCash":
            case "AlternateBloonsRounds":
                return 1.08f;
            // CHIMPS (1.08x — Hard pricing)
            case "Clicks":
                return 1.08f;
            // Impoppable tier (1.20x)
            case "Impoppable":
                return 1.20f;
        }

        // Fall back to difficulty name
        return difficulty switch
        {
            "Easy" => 0.85f,
            "Hard" => 1.08f,
            "Impoppable" => 1.20f,
            _ => 1.00f,
        };
    }

    private static float DetectCurrentMultiplier(GameModel gameModel, string mode)
    {

        if (mode != "Standard")
            return GetDifficultyMultiplier("", mode);


        foreach (var tower in gameModel.towers)
        {
            if (tower.name == "DartMonkey")
            {
                float cost = tower.cost;
                if (cost <= 175) return 0.85f;
                if (cost <= 210) return 1.00f;
                if (cost <= 230) return 1.08f;
                return 1.20f;
            }
        }

        return 1.00f;
    }

    public override void OnNewGameModel(GameModel gameModel)
    {
        currentGameModel = gameModel;

        if (InGameData.CurrentGame?.gameEventId == Patches.InMap.VictoryMapBossManager.EventId)
            gameModel.endRound = 140;

        if (!sessionHandler.ready || !sessionHandler.ProgressivePricesEnabled) return;

        float[] targets = { 1.20f, 1.08f, 1.00f, 0.85f };
        float target = targets[System.Math.Min(sessionHandler.ProgressivePricesCount, 3)];

        string mode = sessionHandler.currentMode ?? "";
        float current = DetectCurrentMultiplier(gameModel, mode);

        ModHelper.Msg<BloonsArchipelago>($"Progressive Prices: currentMode=\"{mode}\", target={target:F2}x, detected={current:F2}x");

        float ratio = target / current;
        if (System.Math.Abs(ratio - 1.0f) < 0.001f)
        {
            ModHelper.Msg<BloonsArchipelago>($"Progressive Prices: no adjustment needed (ratio={ratio:F3})");
            return;
        }

        foreach (var tower in gameModel.towers)
            tower.cost = (int)System.Math.Round(tower.cost * ratio);
        foreach (var upgrade in gameModel.upgrades)
            upgrade.cost = (int)System.Math.Round(upgrade.cost * ratio);

        ModHelper.Msg<BloonsArchipelago>($"Progressive Prices: applied ratio {ratio:F3} (target {target:F2}x / detected {current:F2}x)");
    }

    private static bool _xpTableDumped = false;


    private static void TryDumpXPTable()
    {
        if (_xpTableDumped) return;
        var rankInfo = Il2CppAssets.Scripts.Data.GameData._instance?.rankInfo;
        if (rankInfo == null) return;
        _xpTableDumped = true;
        try
        {
            var sb = new System.Text.StringBuilder("{");
            long cumXP = 0;
            for (int rank = 1; rank <= 150; rank++)
            {
                cumXP += rankInfo.GetXpDiffForRankFromPrev(rank);
                sb.Append($"\"{rank}\":{cumXP}");
                if (rank < 150) sb.Append(',');
            }
            sb.Append('}');
            string path = System.IO.Path.Combine(
                ModContent.GetInstance<BloonsArchipelago>().GetModDirectory(),
                "btd6_xp_table.json");
            System.IO.File.WriteAllText(path, sb.ToString());
            ModHelper.Msg<BloonsArchipelago>($"XP table written to {path}");
        }
        catch (System.Exception ex)
        {
            MelonLogger.Warning($"[BloonsArchipelago] XP table dump failed: {ex.Message}");
        }
    }

    public override void OnUpdate()
    {
        TryDumpXPTable();

        ProcessNotifications();
        Patches.InMap.APNotificationPanel.Update();

        if (InGame.instance == null)
        {
            hasSnappedThisSession = false;

            // Clear any stale DeathLink state so flags don't carry over between matches.
            if (sessionHandler != null)
            {
                sessionHandler.PendingRemoteDeath = false;
                sessionHandler._receivingRemoteDeath = false;
            }

            if (Patches.InMap.FreezeTrapManager.IsActive)
            {
                Patches.InMap.FreezeTrapManager.CleanupAll();
            }

            Patches.InMap.BeeTrapManager.CleanupAll();
            Patches.InMap.SpeedUpTrapManager.CleanupAll();
            Patches.InMap.LiteratureTrapManager.CleanupAll();
            Patches.InMap.MonkeyBoostManager.CleanupAll();
            Patches.InMap.MonkeyStormManager.CleanupAll();
            Patches.InMap.CashDropManager.CleanupAll();
            Patches.InMap.VictoryMapBossStartingCashPatch.Reset();

            Patches.HomeMenu.MonkeyTierDisplay.UpdateMonkeyTierDisplays();

            if (Patches.InMap.VictoryMapBossManager.AutoStartPending && MenuManager.instance != null)
            {
                var menu = MenuManager.instance.GetCurrentMenu();
                var modeScreen = menu?.TryCast<ModeScreen>();
                if (modeScreen != null)
                {
                    Patches.InMap.VictoryMapBossManager.AutoStartPending = false;
                    MelonLogger.Msg("[VictoryMapBoss] Auto-selecting Standard mode to start game.");
                    modeScreen.OnModeSelected("Standard", false);
                }
            }

            return;
        }


        var shDL = sessionHandler;
        if (shDL != null && shDL.ready && shDL.PendingRemoteDeath)
        {
            shDL.PendingRemoteDeath = false;
            shDL._receivingRemoteDeath = true; // suppress outgoing re-broadcast
            try { InGame.instance.SetHealth(0); } catch { }
        }

        Patches.InMap.FreezeTrapManager.Update();
        Patches.InMap.BeeTrapManager.Update();
        Patches.InMap.SpeedUpTrapManager.Update();
        Patches.InMap.LiteratureTrapManager.Update();
        Patches.InMap.MonkeyBoostManager.Update();
        Patches.InMap.MonkeyStormManager.Update();
        Patches.InMap.CashDropManager.Update();
        if (Patches.InMap.VictoryMapBossManager.IsActive)
            Patches.InMap.VictoryMapBossStartingCashPatch.TryGiveCash();

        Patches.InMap.PopTierLockPatch.UpdateButtonDisplays();
    }

    private static void ProcessNotifications()
    {

        while (sessionHandler.notifications.TryDequeue(out var notif))
            pendingNotifications.Enqueue(notif);


        float now = Time.unscaledTime;
        if (pendingNotifications.Count > 0 && now - lastNotificationTime >= 1f)
        {
            var notif = pendingNotifications.Dequeue();
            if (showNotifications)
                Patches.InMap.APNotificationPanel.Show(notif);
            lastNotificationTime = now;
        }
    }
}
