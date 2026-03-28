using HarmonyLib;
using Il2CppAssets.Scripts.Unity.Player;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.TowerSelectionMenu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BloonsArchipelago.Patches.InMap
{
    [HarmonyPatch(typeof(UpgradeButton), nameof(UpgradeButton.HasUnlockedUpgrade))]
    internal class UpgradesWontWork
    {
        [HarmonyPrefix]
        public static bool Prefix(ref bool __result)
        {
            if (BloonsArchipelago.sessionHandler.ready)
            {
                __result = true;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Btd6Player), nameof(Btd6Player.HasPurchasedParagonUpgrade))]
    internal class ParagonUnlocker
    {
        [HarmonyPrefix]
        public static bool Prefix(ref bool __result)
        {
            if (BloonsArchipelago.sessionHandler.ready)
            {
                __result = true;
                return false;
            }
            return true;
        }
    }
}
