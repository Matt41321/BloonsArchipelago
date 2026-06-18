using BTD_Mod_Helper.Extensions;
using Il2CppTMPro;
using MelonLoader;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;

namespace BloonsArchipelago.Patches.InMap
{
    internal static class MathQuizTrapManager
    {
        public static volatile int PendingMathQuizCount = 0;

        private enum QuizState { Idle, Question, Result }
        private static QuizState _state = QuizState.Idle;

        private static GameObject _canvasGo;
        private static TextMeshProUGUI _resultTMP;
        private static Image _timerFill;

        private static readonly RectTransform[] _buttonRects  = new RectTransform[4];
        private static readonly Image[]         _buttonImages = new Image[4];

        private static readonly Color ColNormal  = new Color(0.15f, 0.15f, 0.25f, 1f);
        private static readonly Color ColHover   = new Color(0.30f, 0.30f, 0.50f, 1f);
        private static readonly Color ColCorrect = new Color(0.10f, 0.55f, 0.10f, 1f);
        private static readonly Color ColWrong   = new Color(0.60f, 0.10f, 0.10f, 1f);

        private static int    _correctIndex;
        private static string _correctAnswerText;
        private static float  _timeLimit;
        private static float  _timeLeft;
        private static float  _resultUntil;

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
            catch (Exception ex) { MelonLogger.Warning($"[MathQuiz] Font search: {ex.Message}"); }
            return _cachedFont;
        }

        public static void Update()
        {
            switch (_state)
            {
                case QuizState.Idle:
                    if (PendingMathQuizCount > 0)
                    {
                        PendingMathQuizCount--;
                        try { StartQuiz(); }
                        catch (Exception ex) { MelonLogger.Warning($"[MathQuiz] StartQuiz error: {ex}"); }
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

        public static void CleanupAll()
        {
            PendingMathQuizCount = 0;
            _state = QuizState.Idle;
            _fontSearched = false;
            _cachedFont = null;
            DestroyOverlay();
        }

        private static void StartQuiz()
        {
            GenerateProblem(
                out string equation,
                out string header,
                out Color headerColor,
                out float timeLimit,
                out string[] optionLabels,
                out int correctIndex,
                out string correctAnswerText);

            _correctIndex      = correctIndex;
            _correctAnswerText = correctAnswerText;
            _timeLimit         = timeLimit;
            _timeLeft          = timeLimit;

            BuildOverlay(equation, header, headerColor, optionLabels);
            _state = QuizState.Question;
            MelonLogger.Msg($"[MathQuiz] \"{equation}\"  answer={correctAnswerText}  time={timeLimit}s");
        }

        private static void TickQuestion()
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
                ShowResult(false, "TIME'S UP!\nCash quartered!");
            }
        }

        private static void HandleChoice(int index)
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

        private static void ApplyPenalty()
        {
            try
            {
                var inGame = InGame.instance;
                if (inGame == null) return;
                double current = inGame.GetCash();
                inGame.AddCash(-(current * 0.75));
                MelonLogger.Msg($"[MathQuiz] Cash quartered: {current:F0} → {current / 4.0:F0}");
            }
            catch (Exception ex) { MelonLogger.Warning($"[MathQuiz] Penalty failed: {ex.Message}"); }
        }

        private static void ShowResult(bool correct, string msg)
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

        private static void GenerateProblem(
            out string   equation,
            out string   header,
            out Color    headerColor,
            out float    timeLimit,
            out string[] optionLabels,
            out int      correctIndex,
            out string   correctAnswerText)
        {
            switch (_rng.Next(3))
            {
                case 0:
                    GenerateEasy(out equation, out optionLabels, out correctIndex, out correctAnswerText);
                    header      = "MATH QUIZ  —  EASY";
                    headerColor = new Color(0.3f, 0.9f, 0.3f);
                    timeLimit   = 5f;
                    break;
                case 1:
                    GenerateMedium(out equation, out optionLabels, out correctIndex, out correctAnswerText);
                    header      = "MATH QUIZ  —  MEDIUM";
                    headerColor = new Color(1f, 0.85f, 0.1f);
                    timeLimit   = 15f;
                    break;
                default:
                    GenerateHard(out equation, out optionLabels, out correctIndex, out correctAnswerText);
                    header      = "MATH QUIZ  —  HARD";
                    headerColor = new Color(1f, 0.25f, 0.25f);
                    timeLimit   = 20f;
                    break;
            }
        }

        // Easy
        private static void GenerateEasy(
            out string equation, out string[] optionLabels,
            out int correctIndex, out string correctAnswerText)
        {
            int x, a, b, c;
            do { x = _rng.Next(-8, 9); } while (x == 0);
            a = _rng.Next(1, 6);
            do { b = _rng.Next(-10, 11); } while (b == 0);
            c = a * x + b;

            equation = FormatLinear(a, b, c);
            correctAnswerText = $"x = {x}";

            var used = new HashSet<int> { x };
            var vals = new List<int>    { x };
            while (vals.Count < 4)
            {
                int w = _rng.Next(-10, 11);
                if (used.Add(w)) vals.Add(w);
            }
            Shuffle(vals);

            correctIndex = vals.IndexOf(x);
            optionLabels = vals.ConvertAll(v => $"x = {v}").ToArray();
        }

        // Medium
        private static void GenerateMedium(
            out string equation, out string[] optionLabels,
            out int correctIndex, out string correctAnswerText)
        {
            int r1, r2;
            do
            {
                r1 = _rng.Next(-5, 6);
                r2 = _rng.Next(-5, 6);
            }
            while (r1 == r2);
            if (r1 > r2) (r1, r2) = (r2, r1);

            equation = FormatQuadratic(-(r1 + r2), r1 * r2);
            correctAnswerText = FormatRoots(r1, r2);

            var opts = GenerateRootPairOptions(r1, r2, rangeMin: -5, rangeMax: 5);
            correctIndex = opts.FindIndex(p => p == (r1, r2));
            optionLabels = opts.ConvertAll(p => FormatRoots(p.Item1, p.Item2)).ToArray();
        }

        // Hard
        private static void GenerateHard(
            out string equation, out string[] optionLabels,
            out int correctIndex, out string correctAnswerText)
        {
            int a = _rng.Next(-5, 6);
            int k = _rng.Next(1, 7);

            int r1 = Math.Min(a - k, a + k);
            int r2 = Math.Max(a - k, a + k);

            equation = FormatCompletedSquare(a, k);
            correctAnswerText = FormatRoots(r1, r2);

            // Wrong options
            var used = new HashSet<(int, int)> { (r1, r2) };
            var opts = new List<(int, int)>    { (r1, r2) };
            int attempts = 0;
            while (opts.Count < 4 && attempts < 200)
            {
                attempts++;
                int wa = _rng.Next(-5, 6);
                int wk = _rng.Next(1, 7);
                int wr1 = Math.Min(wa - wk, wa + wk);
                int wr2 = Math.Max(wa - wk, wa + wk);
                if (used.Add((wr1, wr2))) opts.Add((wr1, wr2));
            }
            Shuffle(opts);

            correctIndex = opts.FindIndex(p => p == (r1, r2));
            optionLabels = opts.ConvertAll(p => FormatRoots(p.Item1, p.Item2)).ToArray();
        }

        private static List<(int, int)> GenerateRootPairOptions(int r1, int r2, int rangeMin, int rangeMax)
        {
            var used = new HashSet<(int, int)> { (r1, r2) };
            var opts = new List<(int, int)>    { (r1, r2) };
            int attempts = 0;
            while (opts.Count < 4 && attempts < 200)
            {
                attempts++;
                int a = _rng.Next(rangeMin, rangeMax + 1);
                int b = _rng.Next(rangeMin, rangeMax + 1);
                if (a == b) continue;
                if (a > b) (a, b) = (b, a);
                if (used.Add((a, b))) opts.Add((a, b));
            }
            Shuffle(opts);
            return opts;
        }

        private static void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private static string FormatLinear(int a, int b, int c)
        {
            string lhs = a == 1 ? "x" : $"{a}x";
            if (b > 0) lhs += $" + {b}";
            else if (b < 0) lhs += $" - {Math.Abs(b)}";
            return $"{lhs} = {c}";
        }

        private static string FormatQuadratic(int b, int c)
        {
            string eq = "x²"; // x²
            if      (b ==  1) eq += " + x";
            else if (b == -1) eq += " - x";
            else if (b  >  0) eq += $" + {b}x";
            else if (b  <  0) eq += $" - {Math.Abs(b)}x";
            if      (c  >  0) eq += $" + {c}";
            else if (c  <  0) eq += $" - {Math.Abs(c)}";
            return eq + " = 0";
        }

        private static string FormatCompletedSquare(int a, int k)
        {
            string inner = a == 0 ? "x"
                         : a  > 0 ? $"(x - {a})"
                                  : $"(x + {Math.Abs(a)})";
            return $"{inner}² = {k * k}"; // ²
        }

        private static string FormatRoots(int r1, int r2)
            => $"x = {r1},   x = {r2}";

        private static void BuildOverlay(string equation, string header, Color headerColor, string[] optionLabels)
        {
            DestroyOverlay();

            _canvasGo = new GameObject("MathQuizCanvas");
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
            hdrTMP.text = header;
            hdrTMP.color = headerColor;
            hdrTMP.fontSize = 38;
            hdrTMP.fontStyle = FontStyles.Bold;
            hdrTMP.alignment = TextAlignmentOptions.Center;
            hdrTMP.enableWordWrapping = false;

            var inst = UIRect("Inst", panel, new Vector2(0f, 0.74f), new Vector2(1f, 0.84f));
            var instTMP = inst.AddComponent<TextMeshProUGUI>();
            instTMP.text = "Solve for x";
            instTMP.color = new Color(0.65f, 0.65f, 0.65f);
            instTMP.fontSize = 29;
            instTMP.alignment = TextAlignmentOptions.Center;

            // Equation
            var qGO = UIRect("Equation", panel, new Vector2(0.03f, 0.52f), new Vector2(0.97f, 0.74f));
            var qTMP = qGO.AddComponent<TextMeshProUGUI>();
            if (font != null) qTMP.font = font;
            qTMP.text = equation;
            qTMP.color = Color.white;
            qTMP.fontSize = 54;
            qTMP.fontStyle = FontStyles.Bold;
            qTMP.alignment = TextAlignmentOptions.Center;

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
                lblTMP.text = optionLabels[i];
                lblTMP.color = Color.white;
                lblTMP.fontSize = 36;
                lblTMP.fontStyle = FontStyles.Bold;
                lblTMP.alignment = TextAlignmentOptions.Center;
                lblTMP.enableWordWrapping = false;
            }

            // Timer bar
            var timerBg = UIRect("TimerBg", panel, new Vector2(0.02f, 0.50f), new Vector2(0.98f, 0.525f));
            timerBg.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f);
            var timerFillGO = UIRect("TimerFill", timerBg, Vector2.zero, Vector2.one);
            _timerFill = timerFillGO.AddComponent<Image>();
            _timerFill.type       = Image.Type.Filled;
            _timerFill.fillMethod = Image.FillMethod.Horizontal;
            _timerFill.fillAmount = 1f;
            _timerFill.color      = Color.green;

            // Results
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

        private static void DestroyOverlay()
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
            _timerFill = null;
        }
    }
}
