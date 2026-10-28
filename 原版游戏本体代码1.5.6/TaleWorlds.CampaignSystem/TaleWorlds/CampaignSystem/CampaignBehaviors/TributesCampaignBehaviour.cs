using TaleWorlds.CampaignSystem.MapNotificationTypes;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class TributesCampaignBehaviour : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		CampaignEvents.OnClanEarnedGoldFromTributeEvent.AddNonSerializedListener(this, OnClanEarnedGoldFromTribute);
	}

	private static void OnClanEarnedGoldFromTribute(Clan clan, IFaction payerFaction)
	{
		StanceLink stanceWith = clan.MapFaction.GetStanceWith(payerFaction);
		if ((clan == Clan.PlayerClan || payerFaction == Clan.PlayerClan.MapFaction) && stanceWith.GetRemainingTributePaymentCount() == 0)
		{
			bool num = payerFaction == Clan.PlayerClan.MapFaction;
			TextObject textObject = (num ? new TextObject("{=LJFXfmpn}The tribute your kingdom owed to {ENEMY_FACTION} is now complete.") : new TextObject("{=aod7KVc8}The tribute {ENEMY_FACTION} owed to your kingdom is now complete."));
			IFaction faction = (num ? clan.MapFaction : payerFaction);
			textObject.SetTextVariable("ENEMY_FACTION", faction.Name);
			Campaign.Current.CampaignInformationManager.NewMapNoticeAdded(new TributeFinishedMapNotification(textObject, faction));
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
	}
}
