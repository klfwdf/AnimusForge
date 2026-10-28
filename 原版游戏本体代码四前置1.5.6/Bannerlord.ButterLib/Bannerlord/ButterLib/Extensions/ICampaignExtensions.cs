using Bannerlord.ButterLib.DistanceMatrix;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace Bannerlord.ButterLib.Extensions;

internal interface ICampaignExtensions
{
	DistanceMatrix<Settlement>? GetDefaultSettlementDistanceMatrix(Campaign campaign);

	DistanceMatrix<Clan>? GetDefaultClanDistanceMatrix(Campaign campaign);

	DistanceMatrix<Kingdom>? GetDefaultKingdomDistanceMatrix(Campaign campaign);
}
