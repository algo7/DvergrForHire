using System;
using System.Globalization;

namespace DvergrForHire
{
    /// <summary>Prices, who can be hired, and the texts (used by Mercenary). Unity-free, unit-tested.</summary>
    internal static class HireRules
    {
        public enum Hire
        {
            Hire,
            NotEnoughCoins,
            /// <summary>Not for hire: provoked, already hired, dead or no price. Nothing happens, no message.</summary>
            NotHireable,
        }

        /// <summary>
        /// Base price + StarPrice per star (level 1 = no stars; a level below 1 has none, so a hire is never free). No base
        /// price (not a hireable kind) stays 0 whatever the stars, so it stays not for hire.
        /// </summary>
        public static int Price(int basePrice, int level) =>
            basePrice <= 0 ? 0 : basePrice + DvergrSettings.StarPrice * Math.Max(0, level - 1);

        /// <param name="tamed">Hired already, or paid here and the tamed flag not seen yet.</param>
        public static Hire Check(bool alive, bool tamed, bool provoked, int coins, int price)
        {
            if (!alive || tamed || provoked || price <= 0) return Hire.NotHireable;
            return coins >= price ? Hire.Hire : Hire.NotEnoughCoins;
        }

        /// <summary>
        /// This game hired it less than RecentHireSeconds ago (real time): E does nothing, so a double press doesn't toggle
        /// the new hire to stay. (Paying twice is prevented by AfterPaying, however long the tamed flag takes.)
        /// </summary>
        public static bool RecentlyHired(double hiredAt, double now) =>
            hiredAt > 0 && now >= hiredAt && now - hiredAt < DvergrSettings.RecentHireSeconds;

        public enum Paid
        {
            Waiting,
            /// <summary>Send "tamed" again (vanilla's SetTamed is idempotent on the game running it).</summary>
            Resend,
            /// <summary>The tamed flag is seen here: the payment is done.</summary>
            Confirmed,
        }

        /// <summary>
        /// The hiring game after paying: the Dvergr counts as hired there until its tamed flag is seen, however long the sync
        /// takes, so E never pays twice. Until then "tamed" is re-sent every TamedResendSeconds, in case the game running
        /// the Dvergr changed before the first one arrived.
        /// </summary>
        public static Paid AfterPaying(bool tamed, double lastSent, double now)
        {
            if (tamed) return Paid.Confirmed;
            return now - lastSent >= DvergrSettings.TamedResendSeconds ? Paid.Resend : Paid.Waiting;
        }

        public enum EPress
        {
            /// <summary>Pay and hire.</summary>
            Hire,
            /// <summary>Nothing happens (E is used up, vanilla doesn't run).</summary>
            Nothing,
            /// <summary>Vanilla Tameable.Interact: follow / stay, Shift+E rename.</summary>
            Vanilla,
            /// <summary>A post's recruiter: its own E (PostRules.RecruiterInteract).</summary>
            Recruiter,
        }

        /// <summary>
        /// What E does on a Dvergr (the E patch), decided in this order: a recruiter has its own E; right after this game hired
        /// it, nothing (a double press doesn't make it stay); a hired Dvergr gets vanilla follow / stay / rename; while paid but
        /// the tamed flag isn't back, or for a held E or Shift+E, nothing (never pays twice, no rename on a wild one); else hire.
        /// </summary>
        public static EPress WhatEDoes(bool recruiter, bool recentlyHired, bool tamed, bool paid, bool hold, bool alt)
        {
            if (recruiter) return EPress.Recruiter;
            if (recentlyHired) return EPress.Nothing;
            if (tamed) return EPress.Vanilla;
            if (paid || hold || alt) return EPress.Nothing;
            return EPress.Hire;
        }

        /// <summary>
        /// A creature created right after SpawnAbility.FindTarget is that ability's summon when it's created in the same frame
        /// and is one of the ability's summon prefabs; it's marked when the summoner is hired.
        /// </summary>
        public static bool MarkSummon(bool summonerHired, bool sameFrame, bool isItsSummonPrefab) =>
            summonerHired && sameFrame && isItsSummonPrefab;

        /// <summary>Building pieces take no damage from a hired Dvergr or from what a hired Dvergr summoned (mistiles).</summary>
        public static bool BuildingSafeFrom(bool hired, bool hiredSummon) => hired || hiredSummon;

        /// <summary>A hover line for E, styled like vanilla's (localized later: $KEY_Use becomes the key).</summary>
        public static string UseLine(string action) => "[<color=yellow><b>$KEY_Use</b></color>] " + action;

        /// <param name="vanilla">
        /// Tameable.GetHoverText's text. For a tamed creature: name + status, the pet line, then the rename line(s), which are
        /// kept because vanilla picks the keyboard or gamepad key there.
        /// </param>
        public static string Hover(string name, bool hired, bool provoked, int price, string vanilla)
        {
            if (hired) return name + " ( Hired )\n" + UseLine("Follow / Stay") + RenameLines(vanilla);
            if (provoked) return ""; // like a vanilla Dvergr: no hover text
            return name + "\n" + UseLine("Hire: " + price.ToString(CultureInfo.InvariantCulture) + " coins");
        }

        /// <summary>
        /// Tameable.IsHungry for a Dvergr: hired ones are never hungry. BaseAI.UpdateRegeneration only heals a tamed creature
        /// with a Tameable when it isn't hungry, and hired Dvergr never eat, so they'd never regain health (wild Dvergr do).
        /// Wild ones keep vanilla's answer, so vanilla taming still never starts.
        /// </summary>
        public static bool Hungry(bool vanillaHungry, bool hired) => vanillaHungry && !hired;

        /// <summary>Vanilla tamed hover text's lines from the third on (the rename line, keyboard or gamepad key), "\n"-led, or "".</summary>
        internal static string RenameLines(string vanilla)
        {
            var lines = (vanilla ?? "").Split('\n');
            return lines.Length > 2 ? "\n" + string.Join("\n", lines, 2, lines.Length - 2) : "";
        }


        public static string NotEnoughCoins(int price) => "Not enough coins (" + price.ToString(CultureInfo.InvariantCulture) + ")";

        public static string Hired(string name) => name + " hired";
    }
}
