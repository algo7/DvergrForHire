using DvergrForHire;

internal static partial class Tests
{
    private static void Test_Price_FiftyPerStar()
    {
        // User, 2026-10-05: "for star cost just plus 50 for each star" (level 1 = no stars).
        Eq(100, HireRules.Price(100, 1), "no stars");
        Eq(150, HireRules.Price(100, 2), "1 star: +50");
        Eq(200, HireRules.Price(100, 3), "2 stars: +100");
        Eq(300, HireRules.Price(200, 3), "Ashlands, 2 stars");
    }

    private static void Test_Price_LevelBelowOneCountsAsOne()
    {
        Eq(100, HireRules.Price(100, 0), "level 0: no stars, never free");
        Eq(100, HireRules.Price(100, -3), "negative level: no stars, never free");
    }

    private static void Test_Price_NoBasePriceStaysNotForHire()
    {
        Eq(0, HireRules.Price(0, 3), "not a hireable kind: stars don't give it a price");
        Eq(HireRules.Hire.NotHireable, HireRules.Check(true, false, false, 9999, HireRules.Price(0, 3)), "so it can't be hired");
    }

    private static void Test_Hire_WildAndPeacefulWithCoins()
    {
        Eq(HireRules.Hire.Hire, HireRules.Check(true, false, false, 9999, 500), "enough coins");
    }

    private static void Test_Hire_ExactlyThePrice()
    {
        Eq(HireRules.Hire.Hire, HireRules.Check(true, false, false, 500, 500), "exactly the price");
        Eq(HireRules.Hire.NotEnoughCoins, HireRules.Check(true, false, false, 499, 500), "one coin short");
        Eq(HireRules.Hire.NotEnoughCoins, HireRules.Check(true, false, false, 0, 500), "no coins");
    }

    private static void Test_Hire_NotProvokedTamedOrDead()
    {
        Eq(HireRules.Hire.NotHireable, HireRules.Check(true, false, true, 9999, 500), "provoked: won't work for you");
        Eq(HireRules.Hire.NotHireable, HireRules.Check(true, false, true, 0, 500), "provoked and broke: no coin message either");
        Eq(HireRules.Hire.NotHireable, HireRules.Check(true, true, false, 9999, 500), "already hired");
        Eq(HireRules.Hire.NotHireable, HireRules.Check(false, false, false, 9999, 500), "dead");
        Eq(HireRules.Hire.NotHireable, HireRules.Check(true, false, false, 9999, 0), "no price: not a hireable kind");
    }

    private static void Test_Hire_SecondPressRightAfterHiring()
    {
        False(HireRules.RecentlyHired(0, 100), "never hired here");
        True(HireRules.RecentlyHired(100, 100), "the same moment");
        True(HireRules.RecentlyHired(100, 102.9), "2.9 s later: E still does nothing (no toggle to stay)");
        False(HireRules.RecentlyHired(100, 103.1), "3.1 s later: E is follow / stay again");
        False(HireRules.RecentlyHired(100, 99), "clock went back: not stuck");
    }

    private static void Test_Hire_PaidCountsAsHiredUntilTheFlagArrives()
    {
        // Mercenary passes "tamed || paid": paid stays until the tamed flag is seen, however slow the sync.
        Eq(HireRules.Hire.NotHireable, HireRules.Check(true, true, false, 9999, 500), "paid, flag not back after 10 s: no second payment");
        Eq(HireRules.Paid.Waiting, HireRules.AfterPaying(false, 100, 104.9), "flag not back yet");
        Eq(HireRules.Paid.Resend, HireRules.AfterPaying(false, 100, 105), "5 s without the flag: send 'tamed' again");
        Eq(HireRules.Paid.Confirmed, HireRules.AfterPaying(true, 100, 101), "the flag is seen: done");
        Eq(HireRules.Paid.Confirmed, HireRules.AfterPaying(true, 100, 500), "even late");
    }

    private const string KeyUse = "[<color=yellow><b>$KEY_Use</b></color>] ";

    private static void Test_Hover_WildShowsThePrice()
    {
        Eq("Dvergr rogue\n" + KeyUse + "Hire: 1500 coins", HireRules.Hover("Dvergr rogue", false, false, 1500, "Dvergr rogue ( Wild, Hungry )"), "wild");
        Eq("Dvergr rogue\n" + KeyUse + "Hire: 12000 coins", HireRules.Hover("Dvergr rogue", false, false, 12000, ""), "no thousands separator");
    }

    private static void Test_Hover_ProvokedShowsNothing()
    {
        Eq("", HireRules.Hover("Dvergr rogue", false, true, 500, "Dvergr rogue ( Wild, Hungry )"), "like a vanilla Dvergr");
    }

    private static void Test_Hover_HiredKeepsVanillasRenameLine()
    {
        var vanilla = "Dvergr rogue ( Tame, Hungry )\n[<color=yellow><b>E</b></color>] Pet\n[<color=yellow><b>L-Shift + E</b></color>] Rename";
        Eq("Dvergr rogue ( Hired )\n" + KeyUse + "Follow / Stay\n[<color=yellow><b>L-Shift + E</b></color>] Rename",
            HireRules.Hover("Dvergr rogue", true, false, 500, vanilla), "status and E line ours, rename line vanilla's");
        Eq("Dvergr rogue ( Hired )\n" + KeyUse + "Follow / Stay", HireRules.Hover("Dvergr rogue", true, false, 500, "Dvergr rogue ( Wild, Hungry )"),
            "just hired, tamed flag not back yet: no rename line");
        Eq("Dvergr rogue ( Hired )\n" + KeyUse + "Follow / Stay", HireRules.Hover("Dvergr rogue", true, true, 500, null), "hired wins over provoked; no vanilla text");
    }

    private static void Test_Hired_NeverHungrySoTheyHeal()
    {
        // BaseAI.UpdateRegeneration only heals a tamed creature with a Tameable when it isn't hungry, and hired Dvergr
        // never eat: counted as hungry they'd never regain health (wild Dvergr do).
        False(HireRules.Hungry(true, true), "hired: never hungry, so it regenerates like a wild Dvergr");
        True(HireRules.Hungry(true, false), "wild: vanilla's answer (taming never starts: it needs not hungry)");
        False(HireRules.Hungry(false, false), "vanilla says fed: unchanged");
    }

    private static void Test_Messages()
    {
        Eq("Not enough coins (1000)", HireRules.NotEnoughCoins(1000), "too few coins");
        Eq("Dvergr mage hired", HireRules.Hired("Dvergr mage"), "hired");
    }
}
