using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DvergrForHire
{
    /// <summary>
    /// Makes the vanilla Dvergr prefabs hireable in place, so every Dvergr (spawned, saved, in camps) gets it: a vanilla
    /// Tameable (commandable, never fed: vanilla taming never starts) and a Mercenary. A pre-flight checks everything first;
    /// on any problem nothing changes and the Dvergr stay vanilla.
    /// </summary>
    internal static class DvergrSetup
    {
        public static bool Run(ZNetScene scene, out string message)
        {
            var prefabs = DvergrSettings.Hireable.Select(h => scene.GetPrefab(h.Prefab)).ToList();
            if (prefabs.All(p => p != null && p.GetComponent<Mercenary>() != null))
            {
                SetStatics(scene); // cheap insurance: the coin name and hire sound from this scene's prefabs
                message = "Dvergr already hireable (the prefabs survived the scene change)";
                return true;
            }
            var problem = PreFlight(scene, prefabs);
            if (problem != null)
            {
                message = "DvergrForHire leaves the Dvergr alone: " + problem;
                return false;
            }
            SetStatics(scene);
            foreach (var prefab in prefabs)
            {
                SettingsApplier.Apply(prefab.AddComponent<Tameable>(), DvergrSettings.TameableSettings, scene.GetPrefab);
                prefab.AddComponent<Mercenary>().m_basePrice = DvergrSettings.BasePrice(prefab.name);
            }
            message = "Dvergr can be hired: " + string.Join(", ", DvergrSettings.Hireable.Select(h => $"{h.Prefab} {h.Price}"));
            return true;
        }

        private static void SetStatics(ZNetScene scene)
        {
            var coins = scene.GetPrefab(DvergrSettings.Coins);
            var drop = coins != null ? coins.GetComponent<ItemDrop>() : null;
            if (drop != null) Mercenary.s_coinName = drop.m_itemData.m_shared.m_name;
            if (scene.GetPrefab(DvergrSettings.HireSound) != null)
                Mercenary.s_hireEffect = SettingsApplier.Effects(new[] { DvergrSettings.HireSound }, scene.GetPrefab);
        }

        /// <summary>Null when everything the setup needs is there and no other mod made the Dvergr tameable; else why not.</summary>
        private static string PreFlight(ZNetScene scene, List<GameObject> prefabs)
        {
            foreach (var name in DvergrSettings.RequiredPrefabs)
                if (scene.GetPrefab(name) == null) return $"the game has no prefab '{name}' (game update?)";
            if (scene.GetPrefab(DvergrSettings.Coins).GetComponent<ItemDrop>() == null)
                return $"{DvergrSettings.Coins} is not an item (game update?)";
            foreach (var name in DvergrSettings.EffectPrefabs)
                if (scene.GetPrefab(name).GetComponent<ZNetView>() == null) return $"the effect '{name}' isn't networked (game update?)";
            foreach (var setting in DvergrSettings.TameableSettings)
            {
                var why = setting.Problem();
                if (why != null) return why + " (game update?)";
            }
            foreach (var prefab in prefabs)
            {
                if (prefab.GetComponent<Humanoid>() == null || prefab.GetComponent<MonsterAI>() == null)
                    return $"{prefab.name} isn't a Humanoid with a MonsterAI (game update or another mod)";
                if (prefab.GetComponent<Mercenary>() != null) // before the Tameable check: our own half-finished setup
                    return "only some Dvergr were set up earlier this session: restart the game";
                if (prefab.GetComponent<Tameable>() != null)
                    return $"{prefab.name} can already be tamed: another mod changed the Dvergr";
            }
            return null;
        }
    }
}
