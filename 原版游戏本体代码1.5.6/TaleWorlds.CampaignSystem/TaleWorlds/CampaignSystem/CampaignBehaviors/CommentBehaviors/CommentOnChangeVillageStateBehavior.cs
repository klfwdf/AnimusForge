using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors.CommentBehaviors;

public class CommentOnChangeVillageStateBehavior : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		CampaignEvents.VillageStateChanged.AddNonSerializedListener(this, OnVillageStateChanged);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void OnVillageStateChanged(Village village, Village.VillageStates oldState, Village.VillageStates newState, MobileParty raiderParty)
	{
		if (newState != Village.VillageStates.Normal && raiderParty != null && (raiderParty.LeaderHero == Hero.MainHero || village.Owner.Settlement.OwnerClan.Leader == Hero.MainHero || village.Settlement.MapFaction.IsKingdomFaction || raiderParty.MapFaction.IsKingdomFaction))
		{
			LogEntry.AddLogEntry(new VillageStateChangedLogEntry(village, oldState, newState, raiderParty));
		}
	}
}
