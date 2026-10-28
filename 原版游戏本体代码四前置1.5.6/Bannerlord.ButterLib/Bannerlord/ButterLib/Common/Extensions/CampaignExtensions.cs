using Bannerlord.ButterLib.DistanceMatrix;
using Bannerlord.ButterLib.Extensions;
using Microsoft.Extensions.DependencyInjection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace Bannerlord.ButterLib.Common.Extensions;

public static class CampaignExtensions
{
	private static ICampaignExtensions? _instance;

	private static ICampaignExtensions? Instance => _instance ?? (_instance = ButterLibSubModule.Instance?.GetServiceProvider()?.GetService<ICampaignExtensions>());

	public static DistanceMatrix<Settlement>? GetDefaultSettlementDistanceMatrix(this Campaign campaign)
	{
		return Instance?.GetDefaultSettlementDistanceMatrix(campaign);
	}

	public static DistanceMatrix<Clan>? GetDefaultClanDistanceMatrix(this Campaign campaign)
	{
		return Instance?.GetDefaultClanDistanceMatrix(campaign);
	}

	public static DistanceMatrix<Kingdom>? GetDefaultKingdomDistanceMatrix(this Campaign campaign)
	{
		return Instance?.GetDefaultKingdomDistanceMatrix(campaign);
	}
}
