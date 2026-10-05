using System;
using UnityEngine;

namespace DvergrForHire
{
    /// <summary>
    /// On the vanilla Dvergr lantern pole prefab, in games with the mod. Inert on ordinary poles (no hover text, E does
    /// nothing: vanilla). On a hiring post (its ZDO has DvergrForHire_Post): hover with the kind and this game's star choice,
    /// E cycles the stars for this game's next hire there (local, not saved); every second, once its recruiter has been gone
    /// for PostGoneSeconds (it died), the post breaks: vanilla WearNTear.Remove, handled by whichever game controls the pole
    /// (with or without the mod), with the break effect and the pole's materials dropped. Never throws.
    /// </summary>
    public sealed class HiringPost : MonoBehaviour, Hoverable, Interactable
    {
        private static readonly int s_postHash = PostSettings.PostKey.GetStableHashCode();
        private static bool s_errorLogged;

        private ZNetView m_nview;
        private readonly PostWatch m_recruiterWatch = new PostWatch();
        private bool m_breakLogged;

        /// <summary>Stars for this game's next hire at this post (0..2).</summary>
        internal int Stars { get; private set; }

        private void Awake()
        {
            m_nview = GetComponent<ZNetView>();
            InvokeRepeating(nameof(Tick), UnityEngine.Random.Range(0.2f, 1f), 1f);
        }

        /// <summary>Break the post once its recruiter has been gone for PostGoneSeconds; retried every second until it's gone.</summary>
        private void Tick()
        {
            try
            {
                var kind = Kind;
                if (kind == null || ZDOMan.instance == null || ZNetScene.instance == null) return; // an ordinary lantern pole
                var zdo = m_nview.GetZDO();
                // Linked by the vanilla spawner connection; after a world load a dead recruiter leaves it "Spawned, no target".
                if (zdo.GetConnectionType() != ZDOExtraData.ConnectionType.Spawned) return;
                var recruiter = zdo.GetConnectionZDOID(ZDOExtraData.ConnectionType.Spawned);
                var found = !recruiter.IsNone() && ZDOMan.instance.GetZDO(recruiter) != null;
                var areaReady = found || ZNetScene.instance.IsAreaReady(transform.position);
                if (!PostRules.PoleBreaks(true, found, areaReady, m_recruiterWatch, Time.unscaledTimeAsDouble)) return;
                var wearNTear = GetComponent<WearNTear>();
                if (wearNTear != null) wearNTear.Remove(); // drops the pole's materials, like any destroyed piece
                if (m_breakLogged) return;
                m_breakLogged = true;
                Plugin.Log.LogInfo($"A {kind.Label} recruiter died: its hiring post broke");
            }
            catch (Exception e)
            {
                LogOnce(e);
            }
        }

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
