using MelonLoader;
using System;
using UnityEngine;

namespace BloonsArchipelago.Patches.InMap
{
    internal static class ZoomTrapManager
    {
        public static volatile int PendingZoomCount = 0;

        private const float DURATION = 10f;
        private const float ZOOM_FACTOR = 0.25f;
        private const float FALLBACK_DIST = 300f;

        private static bool _active = false;
        private static float _endTime;
        private static Camera _camera;
        private static float _savedFov;
        private static float _savedOrthoSize;
        private static bool _savedOrthographic;
        private static Vector3 _savedPosition;

        public static bool IsActive => _active;

        public static void Update()
        {
            while (PendingZoomCount > 0)
            {
                PendingZoomCount--;
                try { Activate(); }
                catch (Exception ex) { MelonLogger.Warning($"[ZoomTrap] Error: {ex}"); }
            }

            if (!_active) return;

            if (Time.unscaledTime >= _endTime)
            {
                Deactivate();
                return;
            }

            try { Tick(); }
            catch { }
        }

        private static void Tick()
        {
            if (_camera == null) return;

            float nx = Mathf.Clamp01(Input.mousePosition.x / Screen.width) - 0.5f;
            float ny = Mathf.Clamp01(Input.mousePosition.y / Screen.height) - 0.5f;
            float h0, hz;
            if (_savedOrthographic)
            {
                h0 = _savedOrthoSize * 2f;
                hz = _savedOrthoSize * ZOOM_FACTOR * 2f;
                _camera.orthographicSize = _savedOrthoSize * ZOOM_FACTOR;
            }
            else
            {
                float dist = FALLBACK_DIST;
                Vector3 fwd = _camera.transform.forward;
                if (Mathf.Abs(fwd.z) > 0.05f)
                {
                    float t = (0f - _savedPosition.z) / fwd.z;
                    if (t > 1f) dist = t;
                }
                h0 = 2f * dist * Mathf.Tan(_savedFov * 0.5f * Mathf.Deg2Rad);
                hz = 2f * dist * Mathf.Tan(_savedFov * ZOOM_FACTOR * 0.5f * Mathf.Deg2Rad);
                _camera.fieldOfView = _savedFov * ZOOM_FACTOR;
            }

            float w0 = h0 * _camera.aspect;
            float wz = hz * _camera.aspect;

            _camera.transform.position = _savedPosition + new Vector3(nx * (w0 - wz), ny * (h0 - hz), 0f);
        }

        private static void Activate()
        {
            if (_active)
            {
                _endTime += DURATION;
                MelonLogger.Msg("[ZoomTrap] Extended zoom duration.");
                return;
            }

            var cam = Camera.main;
            if (cam == null)
            {
                MelonLogger.Warning("[ZoomTrap] No main camera found.");
                return;
            }

            _camera = cam;
            _savedOrthographic = cam.orthographic;
            _savedFov = cam.fieldOfView;
            _savedOrthoSize = cam.orthographicSize;
            _savedPosition = cam.transform.position;
            _active = true;
            _endTime = Time.unscaledTime + DURATION;
            MelonLogger.Msg("[ZoomTrap] Activated — zoomed in, view follows the cursor!");
        }

        private static void Deactivate()
        {
            _active = false;
            try
            {
                if (_camera != null)
                {
                    if (_savedOrthographic) _camera.orthographicSize = _savedOrthoSize;
                    else _camera.fieldOfView = _savedFov;
                    _camera.transform.position = _savedPosition;
                }
                MelonLogger.Msg("[ZoomTrap] Deactivated — zoom restored.");
            }
            catch (Exception ex) { MelonLogger.Warning($"[ZoomTrap] Restore failed: {ex.Message}"); }
            _camera = null;
        }

        public static void CleanupAll()
        {
            PendingZoomCount = 0;
            if (_active) Deactivate();
        }
    }
}
