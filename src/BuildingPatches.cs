using System;
using HarmonyLib;

namespace DvergrForHire
{
    /// <summary>
    /// Hired Dvergr never damage building pieces or ships (WearNTear). Their fireball, cluster bomb and ice bolt explode in a
    /// 3 m blast that hits every piece in range (Projectile.DoAOE checks friend or enemy for creatures only), and stray bolts
    /// and melee swings hit pieces too. WearNTear.Damage runs where the hit is worked out: the game running the Dvergr and its
    /// projectiles, which a modded game takes control of. A hired support mage's mistiles are their own creatures; SummonPatches
    /// marks them when they're created, so their explosions are dropped too. A shot that lands after its Dvergr died has no
    /// attacker; ShotPatches says when a hired Dvergr's shot is hitting. Never throws (on error: vanilla damage).
    /// </summary>
    [HarmonyPatch]
    internal static class BuildingPatches
    {
        private static bool s_errorLogged;

        [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.Damage))]
        [HarmonyPrefix]
        private static bool Damage(HitData hit)
        {
            try
            {
                if (hit == null) return true;
                var attacker = hit.GetAttacker();
                return !HireRules.BuildingSafeFrom(Mercenary.IsHired(attacker), Mercenary.IsHiredSummon(attacker), ShotPatches.HiredShotHitting);
            }
            catch (Exception e)
            {
                if (!s_errorLogged)
                {
                    s_errorLogged = true;
                    Plugin.Log.LogError($"Building protection failed (logged once): {e}");
                }
                return true;
            }
        }
    }
}
