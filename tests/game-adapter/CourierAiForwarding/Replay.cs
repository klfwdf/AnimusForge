using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using AnimusForge;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
#if API14
using TaleWorlds.CampaignSystem.Settlements;
#endif

public static class Replay
{
    private static int checks;
    private static void Check(bool value, string name)
    {
        if (!value) throw new Exception("FAIL " + name);
        checks++;
        Console.WriteLine("PASS " + name);
    }

    private sealed class Inner : DefaultMobilePartyAIModel
    {
        public override float AiCheckInterval { get { return 1.25f; } }
        public override float GetPatrolRadius(MobileParty party, CampaignVec2 point) { return 37f; }
#if API14
        public float PortDays = 0.5f;
        public Settlement SeenSettlement;
        public bool SeenPort;
        public override float FortificationPortPatrolDistanceAsDays { get { return PortDays; } }
        public override float GetSettlementNearbyThreatAndAllyCheckRadius(Settlement settlement, bool isPort)
        {
            SeenSettlement = settlement;
            SeenPort = isPort;
            return isPort ? 73f : 29f;
        }
#endif
    }

    public static int Main()
    {
        try
        {
            // Catch interface growth that would silently inherit the default implementation.
            foreach (MethodInfo member in typeof(MobilePartyAIModel).GetMethods().Where(m => m.IsAbstract))
            {
                Type[] signature = member.GetParameters().Select(p => p.ParameterType).ToArray();
                MethodInfo implementation = typeof(CourierMobilePartyAIModel).GetMethod(member.Name, signature);
                Check(implementation != null && implementation.DeclaringType == typeof(CourierMobilePartyAIModel), "interface member " + member.Name);
            }
            Inner inner = new Inner();
            CourierMobilePartyAIModel wrapper = new CourierMobilePartyAIModel(inner);
            Check(wrapper.AiCheckInterval == 1.25f, "common property forwards");
            Check(wrapper.GetPatrolRadius(null, default(CampaignVec2)) == 37f, "common patrol method forwards");
#if API14
            Check(wrapper.FortificationPortPatrolDistanceAsDays == 0.5f, "DLC port patrol distance survives wrapping");
            inner.PortDays = 0.875f;
            Check(wrapper.FortificationPortPatrolDistanceAsDays == 0.875f, "inner value remains authoritative, without caching or hardcoding");
            Settlement settlement = (Settlement)FormatterServices.GetUninitializedObject(typeof(Settlement));
            Check(wrapper.GetSettlementNearbyThreatAndAllyCheckRadius(settlement, true) == 73f, "port threat radius forwards");
            Check(object.ReferenceEquals(inner.SeenSettlement, settlement) && inner.SeenPort, "port arguments preserve identity");
            Check(wrapper.GetSettlementNearbyThreatAndAllyCheckRadius(settlement, false) == 29f, "land threat radius forwards");
            Check(object.ReferenceEquals(inner.SeenSettlement, settlement) && !inner.SeenPort, "land arguments preserve identity");
            Check(new CourierMobilePartyAIModel(null).FortificationPortPatrolDistanceAsDays == new DefaultMobilePartyAIModel().FortificationPortPatrolDistanceAsDays, "null inner keeps native fallback");
#endif
            Console.WriteLine("PASS total=" + checks + " nativeCampaignStarted=false");
            return 0;
        }
        catch (Exception e) { Console.WriteLine(e); return 1; }
    }
}
