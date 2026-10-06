using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DvergrForHire
{
    /// <summary>Marks a hammer entry's stand-in. It is never placed: PostPatches places the vanilla lantern pole instead.</summary>
    public sealed class PostStandIn : MonoBehaviour
    {
        /// <summary>PostKind.Id (serialized, so the placement ghost has it too).</summary>
        public string m_kind;
    }

    /// <summary>
    /// The six hiring post entries in the hammer: copies of the vanilla Dvergr lantern pole under a disabled holder (so they
    /// never wake up: no ZDO, no world object, not in ZNetScene) with their own name, description and cost (the pole's own
    /// cost + the kind's fee). Made once per game session; added to the hammer's piece table and HiringPost added to the
    /// vanilla pole prefab on every world load if missing. A pre-flight comes first; on any miss posts are off.
    /// </summary>
    internal static class PostSetup
    {
        /// <summary>The vanilla lantern pole's Piece: what a post really places.</summary>
        internal static Piece s_pole;

        private static GameObject s_holder;
        private static readonly List<GameObject> s_standIns = new List<GameObject>();

        public static bool Run(ZNetScene scene, out string message)
        {
            var problem = PreFlight(scene, out var pole, out var table);
            if (problem != null)
            {
                message = "Hiring posts are off: " + problem;
                return false;
            }
            s_pole = pole.GetComponent<Piece>();
            if (s_holder == null) MakeStandIns(pole); // before HiringPost goes on the pole, so stand-ins don't carry it
            if (pole.GetComponent<HiringPost>() == null) pole.AddComponent<HiringPost>();
            foreach (var standIn in s_standIns)
                if (!table.m_pieces.Contains(standIn)) table.m_pieces.Add(standIn);
            message = "Hiring posts in the hammer: " + string.Join(", ", PostSettings.Kinds.Select(k => k.Label));
            return true;
        }

        /// <summary>Null when everything posts need is there; else why not.</summary>
        private static string PreFlight(ZNetScene scene, out GameObject pole, out PieceTable table)
        {
            table = null;
            pole = scene.GetPrefab(PostSettings.Pole);
            if (pole == null || pole.GetComponent<Piece>() == null) return $"the game has no '{PostSettings.Pole}' piece (game update?)";
            var db = ObjectDB.instance;
            if (db == null) return "the item database isn't ready";
            var hammer = db.GetItemPrefab(PostSettings.Hammer);
            var drop = hammer != null ? hammer.GetComponent<ItemDrop>() : null;
            table = drop != null ? drop.m_itemData.m_shared.m_buildPieces : null;
            if (table == null) return "the hammer has no build list (game update?)";
            foreach (var item in PostSettings.FeeItems)
            {
                var prefab = db.GetItemPrefab(item);
                if (prefab == null || prefab.GetComponent<ItemDrop>() == null) return $"the game has no item '{item}' (game update?)";
            }
            foreach (var kind in PostSettings.Kinds)
            {
                var prefab = scene.GetPrefab(kind.Prefab);
                if (prefab == null || prefab.GetComponent<Mercenary>() == null) return $"{kind.Prefab} can't be hired (see the Dvergr setup above)";
            }
            return null;
        }

        private static void MakeStandIns(GameObject pole)
        {
            s_holder = new GameObject("DvergrForHire_PostStandIns");
            s_holder.SetActive(false); // children never run Awake: no ZDO, no world object
            Object.DontDestroyOnLoad(s_holder);
            var db = ObjectDB.instance;
            foreach (var kind in PostSettings.Kinds)
            {
                var standIn = Object.Instantiate(pole, s_holder.transform);
                standIn.name = "DvergrForHire_post_" + kind.Id;
                var piece = standIn.GetComponent<Piece>();
                piece.m_name = PostRules.EntryName(kind.Label);
                piece.m_description = PostRules.EntryDescription(kind.Label);
                piece.m_usage = PostSettings.Section;
                piece.m_resources = piece.m_resources
                    .Concat(kind.Fee.Select(f => new Piece.Requirement
                    {
                        m_resItem = db.GetItemPrefab(f.Item).GetComponent<ItemDrop>(),
                        m_amount = f.Amount,
                        m_recover = false,
                    }))
                    .ToArray();
                standIn.AddComponent<PostStandIn>().m_kind = kind.Id;
                s_standIns.Add(standIn);
            }
        }
    }
}
