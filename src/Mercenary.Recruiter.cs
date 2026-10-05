using System.Collections.Generic;
using UnityEngine;

namespace DvergrForHire
{
    /// <summary>
    /// Recruiter mode (hiring posts): a hired Dvergr linked to its post's lantern pole by DvergrForHire_Recruiter. It always
    /// stays (no follow / stay toggle); E hires a new Dvergr of its kind in front of the player with the stars picked at its
    /// post; Shift+E is vanilla rename; on the game running it, it leaves once its post has been gone for PostGoneSeconds.
    /// </summary>
    public sealed partial class Mercenary
    {
        private static readonly KeyValuePair<int, int> s_recruiterHash = ZDO.GetHashZDOID(PostSettings.RecruiterKey);

        private readonly PostWatch m_postWatch = new PostWatch();

        private ZDOID PostId => m_nview.GetZDO().GetZDOID(s_recruiterHash);

        private bool IsRecruiter => !PostId.IsNone();

        private PostKind Kind => PostSettings.ByPrefab(Utils.GetPrefabName(gameObject));

        /// <summary>The stars this game picked at the recruiter's post (none when the post isn't loaded here).</summary>
        private int PostStars
        {
            get
            {
                var post = ZNetScene.instance != null ? ZNetScene.instance.FindInstance(PostId) : null;
                var hiring = post != null ? post.GetComponent<HiringPost>() : null;
                return hiring != null ? hiring.Stars : 0;
            }
        }

        /// <summary>A new post's recruiter, made by the placing game (which owns it): hired, staying, linked to its pole.</summary>
        internal static void CreateRecruiter(ZNetView pole, string kindId, Vector3 placer)
        {
            var kind = PostSettings.ById(kindId);
            var prefab = kind != null && ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(kind.Prefab) : null;
            if (prefab == null || pole == null || !pole.IsValid()) return;
            var toPlacer = placer - pole.transform.position;
            toPlacer.y = 0;
            var dir = toPlacer.sqrMagnitude > 0.01f ? toPlacer.normalized : pole.transform.forward;
            var pos = pole.transform.position + dir * PostSettings.RecruiterOffset + Vector3.up * 0.5f;
            var recruiter = Instantiate(prefab, pos, Quaternion.LookRotation(dir));
            recruiter.GetComponent<ZNetView>().GetZDO().Set(s_recruiterHash, pole.GetZDO().m_uid);
            recruiter.GetComponent<Character>().SetTamed(true);
            recruiter.GetComponent<BaseAI>().SetPatrolPoint(); // stay here (vanilla "stay"; the patrol point is in the ZDO)
            Plugin.Log.LogInfo($"Hiring post ({kind.Label}) placed: its recruiter arrived");
        }

        private string RecruiterHoverText(string vanilla)
        {
            var kind = Kind;
            if (kind == null) return vanilla;
            var price = HireRules.Price(kind.Price, PostStars + 1);
            return Localization.instance.Localize(PostRules.RecruiterHover(kind.Label, price, vanilla));
        }

        /// <summary>The E patch for a recruiter: null = vanilla (Shift+E rename); else the result.</summary>
        private bool? RecruiterInteract(Humanoid user, bool hold, bool alt)
        {
            switch (PostRules.RecruiterInteract(hold, alt))
            {
                case PostRules.RecruiterE.Vanilla:
                    return null;
                case PostRules.RecruiterE.Nothing:
                    return false;
                default:
                    return HireAtPost(user);
            }
        }

        /// <summary>
        /// On the game of the player pressing E: pay like a camp hire, then create the kind in front of the player with the
        /// post's stars, hired and following. This game owns the new Dvergr, so tamed and follow apply at once.
        /// </summary>
        private bool HireAtPost(Humanoid user)
        {
            var player = user as Player;
            var kind = Kind;
            if (player == null || player != Player.m_localPlayer || kind == null || string.IsNullOrEmpty(s_coinName)) return false;
            var prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(kind.Prefab) : null;
            if (prefab == null) return false;
            var stars = PostStars;
            var price = HireRules.Price(kind.Price, stars + 1);
            var inventory = player.GetInventory();
            var coins = inventory.CountItems(s_coinName);
            switch (HireRules.Check(!m_character.IsDead(), false, false, coins, price))
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
            var forward = player.transform.forward;
            var pos = player.transform.position + forward * PostSettings.HireOffset + Vector3.up * 0.5f;
            var hire = Instantiate(prefab, pos, Quaternion.LookRotation(-forward));
            var character = hire.GetComponent<Character>();
            var tameable = hire.GetComponent<Tameable>();
            character.SetLevel(stars + 1);
            character.SetTamed(true);
            tameable.Command(player, message: false); // follow the hirer at once
            s_hireEffect?.Create(pos, hire.transform.rotation);
            player.Message(MessageHud.MessageType.Center, HireRules.Hired(tameable.GetName()));
            Plugin.Log.LogInfo($"Hired a {kind.Label} at a hiring post (level {stars + 1}) for {price} coins");
            return true;
        }

        /// <summary>On the game running the recruiter: true (and it's removed) once its post has been gone for PostGoneSeconds.</summary>
        private bool RecruiterLeaves()
        {
            var postThere = ZDOMan.instance != null && ZDOMan.instance.GetZDO(PostId) != null;
            if (!m_postWatch.ShouldLeave(postThere, Time.unscaledTimeAsDouble)) return false;
            Plugin.Log.LogInfo("A hiring post is gone: its recruiter left");
            m_nview.Destroy();
            return true;
        }
    }
}
