using Il2CppTMPro;
using MelonLoader;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BloonsArchipelago.Patches.InMap
{
    internal static class LiteratureTrapManager
    {
        public static volatile int PendingLiteratureCount = 0;

        private static readonly List<LiteratureScroll> _scrolls = new();
        private static GameObject _canvasGo = null;

        private static readonly string[] _passages = new[]
        {
            "For God so loved the world that he gave his one and only Son, that whoever believes in him shall not perish but have eternal life.",
            "The Lord is my shepherd; I shall not want. He makes me lie down in green pastures. He leads me beside still waters. He restores my soul.",
            "Trust in the Lord with all your heart and lean not on your own understanding; in all your ways submit to him, and he will make your paths straight.",
            "Love is patient, love is kind. It does not envy, it does not boast, it is not proud. It does not dishonor others, it is not self-seeking, it is not easily angered, it keeps no record of wrongs.",
            "To be, or not to be, that is the question: Whether 'tis nobler in the mind to suffer the slings and arrows of outrageous fortune, or to take arms against a sea of troubles and by opposing end them.",
            "All the world's a stage, and all the men and women merely players; they have their exits and their entrances, and one man in his time plays many parts.",
            "Victorious warriors win first and then go to war, while defeated warriors go to war first and then seek to win.",
            "It is a truth universally acknowledged, that a single man in possession of a good fortune, must be in want of a wife. However little known the feelings or views of such a man may be on his first entering a neighbourhood, this truth is so well fixed in the minds of the surrounding families, that he is considered the rightful property of someone or other of their daughters. ‘My dear Mr. Bennet,’ said his lady to him one day, ‘have you heard that Netherfield Park is let at last?",
            "Call me Ishmael. Some years ago—never mind how long precisely—having little or no money in my purse, and nothing particular to interest me on shore, I thought I would sail about a little and see the watery part of the world. It is a way I have of driving off the spleen and regulating the circulation. Whenever I find myself growing grim about the mouth; whenever it is a damp, drizzly November in my soul; then, I account it high time to get to sea as soon as I can.",
            "It was the best of times, it was the worst of times, it was the age of wisdom, it was the age of foolishness, it was the epoch of belief, it was the epoch of incredulity, it was the season of Light, it was the season of Darkness, it was the spring of hope, it was the winter of despair. We had everything before us, we had nothing before us, we were all going direct to Heaven, we were all going direct the other way.",
            "On an exceptionally hot evening early in July a young man came out of the garret in which he lodged in S. Place and walked slowly, as though in hesitation, towards K. bridge. He had successfully avoided meeting his landlady on the staircase. His garret was under the roof of a high, five-storied house and was more like a cupboard than a room. The landlady who provided him with garret, dinners, and attendance, lived on the floor below.",
            "There’s a passage I memorized—about walking the earth, about tyranny and evil men. I used to think it was about righteous men standing up against the wicked, but I’ve been thinking lately… maybe it’s about the way people try to justify what they do. Maybe I’m the tyranny. Maybe you’re the weak. Or maybe I’m just trying real hard to be the shepherd. You see, the world is messy, full of people doing wrong, and every choice feels like it puts you on one side or the other.",
            "So we’re doing this today—yes, this was a mistake, but we’re already here. You ever just start something and immediately regret it? That’s this. But we commit. That’s what we do. We don’t back out. We push forward, even when everything is telling us this is a terrible idea. And somehow, against all odds, it works out… or it doesn’t, and that’s also content. Either way, you clicked, you’re watching, so technically, I’ve already won.",
            "01000010 01101111 01100100 01100101 01101110 00100000 01000010 01101111 01100100 01100101 01101110 00100000 01000010 01101111 01100100 01100101 01101110 00100000 01000010 01101111 01100100 01100101 01101110 00100000 01000010 01101111 01100100 01100101 01101110 00100000 01000010 01101111 01100100 01100101 01101110",
            "So, you’re telling me you’ve never had a burger with cheese on it? Like, how does that even happen? Do you live under a rock? Like, I get it, you’re special, but come on. You can’t just skip the basics of food and pretend like it’s no big deal. It’s not about being fancy, it’s just... it’s a burger with cheese. That’s it. There’s no philosophical depth here, just cheese on a burger.",
            "He Who Would Climb a Ladder Must Begin at the Bottom",
            "If there were a place that shone brighter than where you are, would you go see it? Or just keep staring?",
            "And when you pray, do not keep on babbling like pagans, for they think they will be heard because of their many words.",
        };

        private static readonly System.Random _rng = new();
        private static TMP_FontAsset _cachedFont = null;
        private static bool _fontSearched = false;

        private static TMP_FontAsset GetFont()
        {
            if (_cachedFont != null) return _cachedFont;
            if (_fontSearched) return null;
            _fontSearched = true;
            try
            {
                var allFonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
                MelonLogger.Msg($"[LiteratureTrap] Found {allFonts.Length} TMP fonts");
                foreach (var f in allFonts)
                {
                    if (f == null) continue;
                    MelonLogger.Msg($"[LiteratureTrap]   font: {f.name}");
                    if (f.name.Contains("LuckiestGuy") || f.name.Contains("Luckiest") ||
                        f.name.ToLower().Contains("btd") || f.name.ToLower().Contains("bloons"))
                    {
                        _cachedFont = f;
                        MelonLogger.Msg($"[LiteratureTrap] Using font: {f.name}");
                        return _cachedFont;
                    }
                }
                if (allFonts.Length > 0 && allFonts[0] != null)
                {
                    _cachedFont = allFonts[0];
                    MelonLogger.Msg($"[LiteratureTrap] Fallback font: {_cachedFont.name}");
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[LiteratureTrap] Font search failed: {ex.Message}");
            }
            return _cachedFont;
        }

        private static void EnsureCanvas()
        {
            if (_canvasGo != null) return;

            _canvasGo = new GameObject("LiteratureTrapCanvas");
            UnityEngine.Object.DontDestroyOnLoad(_canvasGo);

            var canvas = _canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10000;

            var scaler = _canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

            MelonLogger.Msg($"[LiteratureTrap] Canvas created. Screen={Screen.width}x{Screen.height}");
        }

        public static void Update()
        {
            while (PendingLiteratureCount > 0)
            {
                PendingLiteratureCount--;
                MelonLogger.Msg("[LiteratureTrap] Activating scroll");
                ActivateScroll();
            }

            for (int i = _scrolls.Count - 1; i >= 0; i--)
            {
                if (_scrolls[i].Update())
                    _scrolls.RemoveAt(i);
            }
        }

        private static void ActivateScroll()
        {
            try
            {
                EnsureCanvas();
                string passage = _passages[_rng.Next(_passages.Length)];
                var font = GetFont();
                MelonLogger.Msg($"[LiteratureTrap] Creating scroll, font={(font != null ? font.name : "null")}");
                var scroll = new LiteratureScroll(_canvasGo, passage, font);
                _scrolls.Add(scroll);
                MelonLogger.Msg($"[LiteratureTrap] Scroll added. Count={_scrolls.Count}");
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[LiteratureTrap] ActivateScroll error: {ex.Message}\n{ex.StackTrace}");
            }
        }

        public static void CleanupAll()
        {
            foreach (var s in _scrolls)
                s.Destroy();
            _scrolls.Clear();
            _fontSearched = false;
            _cachedFont = null;

            if (_canvasGo != null)
            {
                UnityEngine.Object.Destroy(_canvasGo);
                _canvasGo = null;
            }
        }
    }

    internal class LiteratureScroll
    {
        private readonly GameObject _go;
        private readonly RectTransform _rt;
        private float _time = 0f;

        private readonly float _startY;
        private readonly float _endY;
        private const float DURATION = 85.7f; 

        public LiteratureScroll(GameObject canvas, string text, TMP_FontAsset font)
        {
            float w = Screen.width;
            float h = Screen.height;

            _go = new GameObject("LiteratureText");
            _go.transform.SetParent(canvas.transform, false);

            _rt = _go.AddComponent<RectTransform>();


            _rt.anchorMin = new Vector2(0.5f, 0.5f);
            _rt.anchorMax = new Vector2(0.5f, 0.5f);
            _rt.pivot     = new Vector2(0.5f, 1f);   


            float boxHeight = h * 8f;
            _rt.sizeDelta = new Vector2(w * 0.9f, boxHeight);


            _startY = -(h * 0.5f);
            _endY   =   h * 0.5f + boxHeight;

            _rt.anchoredPosition = new Vector2(0f, _startY);

            if (font != null)
            {
                var tmp = _go.AddComponent<TextMeshProUGUI>();
                tmp.font = font;
                tmp.text = text;
                tmp.fontSize = h * 0.0864f;
                tmp.alignment = (TextAlignmentOptions)258;  
                tmp.color = new Color(1f, 0.92f, 0f, 1f);
                tmp.outlineWidth = 0.25f;
                tmp.outlineColor = new Color32(0, 0, 0, 255);
                tmp.enableWordWrapping = true;
                tmp.overflowMode = TextOverflowModes.Overflow;
                tmp.raycastTarget = false;
                MelonLogger.Msg("[LiteratureTrap] TextMeshProUGUI added (with font)");
            }
            else
            {
                var txt = _go.AddComponent<Text>();
                txt.text = text;
                txt.fontSize = (int)(h * 0.042f);
                txt.alignment = TextAnchor.UpperCenter;
                txt.color = new Color(1f, 0.92f, 0f, 1f);
                txt.horizontalOverflow = HorizontalWrapMode.Wrap;
                txt.verticalOverflow = VerticalWrapMode.Overflow;

                var outline = _go.AddComponent<Outline>();
                outline.effectColor = new Color(0f, 0f, 0f, 1f);
                outline.effectDistance = new Vector2(2f, -2f);
                MelonLogger.Msg("[LiteratureTrap] UI.Text fallback added (no TMP font)");
            }
        }

        public bool Update()
        {
            if (_go == null) return true;

            _time += Time.deltaTime;
            float t = _time / DURATION;
            float y = Mathf.Lerp(_startY, _endY, t);
            _rt.anchoredPosition = new Vector2(0f, y);

            return t >= 1f;
        }

        public void Destroy()
        {
            if (_go != null)
                UnityEngine.Object.Destroy(_go);
        }
    }
}
