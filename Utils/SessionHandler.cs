using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;

using BTD_Mod_Helper;

using Il2CppAssets.Scripts.Data.MapSets;
using GameData = Il2CppAssets.Scripts.Data.GameData;

using MelonLoader;

using System;
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

        public List<string> notifications = new();
        public List<string> previousNotifications = new();

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

        private static HashSet<string> _validMapIds;

        public string APID = "";
        public string VictoryMap = "";
        public long MedalRequirement = 0;
        public long Difficulty = 0;
        public int Medals = 0;

        public string currentMap = "";
        public string currentMode = "";

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

        public SessionHandler(string url, int port, string slot, string password)
        {
            if (defaultMapList == null)
            {
                defaultMapList = GameData._instance.mapSet.Maps.items;
                RebuildValidMapIds();
            }

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

                    notifications.Add("You've received " + itemName + " from " + itemPlayer + " at " + itemLocation);
                    if (itemName is not null)
                    {
                        if (itemName.Contains("-MUnlock"))
                        {
                            MapsUnlocked.Add(itemName.Replace("-MUnlock", ""));
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

            APID = session.RoomState.Seed;
            if (BloonsArchipelago.notifJson.APWorlds.ContainsKey(APID))
            {
                previousNotifications.AddRange(BloonsArchipelago.notifJson.APWorlds[APID]);
            }

            if (session.DataStorage["XP-" + PlayerSlotName()])
            {
                XPTracker = new ArchipelagoXP(session.DataStorage["Level-" + PlayerSlotName()], session.DataStorage["XP-" + PlayerSlotName()], (Int64)slotData["staticXPReq"], (Int64)slotData["maxLevel"], (bool)slotData["xpCurve"]);
            }
            else
            {
                XPTracker = new ArchipelagoXP((Int64)slotData["staticXPReq"], (Int64)slotData["maxLevel"], (bool)slotData["xpCurve"]);
            }

            VictoryMap = (string)slotData["victoryLocation"];
            MedalRequirement = (Int64)slotData["medalsNeeded"];
            Difficulty = (Int64)slotData["difficulty"];

            if (slotData.ContainsKey("progressiveKnowledge"))
                ProgressiveKnowledgeMode = (bool)slotData["progressiveKnowledge"];

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

        public void CompleteCheck(string checkstring)
        {
            try
            {
                session.Locations.CompleteLocationChecks(session.Locations.GetLocationIdFromName("Bloons TD6", checkstring));
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[BloonsArchipelago] Failed to complete check '{checkstring}': {ex.Message}");
            }
        }

        public void RefreshAllData()
        {
            MapsUnlocked.Clear();
            MonkeysUnlocked.Clear();
            KnowledgeUnlocked.Clear();
            HeroesUnlocked.Clear();
            Medals = 0;
            ProgressiveKnowledgeCount = 0;

            foreach (var item in session.Items.AllItemsReceived)
            {
                string itemName = item.ItemName;
                if (itemName == null) continue;

                if (itemName.Contains("-MUnlock"))
                    MapsUnlocked.Add(itemName.Replace("-MUnlock", ""));
                else if (itemName.Contains("-TUnlock"))
                    MonkeysUnlocked.Add(itemName.Replace("-TUnlock", ""));
                else if (itemName == "Progressive Knowledge")
                    ProgressiveKnowledgeCount++;
                else if (itemName.Contains("-KUnlock"))
                    KnowledgeUnlocked.Add(itemName.Replace("-KUnlock", ""));
                else if (itemName.Contains("-HUnlock"))
                    HeroesUnlocked.Add(itemName.Replace("-HUnlock", ""));
                else if (itemName == "Medal")
                    Medals++;
            }

            if (ProgressiveKnowledgeCount > 0)
                RefreshKnowledgeUnlocked();

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
                long locationID = session.Locations.GetLocationIdFromName("Bloons TD6", locationString);
                return session.Locations.AllLocationsChecked.Contains(locationID);
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
                if (map?.id == null) continue;

                if (!_validMapIds.Contains(map.id))
                {
                    MelonLogger.Warning($"[BloonsArchipelago] Skipping map '{map.id}' — not found in current GameData (renamed or removed in a BTD6 update?).");
                    continue;
                }

                if (MapsUnlocked.Contains(map.id) || map.id == VictoryMap)
                {
                    if (map.id != VictoryMap)
                        CompleteCheck(map.id + "-Unlock");
                    mapDetails.Add(map);
                }
            }
            return mapDetails.ToArray();
        }

        public string PlayerSlotName()
        {
            int slot = session.ConnectionInfo.Slot;
            string name = session.Players.GetPlayerName(slot);
            return name;
        }
    }
}
