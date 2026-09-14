using BTD_Mod_Helper.Api;
using BTD_Mod_Helper.Api.Enums;
using BTD_Mod_Helper.Extensions;
using Il2CppAssets.Scripts.Unity;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.StoreMenu;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BloonsArchipelago.Patches.InMap
{
    internal static class PathDiamondOverlayManager
    {
        private const float UPDATE_INTERVAL = 0.5f;

        private const string UnlockedSprite = VanillaSprites.MkOnGreen;
        private const string LockedSprite = VanillaSprites.MkOffRed;

        private static readonly string[] PathNames = { "TopPath", "MiddlePath", "BottomPath" };

        // Vertical bands
        private static readonly (float min, float max)[] BadgeBands =
        {
            (0.68f, 0.94f),
            (0.42f, 0.68f),
            (0.16f, 0.42f),
        };

        private class OverlaySet
        {
            public GameObject root = null!;
            public Image[] badges = null!;
            public bool?[] shownUnlocked = new bool?[3];
        }

        private static readonly Dictionary<IntPtr, OverlaySet> _overlays = new();

        private static float _timer;

        private static RectTransform CreateStretchedChild(string name, Transform parent, int layer,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name);
            go.layer = layer;
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        private static OverlaySet CreateOverlay(TowerPurchaseButton button)
        {
            var buttonGo = button.gameObject;
            int layer = buttonGo.layer;
            var rootRt = CreateStretchedChild("APPathDiamonds", buttonGo.transform, layer,
                new Vector2(0.03f, 0f), new Vector2(0.26f, 1f));
            rootRt.SetAsLastSibling();

            var badges = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                var rt = CreateStretchedChild($"{PathNames[i]}Diamond", rootRt, layer,
                    new Vector2(0f, BadgeBands[i].min), new Vector2(1f, BadgeBands[i].max));

                var img = rt.gameObject.AddComponent<Image>();
                img.preserveAspect = true;
                img.raycastTarget = false;
                badges[i] = img;
            }

            return new OverlaySet { root = rootRt.gameObject, badges = badges };
        }

        private static void DestroyOverlay(IntPtr ptr)
        {
            if (!_overlays.TryGetValue(ptr, out var overlay)) return;
            try { if (overlay.root != null) Object.Destroy(overlay.root); } catch { }
            _overlays.Remove(ptr);
        }

        public static void UpdateOverlays()
        {
            try
            {
                float now = Time.unscaledTime;
                if (now - _timer < UPDATE_INTERVAL) return;
                _timer = now;

                var sh = BloonsArchipelago.sessionHandler;
                bool active = sh != null && sh.ready && sh.UpgradeSanityEnabled;
                if (!active)
                {
                    if (_overlays.Count > 0) CleanupAll();
                    return;
                }

                var buttons = Object.FindObjectsOfType<TowerPurchaseButton>();
                var seen = new HashSet<IntPtr>();

                if (buttons != null)
                {
                    foreach (var button in buttons)
                    {
                        if (button == null) continue;
                        IntPtr ptr = button.Pointer;

                        string? baseId = button.isHero ? null : button.towerModel?.baseId;
                        if (string.IsNullOrEmpty(baseId))
                        {
                            DestroyOverlay(ptr);
                            continue;
                        }

                        seen.Add(ptr);

                        if (!_overlays.TryGetValue(ptr, out var overlay) || overlay.root == null)
                        {
                            overlay = CreateOverlay(button);
                            _overlays[ptr] = overlay;
                        }

                        for (int i = 0; i < 3; i++)
                        {
                            bool unlocked = sh!.PathsUnlocked.Contains($"{baseId}-{PathNames[i]}");
                            if (overlay.shownUnlocked[i] == unlocked) continue;

                            overlay.badges[i].SetSprite(
                                Game.instance.CreateSpriteReference(unlocked ? UnlockedSprite : LockedSprite));
                            overlay.shownUnlocked[i] = unlocked;
                        }
                    }
                }

                foreach (var p in _overlays.Keys.Where(p => !seen.Contains(p)).ToList())
                    DestroyOverlay(p);
            }
        }

        public static void CleanupAll()
        {
            foreach (var overlay in _overlays.Values)
            {
                try { if (overlay.root != null) Object.Destroy(overlay.root); } catch { }
            }
            _overlays.Clear();
            _timer = 0f;
        }
    }
}
