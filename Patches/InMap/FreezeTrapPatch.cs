using BTD_Mod_Helper;
using BTD_Mod_Helper.Api;
using BTD_Mod_Helper.Extensions;
using HarmonyLib;
using Il2CppAssets.Scripts.Simulation.Towers;
using Il2CppAssets.Scripts.Simulation.Towers.Weapons;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using MelonLoader;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BloonsArchipelago.Patches.InMap
{
    [HarmonyPatch(typeof(Weapon), nameof(Weapon.Process))]
    internal class WeaponProcessFreezePatch
    {
        [HarmonyPrefix]
        private static bool Prefix(Weapon __instance)
        {
            try
            {
                var tower = __instance.attack?.tower;
                if (tower != null && FreezeTrapManager.IsTowerFrozen(tower))
                    return false;
            }
            catch { }
            return true;
        }
    }

    [HarmonyPatch(typeof(Tower), nameof(Tower.Initialise))]
    internal class FreezeTrapNewTowerPatch
    {
        [HarmonyPostfix]
        private static void Postfix(Tower __instance)
        {
            try
            {
                if (!FreezeTrapManager.IsActive) return;
                FreezeTrapManager.FreezeSingleTower(__instance);
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[BloonsArchipelago] Error freezing newly placed tower: {ex.Message}");
            }
        }
    }

    internal static class FreezeTrapManager
    {
        private static readonly Dictionary<int, GameObject> _overlays = new();
        private static readonly HashSet<int> _frozenTowers = new();

        private static Sprite _freezeSprite = null;
        private static float _freezeEndTime = 0f;
        private static bool _freezeActive = false;

        public static volatile int PendingFreezeCount = 0;

        public static bool IsActive => _freezeActive;

        public static bool IsTowerFrozen(Tower tower)
        {
            if (tower == null) return false;
            return _frozenTowers.Contains(tower.Id.GetHashCode());
        }

        private static Sprite GetFreezeSprite()
        {
            if (_freezeSprite == null)
            {
                try
                {
                    _freezeSprite = ModContent.GetSprite<BloonsArchipelago>("freeze");
                }
                catch (Exception ex)
                {
                    MelonLogger.Warning($"[BloonsArchipelago] Could not load freeze sprite: {ex.Message}");
                }
            }
            return _freezeSprite;
        }

        public static void ActivateFreeze()
        {
            var inGame = InGame.instance;
            if (inGame == null) return;

            if (_freezeActive)
            {
                _freezeEndTime += 7.5f;
            }
            else
            {
                _freezeActive = true;
                _freezeEndTime = Time.time + 7.5f;
            }

            FreezeAllTowers();
        }

        public static void Update()
        {
            while (PendingFreezeCount > 0)
            {
                PendingFreezeCount--;
                ActivateFreeze();
            }

            if (!_freezeActive) return;

            if (Time.time >= _freezeEndTime)
            {
                UnfreezeAllTowers();
                _freezeActive = false;
            }
        }

        private static void FreezeAllTowers()
        {
            var inGame = InGame.instance;
            if (inGame == null) return;

            foreach (var tower in inGame.GetTowers())
            {
                try
                {
                    if (tower == null) continue;
                    FreezeSingleTower(tower);
                }
                catch (Exception ex)
                {
                    MelonLogger.Warning($"[BloonsArchipelago] Error freezing individual tower: {ex.Message}");
                }
            }
        }

        public static void FreezeSingleTower(Tower tower)
        {
            if (tower == null) return;

            // Ice Monkey and Silas are immune
            if (tower.towerModel?.baseId == "IceMonkey") return;
            if (tower.towerModel?.baseId == "Silas") return;

            int id = tower.Id.GetHashCode();
            if (_frozenTowers.Contains(id)) return;

            _frozenTowers.Add(id);

            try
            {
                tower.entity.isPaused = true;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[BloonsArchipelago] Could not pause tower entity: {ex.Message}");
            }

            try
            {
                AddOverlayToTower(tower, id);
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[BloonsArchipelago] Could not add freeze overlay: {ex.Message}");
            }
        }

        private static void AddOverlayToTower(Tower tower, int id)
        {
            if (_overlays.ContainsKey(id)) return;

            var sprite = GetFreezeSprite();
            if (sprite == null) return;

            try
            {
                Transform displayTransform = null;
                try
                {
                    displayTransform = tower.Node?.graphic?.transform;
                }
                catch { }

                Vector3 worldPos;
                if (displayTransform != null)
                {
                    worldPos = displayTransform.position;
                }
                else
                {
                    var pos = tower.Position;
                    if (pos == null) return;
                    worldPos = new Vector3(pos.X, pos.Y, 0f);
                }

                var overlayGo = new GameObject("FreezeOverlay");

                var cam = Camera.main;
                if (cam != null)
                {
                    overlayGo.transform.position = worldPos - cam.transform.forward * 10f + new Vector3(0f, 0.4f, 0f);
                    overlayGo.transform.rotation = cam.transform.rotation;
                }
                else
                {
                    overlayGo.transform.position = worldPos + new Vector3(0f, 0.4f, 0f);
                }

                overlayGo.transform.localScale = new Vector3(0.25f, 0.25f, 1f);

                var sr = overlayGo.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sortingLayerName = "TowerGraphic";
                sr.sortingOrder = 9999;

                _overlays[id] = overlayGo;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[BloonsArchipelago] Error creating overlay: {ex.Message}");
            }
        }

        public static void UnfreezeAllTowers()
        {
            var inGame = InGame.instance;
            if (inGame != null)
            {
                foreach (var tower in inGame.GetTowers())
                {
                    try
                    {
                        if (tower == null) continue;
                        int id = tower.Id.GetHashCode();
                        if (_frozenTowers.Contains(id))
                            tower.entity.isPaused = false;
                    }
                    catch { }
                }
            }

            CleanupAll();
        }

        public static void CleanupAll()
        {
            var inGame = InGame.instance;
            if (inGame != null && _frozenTowers.Count > 0)
            {
                foreach (var tower in inGame.GetTowers())
                {
                    try
                    {
                        if (tower == null) continue;
                        int id = tower.Id.GetHashCode();
                        if (_frozenTowers.Contains(id))
                            tower.entity.isPaused = false;
                    }
                    catch { }
                }
            }

            foreach (var kvp in _overlays)
            {
                try
                {
                    if (kvp.Value != null)
                        UnityEngine.Object.Destroy(kvp.Value);
                }
                catch { }
            }

            _overlays.Clear();
            _frozenTowers.Clear();
            _freezeActive = false;
        }
    }
}
