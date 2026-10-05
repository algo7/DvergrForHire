using System.Linq;
using DvergrForHire;

internal static partial class Tests
{
    private static FieldSetting Setting(FieldSetting[] table, string field) => table.Single(s => s.Field == field);

    private static void Test_Settings_EveryFieldExistsWithTheRightType()
    {
        foreach (var setting in DvergrSettings.TameableSettings)
            Eq(null, setting.Problem(), setting.ToString());
    }

    private static void Test_Settings_ProblemReportsMistakes()
    {
        True(FieldSetting.Of<Tameable>("m_nope", 1f).Problem() != null, "a missing field");
        True(FieldSetting.Of<Tameable>("m_tamingTime", 1).Problem() != null, "int for a float field");
        True(FieldSetting.Effects<Tameable>("m_tamingTime", "x").Problem() != null, "effects for a float field");
    }

    private static void Test_Settings_RequiredPrefabs()
    {
        Eq("Dverger,DvergerMage,DvergerMageFire,DvergerMageIce,DvergerMageSupport,DvergerAshlands,DvergerDeepNorth,Coins,sfx_coins_placed,sfx_dverger_vo_idle",
            string.Join(",", DvergrSettings.RequiredPrefabs), "every prefab the setup uses, once");
    }

    private static void Test_Settings_PricesFromTheUser()
    {
        foreach (var mistlands in new[] { "Dverger", "DvergerMage", "DvergerMageFire", "DvergerMageIce", "DvergerMageSupport" })
            Eq(500, DvergrSettings.BasePrice(mistlands), mistlands);
        Eq(1000, DvergrSettings.BasePrice("DvergerAshlands"), "Ashlands");
        Eq(1500, DvergrSettings.BasePrice("DvergerDeepNorth"), "Deep North");
        Eq(0, DvergrSettings.BasePrice("DvergerTest"), "the dev-only Dvergr isn't for hire");
        Eq(7, DvergrSettings.Hireable.Length, "seven kinds");
    }

    private static void Test_Settings_TameableNeverTamesByItself()
    {
        var t = DvergrSettings.TameableSettings;
        Eq(true, (bool)Setting(t, "m_commandable").Value, "E = follow / stay");
        Eq(false, (bool)Setting(t, "m_startsTamed").Value, "wild until hired");
        Eq("sfx_dverger_vo_idle", string.Join(",", Setting(t, "m_petEffect").PrefabNames), "E plays the Dvergr's own voice");
        Eq("", string.Join(",", Setting(t, "m_tamedEffect").PrefabNames), "no taming effect (the hire plays the coin sound)");
        Eq("", string.Join(",", Setting(t, "m_sootheEffect").PrefabNames), "no taming hearts");
        False(DvergrSettings.RequiredPrefabs.Any(n => n.ToLowerInvariant().Contains("wolf")), "no wolf effect anywhere");
    }
}
