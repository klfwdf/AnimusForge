using System;
using System.Collections;
using System.Reflection;

internal static class NotorietyOwnerReplay
{
    private const BindingFlags M = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    internal static void Run(Assembly assembly)
    {
        Type hostType = assembly.GetType("AnimusForge.PlayerNotorietyBehavior", true);
        object host = Activator.CreateInstance(hostType);
        object Get(object target, string name) => target.GetType().GetField(name, M)?.GetValue(target)
            ?? target.GetType().GetProperty(name, M).GetValue(target);
        void Set(object target, string name, object value) => target.GetType().GetField(name, M).SetValue(target, value);
        object Call(object target, string method, params object[] args)
        {
            foreach (MethodInfo candidate in target.GetType().GetMethods(M))
            {
                if (candidate.Name != method || candidate.GetParameters().Length != args.Length) continue;
                ParameterInfo[] ps = candidate.GetParameters();
                bool match = true;
                for (int i = 0; i < args.Length; i++)
                    if (args[i] != null && !ps[i].ParameterType.IsInstanceOfType(args[i])) match = false;
                if (match) return candidate.Invoke(target, args);
            }
            throw new MissingMethodException(method);
        }
        void Check(bool ok, string label) { if (!ok) throw new InvalidOperationException("Notoriety: " + label); }
        object owner = Get(host, "_observationOwner");
        object state = Get(host, "_state");
        Check(ReferenceEquals(state, Get(owner, "State")), "single persisted state");
        Check(ReferenceEquals(Get(host, "_activeConversationStates"), Get(owner, "Active")), "single session cache");
        Check(Call(owner, "GetKnowledge", "__player__", true) == null, "player not observer");
        Check(Call(owner, "GetKnowledge", " ", true) == null, "blank rejected");
        Check(Call(owner, "GetKnowledge", "unknown", false) == null, "read does not create");
        object npc = Call(host, "GetNpcKnowledgeState", " TEST ", true);
        Check(ReferenceEquals(npc, Call(host, "GetNpcKnowledgeState", "test", true)), "stable normalized identity");
        Set(npc, "LastCourierSentDistance", -2f);
        Call(owner, "GetKnowledge", "test", true);
        Check((float)Get(npc, "LastCourierSentDistance") == -1f, "distance sentinel repair");
        Check(!(bool)Call(owner, "CanKnowRecent", npc, true, 5f), "unknown distance rejected");
        Set(npc, "LastCourierSentDistance", 5f);
        Check((bool)Call(owner, "CanKnowRecent", npc, true, 5f), "courier distance inclusive");
        Check(!(bool)Call(owner, "CanKnowRecent", npc, true, 4.99f), "courier beyond threshold");
        Check(!(bool)Call(owner, "CanKnowRecent", npc, false, 100f), "face to face needs completed session");
        Call(host, "AddCultureNotoriety", " EMPIRE ", 30.0, "fixture");
        Check(Math.Abs((double)Get(state, "WorldNotoriety") - 10.0 / 3.0) < 1e-9, "clamped culture third world share");
        Check((int)Call(host, "GetCultureNotoriety", "empire") == 10, "real propagation clamps ten");
        Call(host, "AddCultureNotoriety", "empire", -1.0, "fixture");
        Check((int)Call(host, "GetCultureNotoriety", "empire") == 10, "negative propagation ignored");
        Set(npc, "PersonalKnownBonus", 95.0);
        Check((int)Call(owner, "EffectiveNotoriety", "empire", 10.0, npc) == 100, "effective total saturation");
        object active = Call(owner, "BeginConversation", "test", 33, false, 10, 240);
        Check(ReferenceEquals(active, Call(owner, "BeginConversation", "TEST", 100, true, 11, 260)), "repeat keeps frozen roll");
        Check(!(bool)Get(active, "KnowsMajorThisSession"), "repeat cannot upgrade failed roll");
        Set(active, "LineCount", 3);
        Set(active, "HasLegacyLines", true);
        Call(host, "FinalizeConversationByHeroId", "test");
        Call(host, "FinalizeConversationByHeroId", "test");
        Check((int)Get(npc, "CompletedConversationSessions") == 1, "host repeated finalize once");
        Check((double)Get(npc, "PersonalKnownBonus") == 100.0, "three per line saturated");
        Check((bool)Call(owner, "CanKnowRecent", npc, false, 0f), "completed face to face unlock");
        Call(owner, "BeginConversation", "test", 33, false, 10, 240);
        Call(host, "FinalizeConversationByHeroId", "test");
        Check((int)Get(npc, "CompletedConversationSessions") == 1, "prompt only is not conversation");
        Call(owner, "MarkKnown", npc, 12);
        Call(owner, "MarkKnown", npc, 18);
        Check((int)Get(npc, "KnownAtDay") == 12, "first recognition day retained");
        Call(owner, "BeginConversation", "test", 100, true, 10, 240);
        Call(host, "SetLowProfileModeEnabled", true);
        Check((bool)Get(Get(host, "_state"), "LowProfileModeEnabled"), "host low profile state");
        Check(((IDictionary)Get(owner, "Active")).Count == 0, "low profile abandons active sessions");
        Check(!(bool)Call(owner, "SetLowProfile", true), "same toggle idempotent");
        object loaded = Activator.CreateInstance(state.GetType(), true);
        hostType.GetProperty("_state", M).SetValue(host, loaded);
        Check(ReferenceEquals(Get(owner, "State"), loaded), "load replaces sole state graph");
        Check(Call(owner, "GetKnowledge", "test", false) == null, "old knowledge not leaked after load");
        Console.WriteLine("PASS notorietyOwnerReplay singleState=1 normalizedIdentity=1 propagation=1 courierBoundary=1 frozenRoll=1 legacyOnce=1 lowProfile=1 load=1; no live qualification/RNG/save acceptance");
    }
}
