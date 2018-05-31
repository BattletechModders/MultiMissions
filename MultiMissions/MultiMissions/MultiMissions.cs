using Harmony;
using System.Reflection;

namespace MultiMissions
{
    public class MultiMissions
    {
        public static void Init() {
            var harmony = HarmonyInstance.Create("de.morphyum.MultiMissions");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}
