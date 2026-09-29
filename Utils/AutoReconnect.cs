using Archipelago.MultiClient.Net;
using MelonLoader;
using System;
using System.Threading.Tasks;
using UnityEngine;

namespace BloonsArchipelago.Utils
{
    // Retries the connection every few seconds after a drop.
    internal static class AutoReconnect
    {
        private const float RetryInterval = 5f;
        private const int ProbeTimeoutMs = 4000;

        private static float _lastAttempt = -RetryInterval;
        private static Task<bool> _probe;
        private static SessionHandler _probeOwner;
        private static string _url, _slot, _password;
        private static int _port;

        public static void Update(string url, int port, string slot, string password)
        {
            var sh = BloonsArchipelago.sessionHandler;
            if (sh == null) return;

            if (sh.ready && !sh.ConnectionLost)
            {
                try
                {
                    if (sh.session != null && !sh.session.Socket.Connected)
                        sh.MarkConnectionLost();
                }
                catch { }
            }

            if (_probe != null)
            {
                if (!_probe.IsCompleted) return;
                bool reachable = _probe.Status == TaskStatus.RanToCompletion && _probe.Result;
                var owner = _probeOwner;
                _probe = null;
                _probeOwner = null;

                if (owner != BloonsArchipelago.sessionHandler || !owner.ConnectionLost) return;

                if (reachable)
                    Reconnect(owner);
                return;
            }

            if (!sh.ConnectionLost) return;

            float now = Time.unscaledTime;
            if (now - _lastAttempt < RetryInterval) return;
            _lastAttempt = now;

            _url = url; _port = port; _slot = slot; _password = password;
            _probeOwner = sh;
            string pUrl = url; int pPort = port;
            _probe = Task.Run(() => Probe(pUrl, pPort));
        }

        private static bool Probe(string url, int port)
        {
            ArchipelagoSession probe = null;
            try
            {
                probe = ArchipelagoSessionFactory.CreateSession(url, port);
                var connect = probe.ConnectAsync();
                return connect.Wait(ProbeTimeoutMs) && connect.Status == TaskStatus.RanToCompletion;
            }
            catch
            {
                return false;
            }
            finally
            {
                try { probe?.Socket?.DisconnectAsync(); } catch { }
            }
        }

        private static void Reconnect(SessionHandler old)
        {
            MelonLogger.Msg("[BloonsArchipelago] Server reachable, reconnecting...");
            try { old.SaveProgress(); } catch { }

            SessionHandler fresh;
            try
            {
                fresh = new SessionHandler(_url, _port, _slot, _password, old.previousNotifications);
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[BloonsArchipelago] Reconnect failed: {ex.Message}");
                return;
            }

            if (!fresh.ready)
            {
                MelonLogger.Warning("[BloonsArchipelago] Reconnect failed, retrying.");
                return;
            }

            fresh.InheritStateFrom(old);
            BloonsArchipelago.sessionHandler = fresh;
            old.Disconnect();
            Patches.InMap.PopTierLockPatch.ResyncChecks();

            fresh.notifications.Enqueue(new APNotification
            {
                ItemName   = "Reconnected.",
                From       = "",
                FullText   = "Reconnected",
                IsOutgoing = true,
                ItemColor  = new Color(0.00f, 1.00f, 0.53f),
            });
            MelonLogger.Msg("[BloonsArchipelago] Reconnected to Archipelago server.");
        }
    }
}
