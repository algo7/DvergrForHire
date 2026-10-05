using System;
using System.Collections.Generic;

namespace DvergrForHire
{
    /// <summary>Copying the name into the vanilla override-name field, which players without the mod see. Unity-free.</summary>
    internal static class NameCopy
    {
        /// <summary>The name as players with the mod see it (vanilla strips formatting tags), or empty for none.</summary>
        public static string Target(string tamedName)
        {
            var shown = tamedName?.RemoveRichTextTags();
            return string.IsNullOrWhiteSpace(shown) ? "" : shown;
        }

        /// <summary>Only when it differs, so the Dvergr's data isn't re-sent every tick.</summary>
        public static bool ShouldWrite(string tamedName, string shown) => Target(tamedName) != (shown ?? "");
    }

    /// <summary>
    /// The game running a hired Dvergr stamps a hidden heartbeat on it every 10 s when it has the mod (DvergrSettings.BeatKey).
    /// Unity-free.
    /// </summary>
    internal static class Takeover
    {
        private static readonly long BeatTicks = (long)(DvergrSettings.BeatSeconds * TimeSpan.TicksPerSecond);

        /// <summary>Owner: stamp the heartbeat now (never stamped, 10 s since the last one, or a stamp from the future).</summary>
        public static bool ShouldBeat(long beatTicks, long nowTicks) =>
            beatTicks <= 0 || beatTicks > nowTicks || nowTicks - beatTicks >= BeatTicks;
    }

    /// <summary>
    /// One hired Dvergr, on a game that doesn't run it: whether to take control of it (ported from SealsAtHome 1.0.2). A
    /// runner is found to have no mod when this game sees neither the runner nor the heartbeat change for StaleSeconds of
    /// real time (the world clock can't be used: sleeping fast-forwards it ~33x). Players found so (noMod: peer id, new on
    /// every connection → real time found) are taken from at once for ForgetSeconds, which also redoes a takeover the game
    /// undid; then they're watched again. Unity-free.
    /// </summary>
    internal sealed class TakeoverWatch
    {
        public enum Step
        {
            Wait,
            Claim,
            /// <summary>Claim; the runner was just found to have no mod (worth a log line).</summary>
            FoundNoMod,
        }

        private bool m_watching;
        private long m_owner;
        private long m_beat;
        private double m_since;

        /// <param name="owner">The Dvergr's owner (peer id); 0 = none yet.</param>
        /// <param name="beat">The heartbeat as last seen; its value only matters when it changes.</param>
        /// <param name="nowSeconds">Real time (seconds).</param>
        public Step Decide(bool isOwner, long owner, long beat, double nowSeconds, IDictionary<long, double> noMod)
        {
            if (isOwner || owner == 0)
            {
                m_watching = false;
                return Step.Wait;
            }
            if (noMod.TryGetValue(owner, out var found))
            {
                if (nowSeconds - found <= DvergrSettings.ForgetSeconds) return Step.Claim;
                noMod.Remove(owner);
            }
            if (!m_watching || owner != m_owner || beat != m_beat)
            {
                m_watching = true;
                m_owner = owner;
                m_beat = beat;
                m_since = nowSeconds;
                return Step.Wait;
            }
            if (nowSeconds - m_since <= DvergrSettings.StaleSeconds) return Step.Wait;
            noMod[owner] = nowSeconds;
            return Step.FoundNoMod;
        }
    }

    /// <summary>
    /// "Follow me" after a hire, on the hiring player's game. Tameable's Command toggles follow and only modded games handle
    /// it, and RPCs to a modded owner arrive, so a Command is lost only when the game it went to lacks the mod. Re-send only
    /// to a new owner that has the mod (we own it, or its heartbeat moved since we first saw that owner), and never twice
    /// to the same owner: a late "follow" update would otherwise make the re-send toggle the Dvergr back to stay. Unity-free.
    /// </summary>
    internal sealed class PendingFollow
    {
        public enum Step
        {
            Wait,
            Send,
            /// <summary>It follows the hirer: forget this.</summary>
            Done,
            GiveUp,
        }

        private readonly string m_hirer;
        private readonly double m_start;
        private long m_sentTo;
        private bool m_watching;
        private long m_watchOwner;
        private long m_watchBeat;

        /// <param name="hirer">The hiring player's name (what Tameable writes into the follow field).</param>
        /// <param name="sentTo">The owner the first Command went to.</param>
        public PendingFollow(string hirer, long sentTo, double nowSeconds)
        {
            m_hirer = hirer;
            m_sentTo = sentTo;
            m_start = nowSeconds;
        }

        /// <param name="follow">The Dvergr's follow field (a player name, "" for none).</param>
        public Step Decide(string follow, long owner, long beat, bool weOwnIt, double nowSeconds)
        {
            if (!string.IsNullOrEmpty(m_hirer) && follow == m_hirer) return Step.Done;
            if (nowSeconds - m_start > DvergrSettings.FollowGiveUpSeconds) return Step.GiveUp;
            if (owner == 0 || owner == m_sentTo)
            {
                m_watching = false;
                return Step.Wait;
            }
            if (!weOwnIt)
            {
                if (!m_watching || owner != m_watchOwner)
                {
                    m_watching = true;
                    m_watchOwner = owner;
                    m_watchBeat = beat;
                    return Step.Wait;
                }
                if (beat == m_watchBeat) return Step.Wait;
            }
            m_sentTo = owner;
            m_watching = false;
            return Step.Send;
        }
    }
}
