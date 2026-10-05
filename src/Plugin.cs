using System;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DvergrForHire
{
    [BepInPlugin(Guid, Name, PluginVersion)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string Guid = "algo7.dvergrforhire";
        public const string Name = "DvergrForHire";
        public const string PluginVersion = PluginInfo.Version; // from the git tag, generated at build time (MinVer)

        internal static ManualLogSource Log;

        /// <summary>Both hire hooks (hover text, E) are in: without them the prompt would do nothing, so no setup.</summary>
        private static bool s_hireHooks;

        /// <summary>The building protection hook is in (hiring works without it).</summary>
        private static bool s_buildingHook;

        /// <summary>Both hiring post hooks (placing, marking) are in.</summary>
        private static bool s_postHooks;

        private void Awake()
        {
            Log = Logger;
            if (Application.isBatchMode)
            {
                Log.LogInfo($"{Name} loaded (v{PluginVersion}): dedicated server, nothing to do");
                return;
            }
            var harmony = new Harmony(Guid);
            s_hireHooks = TryPatch(harmony, typeof(HirePatches), "hire prompt and E hooks");
            s_buildingHook = TryPatch(harmony, typeof(BuildingPatches), "building protection hook");
            s_postHooks = TryPatch(harmony, typeof(PostPatches), "hiring post hooks");
            SceneManager.sceneLoaded += OnSceneLoaded;
            Log.LogInfo($"{Name} loaded (v{PluginVersion})");
        }

        private void OnDestroy() => SceneManager.sceneLoaded -= OnSceneLoaded;

        /// <summary>
        /// The game scene: ZNetScene.Awake has registered every prefab, and creatures are only created later in
        /// ZNetScene.Update, so the Dvergr prefabs are set up before any Dvergr exists. Never throws.
        /// </summary>
        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            try
            {
                var netScene = ZNetScene.instance;
                if (netScene == null) return; // main menu
                if (!s_hireHooks)
                {
                    Log.LogWarning("Hiring is off: the hire prompt / E hooks couldn't be installed (see above)");
                    return;
                }
                var hiring = DvergrSetup.Run(netScene, out var message);
                if (hiring) Log.LogInfo(message);
                else Log.LogWarning(message);
                if (hiring && s_postHooks)
                {
                    if (PostSetup.Run(netScene, out var posts)) Log.LogInfo(posts);
                    else Log.LogWarning(posts);
                }
                else if (hiring) Log.LogWarning("Hiring posts are off: the post hooks couldn't be installed (see above)");
                if (!s_buildingHook) Log.LogWarning("Hired Dvergr can damage buildings: the protection hook couldn't be installed (see above)");
            }
            catch (Exception e)
            {
                Log.LogError($"Dvergr setup failed, Dvergr stay vanilla: {e}");
            }
        }

        /// <summary>True when patched; false (logged) on failure.</summary>
        private static bool TryPatch(Harmony harmony, Type patches, string what)
        {
            try
            {
                harmony.PatchAll(patches);
                return true;
            }
            catch (Exception e)
            {
                Log.LogError($"Could not install the {what}: {e}");
                return false;
            }
        }
    }
}
