using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace DvergrForHire
{
    /// <summary>
    /// Placing a hiring post: Player.PlacePiece gets a stand-in (the hammer entry) and places the vanilla lantern pole
    /// instead; Piece.SetCreator, called by PlacePiece on the new pole, marks it as a post; then the placer's game creates the
    /// recruiter. The cost comes off the stand-in (Player.UpdatePlacement uses the selected piece). Never throws.
    /// </summary>
    [HarmonyPatch]
    internal static class PostPatches
    {
        private static readonly int s_postHash = PostSettings.PostKey.GetStableHashCode();
        private static readonly int s_fromPostHash = PostSettings.FromPostKey.GetStableHashCode();
        private static bool s_errorLogged;

        /// <summary>Set while our prefix places a pole for a stand-in.</summary>
        private static PostStandIn s_placing;

        /// <summary>The pole SetCreator marked during that call.</summary>
        private static ZNetView s_placed;

        [HarmonyPatch(typeof(Player), nameof(Player.PlacePiece))]
        [HarmonyPrefix]
        private static bool PlacePiece(Player __instance, Piece piece, Vector3 pos, Quaternion rot, bool doAttack, bool cheated)
        {
            PostStandIn standIn;
            try
            {
                standIn = piece != null ? piece.GetComponent<PostStandIn>() : null;
                if (standIn == null) return true; // any other piece: vanilla
                if (PostSetup.s_pole == null)
                {
                    // Never let vanilla create the stand-in itself: no other game knows that prefab.
                    Plugin.Log.LogWarning("A hiring post can't be placed: the posts weren't set up (see the log above)");
                    return false;
                }
            }
            catch (Exception e)
            {
                LogOnce(e);
                return true;
            }
            try
            {
                s_placing = standIn;
                s_placed = null;
                __instance.PlacePiece(PostSetup.s_pole, pos, rot, doAttack, cheated); // the vanilla pole (this prefix lets it through)
                if (s_placed != null) Mercenary.CreateRecruiter(s_placed, standIn.m_kind, __instance.transform.position);
            }
            catch (Exception e)
            {
                LogOnce(e);
            }
            finally
            {
                s_placing = null;
                s_placed = null;
            }
            return false;
        }

        [HarmonyPatch(typeof(Piece), nameof(Piece.SetCreator))]
        [HarmonyPostfix]
        private static void SetCreator(Piece __instance)
        {
            try
            {
                if (s_placing == null) return;
                var nview = __instance.GetComponent<ZNetView>();
                if (nview == null || !nview.IsValid()) return;
                nview.GetZDO().Set(s_postHash, s_placing.m_kind);
                s_placed = nview;
            }
            catch (Exception e)
            {
                LogOnce(e);
            }
        }

        /// <summary>
        /// Dvergr a post made (DvergrForHire_FromPost) drop no loot: both vanilla loot paths, the direct drop on death and the
        /// ragdoll's saved loot list, build it here, in the game that handles the death.
        /// </summary>
        [HarmonyPatch(typeof(CharacterDrop), nameof(CharacterDrop.GenerateDropList))]
        [HarmonyPostfix]
        private static void GenerateDropList(CharacterDrop __instance, ref List<KeyValuePair<GameObject, int>> __result)
        {
            try
            {
                var nview = __instance.GetComponent<ZNetView>();
                if (nview == null || !nview.IsValid()) return;
                if (!PostRules.DropsLoot(nview.GetZDO().GetBool(s_fromPostHash))) __result = new List<KeyValuePair<GameObject, int>>();
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
            Plugin.Log.LogError($"Placing a hiring post failed (logged once): {e}");
        }
    }
}
