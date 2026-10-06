using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class DesertionCampaignBehavior : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		CampaignEvents.DailyTickPartyEvent.AddNonSerializedListener(this, DailyTickParty);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void DailyTickParty(MobileParty mobileParty)
	{
		if (mobileParty.IsActive && !mobileParty.IsCurrentlyAtSea && !mobileParty.IsDisbanding && mobileParty.Party.MapEvent == null && (mobileParty.IsLordParty || mobileParty.IsCaravan || mobileParty.IsGarrison) && mobileParty.MemberRoster.TotalRegulars > 0)
		{
			CheckDesertionForParty(mobileParty);
		}
	}

	private static void CheckDesertionForParty(MobileParty mobileParty)
	{
		TroopRoster troopsToDesert = Campaign.Current.Models.PartyDesertionModel.GetTroopsToDesert(mobileParty);
		if (troopsToDesert.Count <= 0)
		{
			return;
		}
		int numberOfAllMembers = mobileParty.Party.NumberOfAllMembers;
		foreach (TroopRosterElement item in troopsToDesert.GetTroopRoster())
		{
			mobileParty.MemberRoster.AddToCounts(item.Character, -item.Number, insertAtFront: false, -item.WoundedNumber);
		}
		int numberOfAllMembers2 = mobileParty.Party.NumberOfAllMembers;
		if ((float)troopsToDesert.TotalManCount > (float)numberOfAllMembers * 0.4f && numberOfAllMembers > 10)
		{
			Debug.Print($"[High Desertion Alert]  Deserted troop count for party: {mobileParty.StringId} is: {troopsToDesert.TotalManCount}, remaining troop count: {numberOfAllMembers2}, all member count before desertion: {numberOfAllMembers}");
		}
		CampaignEventDispatcher.Instance.OnTroopsDeserted(mobileParty, troopsToDesert);
		if (numberOfAllMembers2 == 0)
		{
			DestroyPartyAction.Apply(null, mobileParty);
		}
	}
}
