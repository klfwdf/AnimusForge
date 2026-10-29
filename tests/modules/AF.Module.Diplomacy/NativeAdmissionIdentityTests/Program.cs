using System;
using System.Reflection;
using System.Runtime.Serialization;
using AnimusForge;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.ObjectSystem;
using TaleWorlds.Library;

internal static class Program
{
    private static int checks;
    private static T Empty<T>() => (T)FormatterServices.GetUninitializedObject(typeof(T));
    private static void Set(object instance, string field, object value) => instance.GetType()
        .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(instance, value);
    private static void Check(bool value, string name)
    { if (!value) throw new Exception("FAIL " + name); checks++; }
    private static Campaign CampaignWith(Kingdom country, Hero alive, Hero dead)
    {
        var manager = new CampaignObjectManager();
        Set(manager, "_aliveHeroes", new MBList<Hero> { alive });
        Set(manager, "_deadOrDisabledHeroes", new MBList<Hero> { dead });
        Set(manager, "_kingdoms", new MBList<Kingdom> { country });
        typeof(CampaignObjectManager).GetMethod("InitializeManagerObjectLists", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(manager, null);
        var campaign = Empty<Campaign>();
        Set(campaign, "<CampaignObjectManager>k__BackingField", manager);
        typeof(Campaign).GetField("<Current>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, campaign);
        return campaign;
    }
    private static int Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        try { Run(); Console.WriteLine("PASS " + checks + " real native admission/registry checks; synthetic campaign, no live game"); return 0; }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
    private static void Run()
    {
        var objects = MBObjectManager.Init();
        objects.RegisterType<Hero>("Hero", "Heroes", 1);
        objects.RegisterType<Kingdom>("Kingdom", "Kingdoms", 2);
        objects.RegisterType<Settlement>("Settlement", "Settlements", 3);
        var country = Empty<Kingdom>(); country.StringId = "test_country";
        var clan = Empty<Clan>(); Set(clan, "_kingdom", country);
        var alive = Empty<Hero>(); alive.StringId = "lord_5_6"; Set(alive, "_clan", clan);
        Set(alive, "_heroState", Hero.CharacterStates.Active);
        var dead = Empty<Hero>(); dead.StringId = "dead_hero"; Set(dead, "_clan", clan);
        Set(dead, "_heroState", Hero.CharacterStates.Dead);
        CampaignWith(country, alive, dead);
        var host = WorldDiplomacyBehavior.CreateAdmissionHost();
        var reader = (IDiplomacyAdmissionIdentityHost)host;
        Check(objects.GetObject<Hero>(alive.StringId) == null, "ordinary object store does not contain campaign hero");
        Check(reader.ReadAdmissionIdentity("hero", alive.StringId, country.StringId).State == DiplomacyAdmissionIdentityState.Valid,
            "registered live hero");
        Check(reader.ReadAdmissionIdentity("kingdom", country.StringId, null).State == DiplomacyAdmissionIdentityState.Valid,
            "registered kingdom");
        Check(host.AdmissionSubjectCountry("hero", alive.StringId) == country.StringId, "actual subject-country consumer");
        Check(host.CaptureCountryAdmission(new[] { country.StringId }, new[] { alive.StringId }, new[] { country.StringId })
            .Heroes[alive.StringId] == country.StringId, "actual capture consumer");
        Check(reader.ReadAdmissionIdentity("hero", dead.StringId, country.StringId).Reason == "hero_dead", "dead registry confirms loss");
        Check(reader.ReadAdmissionIdentity("hero", "missing", country.StringId).State == DiplomacyAdmissionIdentityState.Unknown,
            "absent hero remains unknown");
        Check(reader.ReadAdmissionIdentity("kingdom", "missing", null).State == DiplomacyAdmissionIdentityState.Unknown,
            "absent kingdom remains unknown");
        Check(reader.ReadAdmissionIdentity("hero", alive.StringId, "previous_country").State == DiplomacyAdmissionIdentityState.ConfirmedInvalidated,
            "changed allegiance confirms mismatch");
        Set(alive, "_clan", null);
        Check(reader.ReadAdmissionIdentity("hero", alive.StringId, country.StringId).State == DiplomacyAdmissionIdentityState.Unknown,
            "missing affiliation is not confirmed loss");
        Set(alive, "_clan", clan);
        alive.StringId = "CharacterObject_3308";
        Check(reader.ReadAdmissionIdentity("hero", alive.StringId, country.StringId).State == DiplomacyAdmissionIdentityState.Valid,
            "dynamic hero id uses actual registry");
        var settlement = Empty<Settlement>(); settlement.StringId = "test_town";
        objects.RegisterObject(settlement);
        Check(Campaign.Current.CampaignObjectManager.Find<Settlement>(settlement.StringId) == null, "campaign registry is not settlement owner");
        Check(ReferenceEquals(DiplomacyIdentityResolver.Settlement(settlement.StringId), settlement), "native settlement owner");
        var replacement = Empty<Hero>(); replacement.StringId = alive.StringId; Set(replacement, "_clan", clan);
        Set(replacement, "_heroState", Hero.CharacterStates.Active);
        CampaignWith(country, replacement, dead);
        Check(ReferenceEquals(DiplomacyIdentityResolver.Hero(alive.StringId), replacement), "campaign replacement does not reuse stale target");
        typeof(Campaign).GetField("<Current>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, null);
        Check(reader.ReadAdmissionIdentity("hero", alive.StringId, country.StringId).State == DiplomacyAdmissionIdentityState.Unknown,
            "campaign unavailable remains unknown");
        Check(DiplomacyIdentityResolver.Settlement(settlement.StringId) == null, "no cross-session settlement");
        Console.WriteLine("NATIVE " + typeof(Campaign).Assembly.FullName);
    }
}

namespace AnimusForge
{
    // Only unrelated host infrastructure is substituted; the entire production
    // country-admission partial is compiled and executed without method rewrites.
    public sealed partial class WorldDiplomacyBehavior
    {
        internal static IDiplomacyCountryAdmissionHost CreateAdmissionHost() => new OrchestrationHost();
        private static Kingdom ResolveKingdom(string id) => DiplomacyIdentityResolver.Kingdom(id);
        private sealed partial class OrchestrationHost
        { private readonly WorldDiplomacyBehavior _owner = new WorldDiplomacyBehavior(); }
        private Border GetKingdomBorderRelation(Kingdom a, Kingdom b) => throw new InvalidOperationException("unrelated border path not exercised");
        private sealed class Border { public bool SharesBorder; }
    }
}
