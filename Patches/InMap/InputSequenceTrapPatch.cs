using BTD_Mod_Helper.Extensions;
using Il2CppTMPro;
using MelonLoader;
using System;
using UnityEngine;
using UnityEngine.UI;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;

namespace BloonsArchipelago.Patches.InMap
{
    internal static class InputSequenceTrapManager
    {
        public static volatile int PendingInputSequenceCount = 0;

        private enum SeqState { Idle, Question, Result }
        private static SeqState _state = SeqState.Idle;

        private static readonly int[] _sequence = new int[8];
        private static int _currentStep;

        private static GameObject _canvasGo;
        private static readonly TextMeshProUGUI[] _arrowTMPs = new TextMeshProUGUI[8];
        private static readonly Image[]           _arrowBgs  = new Image[8];
        private static TextMeshProUGUI _resultTMP;
        private static GameObject      _arrowRow;
        private static Image           _timerFill;

        private const float TIME_LIMIT = 5f;
        private static float _timeLeft;
        private static float _resultUntil;

        // Arrow colours
        private static readonly Color ColPending  = new Color(0.35f, 0.35f, 0.35f, 1f);
        private static readonly Color ColCurrent  = new Color(1.00f, 0.90f, 0.10f, 1f);
        private static readonly Color ColDone     = new Color(0.10f, 0.80f, 0.10f, 1f);
        private static readonly Color ColWrong    = new Color(0.90f, 0.10f, 0.10f, 1f);
        private static readonly Color ColBgOff    = new Color(0f, 0f, 0f, 0f);
        private static readonly Color ColBgActive = new Color(0.30f, 0.28f, 0f, 0.55f);

        private static readonly string[]  Symbols = { "↑", "↓", "←", "→" };
        private static readonly KeyCode[] Keys    = { KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.LeftArrow, KeyCode.RightArrow };

        private static readonly System.Random _rng = new();
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
            catch (Exception ex) { MelonLogger.Warning($"[InputSeq] Font search: {ex.Message}"); }
            return _cachedFont;
        }

        public static void Update()
        {
            switch (_state)
            {
                case SeqState.Idle:
                    if (PendingInputSequenceCount > 0)
                    {
                        PendingInputSequenceCount--;
                        try { StartSequence(); }
                        catch (Exception ex) { MelonLogger.Warning($"[InputSeq] Start error: {ex}"); }
                    }
                    break;

                case SeqState.Question:
                    TickQuestion();
                    break;

                case SeqState.Result:
                    if (Time.unscaledTime >= _resultUntil)
                    {
                        DestroyOverlay();
                        _state = SeqState.Idle;
                    }
                    break;
            }
        }

        public static void CleanupAll()
        {
            PendingInputSequenceCount = 0;
            _state = SeqState.Idle;
            _fontSearched = false;
            _cachedFont = null;
            DestroyOverlay();
        }

        private static void StartSequence()
        {
            _currentStep = 0;
            for (int i = 0; i < 8; i++)
                _sequence[i] = _rng.Next(4);

            _timeLeft = TIME_LIMIT;
            BuildOverlay();
            _state = SeqState.Question;

            string seq = string.Join(" ", System.Array.ConvertAll(_sequence, d => Symbols[d]));
            MelonLogger.Msg($"[InputSeq] Sequence: {seq}");
        }

        private static void TickQuestion()
        {
            _timeLeft -= Time.unscaledDeltaTime;

            if (_timerFill != null)
            {
                float t = Mathf.Clamp01(_timeLeft / TIME_LIMIT);
                _timerFill.fillAmount = t;
                _timerFill.color = Color.Lerp(Color.red, Color.green, t);
            }

            // Update arrow colours each frame
            for (int i = 0; i < 8; i++)
            {
                if (_arrowTMPs[i] != null)
                    _arrowTMPs[i].color = i < _currentStep ? ColDone
                                        : i == _currentStep ? ColCurrent
                                        : ColPending;

                if (_arrowBgs[i] != null)
                    _arrowBgs[i].color = i == _currentStep ? ColBgActive : ColBgOff;
            }

            // Check arrow key presses
            for (int k = 0; k < 4; k++)
            {
                if (!Input.GetKeyDown(Keys[k])) continue;

                if (k == _sequence[_currentStep])
                {
                    _currentStep++;
                    if (_currentStep == 8)
                        ShowResult(true, "SEQUENCE COMPLETE!");
                }
                else
                {
                    if (_arrowTMPs[_currentStep] != null)
                        _arrowTMPs[_currentStep].color = ColWrong;
                    ApplyPenalty();
                    ShowResult(false, "WRONG KEY!\nCash quartered!");
                }
                return;
            }

            if (_timeLeft <= 0f)
            {
                ApplyPenalty();
                ShowResult(false, "TIME'S UP!\nCash quartered!");
            }
        }

        private static void ApplyPenalty()
        {
            try
            {
                var inGame = InGame.instance;
                if (inGame == null) return;
                double current = inGame.GetCash();
                inGame.AddCash(-(current * 0.75));
                MelonLogger.Msg($"[InputSeq] Cash quartered: {current:F0} → {current / 4.0:F0}");
            }
            catch (Exception ex) { MelonLogger.Warning($"[InputSeq] Penalty failed: {ex.Message}"); }
        }

        private static void ShowResult(bool correct, string msg)
        {
            _state = SeqState.Result;
            _resultUntil = Time.unscaledTime + 2.5f;

            if (_arrowRow != null) _arrowRow.SetActive(false);
            if (_timerFill != null) _timerFill.transform.parent?.gameObject.SetActive(false);

            if (_resultTMP != null)
            {
                _resultTMP.text  = msg;
                _resultTMP.color = correct ? Color.green : Color.red;
                _resultTMP.gameObject.SetActive(true);
            }
        }

        private static void BuildOverlay()
        {
            DestroyOverlay();

            _canvasGo = new GameObject("InputSeqCanvas");
            UnityEngine.Object.DontDestroyOnLoad(_canvasGo);
            var canvas = _canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9998;
            var scaler = _canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            _canvasGo.AddComponent<GraphicRaycaster>();

            var font = GetFont();

            // panel
            var panel = UIRect("Panel", _canvasGo, new Vector2(0.04f, 0.30f), new Vector2(0.96f, 0.70f));
            panel.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.10f, 0.92f);

            // Title
            var hdr = UIRect("Header", panel, new Vector2(0f, 0.78f), new Vector2(1f, 1f));
            var hdrTMP = hdr.AddComponent<TextMeshProUGUI>();
            if (font != null) hdrTMP.font = font;
            hdrTMP.text      = "INPUT SEQUENCE TRAP!";
            hdrTMP.color     = new Color(1f, 0.25f, 0.25f);
            hdrTMP.fontSize  = 60;
            hdrTMP.fontStyle = FontStyles.Bold;
            hdrTMP.alignment = TextAlignmentOptions.Center;
            hdrTMP.enableWordWrapping = false;

            var inst = UIRect("Inst", panel, new Vector2(0f, 0.63f), new Vector2(1f, 0.78f));
            var instTMP = inst.AddComponent<TextMeshProUGUI>();
            if (font != null) instTMP.font = font;
            instTMP.text      = "Press the arrow keys in order!";
            instTMP.color     = new Color(0.75f, 0.75f, 0.75f);
            instTMP.fontSize  = 36;
            instTMP.alignment = TextAlignmentOptions.Center;

            _arrowRow = UIRect("ArrowRow", panel, new Vector2(0.01f, 0.20f), new Vector2(0.99f, 0.63f));

            float slotW = 1f / 8f;
            for (int i = 0; i < 8; i++)
            {
                float x0 = i * slotW;
                float x1 = x0 + slotW;

                // Highlight background for current arrow
                var bg = UIRect($"Bg{i}", _arrowRow, new Vector2(x0 + 0.005f, 0.02f), new Vector2(x1 - 0.005f, 0.98f));
                var bgImg = bg.AddComponent<Image>();
                bgImg.color  = ColBgOff;
                _arrowBgs[i] = bgImg;

                // Arrow symbol text
                var arw = UIRect($"Arrow{i}", _arrowRow, new Vector2(x0, 0f), new Vector2(x1, 1f));
                var arwTMP = arw.AddComponent<TextMeshProUGUI>();
                if (font != null) arwTMP.font = font;
                arwTMP.text      = Symbols[_sequence[i]];
                arwTMP.color     = ColPending;
                arwTMP.fontSize  = 72;
                arwTMP.fontStyle = FontStyles.Bold;
                arwTMP.alignment = TextAlignmentOptions.Center;
                arwTMP.enableWordWrapping = false;
                _arrowTMPs[i] = arwTMP;
            }

            // Timer bar
            var timerBg = UIRect("TimerBg", panel, new Vector2(0.02f, 0.10f), new Vector2(0.98f, 0.18f));
            timerBg.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f);
            var timerFillGO = UIRect("TimerFill", timerBg, Vector2.zero, Vector2.one);
            _timerFill = timerFillGO.AddComponent<Image>();
            _timerFill.type       = Image.Type.Filled;
            _timerFill.fillMethod = Image.FillMethod.Horizontal;
            _timerFill.fillAmount = 1f;
            _timerFill.color      = Color.green;

            // Result
            var rGO = UIRect("Result", panel, new Vector2(0.03f, 0.10f), new Vector2(0.97f, 0.90f));
            _resultTMP = rGO.AddComponent<TextMeshProUGUI>();
            if (font != null) _resultTMP.font = font;
            _resultTMP.fontSize          = 80;
            _resultTMP.fontStyle         = FontStyles.Bold;
            _resultTMP.alignment         = TextAlignmentOptions.Center;
            _resultTMP.enableWordWrapping = true;
            rGO.SetActive(false);
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
            for (int i = 0; i < 8; i++) { _arrowTMPs[i] = null; _arrowBgs[i] = null; }
            _resultTMP = null;
            _arrowRow  = null;
            _timerFill = null;
        }
    }
}
