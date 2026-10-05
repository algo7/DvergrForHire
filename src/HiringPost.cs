using System;
using UnityEngine;

namespace DvergrForHire
{
    /// <summary>
    /// On the vanilla Dvergr lantern pole prefab, in games with the mod. Inert on ordinary poles (no hover text, E does
    /// nothing: vanilla). On a hiring post (its ZDO has DvergrForHire_Post): hover with the kind and this game's star choice,
    /// E cycles the stars for this game's next hire there (local, not saved). Never throws.
    /// </summary>
    public sealed class HiringPost : MonoBehaviour, Hoverable, Interactable
    {
        private static readonly int s_postHash = PostSettings.PostKey.GetStableHashCode();
        private static bool s_errorLogged;

        private ZNetView m_nview;

        /// <summary>Stars for this game's next hire at this post (0..2).</summary>
        internal int Stars { get; private set; }

        private void Awake() => m_nview = GetComponent<ZNetView>();

        /// <summary>This pole's post kind, or null for an ordinary lantern pole.</summary>
        internal PostKind Kind
        {
            get
            {
                if (m_nview == null || !m_nview.IsValid()) return null;
                var id = m_nview.GetZDO().GetString(s_postHash);
                return string.IsNullOrEmpty(id) ? null : PostSettings.ById(id);
            }
        }

        public string GetHoverText()
        {
            try
            {
                var kind = Kind;
                return kind == null ? "" : Localization.instance.Localize(PostRules.PostHover(kind.Label, Stars));
            }
            catch (Exception e)
            {
                LogOnce(e);
                return "";
            }
        }

        public string GetHoverName()
        {
            try
            {
                var kind = Kind;
                return kind == null ? "" : PostRules.EntryName(kind.Label);
            }
            catch (Exception e)
            {
                LogOnce(e);
                return "";
            }
        }

        /// <summary>Hover text height offset (vanilla classes return a serialized m_hoverOffset, 0 by default).</summary>
        public float GetHoverOffset() => 0f;

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            try
            {
                if (hold || alt || Kind == null) return false;
                Stars = PostRules.NextStars(Stars);
                return true;
            }
            catch (Exception e)
            {
                LogOnce(e);
                return false;
            }
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;

        private static void LogOnce(Exception e)
        {
            if (s_errorLogged) return;
            s_errorLogged = true;
            Plugin.Log.LogError($"Hiring post failed (logged once): {e}");
        }
    }
}
