using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.Actions;

public static class DisbandPartyAction
{
	public static void StartDisband(MobileParty disbandParty)
	{
		if (disbandParty.IsDisbanding)
		{
			return;
		}
		if (disbandParty.MemberRoster.TotalManCount == 0)
		{
			DestroyPartyAction.Apply(null, disbandParty);
			return;
		}
		IDisbandPartyCampaignBehavior campaignBehavior = Campaign.Current.GetCampaignBehavior<IDisbandPartyCampaignBehavior>();
		if (campaignBehavior != null && campaignBehavior.IsPartyWaitingForDisband(disbandParty))
		{
			return;
		}
		if (disbandParty.Army != null)
		{
			if (disbandParty == disbandParty.Army.LeaderParty)
			{
				DisbandArmyAction.ApplyByUnknownReason(disbandParty.Army);
			}
			else
			{
				disbandParty.Army = null;
			}
		}
		TextObject textObject = new TextObject("{=ithcVNfA}{CLAN_NAME}{.o} Party");
		textObject.SetTextVariable("CLAN_NAME", (disbandParty.ActualClan != null) ? disbandParty.ActualClan.Name : CampaignData.NeutralFactionName);
		disbandParty.Party.SetCustomName(textObject);
		CampaignEventDispatcher.Instance.OnPartyDisbandStarted(disbandParty);
	}

	public static void CancelDisband(MobileParty disbandParty)
	{
		CampaignEventDispatcher.Instance.OnPartyDisbandCanceled(disbandParty);
		disbandParty.IsDisbanding = false;
		disbandParty.Party.SetCustomName(TextObject.GetEmpty());
		disbandParty.SetMoveModeHold();
	}
}
