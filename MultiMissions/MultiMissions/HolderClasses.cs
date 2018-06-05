using BattleTech;
using BattleTech.Framework;
using System.Collections.Generic;

namespace MultiMissions {
    public class Settings {
        public int maxNumberOfMissions = 2;
        public float bonusFactorPerExtraMission = 0.1f;
    }
    
    public static class Fields {
        public static int missionNumber = 1;
        public static int contractValue = 0;
        public static int originalInitValue = 0;
        public static Dictionary<string, int> alreadyRaised = new Dictionary<string, int>();
    }

    public struct PotentialContract {
        // Token: 0x040089A4 RID: 35236
        public ContractOverride contractOverride;

        // Token: 0x040089A5 RID: 35237
        public Faction employer;

        // Token: 0x040089A6 RID: 35238
        public Faction target;

        // Token: 0x040089A7 RID: 35239
        public int difficulty;
    }
}