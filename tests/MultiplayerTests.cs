using System;
using System.Collections.Generic;
using DvergrForHire;

internal static partial class Tests
{
    private const long Me = 111, Friend = 222, Other = 333;
    private static readonly long Second = TimeSpan.TicksPerSecond;
    private static readonly long Now = 100_000 * TimeSpan.TicksPerMinute;

    // ---- name copy (as SealsAtHome) ----

    private static void Test_NameCopy_WritesOnlyChanges()
    {
        True(NameCopy.ShouldWrite("Brokkr", ""), "new name");
        Eq("Brokkr", NameCopy.Target("Brokkr"), "copied as is");
        False(NameCopy.ShouldWrite("Brokkr", "Brokkr"), "already shown: no ZDO write");
        False(NameCopy.ShouldWrite("", ""), "no name: nothing to do");
        False(NameCopy.ShouldWrite(null, null), "missing values");
    }

    private static void Test_NameCopy_ClearedOrBlankNamesClearTheField()
    {
        True(NameCopy.ShouldWrite("", "Brokkr"), "name cleared: clear the field");
        True(NameCopy.ShouldWrite("  ", "Brokkr"), "blank name: clear the field");
        Eq("", NameCopy.Target("  "), "blank counts as none");
    }

    private static void Test_NameCopy_StripsFormattingTagsLikeVanilla()
    {
        Eq("Brokkr", NameCopy.Target("<b>Brokkr</b>"), "shown as players with the mod see it");
        Eq("", NameCopy.Target("<size=99></size>"), "tags only: no name");
    }

    // ---- heartbeat and takeover (ported from SealsAtHome 1.0.2) ----

    private static bool ClaimsBetween(TakeoverWatch watch, IDictionary<long, double> noMod, double from, double to, Func<double, (long owner, long beat)> dvergr)
    {
        for (var t = from; t <= to; t += 2)
        {
            var (owner, beat) = dvergr(t);
            if (watch.Decide(false, owner, beat, t, noMod) != TakeoverWatch.Step.Wait) return true;
        }
        return false;
    }

    private static void Test_Takeover_OwnerBeatsEvery10Seconds()
    {
        True(Takeover.ShouldBeat(0, Now), "first beat");
        False(Takeover.ShouldBeat(Now - 9 * Second, Now), "not yet");
        True(Takeover.ShouldBeat(Now - 10 * Second, Now), "10 s since the last beat");
        True(Takeover.ShouldBeat(Now + 60 * Second, Now), "the world clock went back: re-stamp, never stuck");
    }

    private static void Test_Takeover_NeverFromAGameThatStamps()
    {
        var noMod = new Dictionary<long, double>();
        False(ClaimsBetween(new TakeoverWatch(), noMod, 0, 120, t => (Friend, 1 + (long)(t / 11))), "a modded runner stamps every 10-12 s: never taken");
        Eq(0, noMod.Count, "nobody mistaken for a game without the mod");
    }

    private static void Test_Takeover_NeverDuringASleepFastForward()
    {
        False(ClaimsBetween(new TakeoverWatch(), new Dictionary<long, double>(), 0, 60, t => (Friend, 1000 + (long)t)), "stamps every tick: never taken");
    }

    private static void Test_Takeover_FromAGameThatNeverStampsAfter30Seconds()
    {
        var noMod = new Dictionary<long, double>();
        var watch = new TakeoverWatch();
        Eq(TakeoverWatch.Step.Wait, watch.Decide(false, Friend, 5, 100, noMod), "first sight");
        Eq(TakeoverWatch.Step.Wait, watch.Decide(false, Friend, 5, 129.9, noMod), "unchanged for 29.9 s");
        Eq(TakeoverWatch.Step.FoundNoMod, watch.Decide(false, Friend, 5, 130.1, noMod), "unchanged for 30.1 s: their game doesn't run the mod");
        True(noMod.ContainsKey(Friend), "remembered");
    }

    private static void Test_Takeover_RemembersAGameWithoutTheMod()
    {
        var noMod = new Dictionary<long, double> { [Friend] = 100 };
        Eq(TakeoverWatch.Step.Claim, new TakeoverWatch().Decide(false, Friend, 5, 100, noMod), "another Dvergr they run: taken at once");
        var watch = new TakeoverWatch();
        Eq(TakeoverWatch.Step.Claim, watch.Decide(false, Friend, 0, 100, noMod), "taken at once");
        Eq(TakeoverWatch.Step.Claim, watch.Decide(false, Friend, 77, 104, noMod), "undone with our own stamp left on it: still retried");
    }

    private static void Test_Takeover_ANewRunnerGetsTheFull30Seconds()
    {
        var noMod = new Dictionary<long, double>();
        var watch = new TakeoverWatch();
        False(ClaimsBetween(watch, noMod, 0, 24, t => (Friend, 5)), "a game without the mod, watched 24 s");
        False(ClaimsBetween(watch, noMod, 26, 80, t => (Other, t < 34 ? 5 : 6 + (long)((t - 34) / 10))), "a new runner that stamps is never taken");
        False(noMod.ContainsKey(Other), "not mistaken for a game without the mod");
    }

    private static void Test_Takeover_NotWhenUnownedOrOurs()
    {
        var noMod = new Dictionary<long, double> { [Friend] = 100 };
        Eq(TakeoverWatch.Step.Wait, new TakeoverWatch().Decide(false, 0, 0, 100, noMod), "no owner yet: vanilla hands it out");
        Eq(TakeoverWatch.Step.Wait, new TakeoverWatch().Decide(true, Me, 0, 100, noMod), "already ours");
    }

    private static void Test_Takeover_ForgetsAfterFiveMinutes()
    {
        var noMod = new Dictionary<long, double> { [Friend] = 100 };
        var watch = new TakeoverWatch();
        Eq(TakeoverWatch.Step.Claim, watch.Decide(false, Friend, 5, 399.9, noMod), "found 4:59.9 ago: still taken at once");
        Eq(TakeoverWatch.Step.Wait, watch.Decide(false, Friend, 5, 400.1, noMod), "found over 5 min ago: watched again");
        False(noMod.ContainsKey(Friend), "forgotten");
    }

    // ---- pending "follow me" after a hire ----

    private const string Hirer = "Astrid";

    private static void Test_Follow_DoneOnceItFollowsTheHirer()
    {
        Eq(PendingFollow.Step.Done, new PendingFollow(Hirer, Me, 0).Decide(Hirer, Me, 0, true, 1), "the first Command arrived");
        Eq(PendingFollow.Step.Done, new PendingFollow(Hirer, Friend, 0).Decide(Hirer, Friend, 0, false, 50), "even late");
    }

    private static void Test_Follow_NeverResendsToTheSameOwner()
    {
        // A modded owner got the first Command, but its "follow" update is slow: a re-send would toggle the Dvergr to stay.
        var follow = new PendingFollow(Hirer, Friend, 0);
        for (var t = 1; t <= 100; t++)
            Eq(PendingFollow.Step.Wait, follow.Decide("", Friend, t, false, t), $"t={t}");
    }

    private static void Test_Follow_ResendsToANewModdedOwnerOnce()
    {
        var follow = new PendingFollow(Hirer, Friend, 0); // Friend has no mod: the Command was lost
        Eq(PendingFollow.Step.Wait, follow.Decide("", Other, 5, false, 30), "a new owner: is it modded?");
        Eq(PendingFollow.Step.Wait, follow.Decide("", Other, 5, false, 31), "no heartbeat yet");
        Eq(PendingFollow.Step.Send, follow.Decide("", Other, 6, false, 32), "its heartbeat moved: it has the mod");
        Eq(PendingFollow.Step.Wait, follow.Decide("", Other, 7, false, 33), "sent once to that owner");
        Eq(PendingFollow.Step.Done, follow.Decide(Hirer, Other, 7, false, 34), "following");
    }

    private static void Test_Follow_ResendsAtOnceWhenWeTakeIt()
    {
        var follow = new PendingFollow(Hirer, Friend, 0);
        Eq(PendingFollow.Step.Send, follow.Decide("", Me, 0, true, 31), "our game took control: send at once");
        Eq(PendingFollow.Step.Wait, follow.Decide("", Me, 0, true, 32), "not twice");
    }

    private static void Test_Follow_NotToAGameThatNeverStamps()
    {
        var follow = new PendingFollow(Hirer, Friend, 0);
        for (var t = 1; t <= 100; t++)
            Eq(PendingFollow.Step.Wait, follow.Decide("", Other, 5, false, t), $"another game without the mod, t={t}");
    }

    private static void Test_Follow_WaitsWithoutAnOwner()
    {
        Eq(PendingFollow.Step.Wait, new PendingFollow(Hirer, Friend, 0).Decide("", 0, 0, false, 10), "no owner for a moment");
    }

    private static void Test_Follow_GivesUpAfterTwoMinutes()
    {
        var follow = new PendingFollow(Hirer, Friend, 0);
        Eq(PendingFollow.Step.Wait, follow.Decide("", Friend, 5, false, 119.9), "still trying");
        Eq(PendingFollow.Step.GiveUp, follow.Decide("", Friend, 5, false, 120.1), "gave up (press E yourself)");
    }
}
