using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class RaidModel : MBGameModel<RaidModel>
{
	public abstract int GoldRewardForEachLostHearth { get; }

	public abstract MBReadOnlyList<(ItemObject, float)> GetCommonLootItemScores();

	public abstract ExplainedNumber CalculateHitDamage(MapEventSide attackerSide, float settlementHitPoints);

	public abstract ExplainedNumber GetRaidLootMultiplier(PartyBase receivingParty);
}
