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

    public override void OnUpdate()
    {
        if (InGame.instance == null)
        {
            mapLoaded = false;
            hasSnappedThisSession = false;
            mapLoadTime = 0f;
            notificationDelayActive = false;
            Patches.HomeMenu.MonkeyTierDisplay.UpdateMonkeyTierDisplays();
            return;
        }

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
