using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace DvergrForHire
{
    /// <summary>
    /// Marks what a hired Dvergr summons (the support mage's mistiles: their own creatures, so their explosion isn't the
    /// mage's hit) with DvergrForHire_HiredSummon, so the building protection covers them. SpawnAbility calls FindTarget right
    /// before it creates each summon, in the same frame (the mistile has no pre-spawn delay); a creature created in that frame
    /// whose prefab is one of that ability's summons gets the marker. Runs in the game running the mage. Never throws.
    /// </summary>
    [HarmonyPatch]
    internal static class SummonPatches
    {
        private static readonly int s_summonHash = DvergrSettings.SummonKey.GetStableHashCode();
        private static FieldInfo s_ownerField;
        private static bool s_errorLogged;

        /// <summary>The ability that last looked for a target, whether its owner is hired, and in which frame.</summary>
        private static SpawnAbility s_spawner;
        private static bool s_spawnerHired;
        private static int s_frame = -1;

        [HarmonyPatch(typeof(SpawnAbility), "FindTarget")]
        [HarmonyPrefix]
        private static void FindTarget(SpawnAbility __instance)
        {
            try
            {
                if (s_ownerField == null) s_ownerField = AccessTools.Field(typeof(SpawnAbility), "m_owner");
                s_spawner = __instance;
                s_spawnerHired = Mercenary.IsHired(s_ownerField?.GetValue(__instance) as Character);
                s_frame = Time.frameCount;
            }
            catch (Exception e)
            {
                LogOnce(e);
            }
        }

        [HarmonyPatch(typeof(Character), "Awake")]
        [HarmonyPostfix]
        private static void CharacterAwake(Character __instance)
        {
            try
            {
                if (s_spawner == null) return; // nothing summoning: almost always
                if (Time.frameCount != s_frame)
                {
                    s_spawner = null;
                    return;
                }
                var name = Utils.GetPrefabName(__instance.gameObject);
                var isItsSummon = s_spawner.m_spawnPrefab != null && Array.Exists(s_spawner.m_spawnPrefab, p => p != null && p.name == name);
                if (!HireRules.MarkSummon(s_spawnerHired, true, isItsSummon)) return;
                var nview = __instance.GetComponent<ZNetView>();
                if (nview != null && nview.IsValid()) nview.GetZDO().Set(s_summonHash, true);
            }
            catch (Exception e)
            {
                LogOnce(e);
            }
        }

        private static void LogOnce(Exception e)
        {
            if (s_errorLogged) return;
            s_errorLogged = true;
            Plugin.Log.LogError($"Marking a hired Dvergr's summon failed (logged once): {e}");
        }
    }
}
