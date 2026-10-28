namespace TaleWorlds.CampaignSystem.Map.DistanceCache;

public interface ISettlementDataHolder
{
	CampaignVec2 GatePosition { get; }

	CampaignVec2 PortPosition { get; }

	string StringId { get; }

	bool IsFortification { get; }

	bool HasPort { get; }
}
