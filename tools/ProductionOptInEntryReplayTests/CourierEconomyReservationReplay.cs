using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

internal static class CourierEconomyReservationReplay
{
    private const BindingFlags PrivateStatic = BindingFlags.NonPublic | BindingFlags.Static;
    private const BindingFlags Fields = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    private const string SessionId = "local7e-courier-session";
    private const string RecipientId = "local7e-recipient";
    private static int _assertions;

    public static void Run(Assembly production)
    {
        Require(production != null, "production assembly is required");
        Type campaign = Assembly.Load("TaleWorlds.CampaignSystem")
            .GetType("TaleWorlds.CampaignSystem.Campaign", true);
        Require(campaign.GetProperty("Current").GetValue(null) == null,
            "Courier reservation replay must not run in a live Campaign.");

        Type courier = TypeOf(production, "AnimusForge.CourierDeliveryBehavior");
        Type sessionType = courier.GetNestedType("CourierSession", BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("missing CourierSession");
        MethodInfo eligible = courier.GetMethod("IsCourierActionSessionEligible", PrivateStatic)
            ?? throw new InvalidOperationException("missing IsCourierActionSessionEligible");
        MethodInfo reserve = courier.GetMethod("TryReserveCourierEconomyOnly", PrivateStatic)
            ?? throw new InvalidOperationException("missing TryReserveCourierEconomyOnly");

        object valid = Session(sessionType);
        Require(IsEligible(eligible, valid, Snapshot(production), SessionId, RecipientId),
            "valid active Courier session was rejected");
        Require(!IsEligible(eligible, Session(sessionType), Snapshot(production, "wrong-session"), SessionId, RecipientId),
            "wrong snapshot session was accepted");
        Require(!IsEligible(eligible, Session(sessionType), Snapshot(production), "wrong-session", RecipientId),
            "wrong expected session was accepted");
        Require(!IsEligible(eligible, Session(sessionType), Snapshot(production, channel: "NativeConversation"), SessionId, RecipientId),
            "wrong interaction channel was accepted");
        Require(!IsEligible(eligible, Session(sessionType), Snapshot(production, subject: "wrong-subject"), SessionId, RecipientId),
            "wrong Courier subject was accepted");
        RequireRejected(eligible, production, sessionType, "Direction", "InboundToPlayer", "inbound session");
        RequireRejected(eligible, production, sessionType, "Stage", "Completed", "terminal session");
        object returning = Session(sessionType);
        Set(returning, "Stage", "Returning");
        Require(IsEligible(eligible, returning, Snapshot(production), SessionId, RecipientId),
            "valid Returning recovery session was rejected");
        RequireRejected(eligible, production, sessionType, "DeliveryApplied", false, "undelivered session");
        RequireRejected(eligible, production, sessionType, "PostprocessConsumed", true, "consumed session");

        object reservable = Session(sessionType);
        Set(reservable, "ReplyPostprocessedText", "existing visible reply");
        Require((bool)reserve.Invoke(null, new[] { reservable }), "first economy-only reservation was rejected");
        Require((bool)Get(reservable, "PostprocessConsumed"), "reservation did not persist the consumed flag");
        Require((string)Get(reservable, "ReplyPostprocessedText") == "existing visible reply",
            "reservation derived visible reply from raw postprocess data");
        Require(!IsEligible(eligible, reservable, Snapshot(production), SessionId, RecipientId),
            "reserved session remained eligible");
        Require(!(bool)reserve.Invoke(null, new[] { reservable }), "second reservation was accepted");

        Type json = (AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "Newtonsoft.Json") ?? Assembly.Load("Newtonsoft.Json"))
            .GetType("Newtonsoft.Json.JsonConvert", true);
        string serialized = (string)json.GetMethod("SerializeObject", new[] { typeof(object) })
            .Invoke(null, new[] { reservable });
        object restored = json.GetMethod("DeserializeObject", new[] { typeof(string), typeof(Type) })
            .Invoke(null, new object[] { serialized, sessionType });
        Require(restored != null && (bool)Get(restored, "PostprocessConsumed"),
            "CourierSession JSON round-trip lost PostprocessConsumed");

        Console.WriteLine("PASS courierEconomyReservation fixtureOnly=1 liveCampaign=0 saveWrite=0 assertions=" + _assertions);
    }

    private static object Session(Type type)
    {
        object session = Activator.CreateInstance(type, nonPublic: true);
        Set(session, "Id", SessionId);
        Set(session, "Direction", "Outbound");
        Set(session, "RecipientHeroId", RecipientId);
        Set(session, "Stage", "GeneratingReply");
        Set(session, "DeliveryApplied", true);
        Set(session, "PostprocessConsumed", false);
        return session;
    }

    private static object Snapshot(Assembly production, string session = SessionId,
        string channel = "Courier", string subject = RecipientId)
    {
        Type channelType = TypeOf(production, "AnimusForge.Refactor.Contracts.InteractionChannel");
        object identity = Activator.CreateInstance(TypeOf(production, "AnimusForge.Refactor.Contracts.InteractionIdentity"),
            session, Enum.Parse(channelType, channel), subject);
        object trace = Activator.CreateInstance(TypeOf(production, "AnimusForge.Refactor.Contracts.TraceContext"),
            "local7e-trace", 1L, 1L, "fixture", "1.4");
        Type candidate = TypeOf(production, "AnimusForge.Refactor.Contracts.InteractionCandidate");
        return Activator.CreateInstance(TypeOf(production, "AnimusForge.Refactor.Contracts.GameInteractionSnapshot"),
            identity, trace, "fixture input", "", 0, 0, Array.CreateInstance(candidate, 0),
            Array.Empty<string>(), new Dictionary<string, string>());
    }

    private static void RequireRejected(MethodInfo eligible, Assembly production, Type type,
        string field, object value, string label)
    {
        object session = Session(type);
        Set(session, field, value);
        Require(!IsEligible(eligible, session, Snapshot(production), SessionId, RecipientId), label + " was accepted");
    }

    private static bool IsEligible(MethodInfo method, object session, object snapshot, string expected, string recipient)
        => (bool)method.Invoke(null, new[] { session, snapshot, expected, recipient });

    private static Type TypeOf(Assembly assembly, string name)
        => assembly.GetType(name, false) ?? throw new InvalidOperationException("missing type " + name);

    private static object Get(object target, string name)
        => target.GetType().GetField(name, Fields)?.GetValue(target)
            ?? throw new InvalidOperationException("missing field " + name);

    private static void Set(object target, string name, object value)
        => (target.GetType().GetField(name, Fields) ?? throw new InvalidOperationException("missing field " + name))
            .SetValue(target, value);

    private static void Require(bool condition, string message)
    {
        _assertions++;
        if (!condition) throw new InvalidOperationException(message);
    }
}
