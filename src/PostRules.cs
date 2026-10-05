using System;
using System.Globalization;

namespace DvergrForHire
{
    /// <summary>Hiring post texts and decisions (used by HiringPost and Mercenary's recruiter mode). Unity-free, unit-tested.</summary>
    internal static class PostRules
    {
        public const int MaxStars = 2;

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
        /// A post's pole, on a game with the mod: true once its recruiter has been gone for PostGoneSeconds (it died), so the
        /// post breaks. Poles placed before they knew their recruiter (not linked) never break by themselves.
        /// </summary>
        public static bool PoleBreaks(bool linked, bool recruiterExists, PostWatch watch, double nowSeconds) =>
            linked && watch.ShouldLeave(recruiterExists, nowSeconds);

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
    /// A recruiter, on the game running it: leave once its post's lantern pole has been missing for PostGoneSeconds of real
    /// time, so a pole whose data only arrives late (loading in, zone edges) doesn't send it away. Unity-free.
    /// </summary>
    internal sealed class PostWatch
    {
        private double m_missingSince = -1;

        public bool ShouldLeave(bool postExists, double nowSeconds)
        {
            if (postExists)
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
