using System;
using HarmonyLib;

namespace DvergrForHire
{
    /// <summary>
    /// The hover text and E of Dvergr, and hired Dvergr never being hungry, so they regain health (only objects with a
    /// Mercenary; every other tameable stays vanilla). Character's hover text comes from its Tameable, and Player finds the
    /// Tameable as the hovered object's Interactable. Never throws (on error: vanilla).
    /// </summary>
    [HarmonyPatch]
    internal static class HirePatches
    {
        private static bool s_errorLogged;

        [HarmonyPatch(typeof(Tameable), nameof(Tameable.GetHoverText))]
        [HarmonyPostfix]
        private static void HoverText(Tameable __instance, ref string __result)
        {
            try
            {
                var mercenary = __instance.GetComponent<Mercenary>();
                if (mercenary != null) __result = mercenary.HoverText(__result);
            }
            catch (Exception e)
            {
                LogOnce(e);
            }
        }

        [HarmonyPatch(typeof(Tameable), nameof(Tameable.Interact))]
        [HarmonyPrefix]
        private static bool Interact(Tameable __instance, Humanoid user, bool hold, bool alt, ref bool __result)
        {
            try
            {
                var mercenary = __instance.GetComponent<Mercenary>();
                var handled = mercenary != null ? mercenary.Interact(user, hold, alt) : null;
                if (handled == null) return true;
                __result = handled.Value;
                return false;
            }
            catch (Exception e)
            {
                LogOnce(e);
                return true;
            }
        }

        [HarmonyPatch(typeof(Tameable), nameof(Tameable.IsHungry))]
        [HarmonyPostfix]
        private static void IsHungry(Tameable __instance, ref bool __result)
        {
            try
            {
                if (__result) __result = HireRules.Hungry(true, Mercenary.IsHired(__instance.GetComponent<Character>()));
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
            Plugin.Log.LogError($"Hire hover / E failed (logged once): {e}");
        }
    }
}
