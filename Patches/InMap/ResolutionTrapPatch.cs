using MelonLoader;
using System;
using UnityEngine;

namespace BloonsArchipelago.Patches.InMap
{
    internal static class ResolutionTrapManager
    {
        public static volatile int PendingResolutionTrapCount = 0;

        private static bool _active = false;
        private static float _endTime = 0f;
        private static int _savedWidth = 0;
        private static int _savedHeight = 0;
        private static FullScreenMode _savedMode;

        public static bool IsActive => _active;

        public static void Update()
        {
            while (PendingResolutionTrapCount > 0)
            {
                PendingResolutionTrapCount--;
                Activate();
            }

            if (!_active) return;

            if (Time.unscaledTime >= _endTime)
                Deactivate();
        }

        private static void Activate()
        {
            if (_active)
            {
                _endTime += 10f;
                MelonLogger.Msg($"[144pTrap] Extended — ends at {_endTime}");
                return;
            }

            _savedWidth = Screen.width;
            _savedHeight = Screen.height;
            _savedMode = Screen.fullScreenMode;
            _active = true;
            _endTime = Time.unscaledTime + 10f;

            try
            {
                Screen.SetResolution(256, 144, _savedMode);
                MelonLogger.Msg("[144pTrap] Activated — resolution set to 256x144");
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[144pTrap] Failed to set resolution: {ex.Message}");
            }
        }

        private static void Deactivate()
        {
            _active = false;
            try
            {
                Screen.SetResolution(_savedWidth, _savedHeight, _savedMode);
                MelonLogger.Msg($"[144pTrap] Deactivated — resolution restored to {_savedWidth}x{_savedHeight}");
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[144pTrap] Failed to restore resolution: {ex.Message}");
            }
        }

        public static void CleanupAll()
        {
            if (!_active) return;
            Deactivate();
        }
    }
}
