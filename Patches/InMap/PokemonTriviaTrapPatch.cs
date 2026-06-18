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
    internal static class PokemonTriviaTrapManager
    {
        public static volatile int PendingPokemonTriviaCount = 0;

        private const float TIME_LIMIT   = 15f;
        private const float RESULT_TIME  = 3f;

        private static readonly System.Random _rng = new();

        private enum State { Idle, Question, Result }
        private static State _state = State.Idle;

        private static readonly (string Q, string[] Correct)[] _questions =
        {
            ("Which Pokemon is the water starter of Johto?", new[] { "totodile" }),
            ("Which Pokemon is the evolved form of Cyndaquil?", new[] { "quilava" }),
            ("Which Pokemon can be found in the Ruins Of Alph?", new[] { "unown" }),
            ("Which Pokemon eventually evolves into Raichu?", new[] { "pichu" }),
            ("Which Pokemon evolves into Feraligatr?", new[] { "totodile" }),
            ("Which Pokemon evolves from Cyndaquil?", new[] { "quilava" }),
            ("Which Pokemon has many forms based on letters?", new[] { "unown" }),
            ("Which Pokemon is known as the Guardian of the Skies?", new[] { "lugia" }),
            ("Which Pokemon knows the move Sacred Fire?", new[] { "hooh" }),
            ("Which of these Pokemon evolve with King's Rock?", new[] { "slowking", "politoed" }),
            ("Which Pokemon is the main pokemon of the 4th Movie?", new[] { "celebi" }),
            ("Which of these Pokemon can be encountered in the Goldenrod City Gym?", new[] { "miltank" }),
            ("Which Pokemon is obtainable from Mr. Pokemon?", new[] { "togepi" }),
            ("Which Rock Type Pokemon blocks the path on Route 36?", new[] { "sudowoodo" }),
            ("Which Pokemon has the highest Defense stat?", new[] { "shuckle" }),
            ("Which Pokemon appears at the top of Tin Tower?", new[] { "hooh" }),
            ("Which Pokemon resides in the Whirl Islands?", new[] { "lugia" }),
            ("Which Pokemon is the time travel Pokemon?", new[] { "celebi" }),
            ("Which Pokemon can only be found at night?", new[] { "hoothoot", "misdreavus" }),
            ("Which Pokemon is evolved into while holding Metal Coat on Scyther?", new[] { "scizor" }),
            ("Which Pokemon evolves from Eevee with high friendship at night?", new[] { "umbreon" }),
            ("Which Pokemon evolves from Eevee with high friendship during day?", new[] { "espeon" }),
            ("Which Pokemon evolves from Gloom with a Sun Stone?", new[] { "bellossom" }),
            ("Which Pokemon delivers presents?", new[] { "delibird" }),
            ("Which Pokemon is the main Pokemon of Pokemon 2000?", new[] { "lugia" }),
            ("Which Pokemon does Ash see on his first day as a trainer?", new[] { "hooh" }),
            ("Which Pokemon does Eusine constantly chase after?", new[] { "suicune" }),
            ("Which Pokemon is the regular form of Paradox Pokemon Flutter Mane?", new[] { "misdreavus" }),
            ("Which Pokemon can learn the move Triple Kick?", new[] { "hitmontop" }),
            ("Which Pokemon is known for spinning webs to catch prey?", new[] { "spinarak" }),
            ("Which Pokemon is an Electric/Water type?", new[] { "chinchou" }),
            ("Which Pokemon is a Fire type that resembles molten lava?", new[] { "slugma" }),
            ("Which Pokemon eventually evolves into Electivire?", new[] { "elekid" }),
            ("Which Pokemon is known for its round, blue body and tail?", new[] { "marill" }),
            ("Which Psychic/Flying type Pokemon is known for its small size and big eyes?", new[] { "natu" }),
            ("Which of these Pokemon has a regional form that evolves into Clodsire?", new[] { "wooper" }),
            ("Which of these evolves from Poliwhirl?", new[] { "slowking", "politoed" }),
            ("Which Pokemon is caught on Mount Silver?", new[] { "larvitar" }),
        };

        private static readonly string[] _pokemonPool =
        {
            "totodile", "quilava", "unown", "pichu", "hooh", "lugia", "celebi", "hoothoot",
            "delibird", "bellossom", "umbreon", "espeon", "miltank", "heracross", "girafarig",
            "slowking", "togepi", "shuckle", "sudowoodo", "scizor", "porygon2", "dunsparce",
            "qwilfish", "crobat", "steelix", "suicune", "misdreavus", "hitmontop", "spinarak",
            "chinchou", "slugma", "elekid", "marill", "natu", "wooper", "swinub", "politoed",
            "ledyba", "larvitar", "corsola", "entei",
            "terriermon", "wizardmon", "cheat",
        };

        private static GameObject _canvasGo;
        private static TextMeshProUGUI _promptTMP;
        private static TextMeshProUGUI _resultTMP;
        private static GameObject _timerBgGo;
        private static Image _timerFill;

        private static readonly RectTransform[] _cellRects = new RectTransform[4];
        private static readonly Image[]         _cellBgs   = new Image[4];
        private static readonly GameObject[]    _cellGos   = new GameObject[4];

        private static string[] _options = new string[4];
        private static int    _correctIndex;
        private static string _correctName;
        private static float  _timeLeft;
        private static float  _resultUntil;

        private static readonly Color ColNormal  = new Color(0.15f, 0.15f, 0.25f, 1f);
        private static readonly Color ColHover   = new Color(0.30f, 0.30f, 0.50f, 1f);
        private static readonly Color ColCorrect = new Color(0.10f, 0.55f, 0.10f, 1f);
        private static readonly Color ColWrong   = new Color(0.60f, 0.10f, 0.10f, 1f);

        private static Dictionary<string, string> _resByName; 
        private static readonly Dictionary<string, Sprite> _spriteCache = new();

        private static TMP_FontAsset _cachedFont;
        private static bool          _fontSearched;

        public static void Update()
        {
            switch (_state)
            {
                case State.Idle:
                    if (PendingPokemonTriviaCount > 0)
                    {
                        PendingPokemonTriviaCount--;
                        try { StartQuestion(); }
                        catch (Exception ex) { MelonLogger.Warning($"[PokemonTrivia] Start error: {ex}"); }
                    }
                    break;

                case State.Question:
                    TickQuestion();
                    break;

                case State.Result:
                    if (Time.unscaledTime >= _resultUntil)
                    {
                        DestroyOverlay();
                        _state = State.Idle;
                    }
                    break;
            }
        }

        public static void CleanupAll()
        {
            PendingPokemonTriviaCount = 0;
            _state = State.Idle;
            _fontSearched = false;
            _cachedFont = null;
            DestroyOverlay();
        }

        private static void StartQuestion()
        {
            var (q, correct) = _questions[_rng.Next(_questions.Length)];

            string displayedCorrect = correct[_rng.Next(correct.Length)];

            var wrongPool = new List<string>();
            foreach (var p in _pokemonPool)
                if (Array.IndexOf(correct, p) < 0)
                    wrongPool.Add(p);
            Shuffle(wrongPool);

            var opts = new List<string> { displayedCorrect, wrongPool[0], wrongPool[1], wrongPool[2] };
            Shuffle(opts);

            for (int i = 0; i < 4; i++) _options[i] = opts[i];
            _correctIndex = opts.IndexOf(displayedCorrect);
            _correctName  = displayedCorrect;
            _timeLeft     = TIME_LIMIT;

            BuildOverlay(q);
            _state = State.Question;
            MelonLogger.Msg($"[PokemonTrivia] \"{q}\"  answer={_correctName}  options=[{string.Join(", ", _options)}]");
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

            Vector2 mouse = Input.mousePosition;

            for (int i = 0; i < 4; i++)
            {
                if (_cellRects[i] == null || _cellBgs[i] == null) continue;
                bool over = RectTransformUtility.RectangleContainsScreenPoint(_cellRects[i], mouse, null);
                _cellBgs[i].color = over ? ColHover : ColNormal;
            }

            if (Input.GetMouseButtonDown(0))
            {
                for (int i = 0; i < 4; i++)
                {
                    if (_cellRects[i] != null &&
                        RectTransformUtility.RectangleContainsScreenPoint(_cellRects[i], mouse, null))
                    {
                        HandleChoice(i);
                        return;
                    }
                }
            }

            if (_timeLeft <= 0f)
            {
                ApplyPenalty();
                StampOverlay(_correctIndex, "correct");
                ShowResult(false, "TIME'S UP!");
            }
        }

        private static void HandleChoice(int index)
        {
            bool correct = index == _correctIndex;
            if (correct)
            {
                _cellBgs[index].color = ColCorrect;
                StampOverlay(index, "correct");
                ShowResult(true, "CORRECT!");
            }
            else
            {
                _cellBgs[index].color = ColWrong;
                StampOverlay(index, "wrong");
                if (_cellBgs[_correctIndex] != null)
                    _cellBgs[_correctIndex].color = ColCorrect;
                ApplyPenalty();
                ShowResult(false, "TOO BAD!");
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
                MelonLogger.Msg($"[PokemonTrivia] Cash quartered: {current:F0} → {current / 4.0:F0}");
            }
            catch (Exception ex) { MelonLogger.Warning($"[PokemonTrivia] Penalty failed: {ex.Message}"); }
        }

        private static void ShowResult(bool correct, string msg)
        {
            _state = State.Result;
            _resultUntil = Time.unscaledTime + RESULT_TIME;

            if (_timerBgGo != null) _timerBgGo.SetActive(false);
            if (_promptTMP != null) _promptTMP.gameObject.SetActive(false);

            if (_resultTMP != null)
            {
                _resultTMP.text  = correct ? msg : $"{msg}  It was {Capitalize(_correctName)}!  Cash quartered!";
                _resultTMP.color = correct ? Color.green : Color.red;
                _resultTMP.gameObject.SetActive(true);
            }
        }

        private static void BuildOverlay(string question)
        {
            DestroyOverlay();

            _canvasGo = new GameObject("PokemonTriviaCanvas");
            UnityEngine.Object.DontDestroyOnLoad(_canvasGo);
            var canvas = _canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9998;
            var scaler = _canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            _canvasGo.AddComponent<GraphicRaycaster>();

            var font = GetFont();

            var panel = UIRect("Panel", _canvasGo, new Vector2(0.13f, 0.12f), new Vector2(0.87f, 0.88f));
            panel.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.10f, 0.92f);

            // Header
            var hdr = UIRect("Header", panel, new Vector2(0f, 0.90f), new Vector2(1f, 1f));
            var hdrTMP = hdr.AddComponent<TextMeshProUGUI>();
            if (font != null) hdrTMP.font = font;
            hdrTMP.text = "POKEMON TRIVIA TRAP!";
            hdrTMP.color = new Color(1.00f, 0.80f, 0.10f);
            hdrTMP.fontSize = 38;
            hdrTMP.fontStyle = FontStyles.Bold;
            hdrTMP.alignment = TextAlignmentOptions.Center;
            hdrTMP.enableWordWrapping = false;

            var qGO = UIRect("Prompt", panel, new Vector2(0.03f, 0.80f), new Vector2(0.97f, 0.90f));
            var qTMP = qGO.AddComponent<TextMeshProUGUI>();
            if (font != null) qTMP.font = font;
            qTMP.text = question;
            qTMP.color = Color.white;
            qTMP.fontSize = 32;
            qTMP.fontStyle = FontStyles.Bold;
            qTMP.alignment = TextAlignmentOptions.Center;
            qTMP.enableWordWrapping = true;
            _promptTMP = qTMP;

            // Timer bar
            var timerBg = UIRect("TimerBg", panel, new Vector2(0.03f, 0.765f), new Vector2(0.97f, 0.785f));
            timerBg.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f);
            _timerBgGo = timerBg;
            var timerFillGO = UIRect("TimerFill", timerBg, Vector2.zero, Vector2.one);
            _timerFill = timerFillGO.AddComponent<Image>();
            _timerFill.type       = Image.Type.Filled;
            _timerFill.fillMethod = Image.FillMethod.Horizontal;
            _timerFill.fillAmount = 1f;
            _timerFill.color      = Color.green;

            // grid
            float[,] xAnchors = { { 0.06f, 0.49f }, { 0.51f, 0.94f } };
            float[,] yAnchors = { { 0.40f, 0.74f }, { 0.04f, 0.38f } };

            for (int i = 0; i < 4; i++)
            {
                int col = i % 2;
                int row = i / 2;

                var cellGO = UIRect($"Cell{i}", panel,
                    new Vector2(xAnchors[col, 0], yAnchors[row, 0]),
                    new Vector2(xAnchors[col, 1], yAnchors[row, 1]));

                var bg = cellGO.AddComponent<Image>();
                bg.color = ColNormal;
                _cellBgs[i]   = bg;
                _cellRects[i] = cellGO.GetComponent<RectTransform>();
                _cellGos[i]   = cellGO;

                // Pokemon sprite
                var imgGO = UIRect($"Mon{i}", cellGO, new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.92f));
                var monImg = imgGO.AddComponent<Image>();
                monImg.preserveAspect = true;
                monImg.raycastTarget  = false;
                var sprite = GetSprite(_options[i]);
                if (sprite != null) monImg.sprite = sprite;
                else { monImg.color = Color.clear; }
            }

            var rGO = UIRect("Result", panel, new Vector2(0.03f, 0.755f), new Vector2(0.97f, 0.90f));
            _resultTMP = rGO.AddComponent<TextMeshProUGUI>();
            if (font != null) _resultTMP.font = font;
            _resultTMP.fontSize           = 34;
            _resultTMP.fontStyle          = FontStyles.Bold;
            _resultTMP.alignment          = TextAlignmentOptions.Center;
            _resultTMP.enableWordWrapping = true;
            rGO.SetActive(false);
        }

        private static void StampOverlay(int index, string overlayName)
        {
            if (index < 0 || index >= 4 || _cellGos[index] == null) return;
            var sprite = GetSprite(overlayName);
            if (sprite == null) return;

            var ov = UIRect("Overlay", _cellGos[index], Vector2.zero, Vector2.one);
            var img = ov.AddComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.raycastTarget  = false;
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
                _cellRects[i] = null;
                _cellBgs[i]   = null;
                _cellGos[i]   = null;
            }
            _promptTMP = null;
            _resultTMP = null;
            _timerBgGo = null;
            _timerFill = null;
        }

        private static void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private static string Capitalize(string s) =>
            string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s.Substring(1);

        private static Sprite GetSprite(string baseName)
        {
            if (_spriteCache.TryGetValue(baseName, out var cached)) return cached;

            EnsureResourceMap();
            if (!_resByName.TryGetValue(baseName, out var resName))
            {
                MelonLogger.Warning($"[PokemonTrivia] Missing sprite: {baseName}");
                _spriteCache[baseName] = null;
                return null;
            }

            try
            {
                var asm = typeof(BloonsArchipelago).Assembly;
                using var stream = asm.GetManifestResourceStream(resName);
                if (stream == null) { _spriteCache[baseName] = null; return null; }

                var bytes = new byte[stream.Length];
                stream.Read(bytes, 0, bytes.Length);

                var tex = new Texture2D(2, 2);
                ImageConversion.LoadImage(tex, bytes);
                var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                _spriteCache[baseName] = sprite;
                return sprite;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[PokemonTrivia] Failed to load {baseName}: {ex.Message}");
                _spriteCache[baseName] = null;
                return null;
            }
        }

        private static void EnsureResourceMap()
        {
            if (_resByName != null) return;
            _resByName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var asm = typeof(BloonsArchipelago).Assembly;
            foreach (var n in asm.GetManifestResourceNames())
            {
                if (n.IndexOf("pkmns", StringComparison.OrdinalIgnoreCase) < 0) continue;
                if (!n.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) continue;
                string noExt = n.Substring(0, n.Length - 4);
                int dot = noExt.LastIndexOf('.');
                string baseName = dot >= 0 ? noExt.Substring(dot + 1) : noExt;
                _resByName[baseName] = n;
            }
            MelonLogger.Msg($"[PokemonTrivia] Mapped {_resByName.Count} pkmns sprite(s).");
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
                    if (f.name.Contains("LuckiestGuy") || f.name.Contains("Luckiest") ||
                        f.name.ToLower().Contains("btd") || f.name.ToLower().Contains("bloons"))
                        return _cachedFont = f;
                }
                if (fonts.Length > 0) _cachedFont = fonts[0];
            }
            catch (Exception ex) { MelonLogger.Warning($"[PokemonTrivia] Font search: {ex.Message}"); }
            return _cachedFont;
        }
    }
}
