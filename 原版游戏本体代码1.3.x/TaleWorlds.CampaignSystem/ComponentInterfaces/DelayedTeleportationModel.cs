using System;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001F2 RID: 498
	public abstract class DelayedTeleportationModel : MBGameModel<DelayedTeleportationModel>
	{
		// Token: 0x170007BA RID: 1978
		// (get) Token: 0x06001F06 RID: 7942
		public abstract float DefaultTeleportationSpeed { get; }

		// Token: 0x06001F07 RID: 7943
		public abstract ExplainedNumber GetTeleportationDelayAsHours(Hero teleportingHero, PartyBase target);

		// Token: 0x06001F08 RID: 7944
		public abstract bool CanPerformImmediateTeleport(Hero hero, MobileParty targetMobileParty, Settlement targetSettlement);
	}
}
