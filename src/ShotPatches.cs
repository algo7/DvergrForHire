using System;
using HarmonyLib;
using UnityEngine;

namespace DvergrForHire
{
    /// <summary>
    /// A shot (projectile or spell area) fired by a hired Dvergr or its summon, marked when it's set up. Vanilla shots only
    /// know their owner as a live object: once the Dvergr has died, a fireball still in the air hits without an attacker
    /// (so the building protection can't see whose it was) and with no friend-or-enemy check (so it hits you and your tames).
    /// The marker keeps both answers. Local to the game running the shot, never networked.
    /// </summary>
    public sealed class HiredShot : MonoBehaviour
    {
        /// <summary>The firing Dvergr's name (Character.m_name), for a spell area's "don't hit its own kind".</summary>
        internal string OwnerName;
    }

    /// <summary>
    /// Shots from hired Dvergr keep counting as theirs after the Dvergr dies: Projectile.Setup / Aoe.Setup mark them (and what a
    /// marked projectile spawns where it lands, e.g. the fire mage's burning ground); while a marked projectile hits or
    /// explodes (OnHit, DoAOE) building pieces take no damage (BuildingPatches); and once the owner is gone a marked shot
    /// spares players, tames and Dvergr that aren't provoked, as it did with the owner there (IsValidTarget, Aoe.ShouldHit).
    /// Shots only hit on the game running them, where they were marked. Never throws (on error: vanilla).
    /// </summary>
    [HarmonyPatch]
    internal static class ShotPatches
    {
        private static bool s_errorLogged;

        /// <summary>A marked projectile is hitting or exploding right now (read by BuildingPatches).</summary>
        internal static bool HiredShotHitting { get; private set; }

        /// <summary>A marked projectile is spawning what it spawns on hit right now: those inherit its marker.</summary>
        private static HiredShot s_spawningFor;

        [HarmonyPatch(typeof(Projectile), nameof(Projectile.Setup))]
        [HarmonyPostfix]
        private static void ProjectileSetup(Projectile __instance, Character owner) => Mark(__instance, owner);

        [HarmonyPatch(typeof(Aoe), nameof(Aoe.Setup))]
        [HarmonyPostfix]
        private static void AoeSetup(Aoe __instance, Character owner) => Mark(__instance, owner);

        private static void Mark(Component shot, Character owner)
        {
            try
            {
                string ownerName;
                if (Mercenary.IsHired(owner) || Mercenary.IsHiredSummon(owner)) ownerName = owner.m_name;
                else if (s_spawningFor != null) ownerName = s_spawningFor.OwnerName;
                else return;
                shot.gameObject.AddComponent<HiredShot>().OwnerName = ownerName;
            }
            catch (Exception e)
            {
                LogOnce(e);
            }
        }

        [HarmonyPatch(typeof(Projectile), nameof(Projectile.OnHit))]
        [HarmonyPrefix]
        private static void OnHit(Projectile __instance, out bool __state) => StartHit(__instance, out __state);

        [HarmonyPatch(typeof(Projectile), nameof(Projectile.OnHit))]
        [HarmonyFinalizer]
        private static void OnHitDone(bool __state) => HiredShotHitting = __state;

        /// <summary>The explosion, also mid-flight (m_hitMidFlight), not only from OnHit.</summary>
        [HarmonyPatch(typeof(Projectile), "DoAOE")]
        [HarmonyPrefix]
        private static void DoAOE(Projectile __instance, out bool __state) => StartHit(__instance, out __state);

        [HarmonyPatch(typeof(Projectile), "DoAOE")]
        [HarmonyFinalizer]
        private static void DoAOEDone(bool __state) => HiredShotHitting = __state;

        /// <summary>Called by OnHit, and on its own when a shot's time runs out (m_spawnOnTtl).</summary>
        [HarmonyPatch(typeof(Projectile), "SpawnOnHit")]
        [HarmonyPrefix]
        private static void SpawnOnHit(Projectile __instance, out HiredShot __state)
        {
            __state = s_spawningFor;
            try
            {
                s_spawningFor = __instance.GetComponent<HiredShot>();
            }
            catch (Exception e)
            {
                LogOnce(e);
            }
        }

        [HarmonyPatch(typeof(Projectile), "SpawnOnHit")]
        [HarmonyFinalizer]
        private static void SpawnOnHitDone(HiredShot __state) => s_spawningFor = __state;

        /// <summary>Saves the flag for the finalizer (OnHit calls DoAOE), then sets it for this projectile.</summary>
        private static void StartHit(Projectile projectile, out bool previous)
        {
            previous = HiredShotHitting;
            try
            {
                HiredShotHitting = previous || projectile.GetComponent<HiredShot>() != null;
            }
            catch (Exception e)
            {
                LogOnce(e);
            }
        }

        /// <summary>A projectile whose owner is gone: friend or foe as if its hired Dvergr were still there.</summary>
        [HarmonyPatch(typeof(Projectile), "IsValidTarget")]
        [HarmonyPostfix]
        private static void IsValidTarget(Projectile __instance, IDestructible destr, Character ___m_owner, ref bool __result)
        {
            try
            {
                if (!__result || ___m_owner != null || !(destr is Character target)) return; // owner there: vanilla decides
                var shot = __instance.GetComponent<HiredShot>();
                if (shot == null) return;
                // Projectiles have no "hit enemies" or "own kind" flag: vanilla hits every foe, and friends only with m_hitFriendly.
                __result = HireRules.ShotHits(Foe(target), __instance.m_hitFriendly, hitsEnemies: true, hitsSameKind: true, sameKind: false);
            }
            catch (Exception e)
            {
                LogOnce(e);
            }
        }

        /// <summary>A spell area (burning ground, heal, nova) whose owner is gone: the same, with the area's own flags.</summary>
        [HarmonyPatch(typeof(Aoe), "ShouldHit")]
        [HarmonyPostfix]
        private static void ShouldHit(Aoe __instance, Collider collider, Character ___m_owner, ref bool __result)
        {
            try
            {
                if (!__result || ___m_owner != null) return; // owner there: vanilla decides
                var shot = __instance.GetComponent<HiredShot>();
                if (shot == null) return;
                var hit = Projectile.FindHitObject(collider);
                var target = hit != null ? hit.GetComponent<Character>() : null;
                if (target == null) return;
                __result = HireRules.ShotHits(Foe(target), __instance.m_hitFriendly, __instance.m_hitEnemy, __instance.m_hitSame,
                    sameKind: target.m_name == shot.OwnerName);
            }
            catch (Exception e)
            {
                LogOnce(e);
            }
        }

        private static bool Foe(Character target)
        {
            var ai = target.GetBaseAI();
            return HireRules.HiredDvergrFoe(target.GetFaction() == Character.Faction.Players, target.IsTamed(),
                target.GetFaction() == Character.Faction.Dverger, ai != null && ai.IsAggravated());
        }

        private static void LogOnce(Exception e)
        {
            if (s_errorLogged) return;
            s_errorLogged = true;
            Plugin.Log.LogError($"Hired Dvergr's shot handling failed (logged once): {e}");
        }
    }
}
