using System.Collections.Generic;
using UnityEngine;

namespace DvergrForHire
{
    /// <summary>Recruiter mode (hiring posts): a hired Dvergr linked to its post's lantern pole by DvergrForHire_Recruiter.</summary>
    public sealed partial class Mercenary
    {
        private static readonly KeyValuePair<int, int> s_recruiterHash = ZDO.GetHashZDOID(PostSettings.RecruiterKey);

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
    }
}
