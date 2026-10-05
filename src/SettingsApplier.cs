using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace DvergrForHire
{
    /// <summary>Writes FieldSetting tables into live components (fields checked by FieldSetting.Problem beforehand).</summary>
    internal static class SettingsApplier
    {
        /// <param name="prefab">Looks up vanilla prefabs by name.</param>
        public static void Apply(Component target, IEnumerable<FieldSetting> settings, Func<string, GameObject> prefab)
        {
            foreach (var setting in settings)
                setting.Component.GetField(setting.Field, BindingFlags.Instance | BindingFlags.Public)
                    .SetValue(target, Resolve(setting, prefab));
        }

        public static EffectList Effects(IEnumerable<string> names, Func<string, GameObject> prefab) => new EffectList
        {
            m_effectPrefabs = names.Select(n => new EffectList.EffectData { m_prefab = prefab(n) }).ToArray(),
        };

        private static object Resolve(FieldSetting setting, Func<string, GameObject> prefab) =>
            setting.Kind == SettingKind.Effects ? Effects(setting.PrefabNames, prefab) : setting.Value;
    }
}
