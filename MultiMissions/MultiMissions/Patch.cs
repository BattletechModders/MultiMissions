using BattleTech;
using BattleTech.Data;
using BattleTech.Framework;
using BattleTech.UI;
using Harmony;
using HBS.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TMPro;
using UnityEngine;

namespace MultiMissions {

    [HarmonyPatch(typeof(StarSystem), "ResetContracts")]
    public static class StarSystem_ResetContracts_Patch {

        static void Prefix(StarSystem __instance) {
            try {
                foreach (Contract con in __instance.SystemContracts) {
                    if (Fields.alreadyRaised.ContainsKey(con)) {
                        Fields.alreadyRaised.Remove(con);
                    }
                }
            }
            catch (Exception e) {
                Logger.LogError(e);
            }
        }
    }

    [HarmonyPatch(typeof(Contract), "SetNegotiatedValues")]
    public static class Contract_SetNegotiatedValues_Patch {

        static void Postfix(Contract __instance) {
            try {
                if (Fields.missionNumber == 1) {
                    Fields.originalInitValue = Mathf.RoundToInt(__instance.InitialContractValue);
                    Fields.contractValue = Mathf.RoundToInt(Fields.originalInitValue * __instance.PercentageContractValue);
                }
            }
            catch (Exception e) {
                Logger.LogError(e);
            }
        }
    }
    [HarmonyPatch(typeof(Contract), "CompleteContract")]
    public static class Contract_CompleteContract_Patch {

        static void Prefix(Contract __instance) {
            try {
                if (Fields.alreadyRaised.ContainsKey(__instance)) {
                    Fields.alreadyRaised.Remove(__instance);
                }
                Settings settings = Helper.LoadSettings();
                ReflectionHelper.InvokePrivateMethode(__instance, "set_InitialContractValue", new object[] { Mathf.RoundToInt(Fields.contractValue / Fields.alreadyRaised[__instance]) });
                ReflectionHelper.InvokePrivateMethode(__instance, "set_PercentageContractValue", new object[] { 1f });
            }
            catch (Exception e) {
                Logger.LogError(e);
            }
        }
    }

    [HarmonyPatch(typeof(SGContractsListItem), "Init")]
    public static class SGContractsListItem_Init_Patch {
        static void Prefix(SGContractsListItem __instance, Contract contract) {
            try {
                if (!Fields.alreadyRaised.ContainsKey(contract)) {
                    Settings settings = Helper.LoadSettings();
                    Thread.Sleep(20);
                    System.Random rnd = new System.Random();
                    int randMissions = rnd.Next(1, settings.numberOfMissions + 1);
                    ReflectionHelper.InvokePrivateMethode(contract, "set_InitialContractValue", new object[] {
                        Mathf.RoundToInt(contract.InitialContractValue * randMissions * (1 + (randMissions-1 * settings.bonusFactorPerExtraMission)))
                    });
                    Fields.alreadyRaised.Add(contract, randMissions);
                }
            }
            catch (Exception e) {
                Logger.LogError(e);
            }
        }

        static void Postfix(SGContractsListItem __instance, Contract contract) {
            try {
                if (Fields.alreadyRaised[contract] != 1) {
                    TextMeshProUGUI contractName = (TextMeshProUGUI)ReflectionHelper.GetPrivateField(__instance, "contractName");
                    ReflectionHelper.InvokePrivateMethode(__instance, "setFieldText", new object[] { contractName, contract.Override.contractName + " (" + Fields.alreadyRaised[contract] + " Missions)" });
                }
            }
            catch (Exception e) {
                Logger.LogError(e);
            }
        }

        [HarmonyPatch(typeof(AAR_SalvageScreen), "OnCompleted")]
        public static class AAR_SalvageScreen_OnCompleted_Patch {
            static void Postfix(AAR_SalvageScreen __instance) {
                try {
                    Settings settings = Helper.LoadSettings();
                    Contract c = (Contract)ReflectionHelper.GetPrivateField(__instance, "contract");
                    if (Fields.missionNumber < Fields.alreadyRaised[c]) {
                        Contract newcon = GetNewContract(__instance.Sim, c);
                        newcon.Override.disableNegotations = true;
                        newcon.Override.disableCancelButton = true;
                        ReflectionHelper.InvokePrivateMethode(newcon, "set_InitialContractValue", new object[] { Mathf.RoundToInt(Fields.originalInitValue / Fields.alreadyRaised[c]) });
                        newcon.Override.negotiatedSalary = c.PercentageContractValue;
                        newcon.Override.negotiatedSalvage = c.PercentageContractSalvage;
                        __instance.Sim.ForceTakeContract(newcon, false);
                        Fields.alreadyRaised.Add(newcon, Fields.alreadyRaised[c]);
                        Fields.missionNumber++;
                    }
                    else {
                        Fields.missionNumber = 1;
                    }
                }
                catch (Exception e) {
                    Logger.LogError(e);
                }
            }
        }
        private static Contract GetNewContract(SimGameState Sim, Contract oldcontract) {
            ContractDifficulty minDiffClamped = (ContractDifficulty)ReflectionHelper.InvokePrivateMethode(Sim, "GetDifficultyEnumFromValue", new object[] { oldcontract.Difficulty });
            ContractDifficulty maxDiffClamped = (ContractDifficulty)ReflectionHelper.InvokePrivateMethode(Sim, "GetDifficultyEnumFromValue", new object[] { oldcontract.Difficulty });
            StarSystem system;
            List<Contract> contractList = new List<Contract>();
            system = Sim.CurSystem;
            int maxContracts = 1;
            int debugCount = 0;
            while (contractList.Count < maxContracts && debugCount < 1000) {
                WeightedList<MapAndEncounters> contractMaps = new WeightedList<MapAndEncounters>(WeightedListType.SimpleRandom, null, null, 0);
                List<ContractType> contractTypes = new List<ContractType>();
                Dictionary<ContractType, List<ContractOverride>> potentialOverrides = new Dictionary<ContractType, List<ContractOverride>>();
                ContractType[] singlePlayerTypes = (ContractType[])ReflectionHelper.GetPrivateStaticField(typeof(SimGameState), "singlePlayerTypes");
                using (MetadataDatabase metadataDatabase = new MetadataDatabase()) {
                    foreach (Contract_MDD contract_MDD in metadataDatabase.GetContractsByDifficultyRange(oldcontract.Difficulty - 1, oldcontract.Difficulty + 1)) {
                        ContractType contractType = contract_MDD.ContractTypeEntry.ContractType;
                        if (singlePlayerTypes.Contains(contractType)) {
                            if (!contractTypes.Contains(contractType)) {
                                contractTypes.Add(contractType);
                            }
                            if (!potentialOverrides.ContainsKey(contractType)) {
                                potentialOverrides.Add(contractType, new List<ContractOverride>());
                            }
                            ContractOverride item = Sim.DataManager.ContractOverrides.Get(contract_MDD.ContractID);
                            potentialOverrides[contractType].Add(item);
                        }
                    }
                    foreach (MapAndEncounters element in metadataDatabase.GetReleasedMapsAndEncountersByContractTypeAndTags(singlePlayerTypes, system.Def.MapRequiredTags, system.Def.MapExcludedTags, system.Def.SupportedBiomes)) {
                        if (!contractMaps.Contains(element)) {
                            contractMaps.Add(element, 0);
                        }
                    }
                }
                if (contractMaps.Count == 0) {
                    Logger.LogLine("Maps0 break");
                    break;
                }
                if (potentialOverrides.Count == 0) {
                    Logger.LogLine("Overrides0 break");
                    break;
                }
                contractMaps.Reset(false);
                WeightedList<Faction> validEmployers = new WeightedList<Faction>(WeightedListType.SimpleRandom, null, null, 0);
                Dictionary<Faction, WeightedList<Faction>> validTargets = new Dictionary<Faction, WeightedList<Faction>>();

                Dictionary<Faction, FactionDef> factions = (Dictionary<Faction, FactionDef>)ReflectionHelper.GetPrivateField(Sim, "factions");

                foreach (Faction faction in system.Def.ContractEmployers) {
                    foreach (Faction faction2 in factions[faction].Enemies) {
                        if (system.Def.ContractTargets.Contains(faction2)) {
                            if (!validTargets.ContainsKey(faction)) {
                                validTargets.Add(faction, new WeightedList<Faction>(WeightedListType.PureRandom, null, null, 0));
                            }
                            validTargets[faction].Add(faction2, 0);
                        }
                    }
                    if (validTargets.ContainsKey(faction)) {
                        validTargets[faction].Reset(false);
                        validEmployers.Add(faction, 0);
                    }
                }
                validEmployers.Reset(false);

                if (validEmployers.Count <= 0 || validTargets.Count <= 0) {
                    Logger.LogLine(string.Format("Cannot find any valid employers or targets for system {0}", system));
                }
                if (validTargets.Count == 0 || validEmployers.Count == 0) {
                    Logger.LogLine(string.Format("There are no valid employers or employers for the system of {0}. Num valid employers: {1}", system.Name, validEmployers.Count));
                    foreach (Faction faction3 in validTargets.Keys) {
                        Logger.LogLine(string.Format("--- Targets for {0}: {1}", faction3, validTargets[faction3].Count));
                    }

                    break;
                }

                int i = debugCount;
                debugCount = i + 1;
                WeightedList<MapAndEncounters> activeMaps = new WeightedList<MapAndEncounters>(WeightedListType.SimpleRandom, contractMaps.ToList(), null, 0);
                List<MapAndEncounters> discardedMaps = new List<MapAndEncounters>();

                List<string> mapDiscardPile = (List<string>)ReflectionHelper.GetPrivateField(Sim, "mapDiscardPile");

                for (int j = activeMaps.Count - 1; j >= 0; j--) {
                    if (mapDiscardPile.Contains(activeMaps[j].Map.MapID)) {
                        discardedMaps.Add(activeMaps[j]);
                        activeMaps.RemoveAt(j);
                    }
                }
                if (activeMaps.Count == 0) {
                    mapDiscardPile.Clear();
                    foreach (MapAndEncounters element2 in discardedMaps) {
                        activeMaps.Add(element2, 0);
                    }
                }
                activeMaps.Reset(false);
                MapAndEncounters level = null;
                List<EncounterLayer_MDD> validEncounters = new List<EncounterLayer_MDD>();


                Dictionary<ContractType, WeightedList<PotentialContract>> validContracts = new Dictionary<ContractType, WeightedList<PotentialContract>>();
                WeightedList<PotentialContract> flatValidContracts = null;
                do {
                    level = activeMaps.GetNext(false);
                    if (level == null) {
                        break;
                    }
                    validEncounters.Clear();
                    validContracts.Clear();
                    flatValidContracts = new WeightedList<PotentialContract>(WeightedListType.WeightedRandom, null, null, 0);
                    foreach (EncounterLayer_MDD encounterLayer_MDD in level.Encounters) {
                        ContractType contractType2 = encounterLayer_MDD.ContractTypeEntry.ContractType;
                        if (contractTypes.Contains(contractType2)) {
                            if (validContracts.ContainsKey(contractType2)) {
                                validEncounters.Add(encounterLayer_MDD);
                            }
                            else {
                                foreach (ContractOverride contractOverride2 in potentialOverrides[contractType2]) {
                                    bool flag = true;
                                    ContractDifficulty difficultyEnumFromValue = (ContractDifficulty)ReflectionHelper.InvokePrivateMethode(Sim, "GetDifficultyEnumFromValue", new object[] { contractOverride2.difficulty });
                                    Faction employer2 = Faction.INVALID_UNSET;
                                    Faction target2 = Faction.INVALID_UNSET;
                                    object[] args = new object[] { system, validEmployers, validTargets, contractOverride2.requirementList, employer2, target2 };
                                    if (difficultyEnumFromValue >= minDiffClamped && difficultyEnumFromValue <= maxDiffClamped && (bool)ReflectionHelper.InvokePrivateMethode(Sim, "GetValidFaction", args)) {
                                        employer2 = (Faction)args[4];
                                        target2 = (Faction)args[5];
                                        int difficulty = Sim.NetworkRandom.Int(oldcontract.Difficulty, oldcontract.Difficulty + 1);
                                        system.SetCurrentContractFactions(employer2, target2);
                                        int k = 0;
                                        while (k < contractOverride2.requirementList.Count) {
                                            RequirementDef requirementDef = new RequirementDef(contractOverride2.requirementList[k]);
                                            EventScope scope = requirementDef.Scope;
                                            TagSet curTags;
                                            StatCollection stats;
                                            switch (scope) {
                                                case EventScope.Company:
                                                    curTags = Sim.CompanyTags;
                                                    stats = Sim.CompanyStats;
                                                    break;
                                                case EventScope.MechWarrior:
                                                case EventScope.Mech:
                                                    goto IL_88B;
                                                case EventScope.Commander:
                                                    goto IL_8E9;
                                                case EventScope.StarSystem:
                                                    curTags = system.Tags;
                                                    stats = system.Stats;
                                                    break;
                                                default:
                                                    goto IL_88B;
                                            }
                                            IL_803:
                                            for (int l = requirementDef.RequirementComparisons.Count - 1; l >= 0; l--) {
                                                ComparisonDef item2 = requirementDef.RequirementComparisons[l];
                                                if (item2.obj.StartsWith("Target") || item2.obj.StartsWith("Employer")) {
                                                    requirementDef.RequirementComparisons.Remove(item2);
                                                }
                                            }
                                            if (!SimGameState.MeetsRequirements(requirementDef, curTags, stats, null)) {
                                                flag = false;
                                                Logger.LogLine("MeetsRequirements break");
                                                break;
                                            }
                                            k++;
                                            continue;
                                            IL_88B:
                                            if (scope != EventScope.Map) {
                                                throw new Exception("Contracts cannot use the scope of: " + requirementDef.Scope);
                                            }
                                            using (MetadataDatabase metadataDatabase2 = new MetadataDatabase()) {
                                                curTags = metadataDatabase2.GetTagSetForTagSetEntry(level.Map.TagSetID);
                                                stats = new StatCollection();
                                                goto IL_803;
                                            }
                                            IL_8E9:
                                            curTags = Sim.CommanderTags;
                                            stats = Sim.CommanderStats;
                                            goto IL_803;
                                        }
                                        if (flag) {
                                            PotentialContract element3 = default(PotentialContract);
                                            element3.contractOverride = contractOverride2;
                                            element3.difficulty = difficulty;
                                            element3.employer = employer2;
                                            element3.target = target2;
                                            validEncounters.Add(encounterLayer_MDD);
                                            if (!validContracts.ContainsKey(contractType2)) {
                                                validContracts.Add(contractType2, new WeightedList<PotentialContract>(WeightedListType.WeightedRandom, null, null, 0));
                                            }
                                            validContracts[contractType2].Add(element3, contractOverride2.weight);
                                            flatValidContracts.Add(element3, contractOverride2.weight);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                while (validContracts.Count == 0 && level != null);
                system.SetCurrentContractFactions(Faction.INVALID_UNSET, Faction.INVALID_UNSET);
                if (validContracts.Count == 0) {
                    if (mapDiscardPile.Count > 0) {
                        mapDiscardPile.Clear();
                    }
                    else {
                        debugCount = 1000;
                        Logger.LogLine(string.Format("[CONTRACT] Unable to find any valid contracts for available map pool. Alert designers.", new object[0]));
                    }
                }
                else {
                    GameContext gameContext = new GameContext(Sim.Context);
                    gameContext.SetObject(GameContextObjectTagEnum.TargetStarSystem, system);
                    Dictionary<ContractType, List<EncounterLayer_MDD>> finalEncounters = new Dictionary<ContractType, List<EncounterLayer_MDD>>();
                    foreach (EncounterLayer_MDD encounterLayer_MDD2 in validEncounters) {
                        ContractType contractType3 = encounterLayer_MDD2.ContractTypeEntry.ContractType;
                        if (!finalEncounters.ContainsKey(contractType3)) {
                            finalEncounters.Add(contractType3, new List<EncounterLayer_MDD>());
                        }
                        finalEncounters[contractType3].Add(encounterLayer_MDD2);
                    }
                    List<PotentialContract> discardedContracts = new List<PotentialContract>();
                    List<string> contractDiscardPile = (List<string>)ReflectionHelper.GetPrivateField(Sim, "contractDiscardPile");
                    for (int m = flatValidContracts.Count - 1; m >= 0; m--) {
                        if (contractDiscardPile.Contains(flatValidContracts[m].contractOverride.ID)) {
                            discardedContracts.Add(flatValidContracts[m]);
                            flatValidContracts.RemoveAt(m);
                        }
                    }
                    if ((float)discardedContracts.Count >= (float)flatValidContracts.Count * Sim.Constants.Story.DiscardPileToActiveRatio || flatValidContracts.Count == 0) {
                        contractDiscardPile.Clear();
                        foreach (PotentialContract element4 in discardedContracts) {
                            flatValidContracts.Add(element4, 0);
                        }
                    }
                    PotentialContract next = flatValidContracts.GetNext(true);
                    ContractType finalContractType = next.contractOverride.contractType;
                    finalEncounters[finalContractType].Shuffle<EncounterLayer_MDD>();
                    string encounterGuid = finalEncounters[finalContractType][0].EncounterLayerGUID;
                    ContractOverride contractOverride3 = next.contractOverride;
                    Faction employer3 = next.employer;
                    Faction target3 = next.target;
                    int targetDifficulty = next.difficulty;
                    Contract con;
                    con = new Contract(level.Map.MapName, level.Map.MapPath, encounterGuid, finalContractType, Sim.BattleTechGame, contractOverride3, gameContext, true, targetDifficulty, 0, null);
                    mapDiscardPile.Add(level.Map.MapID);
                    contractDiscardPile.Add(contractOverride3.ID);
                    Sim.PrepContract(con, employer3, target3, target3, level.Map.BiomeSkinEntry.BiomeSkin, con.Override.travelSeed, system);
                    contractList.Add(con);
                }
            }
            if (debugCount >= 1000) {
                Logger.LogLine("Unable to fill contract list. Please inform AJ Immediately");
            }
            return contractList[0];
        }


    }
}