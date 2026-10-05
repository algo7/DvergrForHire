using System;
using System.Collections.Generic;
using UnityEngine;

namespace DvergrForHire
{
    /// <summary>
    /// On every hireable Dvergr (added to the prefabs by DvergrSetup). The hover text and E (called by HirePatches), the hire
    /// itself on the hiring player's game, and every second on every game: the provokable flag (hired: off, so players with
    /// the mod can't hurt it and hitting it provokes nobody; wild: on, like vanilla). Never throws.
    /// </summary>
    public sealed class Mercenary : MonoBehaviour
    {
        private const float TickSeconds = 1f;

        /// <summary>Price with no stars (serialized: set on the prefab by DvergrSetup, so every Dvergr has it).</summary>
        public int m_basePrice;

        /// <summary>The Coins item's name, read from the game by DvergrSetup.</summary>
        internal static string s_coinName;

        /// <summary>The coin sound played when a Dvergr is hired (DvergrSetup).</summary>
        internal static EffectList s_hireEffect;

        private static readonly HashSet<string> s_errorsLogged = new HashSet<string>();

        private ZNetView m_nview;
        private Character m_character;
        private BaseAI m_ai;
        private Tameable m_tameable;
        private bool? m_hiredApplied;

        /// <summary>Real time this game hired it (0 = never).</summary>
        private double m_hiredAt;

        /// <summary>Paid here, tamed flag not seen yet: counts as hired here (never pays twice); "tamed" re-sent every 5 s.</summary>
        private bool m_paid;

        private double m_tamedSentAt;

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
            var hired = m_character.IsTamed() || m_paid;
            return Localization.instance.Localize(HireRules.Hover(m_tameable.GetName(), hired, m_ai.IsAggravated(), Price, vanilla));
        }

        /// <summary>The E patch: null = let vanilla handle it (follow / stay / rename on hired Dvergr); else the result.</summary>
        internal bool? Interact(Humanoid user, bool hold, bool alt)
        {
            if (!Ready) return null;
            if (RecentlyHired) return false;        // a double press right after hiring doesn't toggle the new hire to "stay"
            if (m_character.IsTamed()) return null; // hired: vanilla
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
            m_tameable.Command(player, message: false); // follow the hirer at once
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
                if (m_hiredApplied == hired) return;
                m_ai.m_aggravatable = !hired;
                m_hiredApplied = hired;
            }
            catch (Exception e)
            {
                if (s_errorsLogged.Add(e.GetType().FullName))
                    Plugin.Log.LogError($"Mercenary failed (each kind of error logged once): {e}");
            }
        }
    }
}
