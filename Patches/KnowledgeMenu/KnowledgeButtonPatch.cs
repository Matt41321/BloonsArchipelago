using BTD_Mod_Helper;
using HarmonyLib;
using Il2CppAssets.Scripts.Data.Knowledge;
using Il2CppAssets.Scripts.Models.Knowledge;
using Il2CppAssets.Scripts.Unity.UI_New.Knowledge;

namespace BloonsArchipelago.Patches.KnowledgeMenu
{
    [HarmonyPatch(typeof(KnowledgeSkillBtn), nameof(KnowledgeSkillBtn.SetState))]
    internal class KnowledgeButtonPatch
    {
        [HarmonyPrefix]
        private static void Prefix(KnowledgeSkillBtn __instance, ref KnowlegdeSkillBtnState state)
        {
            var sh = BloonsArchipelago.sessionHandler;
            if (!sh.ready) return;

            KnowledgeModel model = KnowledgeHelper.GetKnowledge(__instance.knowledgeID);
            if (!sh.KnowledgeUnlocked.Contains(model.name))
            {
                state = KnowlegdeSkillBtnState.Locked;
                return;
            }

            if (sh.ProgressiveKnowledgeMode)
            {
                state = KnowlegdeSkillBtnState.Purchased;
            }
            else
            {
                if (sh.LocationChecked(model.name + "-Tree"))
                    state = KnowlegdeSkillBtnState.Purchased;
                else
                    state = KnowlegdeSkillBtnState.Available;
            }
        }
    }
}
