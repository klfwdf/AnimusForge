using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class KingdomDecisionPermissionModel : MBGameModel<KingdomDecisionPermissionModel>
{
	public abstract bool IsPolicyDecisionAllowed(PolicyObject policy);

	public abstract bool IsWarDecisionAllowedBetweenKingdoms(Kingdom kingdom1, Kingdom kingdom2, out TextObject reason);

	public abstract bool IsPeaceDecisionAllowedBetweenKingdoms(Kingdom kingdom1, Kingdom kingdom2, out TextObject reason);

	public abstract bool IsStartAllianceDecisionAllowedBetweenKingdoms(Kingdom kingdom1, Kingdom kingdom2, out TextObject reason);

	public abstract bool IsAnnexationDecisionAllowed(Settlement annexedSettlement);

	public abstract bool IsExpulsionDecisionAllowed(Clan expelledClan);

	public abstract bool IsKingSelectionDecisionAllowed(Kingdom kingdom);
}
