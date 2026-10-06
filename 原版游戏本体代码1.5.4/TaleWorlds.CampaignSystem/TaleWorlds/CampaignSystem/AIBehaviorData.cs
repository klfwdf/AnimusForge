using System;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Party;

namespace TaleWorlds.CampaignSystem;

public struct AIBehaviorData : IEquatable<AIBehaviorData>
{
	public static readonly AIBehaviorData Invalid = new AIBehaviorData(null, AiBehavior.None, MobileParty.NavigationType.None, willGatherArmy: false, isFromPort: false, isTargetingPort: false);

	public IMapPoint Party;

	public CampaignVec2 Position;

	public AiBehavior AiBehavior;

	public bool WillGatherArmy;

	public bool IsFromPort;

	public bool IsTargetingPort;

	public MobileParty.NavigationType NavigationType;

	public AIBehaviorData(IMapPoint party, AiBehavior aiBehavior, MobileParty.NavigationType navigationType, bool willGatherArmy, bool isFromPort, bool isTargetingPort)
	{
		Party = party;
		AiBehavior = aiBehavior;
		NavigationType = navigationType;
		WillGatherArmy = willGatherArmy;
		IsFromPort = isFromPort;
		IsTargetingPort = isTargetingPort;
		Position = CampaignVec2.Zero;
	}

	public AIBehaviorData(CampaignVec2 position, AiBehavior aiBehavior, MobileParty.NavigationType navigationType, bool willGatherArmy, bool isFromPort, bool isTargetingPort)
	{
		Position = position;
		Party = null;
		AiBehavior = aiBehavior;
		NavigationType = navigationType;
		WillGatherArmy = willGatherArmy;
		IsFromPort = isFromPort;
		IsTargetingPort = isTargetingPort;
	}

	public override bool Equals(object obj)
	{
		if (!(obj is AIBehaviorData))
		{
			return false;
		}
		return (AIBehaviorData)obj == this;
	}

	public bool Equals(AIBehaviorData other)
	{
		return other == this;
	}

	public override int GetHashCode()
	{
		int aiBehavior = (int)AiBehavior;
		int hashCode = aiBehavior.GetHashCode();
		hashCode = ((Party != null) ? ((hashCode * 397) ^ Party.GetHashCode()) : hashCode);
		hashCode = (hashCode * 397) ^ WillGatherArmy.GetHashCode();
		hashCode = (hashCode * 397) ^ IsTargetingPort.GetHashCode();
		hashCode = (hashCode * 397) ^ IsFromPort.GetHashCode();
		hashCode = (hashCode * 397) ^ NavigationType.GetHashCode();
		return (hashCode * 397) ^ Position.GetHashCode();
	}

	public static bool operator ==(AIBehaviorData a, AIBehaviorData b)
	{
		if (a.Party == b.Party && a.AiBehavior == b.AiBehavior && a.NavigationType == b.NavigationType && a.WillGatherArmy == b.WillGatherArmy && a.IsFromPort == b.IsFromPort && a.IsTargetingPort == b.IsTargetingPort)
		{
			return a.Position == b.Position;
		}
		return false;
	}

	public static bool operator !=(AIBehaviorData a, AIBehaviorData b)
	{
		return !(a == b);
	}
}
