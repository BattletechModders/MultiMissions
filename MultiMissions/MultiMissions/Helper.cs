using BattleTech;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MultiMissions {

    public class SaveFields {
        public int missionNumber;
        public int contractValue;
        public int originalInitValue;
        public int currentMultiMissions;
        public Dictionary<string, int> alreadyRaised = new Dictionary<string, int>();

        public SaveFields(int missionNumber, int contractValue, int originalInitValue, Dictionary<string, int> alreadyRaised, int currentMultiMissions) {
            this.missionNumber = missionNumber;
            this.contractValue = contractValue;
            this.originalInitValue = originalInitValue;
            this.alreadyRaised = alreadyRaised;
            this.currentMultiMissions = currentMultiMissions;
        }
    }

    public class Helper {
        public static Settings LoadSettings() {
            try {
                using (StreamReader r = new StreamReader("mods/MultiMissions/settings.json")) {
                    string json = r.ReadToEnd();
                    return JsonConvert.DeserializeObject<Settings>(json);
                }
            }
            catch (Exception ex) {
                Logger.LogError(ex);
                return null;
            }
        }

        public static void SaveState(string instanceGUID, DateTime saveTime) {
            try {
                int unixTimestamp = (int)(saveTime.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                string filePath = "mods/MultiMissions/saves/" + instanceGUID + "-" + unixTimestamp + ".json";
                (new FileInfo(filePath)).Directory.Create();
                using (StreamWriter writer = new StreamWriter(filePath, true)) {
                    SaveFields fields = new SaveFields(Fields.missionNumber, Fields.contractValue, Fields.originalInitValue, Fields.alreadyRaised, Fields.currentMultiMissions);
                    string json = JsonConvert.SerializeObject(fields);
                    writer.Write(json);
                }
            }
            catch (Exception ex) {
                Logger.LogError(ex);
            }
        }

        public static void LoadState(string instanceGUID, DateTime saveTime) {
            try {
                int unixTimestamp = (int)(saveTime.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                string filePath = "mods/MultiMissions/saves/" + instanceGUID + "-" + unixTimestamp + ".json";
                if (File.Exists(filePath)) {
                    using (StreamReader r = new StreamReader(filePath)) {
                        string json = r.ReadToEnd();
                        SaveFields save = JsonConvert.DeserializeObject<SaveFields>(json);
                        Fields.alreadyRaised = save.alreadyRaised;
                        Fields.contractValue = save.contractValue;
                        Fields.missionNumber = save.missionNumber;
                        Fields.originalInitValue = save.originalInitValue;
                    }
                }
            }
            catch (Exception ex) {
                Logger.LogError(ex);
            }
        }
    }
}