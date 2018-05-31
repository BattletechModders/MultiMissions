using BattleTech;
using Newtonsoft.Json;
using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MultiMissions {
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

        public static SalvageDef CreateMechPart(Contract contract, SimGameConstants sc, MechDef m) {
            SalvageDef salvageDef = new SalvageDef();
            salvageDef.Type = SalvageDef.SalvageType.MECH_PART;
            salvageDef.ComponentType = ComponentType.MechPart;
            salvageDef.Count = 1;
            salvageDef.Weight = sc.Salvage.DefaultMechPartWeight;
            DescriptionDef description = m.Description;
            DescriptionDef description2 = new DescriptionDef(description.Id, string.Format("{0} {1}", description.Name, sc.Story.DefaultMechPartName), description.Details, description.Icon, description.Cost, description.Rarity, description.Purchasable, description.Manufacturer, description.Model, description.UIName);
            salvageDef.Description = description2;
            salvageDef.RewardID = contract.GenerateRewardUID();
            return salvageDef;
        }

        public static string GetGameObjectPath(GameObject obj) {
            string path = "/" + obj.name;
            while (obj.transform.parent != null) {
                obj = obj.transform.parent.gameObject;
                path = "/" + obj.name + path;
            }
            return path;
        }

        public static void RecursivelyPrintGameObject(GameObject gameObject, int indent = 0) {
            Logger.LogLine($"{new string(' ', indent)}{GetGameObjectPath(gameObject)}");

            var components = gameObject.GetComponents(typeof(Component));
            foreach (var component in components) {
                Logger.LogLine($"{new string(' ', indent)}  -> {component.GetType()}");
            }

            Logger.LogLine("");

            foreach (Transform tChild in gameObject.transform) {
                RecursivelyPrintGameObject(tChild.gameObject, indent + 2);

            }
        }

        public static void PrintAllObjectsInCurrentScene() {
            var scene = SceneManager.GetActiveScene();
            var rootObjects = scene.GetRootGameObjects();

            foreach (var rootObject in rootObjects) {
                RecursivelyPrintGameObject(rootObject);
                Logger.LogLine("");
            }
        }
    }
}