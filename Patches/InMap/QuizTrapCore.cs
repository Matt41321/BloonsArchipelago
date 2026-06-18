using BTD_Mod_Helper.Extensions;
using Il2CppTMPro;
using MelonLoader;
using System;
using UnityEngine;
using UnityEngine.UI;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;

namespace BloonsArchipelago.Patches.InMap
{
    internal class QuizQuestion
    {
        public string Header;
        public Color HeaderColor = new Color(1f, 0.25f, 0.25f);
        public string Instruction = "";
        public string Prompt;
        public string[] Options;
        public int CorrectIndex;
        public float TimeLimit = 10f;
        public string CorrectAnswerText;
        public string MemorizePrompt;
        public float MemorizeTime = 0f;
    }

    internal class QuizTrapRunner
    {
        private enum QuizState { Idle, Memorize, Question, Result }
        private QuizState _state = QuizState.Idle;

        private readonly string _logTag;

        private GameObject _canvasGo;
        private TextMeshProUGUI _resultTMP;
        private TextMeshProUGUI _promptTMP;
        private GameObject _timerBgGo;
        private Image _timerFill;

        private string _questionPrompt;
        private float  _memorizeEnd;

        private readonly RectTransform[] _buttonRects  = new RectTransform[4];
        private readonly Image[]         _buttonImages = new Image[4];

        private static readonly Color ColNormal  = new Color(0.15f, 0.15f, 0.25f, 1f);
        private static readonly Color ColHover   = new Color(0.30f, 0.30f, 0.50f, 1f);
        private static readonly Color ColCorrect = new Color(0.10f, 0.55f, 0.10f, 1f);
        private static readonly Color ColWrong   = new Color(0.60f, 0.10f, 0.10f, 1f);

        private int    _correctIndex;
        private string _correctAnswerText;
        private float  _timeLimit;
        private float  _timeLeft;
        private float  _resultUntil;

        private TMP_FontAsset _cachedFont;
        private bool          _fontSearched;

        public QuizTrapRunner(string logTag)
        {
            _logTag = logTag;
        }

        public bool Idle => _state == QuizState.Idle;

        private TMP_FontAsset GetFont()
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
            catch (Exception ex) { MelonLogger.Warning($"[{_logTag}] Font search: {ex.Message}"); }
            return _cachedFont;
        }

        public void Begin(QuizQuestion q)
        {
            _correctIndex      = q.CorrectIndex;
            _correctAnswerText = q.CorrectAnswerText ?? q.Options[q.CorrectIndex];
            _timeLimit         = q.TimeLimit;
            _timeLeft          = q.TimeLimit;
            _questionPrompt    = q.Prompt;

            BuildOverlay(q);

            if (q.MemorizeTime > 0f && q.MemorizePrompt != null)
            {
                if (_promptTMP != null) _promptTMP.text = q.MemorizePrompt;
                SetAnswerUIVisible(false);
                _memorizeEnd = Time.unscaledTime + q.MemorizeTime;
                _state = QuizState.Memorize;
            }
            else
            {
                _state = QuizState.Question;
            }
            MelonLogger.Msg($"[{_logTag}] \"{q.Prompt}\"  answer={_correctAnswerText}  time={q.TimeLimit}s");
        }

        public void Update()
        {
            switch (_state)
            {
                case QuizState.Memorize:
                    if (Time.unscaledTime >= _memorizeEnd)
                    {
                        if (_promptTMP != null) _promptTMP.text = _questionPrompt;
                        SetAnswerUIVisible(true);
                        _timeLeft = _timeLimit;
                        _state = QuizState.Question;
                    }
                    break;

                case QuizState.Question:
                    TickQuestion();
                    break;

                case QuizState.Result:
                    if (Time.unscaledTime >= _resultUntil)
                    {
                        DestroyOverlay();
                        _state = QuizState.Idle;
                    }
                    break;
            }
        }

        public void Cleanup()
        {
            _state = QuizState.Idle;
            _fontSearched = false;
            _cachedFont = null;
            DestroyOverlay();
        }

        private void SetAnswerUIVisible(bool visible)
        {
            for (int i = 0; i < 4; i++)
                if (_buttonRects[i] != null)
                    _buttonRects[i].gameObject.SetActive(visible);
            if (_timerBgGo != null)
                _timerBgGo.SetActive(visible);
        }

        private void TickQuestion()
        {
            _timeLeft -= Time.unscaledDeltaTime;

            if (_timerFill != null)
            {
                float t = Mathf.Clamp01(_timeLeft / _timeLimit);
                _timerFill.fillAmount = t;
                _timerFill.color = Color.Lerp(Color.red, Color.green, t);
            }

            Vector2 mouse = Input.mousePosition;

            for (int i = 0; i < 4; i++)
            {
                if (_buttonRects[i] == null || _buttonImages[i] == null) continue;
                bool over = RectTransformUtility.RectangleContainsScreenPoint(_buttonRects[i], mouse, null);
                _buttonImages[i].color = over ? ColHover : ColNormal;
            }

            if (Input.GetMouseButtonDown(0))
            {
                for (int i = 0; i < 4; i++)
                {
                    if (_buttonRects[i] != null &&
                        RectTransformUtility.RectangleContainsScreenPoint(_buttonRects[i], mouse, null))
                    {
                        HandleChoice(i);
                        return;
                    }
                }
            }

            if (_timeLeft <= 0f)
            {
                ApplyPenalty();
                ShowResult(false, $"TIME'S UP!\nAnswer: {_correctAnswerText}\nCash quartered!");
            }
        }

        private void HandleChoice(int index)
        {
            if (index == _correctIndex)
            {
                _buttonImages[index].color = ColCorrect;
                ShowResult(true, "CORRECT!");
            }
            else
            {
                _buttonImages[index].color = ColWrong;
                if (_buttonImages[_correctIndex] != null)
                    _buttonImages[_correctIndex].color = ColCorrect;
                ApplyPenalty();
                ShowResult(false, $"WRONG!\nAnswer: {_correctAnswerText}\nCash quartered!");
            }
        }

        private void ApplyPenalty()
        {
            try
            {
                var inGame = InGame.instance;
                if (inGame == null) return;
                double current = inGame.GetCash();
                inGame.AddCash(-(current * 0.75));
                MelonLogger.Msg($"[{_logTag}] Cash quartered: {current:F0} → {current / 4.0:F0}");
            }
            catch (Exception ex) { MelonLogger.Warning($"[{_logTag}] Penalty failed: {ex.Message}"); }
        }

        private void ShowResult(bool correct, string msg)
        {
            _state = QuizState.Result;
            _resultUntil = Time.unscaledTime + 2.5f;

            for (int i = 0; i < 4; i++)
                if (_buttonRects[i] != null)
                    _buttonRects[i].gameObject.SetActive(false);

            if (_timerFill != null)
                _timerFill.transform.parent?.gameObject.SetActive(false);

            if (_resultTMP != null)
            {
                _resultTMP.text  = msg;
                _resultTMP.color = correct ? Color.green : Color.red;
                _resultTMP.gameObject.SetActive(true);
            }
        }

        private void BuildOverlay(QuizQuestion q)
        {
            DestroyOverlay();

            _canvasGo = new GameObject($"{_logTag}Canvas");
            UnityEngine.Object.DontDestroyOnLoad(_canvasGo);
            var canvas = _canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9998;
            var scaler = _canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            _canvasGo.AddComponent<GraphicRaycaster>();

            var font = GetFont();

            var panel = UIRect("Panel", _canvasGo, new Vector2(0.15f, 0.20f), new Vector2(0.85f, 0.80f));
            panel.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.10f, 0.92f);

            // Header
            var hdr = UIRect("Header", panel, new Vector2(0f, 0.84f), new Vector2(1f, 1f));
            var hdrTMP = hdr.AddComponent<TextMeshProUGUI>();
            if (font != null) hdrTMP.font = font;
            hdrTMP.text = q.Header;
            hdrTMP.color = q.HeaderColor;
            hdrTMP.fontSize = 38;
            hdrTMP.fontStyle = FontStyles.Bold;
            hdrTMP.alignment = TextAlignmentOptions.Center;
            hdrTMP.enableWordWrapping = false;

            var inst = UIRect("Inst", panel, new Vector2(0f, 0.74f), new Vector2(1f, 0.84f));
            var instTMP = inst.AddComponent<TextMeshProUGUI>();
            instTMP.text = q.Instruction;
            instTMP.color = new Color(0.65f, 0.65f, 0.65f);
            instTMP.fontSize = 29;
            instTMP.alignment = TextAlignmentOptions.Center;

            var qGO = UIRect("Prompt", panel, new Vector2(0.03f, 0.52f), new Vector2(0.97f, 0.74f));
            var qTMP = qGO.AddComponent<TextMeshProUGUI>();
            if (font != null) qTMP.font = font;
            qTMP.text = q.Prompt;
            qTMP.color = Color.white;
            qTMP.fontSize = 40;
            qTMP.fontStyle = FontStyles.Bold;
            qTMP.alignment = TextAlignmentOptions.Center;
            qTMP.enableWordWrapping = true;
            _promptTMP = qTMP;

            // grid
            float[,] xAnchors = { { 0.02f, 0.49f }, { 0.51f, 0.98f } };
            float[,] yAnchors = { { 0.27f, 0.50f }, { 0.05f, 0.27f } };

            for (int i = 0; i < 4; i++)
            {
                int col = i % 2;
                int row = i / 2;

                var btnGO = UIRect($"Btn{i}", panel,
                    new Vector2(xAnchors[col, 0], yAnchors[row, 0]),
                    new Vector2(xAnchors[col, 1], yAnchors[row, 1]));

                var img = btnGO.AddComponent<Image>();
                img.color = ColNormal;
                _buttonImages[i] = img;
                _buttonRects[i]  = btnGO.GetComponent<RectTransform>();

                var lbl = UIRect($"Lbl{i}", btnGO, new Vector2(0.02f, 0.05f), new Vector2(0.98f, 0.95f));
                var lblTMP = lbl.AddComponent<TextMeshProUGUI>();
                if (font != null) lblTMP.font = font;
                lblTMP.text = q.Options[i];
                lblTMP.color = Color.white;
                lblTMP.fontSize = 30;
                lblTMP.fontStyle = FontStyles.Bold;
                lblTMP.alignment = TextAlignmentOptions.Center;
                lblTMP.enableWordWrapping = true;
            }

            // Timer bar
            var timerBg = UIRect("TimerBg", panel, new Vector2(0.02f, 0.50f), new Vector2(0.98f, 0.525f));
            timerBg.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f);
            _timerBgGo = timerBg;
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
            _resultTMP.fontSize         = 34;
            _resultTMP.fontStyle        = FontStyles.Bold;
            _resultTMP.alignment        = TextAlignmentOptions.Center;
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

        private void DestroyOverlay()
        {
            if (_canvasGo != null)
            {
                UnityEngine.Object.Destroy(_canvasGo);
                _canvasGo = null;
            }
            for (int i = 0; i < 4; i++)
            {
                _buttonRects[i]  = null;
                _buttonImages[i] = null;
            }
            _resultTMP = null;
            _promptTMP = null;
            _timerBgGo = null;
            _timerFill = null;
        }
    }
}
