using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.CampaignSystem.MapNotificationTypes;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors.CommentBehaviors;

public class CommentOnDeclareWarBehavior : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		CampaignEvents.WarDeclared.AddNonSerializedListener(this, OnWarDeclared);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail)
	{
		DeclareWarLogEntry declareWarLogEntry = new DeclareWarLogEntry(faction1, faction2);
		LogEntry.AddLogEntry(declareWarLogEntry);
		if (faction2 == Hero.MainHero.MapFaction || (faction1 == Hero.MainHero.MapFaction && detail != DeclareWarAction.DeclareWarDetail.CausedByKingdomDecision))
		{
			Campaign.Current.CampaignInformationManager.NewMapNoticeAdded(new WarMapNotification(faction1, faction2, declareWarLogEntry.GetEncyclopediaText()));
		}
	}
}
