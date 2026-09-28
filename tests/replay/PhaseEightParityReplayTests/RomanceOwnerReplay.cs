using System;
using System.Collections;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

internal static class RomanceOwnerReplay
{
    private const BindingFlags M = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    internal static void Run(Assembly assembly)
    {
        Type type = assembly.GetType("AnimusForge.RomanceRelationshipOwner", true);
        object owner = Activator.CreateInstance(type, true);
        object Call(string name, params object[] args) => type.GetMethod(name, M).Invoke(owner, args);
        void Check(bool ok, string label) { if (!ok) throw new InvalidOperationException("Romance: " + label); }
        Call("SetContext", " speaker ", true);
        int consumed = 0;
        Parallel.For(0, 32, _ => { if ((bool)Call("ConsumeContext", "SPEAKER")) Interlocked.Increment(ref consumed); });
        Check(consumed == 1, "concurrent topic consumed once");
        Call("SetContext", "speaker", true);
        Call("SetContext", "speaker", false);
        Check(!(bool)Call("ConsumeContext", "speaker"), "disabled topic unavailable");
        Call("SetContext", "speaker", true);
        Call("ClearContext");
        Check(!(bool)Call("ConsumeContext", "speaker"), "load clears ephemeral topic");
        Call("SetContext", " ", true);
        Check(!(bool)Call("ConsumeContext", " "), "missing speaker rejected");
        Check((int)Call("SetLove", "hero:test", 101) == 100, "positive saturation");
        Check((int)Call("GetLove", "HERO:TEST") == 100, "case insensitive love");
        Check((int)Call("SetLove", "hero:test", -101) == -100, "negative saturation");
        Call("SetLove", "hero:test", 0);
        var values = (IDictionary)type.GetField("PrivateLove", M).GetValue(owner);
        Check(values.Count == 0, "zero removed");
        values["hero:high"] = 1000; values["hero:zero"] = 0; values[" "] = 3;
        Call("NormalizeLove");
        Check(values.Count == 1 && (int)values["hero:high"] == 100, "saved love normalization");
        Check((int)Call("ToLoveLevelIndex", -100) == 1 && (int)Call("ToLoveLevelIndex", 100) == 10, "love levels endpoints");
        Check(!(bool)Call("IsCandidateAge", 17.9f, 55, true), "authority does not bypass minimum age");
        Check((bool)Call("IsCandidateAge", 18f, 55, false), "minimum age inclusive");
        Check((bool)Call("IsCandidateAge", 55f, 55, false), "maximum inclusive");
        Check(!(bool)Call("IsCandidateAge", 56f, 55, false), "ordinary maximum");
        Check((bool)Call("IsCandidateAge", 56f, 55, true), "authority maximum exception");
        Check((bool)Call("IsAgeGapAllowed", 20f, 45f, 25), "gap inclusive");
        Check(!(bool)Call("IsAgeGapAllowed", 20f, 45.1f, 25), "gap rejected");
        string State(bool player, bool target, bool pair, bool clan, bool leader, bool elope, int tier, int relation, int trust, int threshold)
            => (string)Call("GetMarriageRuntimeConstraintState", player, target, pair, clan, leader, elope, tier, relation, trust, threshold);
        Check(State(false,true,true,true,true,true,0,99,99,20)=="no_player_hero", "missing player");
        Check(State(true,false,true,true,true,true,0,99,99,20)=="no_target", "missing target");
        Check(State(true,true,false,true,true,true,0,99,99,20)=="unavailable", "pair gate precedes status");
        Check(State(true,true,true,false,false,true,0,0,0,20)=="clanless_elope_ready", "clanless path");
        Check(State(true,true,true,true,false,false,0,0,0,20)=="member_redirect_elope_blocked", "member path");
        Check(State(true,true,true,true,true,true,-3,99,99,20)=="leader_blocked_tier_gap", "tier gap");
        Check(State(true,true,true,true,true,false,-2,0,0,20)=="leader_need_heavy_brideprice", "heavy price path");
        Check(State(true,true,true,true,true,false,-1,20,19,20)=="leader_need_brideprice_blocked", "both relation and trust required");
        Check(State(true,true,true,true,true,false,-1,20,20,50)=="leader_need_brideprice_ready", "minor price fixed threshold");
        Check(State(true,true,true,true,true,false,2,20,20,20)=="leader_offer_brideprice_major", "major offer threshold");
        Type hostType = assembly.GetType("AnimusForge.RomanceSystemBehavior", true);
        object oldHost = Activator.CreateInstance(hostType);
        object oldOwner = hostType.GetField("_relationshipOwner", M).GetValue(oldHost);
        type.GetMethod("SetContext", M).Invoke(oldOwner, new object[]{"speaker", true});
        object newHost = Activator.CreateInstance(hostType);
        object newOwner = hostType.GetField("_relationshipOwner", M).GetValue(newHost);
        Check(!(bool)type.GetMethod("ConsumeContext", M).Invoke(newOwner, new object[]{"speaker"}), "replacement owner has no old topic authority");
        Console.WriteLine("PASS romanceOwnerReplay atomicContext=1 reset=1 replacement=1 love=1 age=1 eligibilityStates=1; no live marriage/clan/trust/save acceptance");
    }
}
