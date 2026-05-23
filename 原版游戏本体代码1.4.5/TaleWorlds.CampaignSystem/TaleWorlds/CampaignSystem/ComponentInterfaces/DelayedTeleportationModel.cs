using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class DelayedTeleportationModel : MBGameModel<DelayedTeleportationModel>
{
	public abstract float DefaultTeleportationSpeed { get; }

	public abstract ExplainedNumber GetTeleportationDelayAsHours(Hero teleportingHero, PartyBase target);

	public abstract bool CanPerformImmediateTeleport(Hero hero, MobileParty targetMobileParty, Settlement targetSettlement);
}
