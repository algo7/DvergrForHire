using System;
using System.Collections.Generic;
using UnityEngine;

namespace DvergrForHire
{
    /// <summary>
    /// On every hireable Dvergr (added to the prefabs by DvergrSetup). The hover text and E (called by HirePatches), the hire
    /// itself on the hiring player's game, and every second on every game: the provokable flag (hired: off, so players with
    /// the mod can't hurt it and hitting it provokes nobody; wild: on, like vanilla). For hired Dvergr also: take control
    /// from a game without the mod (TakeoverWatch), re-send a lost "follow me" (PendingFollow, hiring game only) and, on the
    /// game running it, stamp the heartbeat and copy the name into the vanilla override-name field. Never throws.
    /// </summary>
    public sealed partial class Mercenary : MonoBehaviour
    {
        private const float TickSeconds = 1f;

        /// <summary>Price with no stars (serialized: set on the prefab by DvergrSetup, so every Dvergr has it).</summary>
        public int m_basePrice;

        /// <summary>The Coins item's name, read from the game by DvergrSetup.</summary>
        internal static string s_coinName;

        /// <summary>The coin sound played when a Dvergr is hired (DvergrSetup).</summary>
        internal static EffectList s_hireEffect;

        private static readonly int s_beatHash = DvergrSettings.BeatKey.GetStableHashCode();

        /// <summary>Players this game found without the mod: peer id (new on every connection) → real time found.</summary>
        private static readonly Dictionary<long, double> s_noMod = new Dictionary<long, double>();

        private static readonly HashSet<long> s_logged = new HashSet<long>();
        private static readonly HashSet<string> s_errorsLogged = new HashSet<string>();

        private ZNetView m_nview;
        private Character m_character;
        private BaseAI m_ai;
        private Tameable m_tameable;
        private bool? m_hiredApplied;
        private readonly TakeoverWatch m_watch = new TakeoverWatch();

        /// <summary>Real time this game hired it (0 = never).</summary>
        private double m_hiredAt;

        /// <summary>Paid here, tamed flag not seen yet: counts as hired here (never pays twice); "tamed" re-sent every 5 s.</summary>
        private bool m_paid;

        private double m_tamedSentAt;

        /// <summary>Only on the hiring player's game, until it follows them (or 2 minutes).</summary>
        private PendingFollow m_pending;

        private void Awake()
        {
            m_nview = GetComponent<ZNetView>();
            m_character = GetComponent<Character>();
            m_ai = GetComponent<BaseAI>();
            m_tameable = GetComponent<Tameable>();
            InvokeRepeating(nameof(Tick), UnityEngine.Random.Range(0.2f, TickSeconds), TickSeconds);
        }

        /// <summary>A hired Dvergr (for the building protection).</summary>
        internal static bool IsHired(Character character) =>
            character != null && character.GetComponent<Mercenary>() != null && character.IsTamed();

        private bool Ready => m_nview != null && m_nview.IsValid() && m_character != null && m_ai != null && m_tameable != null;

        private bool RecentlyHired => HireRules.RecentlyHired(m_hiredAt, Time.unscaledTimeAsDouble);

        private int Price => HireRules.Price(m_basePrice, m_character.GetLevel());

        /// <summary>The hover patch: the price on wild Dvergr, nothing on provoked ones, "( Hired )" on hired ones.</summary>
        internal string HoverText(string vanilla)
        {
            if (!Ready) return vanilla;
            if (IsRecruiter) return RecruiterHoverText(vanilla);
            var hired = m_character.IsTamed() || m_paid;
            return Localization.instance.Localize(HireRules.Hover(m_tameable.GetName(), hired, m_ai.IsAggravated(), Price, vanilla));
        }

        /// <summary>The E patch: null = let vanilla handle it (follow / stay / rename on hired Dvergr); else the result.</summary>
        internal bool? Interact(Humanoid user, bool hold, bool alt)
        {
            if (!Ready) return null;
            if (IsRecruiter) return RecruiterInteract(user, hold, alt); // never follow / stay
            if (RecentlyHired) return false;        // a double press right after hiring doesn't toggle the new hire to "stay"
            if (m_character.IsTamed())
            {
                m_pending = null; // this player's own follow / stay wins over a late re-send of "follow me"
                return null;      // hired: vanilla
            }
            if (m_paid || hold || alt) return false; // paid, tamed flag not back yet: never pay twice
            return TryHire(user);
        }

        /// <summary>On the game of the player pressing E. True when E did something (hired, or the coin message).</summary>
        private bool TryHire(Humanoid user)
        {
            var player = user as Player;
            if (player == null || player != Player.m_localPlayer || string.IsNullOrEmpty(s_coinName)) return false;
            var price = Price;
            var inventory = player.GetInventory();
            var coins = inventory.CountItems(s_coinName);
            switch (HireRules.Check(!m_character.IsDead(), m_character.IsTamed() || m_paid, m_ai.IsAggravated(), coins, price))
            {
                case HireRules.Hire.NotHireable:
                    return false;
                case HireRules.Hire.NotEnoughCoins:
                    player.Message(MessageHud.MessageType.Center, HireRules.NotEnoughCoins(price));
                    return true;
            }
            inventory.RemoveItem(s_coinName, price);
            var left = inventory.CountItems(s_coinName);
            if (left != coins - price)
            {
                Plugin.Log.LogWarning($"Hire stopped: paying {price} of {coins} coins left {left}");
                return true;
            }
            m_hiredAt = Time.unscaledTimeAsDouble;
            m_paid = true;
            m_tamedSentAt = m_hiredAt;
            m_character.SetTamed(true); // vanilla RPC to whichever game runs it, with or without the mod
            s_hireEffect?.Create(transform.position, transform.rotation);
            player.Message(MessageHud.MessageType.Center, HireRules.Hired(m_tameable.GetName()));
            // Follow the hirer at once. With no owner at this moment the RPC would go to every game (target 0), and a later
            // re-send would toggle it back to stay; then the pending follow sends it once to the first game that runs it.
            var owner = m_nview.GetZDO().GetOwner();
            if (owner != 0) m_tameable.Command(player, message: false);
            m_pending = new PendingFollow(player.GetPlayerName(), owner, m_hiredAt);
            Plugin.Log.LogInfo($"Hired {Utils.GetPrefabName(gameObject)} (level {m_character.GetLevel()}) for {price} coins");
            return true;
        }

        /// <summary>The hiring game, after paying: done once the tamed flag is seen; until then re-send it every 5 s.</summary>
        private void ConfirmPayment(bool tamed)
        {
            var now = Time.unscaledTimeAsDouble;
            switch (HireRules.AfterPaying(tamed, m_tamedSentAt, now))
            {
                case HireRules.Paid.Confirmed:
                    m_paid = false;
                    break;
                case HireRules.Paid.Resend:
                    m_character.SetTamed(true); // idempotent on the game running it
                    m_tamedSentAt = now;
                    Plugin.Log.LogInfo("Sent 'hired' again: the tamed flag hasn't come back yet");
                    break;
            }
        }

        private void Tick()
        {
            try
            {
                if (!Ready) return;
                var hired = m_character.IsTamed();
                if (m_paid) ConfirmPayment(hired);
                if (m_hiredApplied != hired)
                {
                    m_ai.m_aggravatable = !hired;
                    m_hiredApplied = hired;
                }
                if (!hired || ZNet.instance == null) return;
                TakeOver(); // first, so nothing below can leave a hired Dvergr with a game without the mod
                FollowUp();
                var postFound = IsRecruiter && FindPost(); // every game: keeps the pole cached for the hover and E

                if (!m_nview.IsOwner())
                {
                    m_postWatch.Reset();
                    return;
                }
                if (IsRecruiter && RecruiterLeaves(postFound)) return;
                var zdo = m_nview.GetZDO();
                var now = ZNet.instance.GetTime().Ticks;
                if (Takeover.ShouldBeat(zdo.GetLong(s_beatHash), now)) zdo.Set(s_beatHash, now);
                var tamedName = zdo.GetString(ZDOVars.s_tamedName);
                if (NameCopy.ShouldWrite(tamedName, zdo.GetString(ZDOVars.s_overrideHoverName)))
                    zdo.Set(ZDOVars.s_overrideHoverName, NameCopy.Target(tamedName));
            }
            catch (Exception e)
            {
                if (s_errorsLogged.Add(e.GetType().FullName))
                    Plugin.Log.LogError($"Mercenary failed (each kind of error logged once): {e}");
            }
        }

        /// <summary>A game without the mod runs this hired Dvergr: run it here instead.</summary>
        private void TakeOver()
        {
            var zdo = m_nview.GetZDO();
            var owner = zdo.GetOwner();
            var step = m_watch.Decide(m_nview.IsOwner(), owner, zdo.GetLong(s_beatHash), Time.unscaledTimeAsDouble, s_noMod);
            if (step == TakeoverWatch.Step.Wait) return;
            m_nview.ClaimOwnership();
            zdo.Set(s_beatHash, ZNet.instance.GetTime().Ticks); // at once, so other modded games see it's taken
            if (step == TakeoverWatch.Step.FoundNoMod) LogTakeover(owner);
        }

        /// <summary>The hiring game: send "follow me" again once a modded game runs the Dvergr.</summary>
        private void FollowUp()
        {
            if (m_pending == null) return;
            var player = Player.m_localPlayer;
            if (player == null)
            {
                m_pending = null;
                return;
            }
            var zdo = m_nview.GetZDO();
            switch (m_pending.Decide(zdo.GetString(ZDOVars.s_follow), zdo.GetOwner(), zdo.GetLong(s_beatHash), m_nview.IsOwner(), Time.unscaledTimeAsDouble))
            {
                case PendingFollow.Step.Send:
                    m_tameable.Command(player, message: false);
                    Plugin.Log.LogInfo("Sent 'follow me' again: a game with the mod runs the hired Dvergr now");
                    break;
                case PendingFollow.Step.Done:
                case PendingFollow.Step.GiveUp:
                    m_pending = null;
                    break;
            }
        }

        /// <summary>Once per player found without the mod, by name when the player list has them.</summary>
        private static void LogTakeover(long owner)
        {
            if (!s_logged.Add(owner)) return;
            var name = "a player";
            foreach (var player in ZNet.instance.GetPlayerList())
                if (player.m_characterID.UserID == owner) name = player.m_name;
            Plugin.Log.LogInfo($"Took control of hired Dvergr from {name}: their game doesn't run DvergrForHire (logged once per player)");
        }
    }
}
