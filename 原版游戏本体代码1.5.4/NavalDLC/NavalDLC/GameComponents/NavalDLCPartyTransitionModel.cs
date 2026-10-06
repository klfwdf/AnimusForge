using Helpers;
using NavalDLC.CharacterDevelopment;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace NavalDLC.GameComponents;

public class NavalDLCPartyTransitionModel : PartyTransitionModel
{
	private const float MinHoursToMoveAnchor = 3f;

	private const float MaxHoursToMoveAnchor = 48f;

	private const float AnchorMoveSpeedPerHour = 35f;

	private const float DisembarkHours = 2f;

	private const float InstantEmbarkDistanceThresholdForAI = 10f;

	public override CampaignTime GetTransitionTimeForEmbarking(MobileParty mobileParty)
	{
		if (!mobileParty.Anchor.IsValid)
		{
			return CampaignTime.Hours(48f);
		}
		float num = float.MaxValue;
		if (mobileParty.CurrentSettlement != null)
		{
			num = Campaign.Current.Models.MapDistanceModel.GetDistance(mobileParty.CurrentSettlement, mobileParty.Anchor.Position, isFromPort: true, MobileParty.NavigationType.Naval);
		}
		else if (mobileParty.EndPositionForNavigationTransition.IsValid())
		{
			num = Campaign.Current.Models.MapDistanceModel.GetDistance(mobileParty.Anchor.Position, mobileParty.EndPositionForNavigationTransition, MobileParty.NavigationType.Naval, out var _);
		}
		if (num < 10f)
		{
			return CampaignTime.Zero;
		}
		return CampaignTime.Hours(GetAnchorReachDurationInHours(num).ResultNumber);
	}

	public override CampaignTime GetTransitionTimeDisembarking(MobileParty mobileParty)
	{
		if (mobileParty.IsInNavalAutoTravel)
		{
			return CampaignTime.Zero;
		}
		ExplainedNumber stat = new ExplainedNumber(2f);
		PerkHelper.AddPerkBonusForParty(NavalPerks.Shipmaster.Unflinching, mobileParty, isPrimaryBonus: true, ref stat);
		return CampaignTime.Hours(stat.ResultNumber);
	}

	public override CampaignTime GetFleetTravelTimeToSettlement(MobileParty mobileParty, Settlement targetSettlement)
	{
		AnchorPoint anchor = mobileParty.Anchor;
		float num = 0f;
		float num2 = 0f;
		if (anchor.IsMovingToPoint)
		{
			num = (float)(anchor.ArrivalTime - CampaignTime.Now).ToHours;
			if (!anchor.IsTargetingSettlement(targetSettlement))
			{
				num2 = Campaign.Current.Models.MapDistanceModel.GetDistance(targetSettlement, anchor.GetTargetPosition(), isFromPort: true, MobileParty.NavigationType.Naval);
			}
		}
		else
		{
			if (!anchor.Position.IsValid())
			{
				return CampaignTime.Hours(48f);
			}
			num2 = Campaign.Current.Models.MapDistanceModel.GetDistance(targetSettlement, anchor.Position, isFromPort: true, MobileParty.NavigationType.Naval);
		}
		ExplainedNumber stat = new ExplainedNumber(0f, includeDescriptions: false, null);
		if (num2 > 0f)
		{
			stat = GetAnchorReachDurationInHours(num2);
			PerkHelper.AddPerkBonusForParty(NavalPerks.Shipmaster.ShoreMaster, mobileParty, isPrimaryBonus: true, ref stat);
		}
		return CampaignTime.Hours(MathF.Min(stat.ResultNumber + num, 48f));
	}

	private ExplainedNumber GetAnchorReachDurationInHours(float distance)
	{
		distance = MathF.Pow(distance, 0.95f);
		ExplainedNumber result = new ExplainedNumber(distance / 35f);
		result.LimitMin(3f);
		result.LimitMax(48f);
		return result;
	}
}
