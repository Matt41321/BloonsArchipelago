using HarmonyLib;
using Il2Cpp;
using Il2CppAssets.Scripts.Unity.UI_New.Main.MapSelect;
using Il2CppAssets.Scripts.Unity.UI_New.Main.ModeSelect;

namespace BloonsArchipelago.Patches.MapMenu
{
    [HarmonyPatch(typeof(ModeButton), nameof(ModeButton.ButtonClicked))]
    internal class GetCurrentMode
    {
        [HarmonyPostfix]
        private static void Postfix(ModeButton __instance)
        {
            BloonsArchipelago.sessionHandler.currentMode = __instance.modeType;
        }
    }

    [HarmonyPatch(typeof(MapButton), nameof(MapButton.OnClick))]
    internal class GetCurrentMap
    {
        [HarmonyPostfix]
        private static void Postfix(MapButton __instance)
        {
            BloonsArchipelago.sessionHandler.currentMap = __instance.mapId;
        }
    }

    [HarmonyPatch(typeof(ContinueGamePanel), nameof(ContinueGamePanel.ContinueClicked))]
    internal class ContinuePatch
    {
        [HarmonyPostfix]
        private static void Postfix(ContinueGamePanel __instance)
        {
            BloonsArchipelago.sessionHandler.currentMode = __instance.saveData.modeName;
        }
    }
}
