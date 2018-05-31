using BattleTech;
using BattleTech.Framework;
using BattleTech.UI;
using Harmony;
using System;

namespace MultiMissions {

    [HarmonyPatch(typeof(AAR_ContractResults_Screen), "ReceiveButtonPress")]
    public static class AAR_ContractResults_Screen_ReceiveButtonPress_Patch {
        static void Prefix(AAR_ContractResults_Screen __instance, ref int __result, Contract contract) {
            try {
                
            }
            catch (Exception e) {
                Logger.LogError(e);
            }
        }
    }
}