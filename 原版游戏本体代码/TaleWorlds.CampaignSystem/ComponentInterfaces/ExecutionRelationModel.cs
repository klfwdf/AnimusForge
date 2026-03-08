using System;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001F0 RID: 496
	public abstract class ExecutionRelationModel : MBGameModel<ExecutionRelationModel>
	{
		// Token: 0x170007B0 RID: 1968
		// (get) Token: 0x06001EF5 RID: 7925
		public abstract int HeroKillingHeroClanRelationPenalty { get; }

		// Token: 0x170007B1 RID: 1969
		// (get) Token: 0x06001EF6 RID: 7926
		public abstract int HeroKillingHeroFriendRelationPenalty { get; }

		// Token: 0x170007B2 RID: 1970
		// (get) Token: 0x06001EF7 RID: 7927
		public abstract int PlayerExecutingHeroFactionRelationPenaltyDishonorable { get; }

		// Token: 0x170007B3 RID: 1971
		// (get) Token: 0x06001EF8 RID: 7928
		public abstract int PlayerExecutingHeroClanRelationPenaltyDishonorable { get; }

		// Token: 0x170007B4 RID: 1972
		// (get) Token: 0x06001EF9 RID: 7929
		public abstract int PlayerExecutingHeroFriendRelationPenaltyDishonorable { get; }

		// Token: 0x170007B5 RID: 1973
		// (get) Token: 0x06001EFA RID: 7930
		public abstract int PlayerExecutingHeroHonorPenalty { get; }

		// Token: 0x170007B6 RID: 1974
		// (get) Token: 0x06001EFB RID: 7931
		public abstract int PlayerExecutingHeroFactionRelationPenalty { get; }

		// Token: 0x170007B7 RID: 1975
		// (get) Token: 0x06001EFC RID: 7932
		public abstract int PlayerExecutingHeroHonorableNobleRelationPenalty { get; }

		// Token: 0x170007B8 RID: 1976
		// (get) Token: 0x06001EFD RID: 7933
		public abstract int PlayerExecutingHeroClanRelationPenalty { get; }

		// Token: 0x170007B9 RID: 1977
		// (get) Token: 0x06001EFE RID: 7934
		public abstract int PlayerExecutingHeroFriendRelationPenalty { get; }

		// Token: 0x06001EFF RID: 7935
		public abstract int GetRelationChangeForExecutingHero(Hero victim, Hero hero, out bool showQuickNotification);
	}
}
