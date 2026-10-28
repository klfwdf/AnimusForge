using Bannerlord.ButterLib.DistanceMatrix;
using Bannerlord.ButterLib.Extensions;
using Bannerlord.ButterLib.Implementation.DistanceMatrix;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace Bannerlord.ButterLib.Implementation.Common.Extensions;

internal sealed class CampaignExtensionsImplementation : ICampaignExtensions
{
	public DistanceMatrix<Settlement>? GetDefaultSettlementDistanceMatrix(Campaign campaign)
	{
		if (campaign.GameStarted)
		{
			GeopoliticsBehavior campaignBehavior = campaign.GetCampaignBehavior<GeopoliticsBehavior>();
			if (campaignBehavior != null)
			{
				return campaignBehavior.SettlementDistanceMatrix;
			}
		}
		return null;
	}

	public DistanceMatrix<Clan>? GetDefaultClanDistanceMatrix(Campaign campaign)
	{
		if (campaign.GameStarted)
		{
			GeopoliticsBehavior campaignBehavior = campaign.GetCampaignBehavior<GeopoliticsBehavior>();
			if (campaignBehavior != null)
			{
				return campaignBehavior.ClanDistanceMatrix;
			}
		}
		return null;
	}

	public DistanceMatrix<Kingdom>? GetDefaultKingdomDistanceMatrix(Campaign campaign)
	{
		if (campaign.GameStarted)
		{
			GeopoliticsBehavior campaignBehavior = campaign.GetCampaignBehavior<GeopoliticsBehavior>();
			if (campaignBehavior != null)
			{
				return campaignBehavior.KingdomDistanceMatrix;
			}
		}
		return null;
	}
}
