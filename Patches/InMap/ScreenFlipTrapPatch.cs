using MelonLoader;
using System;
using UnityEngine;

namespace BloonsArchipelago.Patches.InMap
{
    internal static class ScreenFlipTrapManager
    {
        public static volatile int PendingScreenFlipCount = 0;

        private const float DURATION = 10f;

        private static bool _active = false;
        private static float _endTime;
        private static Camera _camera;
        private static Quaternion _savedRotation;

        public static bool IsActive => _active;

        public static void Update()
        {
            while (PendingScreenFlipCount > 0)
            {
                PendingScreenFlipCount--;
                try { Activate(); }
                catch (Exception ex) { MelonLogger.Warning($"[ScreenFlipTrap] Error: {ex}"); }
            }

            if (!_active) return;

            if (Time.unscaledTime >= _endTime)
            {
                Deactivate();
                return;
            }

            try
            {
                if (_camera != null)
                    _camera.transform.rotation = _savedRotation * Quaternion.Euler(0f, 0f, 180f);
            }
            catch { }
        }

        private static void Activate()
        {
            if (_active)
            {
                _endTime += DURATION;
                MelonLogger.Msg("[ScreenFlipTrap] Extended flip duration.");
                return;
            }

            var cam = Camera.main;
            if (cam == null)
            {
                MelonLogger.Warning("[ScreenFlipTrap] No main camera found.");
                return;
            }

            _camera = cam;
            _savedRotation = cam.transform.rotation;
            _active = true;
            _endTime = Time.unscaledTime + DURATION;
            MelonLogger.Msg("[ScreenFlipTrap] Activated — screen flipped!");
        }

        private static void Deactivate()
        {
            _active = false;
            try
            {
                if (_camera != null)
                    _camera.transform.rotation = _savedRotation;
                MelonLogger.Msg("[ScreenFlipTrap] Deactivated — view restored.");
            }
            catch (Exception ex) { MelonLogger.Warning($"[ScreenFlipTrap] Restore failed: {ex.Message}"); }
            _camera = null;
        }

        public static void CleanupAll()
        {
            PendingScreenFlipCount = 0;
            if (_active) Deactivate();
        }
    }
}
