using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

internal static class RecruitmentOwnerReplay
{
    private const BindingFlags M = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    internal static void Run(Assembly assembly)
    {
        Type type = assembly.GetType("AnimusForge.RewardSystemBehavior", true);
        object host = RuntimeHelpers.GetUninitializedObject(type);
        void Check(bool ok, string label) { if (!ok) throw new InvalidOperationException("Recruitment: " + label); }
        MethodInfo Method(string name, int count) => type.GetMethods(M).Single(m => m.Name == name && m.GetParameters().Length == count);
        for (int i = 0; i < 2; i++)
        {
            object[] heroArgs = { null, false, null };
            Check(!(bool)Method("TryApplyHeroJoinPlayerPartyForExternal", 3).Invoke(host, heroArgs), "missing hero rejected repeatedly");
            Check(((string)heroArgs[2]).StartsWith("执行失败"), "no hero success status");
            object[] npcArgs = { null, -1, null };
            Check(!(bool)Method("TryApplyNonHeroJoinPlayerPartyForExternal", 3).Invoke(host, npcArgs), "missing nonhero rejected repeatedly");
            Check(((string)npcArgs[2]).StartsWith("执行失败"), "no nonhero success status");
            object[] tagArgs = { null, -1, "[ACTION:JOIN_PLAYER_PARTY]", null, null };
            Check(!(bool)Method("TryApplyNonHeroJoinPlayerPartyTagForExternal", 5).Invoke(host, tagArgs), "missing tag subject rejected");
            Check(((IList)tagArgs[3]).Count == 0 && ((IList)tagArgs[4]).Count == 0, "missing subject produces no fact/notification");
            object[] nativeArgs = { null, -1, null, int.MinValue, null };
            Check(!(bool)Method("DoesNativeConversationRequestStillMatch", 5).Invoke(null, nativeArgs), "missing Native request rejected");
        }
        Type owner = type.GetNestedType("RecruitmentOwner", M);
        Check(owner != null && owner.GetFields(M).Length == 0, "sync coordinator retains no live request state");
        Console.WriteLine("PASS recruitmentOwnerReplay real-public-routing/missing-subject/repeat/no-false-fact/Native-missing; positive game mutations remain source-parity-only, live=NOT_RUN");
    }
}
