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

    private static float mapLoadTime = 0f;
    private static bool mapLoaded = false;
    private static bool hasSnappedThisSession = false;
    private static bool notificationDelayActive = false;
    private static float lastNotificationTime = 0f;
    private static Queue<string> pendingNotifications = new Queue<string>();

    static readonly ModSettingString url = "archipelago.gg";
    static readonly ModSettingInt port = 25565;
    static readonly ModSettingString slot = "Player";
    static readonly ModSettingString password = "";
    static readonly ModSettingButton archipelagoConnect = new(() =>
    {
        ModHelper.Msg<BloonsArchipelago>("Connecting...");

        sessionHandler = new SessionHandler(url, port, slot, password);
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
            case "Easy":
            case "PrimaryOnly":
            case "Deflation":
                return 0.85f;
            case "Medium":
            case "Standard":
            case "MilitaryOnly":
            case "Apopalypse":
            case "Reverse":
                return 1.00f;
            case "Hard":
            case "MagicOnly":
            case "DoubleMoabHealth":
            case "HalfCash":
            case "AlternateBloonsRounds":
                return 1.08f;
            case "Clicks": // CHIMPS uses Hard pricing
                return 1.08f;
            case "Impoppable":
                return 1.20f;
        }

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

        // infer difficulty from Dart Monkey cost: Easy≈170, Medium≈200, Hard≈216, Impoppable≈240
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

    public override void OnUpdate()
    {
        if (InGame.instance == null)
        {
            mapLoaded = false;
            hasSnappedThisSession = false;
            mapLoadTime = 0f;
            notificationDelayActive = false;

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

        if (!mapLoaded)
        {
            mapLoaded = true;
            mapLoadTime = Time.time;
            notificationDelayActive = true;
        }

        for (int i = sessionHandler.notifications.Count - 1; i >= 0; i--)
        {
            string notification = sessionHandler.notifications[i];
            if (!sessionHandler.previousNotifications.Contains(notification))
            {
                pendingNotifications.Enqueue(notification);
                sessionHandler.notifications.RemoveAt(i);
                sessionHandler.previousNotifications.Add(notification);
            }
            else
            {
                sessionHandler.notifications.RemoveAt(i);
            }
        }

        if (notificationDelayActive && pendingNotifications.Count > 0)
        {
            float currentTime = Time.time;

            if (currentTime - mapLoadTime >= 1f)
            {
                if (currentTime - lastNotificationTime >= 1f)
                {
                    string notification = pendingNotifications.Dequeue();
                    if (showNotifications)
                    {
                        Game.instance.ShowMessage(notification, 5f, "Archipelago");
                    }
                    lastNotificationTime = currentTime;
                }
            }
        }
    }
}
