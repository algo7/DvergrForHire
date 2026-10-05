using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DvergrForHire
{
    /// <summary>
    /// Recruiter mode (hiring posts): a hired Dvergr marked DvergrForHire_Recruiter, linked from its post's lantern pole by a
    /// vanilla "Spawned" connection (kept across world loads; the pole is found by scanning nearby objects). It always
    /// stays (no follow / stay toggle); E hires a new Dvergr of its kind in front of the player with the stars picked at its
    /// post; Shift+E is vanilla rename; on the game running it, it leaves once its post has been gone for PostGoneSeconds.
    /// </summary>
    public sealed partial class Mercenary
    {
        private static readonly int s_recruiterHash = PostSettings.RecruiterKey.GetStableHashCode();
        private static readonly int s_poleHash = PostSettings.Pole.GetStableHashCode();
        private static readonly List<ZDO> s_nearby = new List<ZDO>();

        private readonly PostWatch m_postWatch = new PostWatch();

        /// <summary>This session's id of the recruiter's pole (ids change on every world load, so it's never saved).</summary>
        private ZDOID m_postId = ZDOID.None;

        private bool IsRecruiter => m_nview.GetZDO().GetBool(s_recruiterHash);

        private PostKind Kind => PostSettings.ByPrefab(Utils.GetPrefabName(gameObject));

        /// <summary>The stars this game picked at the recruiter's post (none when the post isn't loaded here).</summary>
        private int PostStars
        {
            get
            {
                var post = ZNetScene.instance != null && !m_postId.IsNone() ? ZNetScene.instance.FindInstance(m_postId) : null;
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
            var recruiterView = recruiter.GetComponent<ZNetView>();
            recruiterView.GetZDO().Set(s_recruiterHash, true);
            // The vanilla spawner link: kept across world loads (ZDOMan.ConnectSpawners), sent to every game with the pole.
            pole.GetZDO().SetConnection(ZDOExtraData.ConnectionType.Spawned, recruiterView.GetZDO().m_uid);
            recruiter.GetComponent<Character>().SetTamed(true);
            recruiter.GetComponent<BaseAI>().SetPatrolPoint(); // stay here (vanilla "stay"; the patrol point is in the ZDO)
            Plugin.Log.LogInfo($"Hiring post ({kind.Label}) placed: its recruiter arrived");
        }

        private string RecruiterHoverText(string vanilla)
        {
            var kind = Kind;
            if (kind == null) return vanilla;
            var price = PostRules.PostPrice(kind.Price, PostStars + 1);
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
            var price = PostRules.PostPrice(kind.Price, stars + 1);
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
            var pos = HireSpot(player);
            var toPlayer = Vector3.ProjectOnPlane(player.transform.position - pos, Vector3.up);
            var hire = Instantiate(prefab, pos, toPlayer.sqrMagnitude > 0.01f ? Quaternion.LookRotation(toPlayer) : Quaternion.identity);
            var hired = hire.GetComponent<Mercenary>();
            if (hired != null) hired.m_hiredAt = Time.unscaledTimeAsDouble; // a double press doesn't toggle the new hire to stay
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

        private static int s_hireSpotMask;

        /// <summary>The first free spot around the player (PostRules.HireSpots order), 0.5 m up; in front if none is free.</summary>
        private static Vector3 HireSpot(Player player)
        {
            if (s_hireSpotMask == 0)
                s_hireSpotMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "terrain", "character", "character_net", "vehicle");
            var t = player.transform;
            Vector3 At((float Right, float Forward) o) => t.position + t.right * o.Right + t.forward * o.Forward + Vector3.up * 0.5f;
            foreach (var offset in PostRules.HireSpots)
            {
                var spot = At(offset);
                if (!Physics.CheckSphere(spot + Vector3.up * 0.6f, 0.45f, s_hireSpotMask)) return spot; // body height: 0.65..1.55 m
            }
            return At(PostRules.HireSpots[0]);
        }

        /// <summary>
        /// Every game: whether this recruiter's pole is there. Checks the cached pole first; only when that misses, scans the
        /// 3x3 zones around the recruiter (it never strays more than ~20 m from its post) for the pole linked to it.
        /// </summary>
        private bool FindPost()
        {
            if (ZDOMan.instance == null) return false;
            var me = m_nview.GetZDO().m_uid;
            var cached = m_postId.IsNone() ? null : ZDOMan.instance.GetZDO(m_postId);
            if (cached != null && cached.GetConnectionZDOID(ZDOExtraData.ConnectionType.Spawned) == me) return true;
            s_nearby.Clear();
            ZDOMan.instance.FindSectorObjects(ZoneSystem.GetZone(transform.position), new SimulationDistance(1, 0), s_nearby);
            var post = PostRules.PostOf(s_nearby.Where(z => z.GetPrefab() == s_poleHash), z => z.GetConnectionZDOID(ZDOExtraData.ConnectionType.Spawned), me);
            m_postId = post != null ? post.m_uid : ZDOID.None;
            return post != null;
        }

        /// <summary>
        /// On the game running the recruiter: true (and it's removed) once its post has been gone for PostGoneSeconds in an area
        /// this game has fully loaded.
        /// </summary>
        private bool RecruiterLeaves(bool postFound)
        {
            var areaReady = postFound || (ZNetScene.instance != null && ZNetScene.instance.IsAreaReady(transform.position));
            if (!m_postWatch.ShouldLeave(postFound, areaReady, Time.unscaledTimeAsDouble)) return false;
            Plugin.Log.LogInfo("A hiring post is gone: its recruiter left");
            m_nview.Destroy();
            return true;
        }
    }
}
