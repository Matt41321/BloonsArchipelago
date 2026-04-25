using BTD_Mod_Helper.Api;
using BloonsArchipelago.Utils;
using Il2CppTMPro;
using MelonLoader;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BloonsArchipelago.Patches.InMap
{
    internal static class APNotificationPanel
    {
        private static GameObject _canvasGo;
        private static readonly List<APNotifCard> _cards = new();

        private static Sprite _logoSprite;
        private static TMP_FontAsset _cachedFont;
        private static bool _fontSearched;

        private static float CardW  => Screen.height * 0.315f;
        private static float CardH  => Screen.height * 0.066f;
        private static float CardGap => Screen.height * 0.007f;
        private const  float Margin  = 18f;
        private const  int   MaxCards = 6;


        public static void Show(APNotification notif)
        {
            EnsureCanvas();

            if (_cards.Count >= MaxCards)
                _cards[0].ForceExpire();

            float w = CardW;
            float h = CardH;

            float yOffset = Margin + _cards.Count * (h + CardGap);

            _cards.Add(new APNotifCard(_canvasGo, notif, yOffset, w, h, GetLogo(), GetFont()));
        }

        public static void Update()
        {
            bool anyRemoved = false;
            for (int i = _cards.Count - 1; i >= 0; i--)
            {
                if (_cards[i].Update())
                {
                    _cards[i].Destroy();
                    _cards.RemoveAt(i);
                    anyRemoved = true;
                }
            }

            if (anyRemoved)
                RestackCards();
        }

        private static void RestackCards()
        {
            float h = CardH;
            float gap = CardGap;
            for (int i = 0; i < _cards.Count; i++)
                _cards[i].SetTargetY(Margin + i * (h + gap));
        }

        public static void CleanupAll()
        {
            foreach (var c in _cards) c.Destroy();
            _cards.Clear();

            if (_canvasGo != null)
            {
                UnityEngine.Object.Destroy(_canvasGo);
                _canvasGo = null;
            }

            _logoSprite = null;
            _cachedFont = null;
            _fontSearched = false;
        }


        private static void EnsureCanvas()
        {
            if (_canvasGo != null) return;

            _canvasGo = new GameObject("APNotificationCanvas");
            UnityEngine.Object.DontDestroyOnLoad(_canvasGo);

            var canvas = _canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9998;

            var scaler = _canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        }

        private static Sprite GetLogo()
        {
            if (_logoSprite != null) return _logoSprite;
            try { _logoSprite = ModContent.GetSprite<BloonsArchipelago>("ArchipelagoLogo", 50); }
            catch (Exception ex) { MelonLogger.Warning($"[APNotif] Logo load failed: {ex.Message}"); }
            return _logoSprite;
        }

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
                    if (f.name.Contains("LuckiestGuy") || f.name.Contains("Luckiest"))
                    {
                        _cachedFont = f;
                        return _cachedFont;
                    }
                }
                if (fonts.Length > 0 && fonts[0] != null)
                    _cachedFont = fonts[0];
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[APNotif] Font search failed: {ex.Message}");
            }
            return _cachedFont;
        }
    }


    internal class APNotifCard
    {
        private readonly GameObject _go;
        private readonly RectTransform _rt;
        private float _lifetime;
        private bool _destroyed;
        private float _targetY;

        // Timing
        private const float SlideTime   = 0.22f;
        private const float DisplayTime = 5.5f;
        private const float FadeTime    = 0.35f;

        // Slide positions
        private readonly float _hiddenX;  
        private readonly float _shownX;  

        // Fade targets 
        private Image            _bg;
        private Image            _accentBar;
        private Image            _logoImg;
        private TextMeshProUGUI  _titleTmp;
        private TextMeshProUGUI  _subtitleTmp;
        private Text             _titleFallback;
        private Text             _subtitleFallback;

        public APNotifCard(
            GameObject canvas,
            APNotification notif,
            float yOffset,
            float w, float h,
            Sprite logo,
            TMP_FontAsset font)
        {
            _go = new GameObject("APNotifCard");
            _go.transform.SetParent(canvas.transform, false);

            _rt = _go.AddComponent<RectTransform>();
            _rt.anchorMin = new Vector2(1f, 0f);
            _rt.anchorMax = new Vector2(1f, 0f);
            _rt.pivot     = new Vector2(1f, 0f);
            _rt.sizeDelta = new Vector2(w, h);

            float margin = 18f;
            _shownX  = -margin;
            _hiddenX =  w + 10f;   

            _targetY = yOffset;
            _rt.anchoredPosition = new Vector2(_hiddenX, yOffset);

            try   { BuildUI(notif, w, h, logo, font); }
            catch (Exception ex) { MelonLogger.Warning($"[APNotif] Card build error: {ex.Message}"); }
        }



        private void BuildUI(APNotification notif, float w, float h, Sprite logo, TMP_FontAsset font)
        {
            float pad = h * 0.13f;

            // Background
            _bg = _go.AddComponent<Image>();
            _bg.color = new Color(0.04f, 0.06f, 0.12f, 0.92f);

            // Left accent bar
            var barGo = new GameObject("AccentBar");
            barGo.transform.SetParent(_go.transform, false);
            var barRt = barGo.AddComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0f, 0f);
            barRt.anchorMax = new Vector2(0f, 1f);
            barRt.pivot     = new Vector2(0f, 0.5f);
            barRt.sizeDelta = new Vector2(4f, 0f);  
            barRt.anchoredPosition = Vector2.zero;
            _accentBar = barGo.AddComponent<Image>();
            _accentBar.color = notif.IsOutgoing ? notif.ItemColor : CategoryColor(notif.Category);

            // AP logo 
            float logoSz = h * 0.62f;
            float logoCx = 4f + pad + logoSz * 0.5f;

            var logoGo = new GameObject("Logo");
            logoGo.transform.SetParent(_go.transform, false);
            var logoRt = logoGo.AddComponent<RectTransform>();
            logoRt.anchorMin = new Vector2(0f, 0.5f);
            logoRt.anchorMax = new Vector2(0f, 0.5f);
            logoRt.pivot     = new Vector2(0.5f, 0.5f);
            logoRt.sizeDelta = new Vector2(logoSz, logoSz);
            logoRt.anchoredPosition = new Vector2(logoCx, 0f);

            _logoImg = logoGo.AddComponent<Image>();
            if (logo != null)
            {
                _logoImg.sprite = logo;
                _logoImg.preserveAspect = true;
            }
            else
            {
                _logoImg.color = Color.clear;
            }

            // Text 
            float textLeft  = 4f + pad + logoSz + pad * 0.8f;
            float rightPad  = pad;

            Color catColor = CategoryColor(notif.Category);
            string titleStr;
            if (notif.IsOutgoing)
            {
                // Outgoing send
                Color ic = notif.ItemColor;
                string ihex = $"{(int)(ic.r * 255):X2}{(int)(ic.g * 255):X2}{(int)(ic.b * 255):X2}";
                titleStr = $"<color=#{ihex}>{notif.ItemName}</color>";
            }
            else
            {
                string hex = $"{(int)(catColor.r * 255):X2}{(int)(catColor.g * 255):X2}{(int)(catColor.b * 255):X2}";
                titleStr = $"<color=#{hex}>[{notif.Category}]</color> {notif.ItemName}";
            }

            if (font != null)
            {
                // Title
                var titleGo = new GameObject("Title");
                titleGo.transform.SetParent(_go.transform, false);
                var titleRt = titleGo.AddComponent<RectTransform>();
                titleRt.anchorMin  = new Vector2(0f, 0.44f);
                titleRt.anchorMax  = new Vector2(1f, 1f);
                titleRt.offsetMin  = new Vector2(textLeft,  0f);
                titleRt.offsetMax  = new Vector2(-rightPad, -pad * 0.3f);

                _titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
                _titleTmp.font              = font;
                _titleTmp.text              = titleStr;
                _titleTmp.fontSize          = h * 0.221f;
                _titleTmp.color             = Color.white;
                _titleTmp.richText          = true;
                _titleTmp.enableWordWrapping = false;
                _titleTmp.overflowMode      = TextOverflowModes.Ellipsis;
                _titleTmp.raycastTarget     = false;

                // Subtitle 
                var subGo = new GameObject("Subtitle");
                subGo.transform.SetParent(_go.transform, false);
                var subRt = subGo.AddComponent<RectTransform>();
                subRt.anchorMin  = new Vector2(0f, 0f);
                subRt.anchorMax  = new Vector2(1f, 0.44f);
                subRt.offsetMin  = new Vector2(textLeft, pad * 0.25f);
                subRt.offsetMax  = new Vector2(-rightPad, 0f);

                _subtitleTmp = subGo.AddComponent<TextMeshProUGUI>();
                _subtitleTmp.font              = font;
                _subtitleTmp.text              = notif.From;
                _subtitleTmp.fontSize          = h * 0.169f;
                _subtitleTmp.color             = new Color(0.72f, 0.72f, 0.82f, 1f);
                _subtitleTmp.enableWordWrapping = false;
                _subtitleTmp.overflowMode       = TextOverflowModes.Ellipsis;
                _subtitleTmp.raycastTarget      = false;
            }
            else
            {
                var titleGo = new GameObject("Title");
                titleGo.transform.SetParent(_go.transform, false);
                var titleRt = titleGo.AddComponent<RectTransform>();
                titleRt.anchorMin = new Vector2(0f, 0.44f);
                titleRt.anchorMax = new Vector2(1f, 1f);
                titleRt.offsetMin = new Vector2(textLeft,  0f);
                titleRt.offsetMax = new Vector2(-rightPad, -pad * 0.3f);
                _titleFallback = titleGo.AddComponent<Text>();
                _titleFallback.text             = notif.IsOutgoing ? notif.ItemName : $"[{notif.Category}] {notif.ItemName}";
                _titleFallback.fontSize         = (int)(h * 0.21f);
                _titleFallback.color            = Color.white;
                _titleFallback.horizontalOverflow = HorizontalWrapMode.Overflow;
                _titleFallback.verticalOverflow   = VerticalWrapMode.Overflow;

                var subGo = new GameObject("Subtitle");
                subGo.transform.SetParent(_go.transform, false);
                var subRt = subGo.AddComponent<RectTransform>();
                subRt.anchorMin = new Vector2(0f, 0f);
                subRt.anchorMax = new Vector2(1f, 0.44f);
                subRt.offsetMin = new Vector2(textLeft, pad * 0.25f);
                subRt.offsetMax = new Vector2(-rightPad, 0f);
                _subtitleFallback = subGo.AddComponent<Text>();
                _subtitleFallback.text             = notif.From;
                _subtitleFallback.fontSize         = (int)(h * 0.161f);
                _subtitleFallback.color            = new Color(0.72f, 0.72f, 0.82f, 1f);
                _subtitleFallback.horizontalOverflow = HorizontalWrapMode.Overflow;
                _subtitleFallback.verticalOverflow   = VerticalWrapMode.Overflow;
            }
        }

        public bool Update()
        {
            if (_go == null || _destroyed) return true;

            _lifetime += Time.unscaledDeltaTime;

            var curPos = _rt.anchoredPosition;
            if (!Mathf.Approximately(curPos.y, _targetY))
                _rt.anchoredPosition = new Vector2(curPos.x, Mathf.Lerp(curPos.y, _targetY, Time.unscaledDeltaTime * 12f));

            if (_lifetime < SlideTime)
            {
                float t = _lifetime / SlideTime;
                t = 1f - (1f - t) * (1f - t) * (1f - t);
                float x = Mathf.Lerp(_hiddenX, _shownX, t);
                var pos = _rt.anchoredPosition;
                _rt.anchoredPosition = new Vector2(x, pos.y);
            }
            else if (_lifetime < SlideTime + DisplayTime)
            {
                var pos = _rt.anchoredPosition;
                if (pos.x != _shownX)
                    _rt.anchoredPosition = new Vector2(_shownX, pos.y);
            }
            else
            {
                float progress = (_lifetime - SlideTime - DisplayTime) / FadeTime;
                float alpha = 1f - Mathf.Clamp01(progress);
                SetAlpha(alpha);
                if (progress >= 1f) return true;
            }

            return false;
        }

        public void SetTargetY(float y) => _targetY = y;

        public void ForceExpire()
        {
            _lifetime = SlideTime + DisplayTime;
        }


        private void SetAlpha(float a)
        {
            if (_bg != null)        { var c = _bg.color;        c.a = 0.92f * a; _bg.color = c; }
            if (_accentBar != null) { var c = _accentBar.color; c.a = a;          _accentBar.color = c; }
            if (_logoImg != null)   { var c = _logoImg.color;   c.a = a;          _logoImg.color = c; }

            if (_titleTmp    != null) _titleTmp.alpha    = a;
            if (_subtitleTmp != null) _subtitleTmp.alpha = a;

            if (_titleFallback != null)
            { var c = _titleFallback.color; c.a = a; _titleFallback.color = c; }
            if (_subtitleFallback != null)
            { var c = _subtitleFallback.color; c.a = a * 0.82f; _subtitleFallback.color = c; }
        }

        private static Color CategoryColor(string category)
        {
            return category switch
            {
                "Map Unlock"   => new Color(0.00f, 1.00f, 0.53f),   // green
                "Tower Unlock" => new Color(0.33f, 0.60f, 1.00f),   // blue
                "Hero"         => new Color(1.00f, 0.53f, 0.00f),   // orange
                "Knowledge"    => new Color(1.00f, 0.80f, 0.00f),   // yellow
                "Medal"        => new Color(1.00f, 0.84f, 0.00f),   // gold
                "Trap"         => new Color(1.00f, 0.27f, 0.27f),   // red
                "Filler"       => new Color(0.00f, 0.87f, 1.00f),   // cyan
                "Chat"         => new Color(0.75f, 0.75f, 0.75f),   // light grey
                "Progression"      => new Color(0.78f, 0.55f, 1.00f),   // purple
                "Death"            => new Color(0.65f, 0.10f, 0.10f),   // dark red — DeathLink
                "Upgrade Path"     => new Color(0.50f, 0.85f, 0.50f),   // light green
                _              => Color.white,
            };
        }

        public void Destroy()
        {
            _destroyed = true;
            if (_go != null) UnityEngine.Object.Destroy(_go);
        }
    }
}
