using System;
using System.Collections.Generic;
using System.Reflection;

internal static class ProactiveSessionOwnerReplay
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

    internal static void Run(Assembly assembly)
    {
        Type hostType = assembly.GetType("AnimusForge.ProactiveNpcRequestBehavior", true);
        Type ownerType = hostType.GetNestedType("ProactiveRequestSessionOwner", BindingFlags.NonPublic);
        if (ownerType == null || hostType.GetField("_sessionOwner", Members)?.FieldType != ownerType
            || hostType.GetField("_activeSession", Members) != null)
            throw new InvalidOperationException("Proactive session: production host must use one session owner");
        Type sessionType = hostType.GetNestedType("ProactiveNpcRequestSession", BindingFlags.NonPublic);
        object owner = Activator.CreateInstance(ownerType, true);
        object Call(string method, params object[] args) => ownerType.GetMethod(method, Members).Invoke(owner, args);
        object Read(object value, string name) => value.GetType().GetProperty(name, Members).GetValue(value);
        void Set(object value, string name, object item) => value.GetType().GetProperty(name, Members).SetValue(value, item);
        void Check(bool value, string label) { if (!value) throw new InvalidOperationException("Proactive session: " + label); }

        object legacy = Activator.CreateInstance(sessionType, true);
        Set(legacy, "HeroId", "hero-a");
        Set(legacy, "PartyId", "party-a");
        Set(legacy, "NeedType", "MoneyShortage");
        Set(legacy, "NeedTypes", new List<string> { "MoneyShortage", "FoodShortage" });
        Set(legacy, "Stage", "Chasing");
        Set(legacy, "ExpiresAtHours", 18f);
        Call("Import", legacy);
        Check(ReferenceEquals(Read(owner, "Current"), legacy), "legacy session remains same saved DTO");
        Check(!string.IsNullOrWhiteSpace((string)Read(legacy, "Id")), "legacy session receives runtime identity");
        Check(((List<string>)Read(legacy, "NeedTypes")).Count == 1, "legacy multi-need session is normalized");
        Check((bool)Call("MatchesHeroId", "HERO-A") && (bool)Call("MatchesPartyId", "PARTY-A"), "identity is case-insensitive");
        Check((bool)Read(owner, "IsChasing"), "loaded chasing stage");
        Check(!(bool)Call("IsExpired", 18f) && (bool)Call("IsExpired", 18.01f), "expiry remains strict greater-than");
        Check((bool)Call("TryReserveEncounterProbe", 1L) && !(bool)Call("TryReserveEncounterProbe", 2L),
            "chasing probe is throttled without a per-tick allocation");
        Check((bool)ownerType.GetMethod("ShouldCancelForBusyReason", Members).Invoke(null, new object[] { "native_activity_context" })
            && !(bool)ownerType.GetMethod("ShouldCancelForBusyReason", Members).Invoke(null, new object[] { "conversation" }),
            "only existing combat activity reasons cancel chasing");
        Check(!(bool)Call("TryStart", Activator.CreateInstance(sessionType, true)), "duplicate start cannot replace active session");
        Call("MarkOpeningMenu", 4f);
        Check((string)Read(legacy, "Stage") == "OpeningMenu" && (float)Read(legacy, "EncounterOpenedAtHours") == 4f, "menu opening is atomic");
        Check(!(bool)Call("TryReserveEncounterProbe", long.MaxValue), "non-chasing stage does not probe");
        Call("ReturnToChasing");
        Check((bool)Read(owner, "IsChasing"), "failed encounter may resume chase");
        Call("MarkEncounterOpened", 5f);
        Check((string)Read(legacy, "Stage") == "Menu" && (float)Read(legacy, "EncounterOpenedAtHours") == 5f, "encounter menu transition");
        Call("MarkConversationOpening", true);
        Check((string)Read(legacy, "Stage") == "NativeConversationPending", "native opening transition");
        Call("MarkConversationOpening", false);
        Check((string)Read(legacy, "Stage") == "SceneConversationPending", "scene opening transition");
        Check((bool)Read(owner, "ShouldRecordFatigue"), "need fatigue starts unrecorded");
        Call("MarkFatigueRecorded");
        Check(!(bool)Read(owner, "ShouldRecordFatigue"), "need fatigue records once");
        Check(ReferenceEquals(Call("Clear"), legacy) && Read(owner, "Current") == null, "cancel returns and clears exact session");
        Check(!(bool)Call("MatchesHeroId", "hero-a") && !(bool)Call("IsExpired", 99f), "cleared state cannot match");
        object fresh = Activator.CreateInstance(sessionType, true);
        Set(fresh, "HeroId", "hero-b");
        Set(fresh, "NeedType", "FoodShortage");
        Check((bool)Call("TryStart", fresh) && ReferenceEquals(Read(owner, "Current"), fresh), "new session can start after clear");
        Check(!(bool)Call("MatchesHeroId", "hero-a"), "old hero does not match replacement");
        Console.WriteLine("PASS proactiveSessionOwnerReplay import=1 legacy=1 identity=1 expiry=1 duplicate=1 stages=1 fatigue=1 clear=1; no live encounter/save acceptance");
    }
}
