using BTD_Mod_Helper.Extensions;
using HarmonyLib;
using Il2CppAssets.Scripts.Models.Towers.Weapons;
using Il2CppAssets.Scripts.Simulation.Towers;
using Il2CppAssets.Scripts.Simulation.Towers.Weapons;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace BloonsArchipelago.Patches.InMap
{
    internal static class MonkeyBoostManager
    {
        private const float FRAME_DURATION = 0.05f;
        private const float BOOST_DURATION = 10f;
        private const float OVERLAY_SIZE_FRACTION = 0.1f; 

        private static float _boostEndTime = 0f;
        private static bool _boostActive = false;

        private class TowerOverlay
        {
            public GameObject go = null!;
            public RectTransform rt = null!;
            public Image img = null!;
            public Vector3 worldPos;
        }

        private static readonly Dictionary<int, TowerOverlay> _overlays = new();
        private static readonly HashSet<int> _boostedTowers = new();

        private static GameObject? _canvasGo = null;

        private static Sprite[]? _frames = null;
        private static int _frameIndex = 0;
        private static float _frameTimer = 0f;

        public static bool IsBoostActive => _boostActive;
        public static volatile int PendingBoostCount = 0;

        private static Sprite[] LoadFrames()
        {
            var assembly = typeof(BloonsArchipelago).Assembly;
            var frameNames = assembly.GetManifestResourceNames()
                .Where(n => n.Contains("frame_") && n.EndsWith(".png"))
                .OrderBy(n => n)
                .ToArray();

            MelonLogger.Msg($"[MonkeyBoost] Found {frameNames.Length} frame(s): {string.Join(", ", frameNames)}");

            var sprites = new List<Sprite>();
            foreach (var name in frameNames)
            {
                try
                {
                    using var stream = assembly.GetManifestResourceStream(name);
                    if (stream == null) { MelonLogger.Warning($"[MonkeyBoost] Stream null for {name}"); continue; }

                    var bytes = new byte[stream.Length];
                    stream.Read(bytes, 0, bytes.Length);

                    var tex = new Texture2D(2, 2);
                    ImageConversion.LoadImage(tex, bytes);
                    sprites.Add(Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f)));
                }
                catch (Exception ex) { MelonLogger.Warning($"[MonkeyBoost] Failed to load frame {name}: {ex.Message}"); }
            }

            return sprites.ToArray();
        }

        private static Sprite[]? GetFrames()
        {
            if (_frames == null)
                _frames = LoadFrames();
            return _frames;
        }

        private static void EnsureCanvas()
        {
            if (_canvasGo != null) return;
            _canvasGo = new GameObject("MonkeyBoostCanvas");
            UnityEngine.Object.DontDestroyOnLoad(_canvasGo);
            var canvas = _canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9998;
            _canvasGo.AddComponent<CanvasScaler>();
        }

        public static void Update()
        {

            GetFrames();

            while (PendingBoostCount > 0)
            {
                PendingBoostCount--;
                _boostEndTime = Mathf.Max(_boostActive ? _boostEndTime : Time.time, Time.time) + BOOST_DURATION;
                if (!_boostActive)
                {
                    _boostActive = true;
                    BoostAllTowers();
                }
                MelonLogger.Msg($"[MonkeyBoost] Activated — ends at t={_boostEndTime:F1}");
            }

            if (!_boostActive) return;

            if (Time.time >= _boostEndTime)
            {
                _boostActive = false;
                RemoveAllOverlays();
                MelonLogger.Msg("[MonkeyBoost] Expired.");
                return;
            }

            var frames = GetFrames();
            if (frames != null && frames.Length > 0)
            {
                _frameTimer += Time.deltaTime;
                if (_frameTimer >= FRAME_DURATION)
                {
                    _frameTimer -= FRAME_DURATION;
                    _frameIndex = (_frameIndex + 1) % frames.Length;
                    var currentFrame = frames[_frameIndex];
                    foreach (var overlay in _overlays.Values)
                    {
                        try { if (overlay.img != null) overlay.img.sprite = currentFrame; }
                        catch { }
                    }
                }
            }

            var cam = Camera.main;
            if (cam == null) return;

            float size = Screen.height * OVERLAY_SIZE_FRACTION;
            foreach (var overlay in _overlays.Values)
            {
                try
                {
                    if (overlay.rt == null) continue;
                    var screenPos = cam.WorldToScreenPoint(overlay.worldPos);
                    overlay.rt.sizeDelta = new Vector2(size, size);
                    overlay.rt.anchoredPosition = new Vector2(
                        screenPos.x - Screen.width * 0.5f,
                        screenPos.y - Screen.height * 0.5f
                    );
                }
                catch { }
            }
        }

        private static void BoostAllTowers()
        {
            var inGame = InGame.instance;
            if (inGame == null) return;
            var towers = inGame.GetTowers();
            MelonLogger.Msg($"[MonkeyBoost] BoostAllTowers — found {towers.Count} towers");
            foreach (var tower in towers)
            {
                try { if (tower != null) BoostSingleTower(tower); }
                catch (Exception ex) { MelonLogger.Warning($"[MonkeyBoost] Error boosting tower: {ex.Message}"); }
            }
        }


        private static bool IsNecromancerOrPrinceOfDarkness(Tower tower)
        {
            try
            {
                var model = tower?.towerModel;
                if (model == null) return false;
                if (model.baseId != "WizardMonkey") return false;

                string name = model.name ?? "";
                if (name.Length >= 3)
                {
                    char botPathChar = name[name.Length - 1];
                    if (botPathChar >= '4' && botPathChar <= '9') return true;
                    if (botPathChar >= '0' && botPathChar <= '3') return false;
                }

                try
                {
                    var tiers = model.tiers;
                    return tiers != null && tiers.Length > 2 && tiers[2] >= 4;
                }
                catch
                {
                    return true;
                }
            }
            catch { return false; }
        }

        public static void BoostSingleTower(Tower tower)
        {
            if (tower == null) return;
            if (IsNecromancerOrPrinceOfDarkness(tower)) return;
            int id = tower.Id.GetHashCode();
            if (_boostedTowers.Contains(id)) return;
            _boostedTowers.Add(id);
            AddOverlayToTower(tower, id);
        }

        private static void AddOverlayToTower(Tower tower, int id)
        {
            if (_overlays.ContainsKey(id)) return;

            var frames = GetFrames();
            var firstFrame = (frames != null && frames.Length > 0) ? frames[0] : null;
            if (firstFrame == null) { MelonLogger.Warning("[MonkeyBoost] No frames loaded — skipping overlay"); return; }

            try
            {
                Vector3 worldPos;
                try
                {
                    var graphicTransform = tower.Node?.graphic?.transform;
                    worldPos = graphicTransform != null
                        ? graphicTransform.position
                        : new Vector3(tower.Position.X, tower.Position.Y, 0f);
                }
                catch
                {
                    var pos = tower.Position;
                    if (pos == null) return;
                    worldPos = new Vector3(pos.X, pos.Y, 0f);
                }

                EnsureCanvas();

                float size = Screen.height * OVERLAY_SIZE_FRACTION;
                var cam = Camera.main;
                var screenPos = cam != null
                    ? cam.WorldToScreenPoint(worldPos)
                    : new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);

                var overlayGo = new GameObject("BoostOverlay");
                overlayGo.transform.SetParent(_canvasGo!.transform, false);

                var rt = overlayGo.AddComponent<RectTransform>();
                rt.sizeDelta = new Vector2(size, size);
                rt.anchoredPosition = new Vector2(
                    screenPos.x - Screen.width * 0.5f,
                    screenPos.y - Screen.height * 0.5f
                );

                var img = overlayGo.AddComponent<Image>();
                img.sprite = firstFrame;
                img.preserveAspect = true;
                img.color = new Color(1f, 1f, 1f, 0.5f);

                _overlays[id] = new TowerOverlay { go = overlayGo, rt = rt, img = img, worldPos = worldPos };
                MelonLogger.Msg($"[MonkeyBoost] Overlay created at screen ({screenPos.x:F0},{screenPos.y:F0}) world {worldPos}");
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[MonkeyBoost] Error creating overlay: {ex.Message}");
            }
        }

        private static void RemoveAllOverlays()
        {
            foreach (var overlay in _overlays.Values)
            {
                try { if (overlay.go != null) UnityEngine.Object.Destroy(overlay.go); }
                catch { }
            }
            _overlays.Clear();
            _boostedTowers.Clear();
            _frameIndex = 0;
            _frameTimer = 0f;

            MonkeyBoostWeaponPatch.ClearWeaponBoostCache();

            if (_canvasGo != null)
            {
                UnityEngine.Object.Destroy(_canvasGo);
                _canvasGo = null;
            }
        }

        public static bool IsTowerBoosted(int id) => _boostedTowers.Contains(id);

        public static void CleanupAll()
        {
            _boostActive = false;
            _boostEndTime = 0f;
            PendingBoostCount = 0;
            RemoveAllOverlays();
        }
    }


    [HarmonyPatch(typeof(Weapon), nameof(Weapon.Process))]
    internal static class MonkeyBoostWeaponPatch
    {
        private static readonly Dictionary<IntPtr, float> _savedRates = new();


        private static readonly Dictionary<IntPtr, bool> _zombieCache = new();

        private static readonly Dictionary<IntPtr, bool> _weaponShouldBoostCache = new();

        public static void ClearWeaponBoostCache() => _weaponShouldBoostCache.Clear();

        private static bool ShouldBoostWeapon(Weapon weapon)
        {
            var ptr = weapon.Pointer;
            if (_weaponShouldBoostCache.TryGetValue(ptr, out bool cached)) return cached;
            bool result = ComputeShouldBoostWeapon(weapon);
            _weaponShouldBoostCache[ptr] = result;
            return result;
        }

        private static bool ComputeShouldBoostWeapon(Weapon weapon)
        {
            try
            {
                var tower = weapon.attack?.tower;
                if (tower == null) return false;
                return MonkeyBoostManager.IsTowerBoosted(tower.Id.GetHashCode());
            }
            catch { return false; }
        }


        private static bool IsZombieRaisingWeapon(Weapon weapon)
        {
            var ptr = weapon.Pointer;
            if (_zombieCache.TryGetValue(ptr, out bool cached)) return cached;
            bool result = ComputeIsZombieRaisingWeapon(weapon);
            _zombieCache[ptr] = result;
            return result;
        }

        private static bool ComputeIsZombieRaisingWeapon(Weapon weapon)
        {
            try
            {
                var weaponName = weapon.weaponModel?.name ?? "";
                var attackName = weapon.attack?.attackModel?.name ?? "";
                string combined = weaponName + "|" + attackName;

                if (combined.IndexOf("zombie", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    combined.IndexOf("necro", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    combined.IndexOf("undead", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    combined.IndexOf("raiseDead", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    combined.IndexOf("graveyard", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;


                var model = weapon.attack?.tower?.towerModel;
                if (model == null || model.baseId != "WizardMonkey") return false;

                string towerName = model.name ?? "";
                if (towerName.Length >= 3)
                {
                    char botPathChar = towerName[towerName.Length - 1];
                    if (botPathChar >= '4' && botPathChar <= '9') return true;
                    if (botPathChar >= '0' && botPathChar <= '3') return false;
                }

                try
                {
                    var tiers = model.tiers;
                    return tiers != null && tiers.Length > 2 && tiers[2] >= 4;
                }
                catch
                {
                    return true;
                }
            }
            catch { return false; }
        }

        [HarmonyPrefix]
        private static void Prefix(Weapon __instance)
        {
            if (!MonkeyBoostManager.IsBoostActive) return;
            try
            {
                if (IsZombieRaisingWeapon(__instance)) return;


                if (!ShouldBoostWeapon(__instance)) return;

                var model = __instance.weaponModel;
                if (model == null) return;

                if (_savedRates.TryAdd(__instance.Pointer, model.rate))
                    model.rate = model.rate / 2.0f;
            }
            catch { }
        }

        [HarmonyPostfix]
        private static void Postfix(Weapon __instance)
        {
            try
            {
                if (!_savedRates.TryGetValue(__instance.Pointer, out float saved)) return;
                _savedRates.Remove(__instance.Pointer);
                var model = __instance.weaponModel;
                if (model != null) model.rate = saved;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[MonkeyBoost] Postfix error: {ex.Message}");
            }
        }
    }

}
