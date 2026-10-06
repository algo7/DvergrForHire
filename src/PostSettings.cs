using System.Collections.Generic;
using System.Linq;

namespace DvergrForHire
{
    /// <summary>One hiring post: a hammer entry that places the vanilla Dvergr lantern pole with a recruiter of its kind.</summary>
    internal sealed class PostKind
    {
        public readonly string Id;

        /// <summary>As in "Hiring post: fire mage" (lower case, names keep their capitals).</summary>
        public readonly string Label;

        /// <summary>The recruiter's and every hire's prefab.</summary>
        public readonly string Prefab;

        /// <summary>Hire price with no stars (+ DvergrSettings.StarPrice per star, HireRules.Price).</summary>
        public readonly int Price;

        /// <summary>Paid on top of the lantern pole's own cost; removing the pole refunds only the pole's cost.</summary>
        public readonly (string Item, int Amount)[] Fee;

        public PostKind(string id, string label, string prefab, int price, params (string Item, int Amount)[] fee)
        {
            Id = id;
            Label = label;
            Prefab = prefab;
            Price = price;
            Fee = fee;
        }
    }

    /// <summary>The hiring posts (user, 2026-10-05): one per kind, camp prices, Deep North 400, the home biome's materials as the fee.</summary>
    internal static class PostSettings
    {
        /// <summary>The vanilla piece a post places (black forge; no hover / E of its own).</summary>
        public const string Pole = "piece_dvergr_lantern_pole";

        /// <summary>
        /// The hammer menu section the post entries show in (the menu's sections are the pieces' usage tags). The pole itself is
        /// tagged Lighting, which the entries would copy (user: "add it to the defense section").
        /// </summary>
        public const Piece.UsageTagFlags Section = Piece.UsageTagFlags.Defense;

        /// <summary>The item whose piece table gets the six entries.</summary>
        public const string Hammer = "Hammer";

        /// <summary>Hidden ZDO key on a post's lantern pole: its kind id.</summary>
        public const string PostKey = "DvergrForHire_Post";

        /// <summary>
        /// Hidden ZDO key (bool) on a recruiter. Its pole links it with a vanilla "Spawned" connection, the way a creature spawner
        /// links its creature: ZDO ids change on every world load, and the game re-links connections on load (ZDOMan.ConnectSpawners).
        /// </summary>
        public const string RecruiterKey = "DvergrForHire_Recruiter";

        /// <summary>Hidden ZDO key (bool) on every Dvergr a post made (its recruiter, every hire): they drop no loot.</summary>
        public const string FromPostKey = "DvergrForHire_FromPost";

        /// <summary>Hiring at a post costs this much more than the same Dvergr at a camp, in percent (user: "20% premium on post hire").</summary>
        public const int PremiumPercent = 20;

        /// <summary>Metres from the pole (towards the placing player) where the recruiter appears.</summary>
        public const float RecruiterOffset = 1.5f;

        /// <summary>Metres in front of the player where a hire appears.</summary>
        public const float HireOffset = 2f;

        /// <summary>Real seconds a recruiter's post must be missing before the recruiter leaves (its data may arrive late).</summary>
        public const double PostGoneSeconds = 10;

        private static readonly (string Item, int Amount)[] Mistlands = { ("YggdrasilWood", 10), ("BlackMarble", 5) };

        public static readonly PostKind[] Kinds =
        {
            new PostKind("rogue", "rogue", "Dverger", 100, Mistlands),
            new PostKind("firemage", "fire mage", "DvergerMageFire", 100, Mistlands),
            new PostKind("icemage", "ice mage", "DvergerMageIce", 100, Mistlands),
            new PostKind("supportmage", "support mage", "DvergerMageSupport", 100, Mistlands),
            new PostKind("ashlands", "Ashlands", "DvergerAshlands", 200, ("Blackwood", 10), ("Grausten", 5)),
            new PostKind("deepnorth", "Deep North", "DvergerDeepNorth", 400, ("Frostwood", 10), ("NornThread", 5)),
        };

        public static PostKind ById(string id) => Kinds.FirstOrDefault(k => k.Id == id);

        public static PostKind ByPrefab(string prefab) => Kinds.FirstOrDefault(k => k.Prefab == prefab);

        public static IEnumerable<string> FeeItems => Kinds.SelectMany(k => k.Fee.Select(f => f.Item)).Distinct();
    }
}
