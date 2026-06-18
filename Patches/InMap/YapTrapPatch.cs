using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BloonsArchipelago.Patches.InMap
{
    internal static class YapTrapManager
    {
        public static volatile int PendingYapCount = 0;

        private const float DURATION       = 4f;
        private const float SPAWN_INTERVAL = 0.18f;
        private const int   SOURCE_POOL    = 8;

        private static AudioClip[] _clips = Array.Empty<AudioClip>();

        private static GameObject _audioGo;
        private static AudioSource[] _sources;
        private static int _sourceIndex;

        private static bool  _active;
        private static float _endTime;
        private static float _nextSpawn;

        public static void Update()
        {
            if (!_active)
            {
                if (PendingYapCount > 0)
                {
                    PendingYapCount--;
                    StartYap();
                }
                return;
            }

            float now = Time.unscaledTime;
            if (now >= _endTime)
            {
                _active = false;
                return;
            }

            if (now >= _nextSpawn)
            {
                PlayOverlap();
                _nextSpawn = now + SPAWN_INTERVAL;
            }
        }

        public static void CleanupAll()
        {
            PendingYapCount = 0;
            _active = false;
            if (_sources != null)
                foreach (var s in _sources)
                    if (s != null) s.Stop();
        }

        private static void StartYap()
        {
            EnsureAudioObject();
            EnsureClips();

            if (_clips.Length == 0)
            {
                MelonLogger.Warning("[YapTrap] No Quincy voice clips found in memory — staying quiet.");
                return;
            }

            _active    = true;
            _endTime   = Time.unscaledTime + DURATION;
            _nextSpawn = 0f;
            MelonLogger.Msg($"[YapTrap] Quincy is yapping with {_clips.Length} clip(s)!");
        }

        private static void PlayOverlap()
        {
            if (_clips.Length == 0 || _sources == null) return;
            FireOneShot(_clips[UnityEngine.Random.Range(0, _clips.Length)]);
            FireOneShot(_clips[UnityEngine.Random.Range(0, _clips.Length)]);
        }

        private static void FireOneShot(AudioClip clip)
        {
            if (clip == null) return;
            var src = _sources[_sourceIndex % _sources.Length];
            _sourceIndex++;
            if (src == null) return;
            src.pitch = UnityEngine.Random.Range(0.85f, 1.25f);
            src.PlayOneShot(clip, UnityEngine.Random.Range(0.8f, 1.0f) * GetSfxVolume());
        }

        private static float GetSfxVolume()
        {
            try
            {
                return Mathf.Clamp01(Il2CppAssets.Scripts.Unity.Audio.AudioPrefs.FxVolume);
            }
            catch
            {
                return 1f;
            }
        }

        private static void EnsureAudioObject()
        {
            if (_audioGo != null && _sources != null) return;
            _audioGo = new GameObject("YapTrapAudio");
            UnityEngine.Object.DontDestroyOnLoad(_audioGo);
            _sources = new AudioSource[SOURCE_POOL];
            for (int i = 0; i < SOURCE_POOL; i++)
            {
                var s = _audioGo.AddComponent<AudioSource>();
                s.playOnAwake  = false;
                s.spatialBlend = 0f;
                s.volume       = 1f;
                _sources[i] = s;
            }
        }

        private static void EnsureClips()
        {
            if (_clips.Length > 0) return;
            try
            {
                var found = new List<AudioClip>();
                foreach (var clip in Resources.FindObjectsOfTypeAll<AudioClip>())
                {
                    if (clip == null) continue;
                    if (clip.name.IndexOf("Quincy", StringComparison.OrdinalIgnoreCase) >= 0)
                        found.Add(clip);
                }
                _clips = found.ToArray();
                if (_clips.Length > 0)
                    MelonLogger.Msg($"[YapTrap] Found Quincy clips: {string.Join(", ", found.Select(c => c.name))}");
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[YapTrap] Clip search failed: {ex.Message}");
            }
        }
    }
}
