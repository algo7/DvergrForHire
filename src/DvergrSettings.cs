using System.Collections.Generic;
using System.Linq;
using static DvergrForHire.FieldSetting;

namespace DvergrForHire
{
    /// <summary>Everything DvergrForHire sets or charges: decided by the user, not configurable, vanilla prefabs only.</summary>
    internal static class DvergrSettings
    {
        /// <summary>
        /// The hireable Dvergr prefabs and their price with no stars. Not DvergerTest (dev-only). DvergerDeepNorth only spawns
        /// inside Mørkhalla, an interior dungeon a hired Dvergr can't follow you out of: priced as a one-dungeon helper.
        /// </summary>
        public static readonly (string Prefab, int Price)[] Hireable =
        {
            ("Dverger", 100),
            ("DvergerMage", 100),
            ("DvergerMageFire", 100),
            ("DvergerMageIce", 100),
            ("DvergerMageSupport", 100),
            ("DvergerAshlands", 200),
            ("DvergerDeepNorth", 100),
        };

        /// <summary>Coins each star adds to the price (user: "plus 50 for each star").</summary>
        public const int StarPrice = 50;

        public const string Coins = "Coins";

        /// <summary>Hidden ZDO key (bool) on a creature a hired Dvergr summoned (the support mage's mistiles).</summary>
        public const string SummonKey = "DvergrForHire_HiredSummon";

        /// <summary>Played at the Dvergr when it's hired (networked).</summary>
        public const string HireSound = "sfx_coins_placed";

        /// <summary>The Dvergr's own voice, played on every E like vanilla tames' pet sound (networked).</summary>
        public const string Voice = "sfx_dverger_vo_idle";

        /// <summary>Hidden ZDO key: world time (ticks) of the last heartbeat from a modded game running a hired Dvergr.</summary>
        public const string BeatKey = "DvergrForHire_Beat";

        /// <summary>A modded owner stamps the heartbeat this often (world time).</summary>
        public const double BeatSeconds = 10;

        /// <summary>Real seconds without a new heartbeat or runner before a runner counts as having no mod (TakeoverWatch).</summary>
        public const double StaleSeconds = 30;

        /// <summary>Real seconds a player found without the mod is taken from at once; then they're watched again.</summary>
        public const double ForgetSeconds = 300;

        /// <summary>Real seconds the hiring game keeps trying to get "follow me" to a modded game (PendingFollow).</summary>
        public const double FollowGiveUpSeconds = 120;

        /// <summary>Real seconds E does nothing on a Dvergr this game just hired (a double press doesn't make it stay).</summary>
        public const double RecentHireSeconds = 3;

        /// <summary>Real seconds between re-sends of "tamed" while the hiring game hasn't seen the flag yet.</summary>
        public const double TamedResendSeconds = 5;

        /// <summary>The vanilla Tameable added to every hireable prefab: commandable, never fed, so vanilla taming never starts.</summary>
        public static readonly FieldSetting[] TameableSettings =
        {
            Of<Tameable>("m_commandable", true),
            Of<Tameable>("m_startsTamed", false),
            Of<Tameable>("m_unsummonDistance", 0f),
            Of<Tameable>("m_unsummonOnOwnerLogoutSeconds", 0f),
            Effects<Tameable>("m_petEffect", Voice),
            Effects<Tameable>("m_tamedEffect"),
            Effects<Tameable>("m_sootheEffect"),
        };

        /// <summary>Price with no stars; 0 for anything that isn't for hire.</summary>
        public static int BasePrice(string prefab) => Hireable.Where(h => h.Prefab == prefab).Select(h => h.Price).FirstOrDefault();

        public static IEnumerable<string> EffectPrefabs =>
            new[] { HireSound }.Concat(TameableSettings.SelectMany(s => s.PrefabNames)).Distinct();

        public static IEnumerable<string> RequiredPrefabs =>
            Hireable.Select(h => h.Prefab).Concat(new[] { Coins }).Concat(EffectPrefabs).Distinct();
    }
}
