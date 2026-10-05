using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DvergrForHire
{
    internal enum SettingKind
    {
        /// <summary>A float, bool or int, set as is.</summary>
        Value,

        /// <summary>An EffectList built from vanilla effect prefabs, by name (none = empty list).</summary>
        Effects,
    }

    /// <summary>
    /// One public field the setup sets on a vanilla component, with its value. Prefabs are names, so the tables need no
    /// running engine: the tests (and the runtime pre-flight) check every field against the game's types.
    /// </summary>
    internal sealed class FieldSetting
    {
        public readonly Type Component;
        public readonly string Field;
        public readonly SettingKind Kind;
        public readonly object Value;

        private FieldSetting(Type component, string field, SettingKind kind, object value)
        {
            Component = component;
            Field = field;
            Kind = kind;
            Value = value;
        }

        public static FieldSetting Of<T>(string field, object value) => new FieldSetting(typeof(T), field, SettingKind.Value, value);
        public static FieldSetting Effects<T>(string field, params string[] prefabs) => new FieldSetting(typeof(T), field, SettingKind.Effects, prefabs);

        /// <summary>The type the game's field must have for this setting.</summary>
        public Type ExpectedFieldType => Kind == SettingKind.Effects ? typeof(EffectList) : Value.GetType();

        /// <summary>The vanilla prefabs this setting needs.</summary>
        public IEnumerable<string> PrefabNames => Kind == SettingKind.Effects ? (string[])Value : Enumerable.Empty<string>();

        /// <summary>Null when the game's component has this public field with the expected type; else why not.</summary>
        public string Problem()
        {
            var field = Component.GetField(Field, BindingFlags.Instance | BindingFlags.Public);
            if (field == null) return $"{this} doesn't exist";
            if (field.FieldType != ExpectedFieldType) return $"{this} is a {field.FieldType.Name}, not a {ExpectedFieldType.Name}";
            return null;
        }

        public override string ToString() => $"{Component.Name}.{Field}";
    }
}
