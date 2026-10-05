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
        Eq(100, HireRules.Price(PostSettings.ById("rogue").Price, 1), "rogue, no stars");
        Eq(150, HireRules.Price(PostSettings.ById("firemage").Price, 2), "fire mage, 1 star");
        Eq(500, HireRules.Price(PostSettings.ById("deepnorth").Price, 3), "Deep North, 2 stars: 400 + 100");
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
        Eq("Ice mage recruiter\n" + KeyUse + "Hire: 150 coins\n[<color=yellow><b>L-Shift + E</b></color>] Rename",
            PostRules.RecruiterHover("ice mage", 150, vanilla), "hire line ours, rename line vanilla's");
        Eq("Rogue recruiter\n" + KeyUse + "Hire: 100 coins", PostRules.RecruiterHover("rogue", 100, null), "no vanilla text");
    }

    private static void Test_Posts_RecruiterE()
    {
        Eq(PostRules.RecruiterE.Hire, PostRules.RecruiterInteract(false, false), "E: hire");
        Eq(PostRules.RecruiterE.Vanilla, PostRules.RecruiterInteract(false, true), "Shift+E: vanilla rename");
        Eq(PostRules.RecruiterE.Nothing, PostRules.RecruiterInteract(true, false), "held E: nothing (never hires every frame)");
        Eq(PostRules.RecruiterE.Nothing, PostRules.RecruiterInteract(true, true), "held Shift+E: nothing, like vanilla");
    }

    private static void Test_Posts_RecruiterLeavesOnlyAfterTenSeconds()
    {
        var watch = new PostWatch();
        False(watch.ShouldLeave(true, 0), "post there");
        False(watch.ShouldLeave(false, 1), "missing: start counting (its data may arrive late)");
        False(watch.ShouldLeave(false, 10.9), "missing 9.9 s");
        True(watch.ShouldLeave(false, 11.1), "missing 10.1 s: leave");

        var back = new PostWatch();
        False(back.ShouldLeave(false, 0), "missing");
        False(back.ShouldLeave(true, 5), "it arrived");
        False(back.ShouldLeave(false, 6), "missing again: counted from 6");
        False(back.ShouldLeave(false, 15.9), "9.9 s");
        True(back.ShouldLeave(false, 16.1), "10.1 s");

        var reset = new PostWatch();
        False(reset.ShouldLeave(false, 0), "missing");
        reset.Reset(); // another game runs the recruiter for a while
        False(reset.ShouldLeave(false, 9), "counted again from 9");
        False(reset.ShouldLeave(false, 18.9), "9.9 s");
        True(reset.ShouldLeave(false, 19.1), "10.1 s");
    }
}
