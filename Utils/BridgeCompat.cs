using Il2CppAssets.Scripts.Models.Powers;
using Il2CppAssets.Scripts.Simulation;
using Il2CppAssets.Scripts.Unity.Bridge;
using UnityEngine;

namespace BloonsArchipelago.Utils
{
    internal static class BridgeCompat
    {
        public static int GetInputId(UnityToSimulation bridge)
        {
            if (bridge == null) return 0;
            return bridge.HasInput() ? bridge.GetInputId() : 0;
        }

        public static void ActivatePower(UnityToSimulation bridge, Vector2 location, PowerModel model)
        {
            bridge.ActivatePower(GetInputId(bridge), location, model);
        }

        public static void AddCash(UnityToSimulation bridge, double amount, Simulation.CashSource source)
        {
            bridge.AddCash(GetInputId(bridge), amount, source);
        }
    }
}
