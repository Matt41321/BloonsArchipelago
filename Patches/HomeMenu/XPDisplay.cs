using BTD_Mod_Helper.Api.Enums;
using BTD_Mod_Helper.Api;

using BTD_Mod_Helper.Extensions;

using HarmonyLib;

using Il2Cpp;
using Il2CppAssets.Scripts.Unity;
using Il2CppAssets.Scripts.Unity.UI_New.Main.PlayerInfo;

using UnityEngine;
using UnityEngine.UI;

namespace BloonsArchipelago.Patches.HomeMenu
{
    [HarmonyPatch(typeof(PlayerInfo), nameof(PlayerInfo.UpdateDisplay))]
    internal class XPDisplay
    {
        private const string MedalTextName = "AP_MedalText";
        private const string MedalImageName = "AP_MedalImage";

        [HarmonyPrefix]
        private static bool Prefix(PlayerInfo __instance)
        {
            var existingText = __instance.transform.Find(MedalTextName);
            var existingImage = __instance.transform.Find(MedalImageName);

            if (!BloonsArchipelago.sessionHandler.ready)
            {
                if (existingText != null) Object.Destroy(existingText.gameObject);
                if (existingImage != null) Object.Destroy(existingImage.gameObject);
                return true;
            }

            Sprite ArchipelagoActiveSprite = ModContent.GetSprite<BloonsArchipelago>("ArchipelagoLogo", 50);
            __instance.rankImg.sprite = ArchipelagoActiveSprite;
            __instance.rankImg.transform.localScale = new Vector3(0.8f, 0.8f, 1);
            __instance.rankImg.transform.localPosition = new Vector3(460, -160, 0);
            __instance.bar.gameObject.SetActive(false);

            // Create medal count text once; reuse on subsequent calls
            NK_TextMeshProUGUI medalTMP;
            if (existingText == null)
            {
                var medalTextObj = Object.Instantiate(__instance.level.gameObject, __instance.gameObject.transform);
                medalTextObj.name = MedalTextName;
                medalTextObj.transform.localPosition = new Vector3(1625, -100, 0);
                medalTMP = medalTextObj.GetComponent<NK_TextMeshProUGUI>();
                medalTMP.textWrappingMode = Il2CppTMPro.TextWrappingModes.NoWrap;
            }
            else
            {
                medalTMP = existingText.GetComponent<NK_TextMeshProUGUI>();
            }
            medalTMP.text = BloonsArchipelago.sessionHandler.Medals + "/" + BloonsArchipelago.sessionHandler.MedalRequirement;

            // Create medal icon once
            if (existingImage == null)
            {
                var medalImageObj = Object.Instantiate(__instance.rankImg.gameObject, __instance.gameObject.transform);
                medalImageObj.name = MedalImageName;
                medalImageObj.transform.localPosition = new Vector3(1400, -160, 0);
                medalImageObj.GetComponent<Image>().SetSprite(Game.instance.CreateSpriteReference(VanillaSprites.MedalGold));
            }

            __instance.level.text = BloonsArchipelago.sessionHandler.XPTracker.Level.ToString();
            __instance.xpInfo.text = BloonsArchipelago.sessionHandler.XPTracker.Maxed
                ? "MAX"
                : BloonsArchipelago.sessionHandler.XPTracker.XP + "/" + BloonsArchipelago.sessionHandler.XPTracker.XPToNext;

            return false;
        }
    }
}
