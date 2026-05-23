using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultKingdomDecisionPermissionModel : KingdomDecisionPermissionModel
{
	private IAllianceCampaignBehavior _allianceCampaignBehavior;

	private IAllianceCampaignBehavior AllianceCampaignBehavior
	{
		get
		{
			if (_allianceCampaignBehavior == null)
			{
				_allianceCampaignBehavior = Campaign.Current.GetCampaignBehavior<IAllianceCampaignBehavior>();
			}
			return _allianceCampaignBehavior;
		}
	}

	public override bool IsPolicyDecisionAllowed(PolicyObject policy)
	{
		return true;
	}

	public override bool IsWarDecisionAllowedBetweenKingdoms(Kingdom kingdom1, Kingdom kingdom2, out TextObject reason)
	{
		reason = null;
		return true;
	}

	public override bool IsPeaceDecisionAllowedBetweenKingdoms(Kingdom kingdom1, Kingdom kingdom2, out TextObject reason)
	{
		reason = null;
		Kingdom callingKingdom = null;
		if (Campaign.Current.Models.DiplomacyModel.IsAtConstantWar(kingdom1, kingdom2))
		{
			reason = new TextObject("{=eNPupZOp}These kingdoms can not declare peace at this time.");
			return false;
		}
		IAllianceCampaignBehavior allianceCampaignBehavior = AllianceCampaignBehavior;
		if (allianceCampaignBehavior != null && allianceCampaignBehavior.IsAtWarByCallToWarAgreement(kingdom1, kingdom2, out callingKingdom))
		{
			reason = GetExplanationForPeaceOfferWithCallToWar(callingKingdom, kingdom1, kingdom2);
			return false;
		}
		IAllianceCampaignBehavior allianceCampaignBehavior2 = AllianceCampaignBehavior;
		if (allianceCampaignBehavior2 != null && allianceCampaignBehavior2.IsAtWarByCallToWarAgreement(kingdom2, kingdom1, out callingKingdom))
		{
			reason = GetExplanationForPeaceOfferWithCallToWar(callingKingdom, kingdom2, kingdom1);
			return false;
		}
		if (!Campaign.Current.Models.DiplomacyModel.IsPeaceSuitable(kingdom1, kingdom2))
		{
			reason = new TextObject("{=JkQ7fmcX}The enemy is not open to negotiations.");
			return false;
		}
		return true;
	}

	public override bool IsAnnexationDecisionAllowed(Settlement annexedSettlement)
	{
		return true;
	}

	public override bool IsExpulsionDecisionAllowed(Clan expelledClan)
	{
		return true;
	}

	public override bool IsKingSelectionDecisionAllowed(Kingdom kingdom)
	{
		return true;
	}

	public override bool IsStartAllianceDecisionAllowedBetweenKingdoms(Kingdom kingdom1, Kingdom kingdom2, out TextObject reason)
	{
		reason = null;
		return true;
	}

	private TextObject GetExplanationForPeaceOfferWithCallToWar(Kingdom callingKingdom, Kingdom calledKingdom, Kingdom kingdomToCallToWarAgainst)
	{
		TextObject empty = TextObject.GetEmpty();
		if (calledKingdom == Clan.PlayerClan.Kingdom)
		{
			empty = new TextObject("{=*}Your realm is not allowed to negotiate peace with {KINGDOM_TO_CALL_TO_WAR_AGAINST} due to your Call to War Agreement with {CALLING_KINGDOM}.");
			empty.SetTextVariable("KINGDOM_TO_CALL_TO_WAR_AGAINST", kingdomToCallToWarAgainst.Name);
			empty.SetTextVariable("CALLING_KINGDOM", callingKingdom.Name);
		}
		else if (kingdomToCallToWarAgainst == Clan.PlayerClan.Kingdom)
		{
			empty = new TextObject("{=*}Your realm is not allowed to negotiate peace with {CALLED_KINGDOM} due to their Call to War Agreement with {CALLING_KINGDOM}.");
			empty.SetTextVariable("CALLED_KINGDOM", calledKingdom.Name);
			empty.SetTextVariable("CALLING_KINGDOM", callingKingdom.Name);
		}
		else
		{
			empty = new TextObject("{=*}{KINGDOM_NAME}  is not allowed to negotiate peace with {CALLED_KINGDOM} due to their Call to War Agreement with {CALLING_KINGDOM}.");
			empty.SetTextVariable("KINGDOM_NAME", kingdomToCallToWarAgainst.Name);
			empty.SetTextVariable("CALLED_KINGDOM", calledKingdom.Name);
			empty.SetTextVariable("CALLING_KINGDOM", callingKingdom.Name);
		}
		return empty;
	}
}
