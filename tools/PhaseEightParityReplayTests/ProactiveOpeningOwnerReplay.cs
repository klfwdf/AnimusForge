using System;
using System.Reflection;

internal static class ProactiveOpeningOwnerReplay
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    internal static void Run(Assembly assembly)
    {
        Type hostType = assembly.GetType("AnimusForge.ProactiveNpcRequestBehavior", true);
        Type type = hostType.GetNestedType("ProactiveOpeningOwner", BindingFlags.NonPublic);
        if (type == null || hostType.GetField("_openingOwner", Members)?.FieldType != type)
            throw new InvalidOperationException("Proactive opening: production host does not own opening state");
        object owner = Activator.CreateInstance(type, true);
        object Call(string name, params object[] args) => type.GetMethod(name, Members).Invoke(owner, args);
        void Check(bool ok, string label) { if (!ok) throw new InvalidOperationException("Proactive opening: " + label); }
        void Open(bool native, string session, string hero, string fact)
            => Call("Open", native, session, hero, fact, "prompt", 1f);
        bool Consume(bool native, string session, string hero, out string fact)
        {
            object[] args = { native, session, hero, null, null };
            bool result = (bool)Call("TryConsume", args);
            fact = (string)args[3];
            return result;
        }
        Open(true, "session-a", "hero-a", "fact-a");
        Check(!(bool)Call("Matches", true, "session-b", "hero-a"), "new session cannot match stale opening");
        Check(!Consume(true, "session-b", "hero-a", out _), "new session cannot consume stale opening");
        Check(!Consume(true, "session-a", "hero-b", out _), "wrong hero rejected");
        Check(Consume(true, "session-a", "HERO-A", out string fact) && fact == "fact-a", "same session consumes once");
        Check(!Consume(true, "session-a", "hero-a", out _), "duplicate consumption rejected");
        Open(false, "session-a", "hero-a", "scene-fact");
        Check(!Consume(true, "session-a", "hero-a", out _), "native cannot consume scene opening");
        Open(true, "session-a", "hero-a", "native-replacement");
        Check(!Consume(false, "session-a", "hero-a", out _), "switching channel retires old opening");
        Open(false, "session-a", "hero-a", "scene-fact");
        object[] peek = { false, "session-a", null, null, null };
        Check((bool)Call("TryPeek", peek) && (string)peek[2] == "hero-a" && (string)peek[3] == "scene-fact", "scene peek preserves fact");
        Call("Clear");
        Check(!Consume(false, "session-a", "hero-a", out _), "cancel or load clears pending opening");
        Open(true, "session-old", "hero-a", "old");
        Open(true, "session-new", "hero-a", "new");
        Check(!Consume(true, "session-old", "hero-a", out _), "replacement rejects old session");
        Check(Consume(true, "session-new", "hero-a", out fact) && fact == "new", "replacement remains consumable");
        object host = Activator.CreateInstance(hostType);
        object hostOwner = hostType.GetField("_openingOwner", Members).GetValue(host);
        type.GetMethod("Open", Members).Invoke(hostOwner, new object[] { true, "host-session", "hero-a", "fact", "prompt", 1f });
        hostType.GetMethod("CancelActiveSession", Members).Invoke(host, new object[] { "replay", false });
        Check(!(bool)type.GetMethod("Matches", Members).Invoke(hostOwner, new object[] { true, "host-session", "hero-a" }), "production cancel clears pending opening");
        Type sessionType = hostType.GetNestedType("ProactiveNpcRequestSession", BindingFlags.NonPublic);
        object legacySession = Activator.CreateInstance(sessionType, true);
        object sessionOwner = hostType.GetField("_sessionOwner", Members).GetValue(host);
        sessionOwner.GetType().GetMethod("Import", Members).Invoke(sessionOwner, new[] { legacySession });
        Check(!string.IsNullOrWhiteSpace((string)sessionType.GetProperty("Id", Members).GetValue(legacySession)), "legacy active session receives identity");
        Console.WriteLine("PASS proactiveOpeningOwnerReplay session=1 hero=1 singleConsume=1 channel=1 clear=1 replacement=1 hostCancel=1 legacyId=1; no live encounter/courier acceptance");
    }
}
