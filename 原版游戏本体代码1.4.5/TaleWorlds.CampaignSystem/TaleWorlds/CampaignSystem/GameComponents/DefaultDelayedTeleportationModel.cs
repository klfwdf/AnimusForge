using Helpers;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultDelayedTeleportationModel : DelayedTeleportationModel
{
	private float MaximumDistanceForDelayAsDays => 2f;

	public override float DefaultTeleportationSpeed => 0.24f;

	public override ExplainedNumber GetTeleportationDelayAsHours(Hero teleportingHero, PartyBase target)
	{
		float num = MaximumDistanceForDelayAsDays * Campaign.Current.EstimatedAverageLordPartySpeed * (float)CampaignTime.HoursInDay;
		float value = 0f;
		IMapPoint mapPoint = teleportingHero.GetMapPoint();
		if (mapPoint != null)
		{
			MobileParty.NavigationType navigationType = ((!teleportingHero.Clan.HasNavalNavigationCapability) ? MobileParty.NavigationType.Default : MobileParty.NavigationType.All);
			if (target.IsSettlement)
			{
				value = ((teleportingHero.CurrentSettlement == null || teleportingHero.CurrentSettlement != target.Settlement) ? DistanceHelper.FindClosestDistanceFromMapPointToSettlement(mapPoint, target.Settlement, navigationType, out var _) : 0f);
			}
			else if (target.IsMobile)
			{
				if (mapPoint is Settlement toSettlement)
				{
					value = DistanceHelper.FindClosestDistanceFromMobilePartyToSettlement(target.MobileParty, toSettlement, navigationType);
				}
				else if (mapPoint is MobileParty to)
				{
					float num2 = DistanceHelper.FindClosestDistanceFromMobilePartyToMobileParty(target.MobileParty, to, navigationType);
					if (num2 < num)
					{
						value = num2;
					}
				}
			}
		}
		value = MathF.Clamp(value, 0f, num);
		return new ExplainedNumber(value * DefaultTeleportationSpeed);
	}

	public override bool CanPerformImmediateTeleport(Hero hero, MobileParty targetMobileParty, Settlement targetSettlement)
	{
		if (targetSettlement == null || targetSettlement.IsUnderSiege || targetSettlement.IsUnderRaid)
		{
			if (targetMobileParty != null && targetMobileParty.MapEvent == null && !targetMobileParty.IsCurrentlyEngagingParty)
			{
				if (targetMobileParty.IsCurrentlyAtSea)
				{
					return targetMobileParty.CurrentSettlement != null;
				}
				return true;
			}
			return false;
		}
		return true;
	}
}
