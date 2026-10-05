using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace DvergrForHire
{
    [BepInPlugin(Guid, Name, PluginVersion)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string Guid = "algo7.dvergrforhire";
        public const string Name = "DvergrForHire";
        public const string PluginVersion = PluginInfo.Version; // from the git tag, generated at build time (MinVer)

        internal static ManualLogSource Log;

        private void Awake()
        {
            Log = Logger;
            // Proves HarmonyX resolves from BepInEx.Core alone (no Jötunn here, unlike LoadoutBuffs).
            Log.LogInfo($"{Name} loaded (v{PluginVersion}), HarmonyX {typeof(Harmony).Assembly.GetName().Version}");
        }
    }
}
