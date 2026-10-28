using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.CampaignSystem.MapNotificationTypes;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors.CommentBehaviors;

public class CommentOnMakePeaceBehavior : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		CampaignEvents.MakePeace.AddNonSerializedListener(this, OnMakePeace);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void OnMakePeace(IFaction faction1, IFaction faction2, MakePeaceAction.MakePeaceDetail detail)
	{
		MakePeaceLogEntry makePeaceLogEntry = new MakePeaceLogEntry(faction1, faction2);
		LogEntry.AddLogEntry(makePeaceLogEntry);
		if (faction2 == Hero.MainHero.MapFaction || (faction1 == Hero.MainHero.MapFaction && detail != MakePeaceAction.MakePeaceDetail.ByKingdomDecision))
		{
			Campaign.Current.CampaignInformationManager.NewMapNoticeAdded(new PeaceMapNotification(faction1, faction2, makePeaceLogEntry.GetEncyclopediaText()));
		}
	}
}
