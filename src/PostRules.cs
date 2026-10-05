using System;
using System.Collections.Generic;
using System.Globalization;

namespace DvergrForHire
{
    /// <summary>Hiring post texts and decisions (used by HiringPost and Mercenary's recruiter mode). Unity-free, unit-tested.</summary>
    internal static class PostRules
    {
        public const int MaxStars = 2;

        /// <summary>
        /// Where a post hire appears, as (right, forward) metres from the player, tried in order until one is free: in front
        /// (PostSettings.HireOffset), then right, left, behind (a hire 2 m ahead can land inside the recruiter or a hut wall).
        /// </summary>
        public static readonly (float Right, float Forward)[] HireSpots =
        {
            (0f, PostSettings.HireOffset),
            (1.5f, 0f),
            (-1.5f, 0f),
            (0f, -1.5f),
        };

        /// <summary>E on a post: none → ★ → ★★ → none; anything out of range goes back to none.</summary>
        public static int NextStars(int stars) => stars < 0 || stars >= MaxStars ? 0 : stars + 1;

        public static string StarsText(int stars) => stars <= 0 ? "none" : new string('★', Math.Min(stars, MaxStars));

        public static string EntryName(string label) => "Hiring post: " + label;

        public static string EntryDescription(string label) =>
            "A Dvergr " + label + " recruiter stays here. Pick the stars on the post, hire at the recruiter.";

        public static string PostHover(string label, int stars) =>
            EntryName(label) + "\n" + HireRules.UseLine("Stars for the next hire: " + StarsText(stars));

        public static string RecruiterName(string label) => char.ToUpperInvariant(label[0]) + label.Substring(1) + " recruiter";

        /// <param name="vanilla">Tameable.GetHoverText's text; its rename line(s) are kept (keyboard or gamepad key).</param>
        public static string RecruiterHover(string label, int price, string vanilla) =>
            RecruiterName(label) + "\n" + HireRules.UseLine("Hire: " + price.ToString(CultureInfo.InvariantCulture) + " coins")
            + HireRules.RenameLines(vanilla);

        /// <summary>
        /// A post's pole, on a game with the mod: true once its recruiter has been gone for PostGoneSeconds (it died) in an area
        /// this game has fully loaded, so the post breaks. Poles not linked to a recruiter never break by themselves.
        /// </summary>
        public static bool PoleBreaks(bool linked, bool recruiterFound, bool areaReady, PostWatch watch, double nowSeconds) =>
            linked && watch.ShouldLeave(recruiterFound, areaReady, nowSeconds);

        /// <summary>The post among <paramref name="poles"/> whose link points at <paramref name="recruiter"/>; default if none.</summary>
        public static TPole PostOf<TPole, TId>(IEnumerable<TPole> poles, Func<TPole, TId> linkedTo, TId recruiter)
        {
            foreach (var pole in poles)
                if (EqualityComparer<TId>.Default.Equals(linkedTo(pole), recruiter)) return pole;
            return default;
        }

        public enum RecruiterE
        {
            /// <summary>Let vanilla handle it (Shift+E: rename).</summary>
            Vanilla,
            Nothing,
            Hire,
        }

        /// <summary>Held E does nothing (vanilla repeats Interact with hold every frame); Shift+E is vanilla rename; E hires.</summary>
        public static RecruiterE RecruiterInteract(bool hold, bool alt) =>
            hold ? RecruiterE.Nothing : alt ? RecruiterE.Vanilla : RecruiterE.Hire;
    }

    /// <summary>
    /// One half of a post (the recruiter watching its pole, or the pole watching its recruiter), on a game with the mod: the
    /// other half is gone once it has been missing for PostGoneSeconds of real time while this game had the whole area loaded
    /// (vanilla ZNetScene.IsAreaReady), so data that only arrives late, or an area at the edge of what this game has loaded,
    /// never counts as gone. Unity-free.
    /// </summary>
    internal sealed class PostWatch
    {
        private double m_missingSince = -1;

        /// <param name="found">The other half is there.</param>
        /// <param name="areaReady">This game has loaded every object around (only then can "not found" mean gone).</param>
        public bool ShouldLeave(bool found, bool areaReady, double nowSeconds)
        {
            if (found || !areaReady)
            {
                m_missingSince = -1;
                return false;
            }
            if (m_missingSince < 0)
            {
                m_missingSince = nowSeconds;
                return false;
            }
            return nowSeconds - m_missingSince >= PostSettings.PostGoneSeconds;
        }

        /// <summary>This game doesn't run the recruiter (any more): start over when it does again.</summary>
        public void Reset() => m_missingSince = -1;
    }
}
