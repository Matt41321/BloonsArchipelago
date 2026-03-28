using BTD_Mod_Helper.Api;
using MelonLoader;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BloonsArchipelago.Patches.InMap
{
    internal static class BeeTrapManager
    {
        private static readonly List<BeeFlight> _bees = new();
        private static Sprite _beeSprite = null;
        private static GameObject _canvasGo = null;
        private static RectTransform _canvasRect = null;

        public static volatile int PendingBeeCount = 0;

        private const int BEES_PER_WAVE = 400;
        private const float SPAWN_INTERVAL = 10f / 400f;

        private static float _spawnTimer = 0f;
        private static int _beesQueued = 0;

        private static Sprite GetBeeSprite()
        {
            if (_beeSprite == null)
            {
                try { _beeSprite = ModContent.GetSprite<BloonsArchipelago>("Bee"); }
                catch (Exception ex) { MelonLogger.Warning($"[BloonsArchipelago] Could not load bee sprite: {ex.Message}"); }
            }
            return _beeSprite;
        }

        private static void EnsureCanvas()
        {
            if (_canvasGo != null) return;

            _canvasGo = new GameObject("BeeTrapCanvas");
            UnityEngine.Object.DontDestroyOnLoad(_canvasGo);

            var canvas = _canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;

            _canvasGo.AddComponent<CanvasScaler>();
            _canvasGo.AddComponent<GraphicRaycaster>();

            _canvasRect = _canvasGo.GetComponent<RectTransform>();
        }

        public static void ActivateBees()
        {
            _beesQueued += BEES_PER_WAVE;
            _spawnTimer = 0f;
        }

        public static void Update()
        {
            while (PendingBeeCount > 0)
            {
                PendingBeeCount--;
                ActivateBees();
            }

            if (_beesQueued > 0)
            {
                _spawnTimer -= Time.deltaTime;
                if (_spawnTimer <= 0f)
                {
                    SpawnBee();
                    _beesQueued--;
                    _spawnTimer = SPAWN_INTERVAL;
                }
            }

            for (int i = _bees.Count - 1; i >= 0; i--)
            {
                if (_bees[i].Update())
                    _bees.RemoveAt(i);
            }
        }

        private static void SpawnBee()
        {
            var sprite = GetBeeSprite();
            if (sprite == null) return;

            try
            {
                EnsureCanvas();

                float w = Screen.width;
                float h = Screen.height;
                float beeSize = h * 0.105f;

                float randomY = UnityEngine.Random.Range(-h * 0.45f, h * 0.45f);

                bool leftToRight = UnityEngine.Random.value > 0.5f;
                float startX = leftToRight ? (-w * 0.5f - beeSize) : (w * 0.5f + beeSize);
                float endX   = leftToRight ? ( w * 0.5f + beeSize) : (-w * 0.5f - beeSize);

                var beeGo = new GameObject("Bee");
                beeGo.transform.SetParent(_canvasGo.transform, false);

                var rt = beeGo.AddComponent<RectTransform>();
                rt.sizeDelta = new Vector2(beeSize, beeSize);
                rt.anchoredPosition = new Vector2(startX, randomY);

                var img = beeGo.AddComponent<Image>();
                img.sprite = sprite;
                img.preserveAspect = true;
                rt.localScale = new Vector3(leftToRight ? 1f : -1f, 1f, 1f);

                float speed = UnityEngine.Random.Range(w * 0.28f, w * 0.49f); // pixels/sec (30% slower)
                float bobFreq = UnityEngine.Random.Range(4f, 8f);
                float bobAmp  = UnityEngine.Random.Range(h * 0.01f, h * 0.025f);

                _bees.Add(new BeeFlight(beeGo, rt, endX, leftToRight, speed, bobFreq, bobAmp));
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[BloonsArchipelago] Error spawning bee: {ex.Message}");
            }
        }

        public static void CleanupAll()
        {
            foreach (var bee in _bees)
                bee.Destroy();
            _bees.Clear();
            _beesQueued = 0;

            if (_canvasGo != null)
            {
                UnityEngine.Object.Destroy(_canvasGo);
                _canvasGo = null;
                _canvasRect = null;
            }
        }
    }

    internal class BeeFlight
    {
        private readonly GameObject _go;
        private readonly RectTransform _rt;
        private readonly float _endX;
        private readonly bool _leftToRight;
        private readonly float _speed;
        private readonly float _bobFreq;
        private readonly float _bobAmp;
        private readonly float _startY;
        private float _time = 0f;

        public BeeFlight(GameObject go, RectTransform rt, float endX, bool leftToRight, float speed, float bobFreq, float bobAmp)
        {
            _go = go;
            _rt = rt;
            _endX = endX;
            _leftToRight = leftToRight;
            _speed = speed;
            _bobFreq = bobFreq;
            _bobAmp = bobAmp;
            _startY = rt.anchoredPosition.y;
        }

        public bool Update()
        {
            if (_go == null || _rt == null) return true;

            _time += Time.deltaTime;

            float x = _rt.anchoredPosition.x + (_leftToRight ? 1f : -1f) * _speed * Time.deltaTime;
            float y = _startY + Mathf.Sin(_time * _bobFreq) * _bobAmp;
            _rt.anchoredPosition = new Vector2(x, y);

            bool passed = _leftToRight ? (x >= _endX) : (x <= _endX);
            if (passed)
            {
                UnityEngine.Object.Destroy(_go);
                return true;
            }
            return false;
        }

        public void Destroy()
        {
            if (_go != null)
                UnityEngine.Object.Destroy(_go);
        }
    }
}
