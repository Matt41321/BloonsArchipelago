using HarmonyLib;
using Il2CppTMPro;
using MelonLoader;
using System;
using UnityEngine;
using UnityEngine.UI;
using Simulation = Il2CppAssets.Scripts.Simulation.Simulation;

namespace BloonsArchipelago.Patches.InMap
{
    [HarmonyPatch(typeof(Simulation), nameof(Simulation.Simulate))]
    internal class SimulationFreezeChaosPatch
    {
        [HarmonyPrefix]
        private static bool Prefix()
        {
            return !ChaosTrapManager.FreezeActive;
        }
    }

    internal static class ChaosTrapManager
    {
        public static volatile int PendingChaosCount = 0;

        public static bool FreezeActive { get; private set; }

        private enum ChaosState { Idle, Frozen }
        private static ChaosState _state = ChaosState.Idle;

        private const float FREEZE_TIME = 5f;

        private static float _phaseEnd;

        private static GameObject _canvasGo;
        private static Image _filterImg;
        private static TextMeshProUGUI _bigTMP;

        private static TMP_FontAsset _cachedFont;
        private static bool          _fontSearched;

        private static TMP_FontAsset GetFont()
        {
            if (_cachedFont != null) return _cachedFont;
            if (_fontSearched) return null;
            _fontSearched = true;
            try
            {
                var fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
                foreach (var f in fonts)
                {
                    if (f == null) continue;
                    if (f.name.Contains("LuckiestGuy") || f.name.Contains("Luckiest") ||
                        f.name.ToLower().Contains("btd") || f.name.ToLower().Contains("bloons"))
                        return _cachedFont = f;
                }
                if (fonts.Length > 0) _cachedFont = fonts[0];
            }
            catch (Exception ex) { MelonLogger.Warning($"[ChaosTrap] Font search: {ex.Message}"); }
            return _cachedFont;
        }

        public static void Update()
        {
            switch (_state)
            {
                case ChaosState.Idle:
                    if (PendingChaosCount > 0)
                    {
                        PendingChaosCount--;
                        try { StartFreeze(); }
                        catch (Exception ex) { MelonLogger.Warning($"[ChaosTrap] Error: {ex}"); }
                    }
                    break;

                case ChaosState.Frozen:
                    TickFilter(intensity: 0.35f);
                    if (Time.unscaledTime >= _phaseEnd)
                    {
                        EndChaos();
                    }
                    break;
            }
        }

        public static void CleanupAll()
        {
            PendingChaosCount = 0;
            FreezeActive = false;
            _state = ChaosState.Idle;
            _fontSearched = false;
            _cachedFont = null;
            DestroyOverlay();
        }

        private static void StartFreeze()
        {
            BuildOverlay();
            FreezeActive = true;
            _state = ChaosState.Frozen;
            _phaseEnd = Time.unscaledTime + FREEZE_TIME;
            MelonLogger.Msg("[ChaosTrap] CHAOS CONTROL — simulation frozen!");
        }

        private static void EndChaos()
        {
            FreezeActive = false;
            _state = ChaosState.Idle;
            DestroyOverlay();
            MelonLogger.Msg("[ChaosTrap] Chaos over — simulation resumed.");
        }

        // Transparent white screen filter
        private static void TickFilter(float intensity)
        {
            if (_filterImg == null) return;
            Color c = Color.white;
            c.a = intensity + Mathf.Sin(Time.unscaledTime * 6f) * 0.04f;
            _filterImg.color = c;
        }

        private static void BuildOverlay()
        {
            DestroyOverlay();

            _canvasGo = new GameObject("ChaosTrapCanvas");
            UnityEngine.Object.DontDestroyOnLoad(_canvasGo);
            var canvas = _canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9996;
            _canvasGo.AddComponent<CanvasScaler>();

            var font = GetFont();

            var filterGo = UIRect("Filter", _canvasGo, Vector2.zero, Vector2.one);
            _filterImg = filterGo.AddComponent<Image>();
            _filterImg.color = new Color(1f, 1f, 1f, 0.2f);
            _filterImg.raycastTarget = false;

            // Big centered "Chaos Control!" text
            var bigGo = UIRect("Big", _canvasGo, new Vector2(0.1f, 0.30f), new Vector2(0.9f, 0.75f));
            _bigTMP = bigGo.AddComponent<TextMeshProUGUI>();
            if (font != null) _bigTMP.font = font;
            _bigTMP.text = "Chaos Control!";
            _bigTMP.fontSize = 90;
            _bigTMP.fontStyle = FontStyles.Bold;
            _bigTMP.alignment = TextAlignmentOptions.Center;
            _bigTMP.color = Color.white;
            _bigTMP.enableWordWrapping = true;
            _bigTMP.raycastTarget = false;
        }

        private static GameObject UIRect(string name, GameObject parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return go;
        }

        private static void DestroyOverlay()
        {
            if (_canvasGo != null)
            {
                UnityEngine.Object.Destroy(_canvasGo);
                _canvasGo = null;
            }
            _filterImg = null;
            _bigTMP = null;
        }
    }
}
