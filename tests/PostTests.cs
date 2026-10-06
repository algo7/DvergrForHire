using System.Linq;
using DvergrForHire;

internal static partial class Tests
{
    private static string Fee(string id) => string.Join(",", PostSettings.ById(id).Fee.Select(f => $"{f.Item} {f.Amount}"));

    private static void Test_Posts_TableFromTheUser()
    {
        Eq("rogue:Dverger:100,firemage:DvergerMageFire:100,icemage:DvergerMageIce:100,supportmage:DvergerMageSupport:100,ashlands:DvergerAshlands:200,deepnorth:DvergerDeepNorth:400",
            string.Join(",", PostSettings.Kinds.Select(k => $"{k.Id}:{k.Prefab}:{k.Price}")), "one post per kind, camp prices, Deep North 400");
        Eq("rogue,fire mage,ice mage,support mage,Ashlands,Deep North", string.Join(",", PostSettings.Kinds.Select(k => k.Label)), "labels");
        foreach (var mistlands in new[] { "rogue", "firemage", "icemage", "supportmage" })
            Eq("YggdrasilWood 10,BlackMarble 5", Fee(mistlands), mistlands + ": Mistlands fee");
        Eq("Blackwood 10,Grausten 5", Fee("ashlands"), "Ashlands fee");
        Eq("Frostwood 10,NornThread 5", Fee("deepnorth"), "Deep North fee");
        Eq("YggdrasilWood,BlackMarble,Blackwood,Grausten,Frostwood,NornThread", string.Join(",", PostSettings.FeeItems), "every fee item, once");
        Eq("piece_dvergr_lantern_pole", PostSettings.Pole, "the post is the vanilla Dvergr lantern pole");
    }

    private static void Test_Posts_InTheDefenseSection()
    {
        // User, 2026-10-06: the posts were only found at the bottom of "All" (they copied the lantern pole's Lighting tag);
        // "add it to the defense section". The build menu's sections are the pieces' usage tags: Defense only, not Lighting.
        Eq(Piece.UsageTagFlags.Defense, PostSettings.Section, "the hammer's Defense section, and only there");
    }

    private static void Test_Posts_EveryKindIsHireable()
    {
        foreach (var kind in PostSettings.Kinds)
            True(DvergrSettings.BasePrice(kind.Prefab) > 0, kind.Prefab + " is one of the hireable Dvergr (has Tameable + Mercenary)");
        Eq("icemage", PostSettings.ByPrefab("DvergerMageIce").Id, "by prefab");
        Eq(null, PostSettings.ByPrefab("DvergerMage"), "the camp mage isn't a post kind");
        Eq(null, PostSettings.ById("nope"), "unknown kind");
    }

    private static void Test_Posts_PriceWithStars()
    {
        // User, 2026-10-05: "add a 20% premium on post hire": the whole price (base + 50 per star) × 1.2.
        Eq(120, PostRules.PostPrice(PostSettings.ById("rogue").Price, 1), "rogue, no stars: 100 × 1.2");
        Eq(180, PostRules.PostPrice(PostSettings.ById("firemage").Price, 2), "fire mage, 1 star: 150 × 1.2");
        Eq(360, PostRules.PostPrice(PostSettings.ById("ashlands").Price, 3), "Ashlands, 2 stars: 300 × 1.2");
        Eq(600, PostRules.PostPrice(PostSettings.ById("deepnorth").Price, 3), "Deep North, 2 stars: 500 × 1.2");
        Eq(0, PostRules.PostPrice(0, 3), "no base price stays not for hire");
        Eq(20, PostSettings.PremiumPercent, "the premium");
    }

    private static void Test_Posts_StarsCycle()
    {
        Eq(1, PostRules.NextStars(0), "none → ★");
        Eq(2, PostRules.NextStars(1), "★ → ★★");
        Eq(0, PostRules.NextStars(2), "★★ → none");
        Eq(0, PostRules.NextStars(-1), "out of range: back to none");
        Eq(0, PostRules.NextStars(7), "out of range: back to none");
        Eq("none", PostRules.StarsText(0), "no stars");
        Eq("★", PostRules.StarsText(1), "one star");
        Eq("★★", PostRules.StarsText(2), "two stars");
    }

    private static void Test_Posts_Texts()
    {
        Eq("Hiring post: fire mage", PostRules.EntryName("fire mage"), "hammer entry");
        Eq("A Dvergr Deep North recruiter stays here. Pick the stars on the post, hire at the recruiter.", PostRules.EntryDescription("Deep North"), "entry description");
        Eq("Hiring post: fire mage\n" + KeyUse + "Stars for the next hire: ★", PostRules.PostHover("fire mage", 1), "post hover");
        Eq("Fire mage recruiter", PostRules.RecruiterName("fire mage"), "capitalised");
        Eq("Deep North recruiter", PostRules.RecruiterName("Deep North"), "names keep their capitals");
        var vanilla = "Dvergr mage ( Tame, Hungry )\n[<color=yellow><b>E</b></color>] Pet\n[<color=yellow><b>L-Shift + E</b></color>] Rename";
        // User, 2026-10-06: the E line says what you get ("[E] Hire: 120 coins" could read as hiring the recruiter himself).
        Eq("Ice mage recruiter\n" + KeyUse + "Hire an ice mage: 150 coins\n[<color=yellow><b>L-Shift + E</b></color>] Rename",
            PostRules.RecruiterHover("ice mage", "an ice mage", 150, vanilla), "hire line ours, rename line vanilla's");
        Eq("Rogue recruiter\n" + KeyUse + "Hire a rogue: 100 coins", PostRules.RecruiterHover("rogue", "a rogue", 100, null), "no vanilla text");
        Eq("a rogue,a fire mage,an ice mage,a support mage,an Ashlands Dvergr,a Deep North Dvergr",
            string.Join(",", PostSettings.Kinds.Select(k => k.One)), "what each post hires, as in \"Hire a rogue\"");
    }

    private static void Test_Posts_RecruiterE()
    {
        Eq(PostRules.RecruiterE.Hire, PostRules.RecruiterInteract(false, false), "E: hire");
        Eq(PostRules.RecruiterE.Vanilla, PostRules.RecruiterInteract(false, true), "Shift+E: vanilla rename");
        Eq(PostRules.RecruiterE.Nothing, PostRules.RecruiterInteract(true, false), "held E: nothing (never hires every frame)");
        Eq(PostRules.RecruiterE.Nothing, PostRules.RecruiterInteract(true, true), "held Shift+E: nothing, like vanilla");
    }

    private static void Test_Posts_HireSpotsFrontFirst()
    {
        // Reviewer: a hire 2 m ahead can land inside the recruiter or a hut wall. Spots (right, forward) from the player, tried
        // in order until one is free: in front (as the user asked), then right, left, behind.
        Eq("0,2;1.5,0;-1.5,0;0,-1.5",
            string.Join(";", PostRules.HireSpots.Select(o => string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0},{1}", o.Right, o.Forward))),
            "front first, then the sides, then behind");
    }

    private static void Test_Follow_NeverSentWaitsForTheFirstOwner()
    {
        // Hiring a camp Dvergr that has no owner at that moment: "follow me" isn't broadcast to everyone (target 0 = all games);
        // the pending follow sends it once to the first game with the mod that runs it.
        var follow = new PendingFollow("Astrid", 0, 0);
        Eq(PendingFollow.Step.Wait, follow.Decide("", 0, 0, false, 1), "still no owner");
        Eq(PendingFollow.Step.Send, follow.Decide("", Me, 0, true, 2), "our game got it: send now");
        Eq(PendingFollow.Step.Wait, follow.Decide("", Me, 0, true, 3), "once");
    }

    private static void Test_Posts_PostMadeDvergrDropNothing()
    {
        // User, 2026-10-05: Dvergr made by a post (recruiters and hires) drop no loot when they die, so posts can't turn coins
        // into gemstones or trophies (a butchered 2★ Deep North would give ~1.6 ancient gemstones for 600 coins).
        False(PostRules.DropsLoot(true), "made by a post: no loot");
        True(PostRules.DropsLoot(false), "camp Dvergr, hired or not: vanilla loot");
        Eq("DvergrForHire_FromPost", PostSettings.FromPostKey, "the hidden marker on post-made Dvergr");
    }

    private static void Test_Posts_PoleBreaksOnlyAfterTenSecondsWithoutItsRecruiter()
    {
        // User, 2026-10-05: "when the post devger died the post should be gone too"; it breaks like a destroyed piece.
        var watch = new PostWatch();
        False(PostRules.PoleBreaks(true, true, true, watch, 0), "recruiter there");
        False(PostRules.PoleBreaks(true, false, true, watch, 1), "recruiter missing: start counting (its data may arrive late)");
        False(PostRules.PoleBreaks(true, false, true, watch, 10.9), "missing 9.9 s");
        True(PostRules.PoleBreaks(true, false, true, watch, 11.1), "missing 10.1 s: the post breaks");
        var old = new PostWatch();
        False(PostRules.PoleBreaks(false, false, true, old, 0), "a post placed before posts linked their recruiter");
        False(PostRules.PoleBreaks(false, false, true, old, 100), "never breaks by itself");
    }

    private static void Test_Posts_RecruiterLeavesOnlyAfterTenSeconds()
    {
        var watch = new PostWatch();
        False(watch.ShouldLeave(true, true, 0), "post there");
        False(watch.ShouldLeave(false, true, 1), "missing: start counting (its data may arrive late)");
        False(watch.ShouldLeave(false, true, 10.9), "missing 9.9 s");
        True(watch.ShouldLeave(false, true, 11.1), "missing 10.1 s: leave");

        var back = new PostWatch();
        False(back.ShouldLeave(false, true, 0), "missing");
        False(back.ShouldLeave(true, true, 5), "it arrived");
        False(back.ShouldLeave(false, true, 6), "missing again: counted from 6");
        False(back.ShouldLeave(false, true, 15.9), "9.9 s");
        True(back.ShouldLeave(false, true, 16.1), "10.1 s");

        var reset = new PostWatch();
        False(reset.ShouldLeave(false, true, 0), "missing");
        reset.Reset(); // another game runs the recruiter for a while
        False(reset.ShouldLeave(false, true, 9), "counted again from 9");
        False(reset.ShouldLeave(false, true, 18.9), "9.9 s");
        True(reset.ShouldLeave(false, true, 19.1), "10.1 s");
    }

    private static void Test_Posts_NotLoadedHereIsNotMissing()
    {
        // Reviewer: at the edge of what this game has loaded, the partner may simply not be loaded here. Only an area this
        // game has fully loaded (vanilla ZNetScene.IsAreaReady) can say "gone".
        var watch = new PostWatch();
        for (var t = 0; t <= 100; t += 5)
            False(watch.ShouldLeave(false, false, t), $"area not loaded here, t={t}: never counts");
        False(watch.ShouldLeave(false, true, 101), "area loaded: start counting now");
        False(watch.ShouldLeave(false, false, 105), "area unloaded again: the count starts over");
        False(watch.ShouldLeave(false, true, 106), "loaded: counted from 106");
        True(watch.ShouldLeave(false, true, 116.1), "10.1 s missing in a loaded area: gone");
        False(PostRules.PoleBreaks(true, false, false, new PostWatch(), 0), "the pole side too");
    }

    private static void Test_Posts_FindsThePoleLinkedToARecruiter()
    {
        // The pole links its recruiter with a vanilla "Spawned" connection (kept across saves); the recruiter finds its pole
        // among nearby objects as the pole whose connection points at it.
        var poles = new[] { (Id: 1, Target: 7), (Id: 2, Target: 9), (Id: 3, Target: 0) };
        Eq(2, PostRules.PostOf(poles, p => p.Target, 9).Id, "the pole that points at recruiter 9");
        Eq(0, PostRules.PostOf(poles, p => p.Target, 5).Id, "none points at 5: default");
    }
}
