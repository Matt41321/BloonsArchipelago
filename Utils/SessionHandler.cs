using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;

using BTD_Mod_Helper;

using Il2CppAssets.Scripts.Data.MapSets;
using GameData = Il2CppAssets.Scripts.Data.GameData;

using MelonLoader;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace BloonsArchipelago.Utils
{
    public class SessionHandler
    {
        public ArchipelagoSession session;
        public bool ready = false;

        public ArchipelagoXP XPTracker;


        public ConcurrentQueue<APNotification> notifications = new();

        public ConcurrentDictionary<string, byte> previousNotifications = new();


        private volatile bool _suppressNotifications = true;

        public List<string> MapsUnlocked = new();
        public List<string> MonkeysUnlocked = new();
        public List<string> KnowledgeUnlocked = new();
        public List<string> HeroesUnlocked = new();

        public int ProgressiveKnowledgeCount = 0;
        public bool ProgressiveKnowledgeMode = false;

        private static readonly Dictionary<string, int> KnowledgeLayerMap = new()
        {
            { "FastTackAttacks", 1 }, { "IncreasedLifespan", 1 }, { "ExtraDartPops", 1 },
            { "PoppyBlades", 2 }, { "HardTacks", 2 }, { "FastGlue", 2 }, { "FraggyFrags", 2 }, { "CheapRangs", 2 }, { "CrossbowReach", 2 },
            { "BigInferno", 3 }, { "IcyChill", 3 }, { "MoreSplattyGlue", 3 }, { "BudgetClusters", 3 }, { "ExtraBounce", 3 }, { "4And4", 3 }, { "ForceVsForce", 3 }, { "RecurringRangs", 3 },
            { "SoCold", 4 }, { "AviationGradeGlue", 4 }, { "HardPress", 4 }, { "MasterDoubleCross", 4 }, { "MegaMauler", 4 },
            { "BigCryoBlast", 5 }, { "Hypothermia", 5 }, { "CheaperSolution", 5 }, { "ViolentImpact", 5 }, { "LongTurbo", 5 }, { "ComeOnEverybody", 5 }, { "BionicAugmentation", 5 },
            { "BonusGlueGunner", 6 }, { "BonusMonkey", 6 }, { "MoreCash", 6 },
            { "NavalUpgrades", 1 }, { "AirforceUpgrades", 1 }, { "EliteMilitaryTraining", 1 }, { "EmergencyUnlock", 1 },
            { "BigBunch", 2 }, { "AcceleratedAerodarts", 2 }, { "CeramicShock", 2 },
            { "ExtraBurnyStuff", 3 }, { "BreakingBallistic", 3 }, { "FasterTakedowns", 3 }, { "TargetedPineapples", 3 }, { "RapidRazors", 3 }, { "CheaperMaiming", 3 }, { "GorgonStorm", 3 },
            { "QuadBurst", 4 }, { "TradeAgreements", 4 }, { "GunCoolant", 4 }, { "PaintStripper", 4 }, { "CrossTheStreams", 4 },
            { "ChargedChinooks", 5 }, { "AeronauticSubsidy", 5 }, { "MasterDefender", 5 }, { "BudgetBattery", 5 }, { "Wingmonkey", 5 }, { "FlankingManeuvers", 5 },
            { "SubAdmiral", 6 }, { "MilitaryConscription", 6 }, { "DoorGunner", 6 }, { "AdvancedLogistics", 6 },
            { "BigBloonSabotage", 7 },
            { "SuperRange", 1 }, { "MagicTricks", 1 }, { "LingeringMagic", 1 },
            { "CheaperDoubles", 2 }, { "HeavyKnockback", 2 }, { "HotMagic", 2 }, { "SpeedyBrewing", 2 }, { "MoMonkeyMoney", 2 },
            { "DiversionTactics", 3 }, { "StrikeDownTheFalse", 3 }, { "WarmOak", 3 }, { "StrongTonic", 3 }, { "FlameJet", 3 },
            { "XrayUltra", 4 }, { "ArcaneImpale", 4 }, { "AcidStability", 4 }, { "ColdFront", 4 },
            { "DeadlyTranquility", 5 }, { "ThereCanBeOnlyOne", 5 }, { "VineRupture", 5 },
            { "ManaShield", 6 }, { "TinyTornadoes", 6 },
            { "FlatPackBuildings", 1 }, { "OneMoreSpike", 1 },
            { "InsiderTrades", 2 }, { "MoreValuableBananas", 2 }, { "FirstLastLineOfDefense", 2 },
            { "MonkeyEducation", 3 }, { "BiggerBanks", 3 }, { "FarmSubsidy", 3 }, { "VigilantSentries", 3 },
            { "VeryShreddy", 4 }, { "BackroomDeals", 4 }, { "InlandRevenueStreams", 4 }, { "ToArms", 4 }, { "ThickerFoams", 4 },
            { "BetterSellDeals", 5 }, { "HiValueMines", 5 }, { "VeteranMonkeyTraining", 5 }, { "GlobalAbilityCooldowns", 5 }, { "BigTraps", 5 }, { "HealthyBananas", 5 },
            { "BankDeposits", 6 }, { "ParagonOfPower", 6 },
            { "HeroicReach", 1 }, { "MoreSplody", 1 }, { "AbilityDiscipline", 1 },
            { "HeroicVelocity", 2 }, { "Scholarships", 2 },
            { "QuickHands", 3 }, { "SelfTaughtHeroes", 3 }, { "AbilityMastery", 3 },
            { "HeroFavors", 4 },
            { "EmpoweredHeroes", 5 }, { "BigBloonBlueprints", 5 },
            { "MonkeysTogetherStrong", 6 }, { "WeakPoint", 6 },
            { "BiggerCamoTrap", 1 }, { "JustOneMore", 1 }, { "CheaperLakes", 1 },
            { "MaulingMoabMines", 2 }, { "LongerDartTime", 2 }, { "BudgetPontoons", 2 },
            { "SupersizeGlueTrap", 3 }, { "LongerBoosts", 3 }, { "PowerfulMonkeyStorm", 3 },
            { "PreGamePrep", 4 }, { "FitFarmers", 4 }, { "AmbushTech", 4 },
            { "BudgetCashDrops", 5 }, { "SupaThrive", 5 },
            { "GrandPrixSpree", 6 },
        };

        private void RefreshKnowledgeUnlocked()
        {
            KnowledgeUnlocked.Clear();
            foreach (var entry in KnowledgeLayerMap)
            {
                if (entry.Value <= ProgressiveKnowledgeCount)
                    KnowledgeUnlocked.Add(entry.Key);
            }
        }

        public static MapDetails[] defaultMapList;
        private static int _defaultMapCount = 0;

        private static HashSet<string> _validMapIds;


        private static readonly Dictionary<string, string> _gameIdToApId = new()
        {
            { "Tutorial", "MonkeyMeadow" },
        };
        private static readonly Dictionary<string, string> _apIdToGameId = new()
        {
            { "MonkeyMeadow", "Tutorial" },
        };

        public static string GameIdToApId(string gameId)
            => _gameIdToApId.TryGetValue(gameId, out string apId) ? apId : gameId;

        public static string ApIdToGameId(string apId)
            => _apIdToGameId.TryGetValue(apId, out string gameId) ? gameId : apId;


        private static readonly Dictionary<string, string> _gameModeToApMode = new()
        {
            { "Clicks", "Chimps" },
        };

        public static string GameModeToApMode(string mode)
            => _gameModeToApMode.TryGetValue(mode, out string apMode) ? apMode : mode;

        public string APID = "";
        public string VictoryMap = "";
        public long MedalRequirement = 0;
        public long Difficulty = 0;
        public int Medals = 0;
        /// 0 = default, 1 = normal boss, 2 = elite boss
        public int GoalType = 0;
        public bool BossGoal => GoalType >= 1;

        public string currentMap = "";
        public string currentMode = "";

        public int RoundSanityInterval = 0;

        public bool ProgressivePricesEnabled = false;
        public int ProgressivePricesCount = 0;

        public bool CategoryLockEnabled = false;

        public int ModifiedBloonsRoundsRemaining = 0;
        public int SpeedUpRoundsRemaining = 0;

        private static readonly Dictionary<string, string[]> CategoryTowers = new()
        {
            { "Primary Monkeys", new[] { "DartMonkey", "BoomerangMonkey", "BombShooter", "TackShooter", "IceMonkey", "GlueGunner", "Desperado" } },
            { "Military Monkeys", new[] { "SniperMonkey", "MonkeySub", "MonkeyBuccaneer", "MonkeyAce", "HeliPilot", "MortarMonkey", "DartlingGunner" } },
            { "Magic Monkeys", new[] { "WizardMonkey", "SuperMonkey", "NinjaMonkey", "Alchemist", "Druid", "Mermonkey" } },
            { "Support Monkeys", new[] { "BananaFarm", "SpikeFactory", "MonkeyVillage", "EngineerMonkey", "BeastHandler" } },
        };

        public bool PopTierChecksEnabled = false;
        public long Tier3PopRequirement = 5000;
        public long Tier4PopRequirement = 25000;
        public long Tier5PopRequirement = 100000;
        public HashSet<string> PermanentlyUnlockedTiers = new();
        public Dictionary<string, long> CumulativePops = new();
        public Dictionary<string, long> SessionEndLivePops = new();

        public void BankTowerPops(string baseId, long pops)
        {
            if (string.IsNullOrEmpty(baseId) || pops <= 0) return;
            if (!CumulativePops.ContainsKey(baseId))
                CumulativePops[baseId] = 0;
            CumulativePops[baseId] += pops;
        }

        public SessionHandler() { }

        public static void RefreshDefaultMapList()
        {
            try
            {
                var currentItems = GameData._instance?.mapSet?.Maps?.items;
                if (currentItems == null || currentItems.Length == 0) return;


                if (_defaultMapCount == 0 || currentItems.Length >= _defaultMapCount)
                {
                    defaultMapList = currentItems;
                    _defaultMapCount = currentItems.Length;
                    RebuildValidMapIds();
                }
            }
            catch { }
        }

        public SessionHandler(string url, int port, string slot, string password)
        {
            RefreshDefaultMapList();

            session = ArchipelagoSessionFactory.CreateSession(url, port);

            LoginResult result;

            try
            {
                result = session.TryConnectAndLogin("Bloons TD6", slot, ItemsHandlingFlags.AllItems, password: password);
            }
            catch (Exception ex)
            {
                result = new LoginFailure(ex.GetBaseException().Message);
            }

            if (!result.Successful)
            {
                LoginFailure failure = (LoginFailure)result;
                string errorMessage = $"Failed to Connect to {url} as {slot}:";
                foreach (string error in failure.Errors)
                {
                    errorMessage += error;
                }
                return;
            }

            ready = true;

            LoginSuccessful loginSuccess = (LoginSuccessful)result;
            Dictionary<string, object> slotData = loginSuccess.SlotData;

            session.Socket.SocketClosed += (reason) =>
            {
                ready = false;
                MelonLogger.Warning("[BloonsArchipelago] Disconnected from Archipelago server.");
            };

            session.Items.ItemReceived += (receivedItemsHelper) =>
            {
                try
                {
                    ItemInfo item = receivedItemsHelper.PeekItem();
                    string itemName = item.ItemName;
                    string itemPlayer = item.Player.Name;
                    string itemLocation = item.LocationName;
                    ModHelper.Msg<BloonsArchipelago>(itemName + " Received from Server");


                    bool selfSend = item.Player.Slot == session.ConnectionInfo.Slot;
                    string from = selfSend
                        ? "You found it!"
                        : "from " + itemPlayer;
                    if (!selfSend)
                    {
                        try
                        {
                            string senderGame = item.Player.Game;
                            if (!string.IsNullOrEmpty(senderGame))
                                from += " (" + senderGame + ")";
                        }
                        catch { }
                    }


                    string fullText = "You've received " + itemName + " from " + itemPlayer + " at " + itemLocation;
                    bool isNew = previousNotifications.TryAdd(fullText, 0);
                    if (isNew && !_suppressNotifications)
                    {
                        notifications.Enqueue(new APNotification
                        {
                            Category = GetItemCategory(itemName),
                            ItemName = GetCleanName(itemName),
                            From     = from,
                            FullText = fullText,
                        });
                    }
                    if (itemName is not null)
                    {
                        if (itemName.Contains("-MUnlock"))
                        {
                            MapsUnlocked.Add(GameIdToApId(itemName.Replace("-MUnlock", "")));
                        }
                        else if (itemName.Contains("-TUnlock"))
                        {
                            MonkeysUnlocked.Add(itemName.Replace("-TUnlock", ""));
                        }
                        else if (itemName == "Progressive Knowledge")
                        {
                            ProgressiveKnowledgeCount++;
                            RefreshKnowledgeUnlocked();
                        }
                        else if (itemName.Contains("-KUnlock"))
                        {
                            KnowledgeUnlocked.Add(itemName.Replace("-KUnlock", ""));
                        }
                        else if (itemName.Contains("-HUnlock"))
                        {
                            HeroesUnlocked.Add(itemName.Replace("-HUnlock", ""));
                        }
                        else if (itemName == "Progressive Prices")
                        {
                            ProgressivePricesCount++;
                        }
                        else if (CategoryTowers.ContainsKey(itemName))
                        {
                            foreach (var tower in CategoryTowers[itemName])
                                if (!MonkeysUnlocked.Contains(tower))
                                    MonkeysUnlocked.Add(tower);
                        }
                        else if (itemName == "Modified Bloons")
                        {
                            if (Il2CppAssets.Scripts.Unity.UI_New.InGame.InGame.instance != null)
                            {
                                ModifiedBloonsRoundsRemaining += 3;
                            }
                        }
                        else if (itemName == "Freeze Trap")
                        {
                            if (Il2CppAssets.Scripts.Unity.UI_New.InGame.InGame.instance != null)
                            {
                                Patches.InMap.FreezeTrapManager.PendingFreezeCount++;
                            }
                        }
                        else if (itemName == "Speed Up Trap")
                        {
                            if (Il2CppAssets.Scripts.Unity.UI_New.InGame.InGame.instance != null)
                            {
                                MelonLoader.MelonLogger.Msg("[SpeedUp] Queued PendingSpeedUpCount++");
                                Patches.InMap.SpeedUpTrapManager.PendingSpeedUpCount++;
                            }
                            else
                            {
                                MelonLoader.MelonLogger.Msg("[SpeedUp] Skipped — InGame.instance is null");
                            }
                        }
                        else if (itemName == "Bee Trap")
                        {
                            if (Il2CppAssets.Scripts.Unity.UI_New.InGame.InGame.instance != null)
                            {
                                Patches.InMap.BeeTrapManager.PendingBeeCount++;
                            }
                        }
                        else if (itemName == "Literature Trap")
                        {
                            if (Il2CppAssets.Scripts.Unity.UI_New.InGame.InGame.instance != null)
                            {
                                Patches.InMap.LiteratureTrapManager.PendingLiteratureCount++;
                            }
                        }
                        else if (itemName == "Monkey Boost")
                        {
                            if (Il2CppAssets.Scripts.Unity.UI_New.InGame.InGame.instance != null)
                            {
                                Patches.InMap.MonkeyBoostManager.PendingBoostCount++;
                            }
                        }
                        else if (itemName == "Monkey Storm")
                        {
                            if (Il2CppAssets.Scripts.Unity.UI_New.InGame.InGame.instance != null)
                            {
                                Patches.InMap.MonkeyStormManager.PendingStormCount++;
                            }
                        }
                        else if (itemName == "Cash Drop")
                        {
                            if (Il2CppAssets.Scripts.Unity.UI_New.InGame.InGame.instance != null)
                            {
                                Patches.InMap.CashDropManager.PendingCashDropCount++;
                            }
                        }
                        else if (itemName == "Medal")
                        {
                            Medals++;
                        }
                    }
                    receivedItemsHelper.DequeueItem();
                }
                catch (Exception ex)
                {
                    MelonLogger.Warning($"[BloonsArchipelago] Error processing received item: {ex.Message}");
                    try { receivedItemsHelper.DequeueItem(); } catch { }
                }
            };

            session.MessageLog.OnMessageReceived += (message) =>
            {
                try
                {
                    if (message is Archipelago.MultiClient.Net.MessageLog.Messages.ChatLogMessage chat)
                    {
                        string chatSender = chat.Player?.Name ?? "?";
                        string chatText   = chat.Message ?? "";
                        notifications.Enqueue(new APNotification
                        {
                            Category   = "Chat",
                            ItemName   = chatText,
                            From       = chatSender,
                            FullText   = chatSender + ": " + chatText,
                            IsOutgoing = false,
                        });
                        return;
                    }

                    if (message is not Archipelago.MultiClient.Net.MessageLog.Messages.ItemSendLogMessage send) return;
                    if (send.Sender.Slot != session.ConnectionInfo.Slot) return;  
                    if (send.Receiver.Slot == session.ConnectionInfo.Slot) return; 

                    string itemName = send.Item.ItemName;
                    string receiverName = send.Receiver.Name;
                    string receiverGame = send.Receiver.Game ?? "";
                    string toLine = string.IsNullOrEmpty(receiverGame)
                        ? "to " + receiverName
                        : "to " + receiverName + " (" + receiverGame + ")";

                    string fullText = "Sent " + itemName + " " + toLine;
                    notifications.Enqueue(new APNotification
                    {
                        Category   = GetItemCategory(itemName),
                        ItemName   = itemName, 
                        From       = toLine,
                        FullText   = fullText,
                        IsOutgoing = true,
                        ItemColor  = FlagsToColor(send.Item.Flags),
                    });
                }
                catch { }
            };

            APID = session.RoomState.Seed;
            if (BloonsArchipelago.notifJson.APWorlds.ContainsKey(APID))
            {
                foreach (var s in BloonsArchipelago.notifJson.APWorlds[APID])
                    previousNotifications.TryAdd(s, 0);
            }

            if (session.DataStorage["XP-" + PlayerSlotName()])
            {
                XPTracker = new ArchipelagoXP(session.DataStorage["Level-" + PlayerSlotName()], session.DataStorage["XP-" + PlayerSlotName()], (Int64)slotData["staticXPReq"], (Int64)slotData["maxLevel"], (bool)slotData["xpCurve"]);
            }
            else
            {
                XPTracker = new ArchipelagoXP((Int64)slotData["staticXPReq"], (Int64)slotData["maxLevel"], (bool)slotData["xpCurve"]);
            }

            VictoryMap = ApIdToGameId((string)slotData["victoryLocation"]);
            MedalRequirement = (Int64)slotData["medalsNeeded"];
            Difficulty = (Int64)slotData["difficulty"];

            if (slotData.ContainsKey("goal"))
                GoalType = (int)(Int64)slotData["goal"];

            if (slotData.ContainsKey("progressiveKnowledge"))
                ProgressiveKnowledgeMode = (bool)slotData["progressiveKnowledge"];

            if (slotData.ContainsKey("roundSanity"))
                RoundSanityInterval = (int)(Int64)slotData["roundSanity"];

            if (slotData.ContainsKey("progressivePrices"))
                ProgressivePricesEnabled = (bool)slotData["progressivePrices"];

            if (slotData.ContainsKey("categoryLock"))
                CategoryLockEnabled = (bool)slotData["categoryLock"];

            if (slotData.ContainsKey("popTierChecks") && (bool)slotData["popTierChecks"])
            {
                PopTierChecksEnabled = true;
                if (slotData.ContainsKey("tier3PopRequirement"))
                    Tier3PopRequirement = (Int64)slotData["tier3PopRequirement"];
                if (slotData.ContainsKey("tier4PopRequirement"))
                    Tier4PopRequirement = (Int64)slotData["tier4PopRequirement"];
                if (slotData.ContainsKey("tier5PopRequirement"))
                    Tier5PopRequirement = (Int64)slotData["tier5PopRequirement"];
            }

            ModHelper.Msg<BloonsArchipelago>(MedalRequirement + " Medals Required to Unlock " + VictoryMap);

            LoadProgress();

            _suppressNotifications = false;
        }

        private string GetProgressSavePath()
        {
            string key = $"{session.RoomState.Seed}_{PlayerSlotName()}";
            foreach (char c in Path.GetInvalidFileNameChars())
                key = key.Replace(c, '_');
            string dir = Path.Combine(Environment.CurrentDirectory, "UserData", "BloonsArchipelago");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, $"PopProgress_{key}.json");
        }

        public void SaveProgress()
        {
            if (!PopTierChecksEnabled) return;
            try
            {
                var data = new PopProgressData
                {
                    CumulativePops = new Dictionary<string, long>(CumulativePops),
                    SessionEndLivePops = new Dictionary<string, long>(SessionEndLivePops),
                    PermanentlyUnlockedTiers = new List<string>(PermanentlyUnlockedTiers)
                };
                File.WriteAllText(GetProgressSavePath(), JsonSerializer.Serialize(data));
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[BloonsArchipelago] Failed to save pop progress: {ex.Message}");
            }
        }

        private void LoadProgress()
        {
            if (!PopTierChecksEnabled) return;
            try
            {
                string path = GetProgressSavePath();
                if (!File.Exists(path)) return;
                var data = JsonSerializer.Deserialize<PopProgressData>(File.ReadAllText(path));
                if (data == null) return;
                if (data.CumulativePops != null)
                    CumulativePops = data.CumulativePops;
                if (data.SessionEndLivePops != null)
                    SessionEndLivePops = data.SessionEndLivePops;
                if (data.PermanentlyUnlockedTiers != null)
                    PermanentlyUnlockedTiers = new HashSet<string>(data.PermanentlyUnlockedTiers);
                MelonLogger.Msg($"[BloonsArchipelago] Loaded pop progress ({CumulativePops.Count} tower(s), {PermanentlyUnlockedTiers.Count} tier(s) unlocked).");
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[BloonsArchipelago] Failed to load pop progress: {ex.Message}");
            }
        }

        public static void RebuildValidMapIds()
        {
            _validMapIds = new HashSet<string>();
            if (defaultMapList == null) return;
            foreach (var map in defaultMapList)
            {
                if (map?.id != null)
                    _validMapIds.Add(map.id);
            }
        }

        // Returns the location ID for a check name, falling back to legacy names
        // so that the mod works with both new and old APworld versions.
        private long ResolveLocationId(string locationName)
        {
            long id = session.Locations.GetLocationIdFromName("Bloons TD6", locationName);
            if (id != -1) return id;

            string legacy = locationName
                .Replace("Chimps", "Clicks")
                .Replace("MonkeyMeadow", "Tutorial");
            if (legacy != locationName)
                id = session.Locations.GetLocationIdFromName("Bloons TD6", legacy);
            return id;
        }

        public void CompleteCheck(string checkstring)
        {
            try
            {
                long locationID = ResolveLocationId(checkstring);
                if (locationID == -1) return;
                session.Locations.CompleteLocationChecks(locationID);
            }
            catch { }
        }

        public void RefreshAllData()
        {
            MapsUnlocked.Clear();
            MonkeysUnlocked.Clear();
            KnowledgeUnlocked.Clear();
            HeroesUnlocked.Clear();
            Medals = 0;
            ProgressiveKnowledgeCount = 0;
            ProgressivePricesCount = 0;

            foreach (var item in session.Items.AllItemsReceived)
            {
                string itemName = item.ItemName;
                if (itemName == null) continue;

                if (itemName.Contains("-MUnlock"))
                    MapsUnlocked.Add(GameIdToApId(itemName.Replace("-MUnlock", "")));
                else if (itemName.Contains("-TUnlock"))
                    MonkeysUnlocked.Add(itemName.Replace("-TUnlock", ""));
                else if (itemName == "Progressive Knowledge")
                    ProgressiveKnowledgeCount++;
                else if (itemName == "Progressive Prices")
                    ProgressivePricesCount++;
                else if (CategoryTowers.ContainsKey(itemName))
                {
                    foreach (var tower in CategoryTowers[itemName])
                        if (!MonkeysUnlocked.Contains(tower))
                            MonkeysUnlocked.Add(tower);
                }
                else if (itemName.Contains("-KUnlock"))
                    KnowledgeUnlocked.Add(itemName.Replace("-KUnlock", ""));
                else if (itemName.Contains("-HUnlock"))
                    HeroesUnlocked.Add(itemName.Replace("-HUnlock", ""));
                else if (itemName == "Medal")
                    Medals++;
            }

            if (ProgressiveKnowledgeCount > 0)
                RefreshKnowledgeUnlocked();

            RefreshDefaultMapList();
            GameData._instance.mapSet.Maps.items = GetMapDetails();
        }

        public void CompleteRando()
        {
            StatusUpdatePacket statusUpdatePackage = new StatusUpdatePacket
            {
                Status = ArchipelagoClientState.ClientGoal
            };
            session.Socket.SendPacket(statusUpdatePackage);
            BloonsArchipelago.notifJson.APWorlds.Remove(APID);
        }

        public bool LocationChecked(string locationString)
        {
            try
            {
                long locationID = ResolveLocationId(locationString);
                return locationID != -1 && session.Locations.AllLocationsChecked.Contains(locationID);
            }
            catch
            {
                return false;
            }
        }

        public MapDetails[] GetMapDetails()
        {
            if (defaultMapList == null) return System.Array.Empty<MapDetails>();

            if (_validMapIds == null) RebuildValidMapIds();

            List<MapDetails> mapDetails = new();
            foreach (var map in defaultMapList)
            {
                try
                {
                    if (map == null) continue;
                    string mapId = map.id;
                    if (string.IsNullOrEmpty(mapId)) continue;

                    if (!_validMapIds.Contains(mapId))
                        continue;

                    string apMapId = GameIdToApId(mapId);
                    if (MapsUnlocked.Contains(apMapId) || mapId == VictoryMap)
                    {
                        if (mapId != VictoryMap)
                            CompleteCheck(apMapId + "-Unlock");
                        mapDetails.Add(map);
                    }
                }
                catch
                {
                    continue;
                }
            }
            return mapDetails.ToArray();
        }

        public void Disconnect()
        {
            if (!ready) return;
            try { session?.Socket?.DisconnectAsync(); } catch { }
            ready = false;
        }

        public string PlayerSlotName()
        {
            int slot = session.ConnectionInfo.Slot;
            string name = session.Players.GetPlayerName(slot);
            return name;
        }

        private static string GetItemCategory(string itemName)
        {
            if (itemName.Contains("-MUnlock")) return "Map Unlock";
            if (itemName.Contains("-TUnlock")) return "Tower Unlock";
            if (itemName.Contains("-HUnlock")) return "Hero";
            if (itemName.Contains("-KUnlock")) return "Knowledge";
            if (itemName == "Progressive Knowledge") return "Progression";
            if (itemName == "Progressive Prices")    return "Progression";
            if (itemName == "Medal")                 return "Medal";
            if (CategoryTowers.ContainsKey(itemName)) return "Tower Unlock";
            if (itemName == "Modified Bloons"  ||
                itemName == "Freeze Trap"      ||
                itemName == "Speed Up Trap"    ||
                itemName == "Bee Trap"         ||
                itemName == "Literature Trap") return "Trap";
            if (itemName == "Monkey Boost"  ||
                itemName == "Monkey Storm"  ||
                itemName == "Cash Drop")       return "Filler";
            return "Item";
        }

        private static string GetCleanName(string itemName)
        {
            if (itemName.Contains("-MUnlock")) return SplitPascalCase(itemName.Replace("-MUnlock", ""));
            if (itemName.Contains("-TUnlock")) return SplitPascalCase(itemName.Replace("-TUnlock", ""));
            if (itemName.Contains("-HUnlock")) return SplitPascalCase(itemName.Replace("-HUnlock", ""));
            if (itemName.Contains("-KUnlock")) return SplitPascalCase(itemName.Replace("-KUnlock", ""));
            return itemName;
        }

        private static string SplitPascalCase(string s)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < s.Length; i++)
            {
                if (i > 0 && char.IsUpper(s[i]) && (char.IsLower(s[i - 1]) || char.IsDigit(s[i - 1])))
                    sb.Append(' ');
                sb.Append(s[i]);
            }
            return sb.ToString();
        }

        private static UnityEngine.Color FlagsToColor(Archipelago.MultiClient.Net.Enums.ItemFlags flags)
        {
            if (flags.HasFlag(Archipelago.MultiClient.Net.Enums.ItemFlags.Trap))
                return new UnityEngine.Color(1.00f, 0.27f, 0.27f);   // red       — Trap
            if (flags.HasFlag(Archipelago.MultiClient.Net.Enums.ItemFlags.Advancement))
                return new UnityEngine.Color(0.78f, 0.55f, 1.00f);   // purple    — Progression
            if (flags.HasFlag(Archipelago.MultiClient.Net.Enums.ItemFlags.NeverExclude))
                return new UnityEngine.Color(0.20f, 0.40f, 0.85f);   // dark blue — Useful
            return new UnityEngine.Color(0.00f, 0.87f, 1.00f);       // light blue — Filler
        }
    }
}
