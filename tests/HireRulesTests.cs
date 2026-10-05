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

    private static void Test_E_WhatItDoesOnADvergr()
    {
        // The E patch on a Dvergr, decided in this order: recruiter mode; a double press right after hiring; vanilla follow /
        // stay / rename on hired Dvergr; nothing while paid, held or with Shift; else hire.
        Eq(HireRules.EPress.Hire, HireRules.WhatEDoes(false, false, false, false, false, false), "wild: E hires");
        Eq(HireRules.EPress.Nothing, HireRules.WhatEDoes(false, false, false, false, true, false), "wild, held E: nothing (vanilla repeats it every frame)");
        Eq(HireRules.EPress.Nothing, HireRules.WhatEDoes(false, false, false, false, false, true), "wild, Shift+E: nothing (no rename on a wild Dvergr)");
        Eq(HireRules.EPress.Nothing, HireRules.WhatEDoes(false, false, false, true, false, false), "paid, tamed flag not back yet: never pays twice");
        Eq(HireRules.EPress.Nothing, HireRules.WhatEDoes(false, true, false, true, false, false), "just hired, flag not back: nothing");
        Eq(HireRules.EPress.Nothing, HireRules.WhatEDoes(false, true, true, false, false, false), "just hired: a double press doesn't make it stay");
        Eq(HireRules.EPress.Vanilla, HireRules.WhatEDoes(false, false, true, false, false, false), "hired: vanilla follow / stay");
        Eq(HireRules.EPress.Vanilla, HireRules.WhatEDoes(false, false, true, false, false, true), "hired, Shift+E: vanilla rename");
        Eq(HireRules.EPress.Vanilla, HireRules.WhatEDoes(false, false, true, true, false, false), "hired, flag seen before 'paid' is cleared: vanilla");
        Eq(HireRules.EPress.Recruiter, HireRules.WhatEDoes(true, true, true, true, true, true), "a recruiter: its own E, whatever else");
        Eq(HireRules.EPress.Recruiter, HireRules.WhatEDoes(true, false, true, false, false, false), "a recruiter: never vanilla follow / stay");
    }

    private static void Test_Summons_MarkedOnlyFromAHiredMagesSpawn()
    {
        // User, 2026-10-05 ("track hired ones"): a support mage's mistile is its own creature, so its explosion isn't the
        // mage's hit. Mistiles a hired mage summons get marked when they're created (same frame as SpawnAbility.FindTarget).
        True(HireRules.MarkSummon(true, true, true), "a hired mage's summon, created in that frame, its prefab: marked");
        False(HireRules.MarkSummon(false, true, true), "a wild mage's mistile: vanilla");
        False(HireRules.MarkSummon(true, false, true), "a later frame: something else is being created");
        False(HireRules.MarkSummon(true, true, false), "another creature created in that frame: not a summon");
    }

    private static void Test_Buildings_SafeFromHiresAndTheirSummons()
    {
        True(HireRules.BuildingSafeFrom(true, false, false), "a hired Dvergr's hit");
        True(HireRules.BuildingSafeFrom(false, true, false), "a hired mage's mistile exploding");
        // User, 2026-10-06 ("fix all noted but unfixed issues"): a fireball or bolt that lands after its hired Dvergr died
        // has no attacker in vanilla's hit, so the shot itself carries the marker.
        True(HireRules.BuildingSafeFrom(false, false, true), "a hired Dvergr's shot landing after it died");
        False(HireRules.BuildingSafeFrom(false, false, false), "anything else: vanilla damage");
    }

    private static void Test_Shots_WhoAHiredDvergrFights()
    {
        // BaseAI.IsEnemy for a tamed Dverger: players, tames and Dvergr that aren't provoked are friends.
        False(HireRules.HiredDvergrFoe(players: true, tamed: false, dvergr: false, provoked: false), "a player");
        False(HireRules.HiredDvergrFoe(players: false, tamed: true, dvergr: false, provoked: false), "a tame (wolf, lox, another hire)");
        False(HireRules.HiredDvergrFoe(players: false, tamed: false, dvergr: true, provoked: false), "a wild Dvergr, not provoked");
        True(HireRules.HiredDvergrFoe(players: false, tamed: false, dvergr: true, provoked: true), "a provoked wild Dvergr");
        True(HireRules.HiredDvergrFoe(players: false, tamed: false, dvergr: false, provoked: false), "a troll");
    }

    private static void Test_Shots_DeadHiresShotsKeepTheirFlags()
    {
        // Vanilla hits everyone with a shot whose owner is gone; a hired Dvergr's shot keeps its own flags instead.
        False(HireRules.ShotHits(foe: false, hitsFriends: false, hitsEnemies: true, hitsSameKind: true, sameKind: false), "fireball vs you");
        True(HireRules.ShotHits(foe: true, hitsFriends: false, hitsEnemies: true, hitsSameKind: true, sameKind: false), "fireball vs a troll");
        True(HireRules.ShotHits(foe: false, hitsFriends: true, hitsEnemies: false, hitsSameKind: true, sameKind: false), "heal vs you");
        False(HireRules.ShotHits(foe: true, hitsFriends: true, hitsEnemies: false, hitsSameKind: true, sameKind: false), "heal vs a troll");
        False(HireRules.ShotHits(foe: true, hitsFriends: false, hitsEnemies: true, hitsSameKind: false, sameKind: true), "fire puddle vs a provoked mage of its kind");
    }

    private static void Test_Messages()
    {
        Eq("Not enough coins (1000)", HireRules.NotEnoughCoins(1000), "too few coins");
        Eq("Dvergr mage hired", HireRules.Hired("Dvergr mage"), "hired");
    }
}
