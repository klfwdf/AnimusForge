using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001A2 RID: 418
	public abstract class KingdomCreationModel : MBGameModel<KingdomCreationModel>
	{
		// Token: 0x1700070F RID: 1807
		// (get) Token: 0x06001C77 RID: 7287
		public abstract int MinimumClanTierToCreateKingdom { get; }

		// Token: 0x17000710 RID: 1808
		// (get) Token: 0x06001C78 RID: 7288
		public abstract int MinimumNumberOfSettlementsOwnedToCreateKingdom { get; }

		// Token: 0x17000711 RID: 1809
		// (get) Token: 0x06001C79 RID: 7289
		public abstract int MinimumTroopCountToCreateKingdom { get; }

		// Token: 0x17000712 RID: 1810
		// (get) Token: 0x06001C7A RID: 7290
		public abstract int MaximumNumberOfInitialPolicies { get; }

		// Token: 0x06001C7B RID: 7291
		public abstract bool IsPlayerKingdomCreationPossible(out List<TextObject> explanations);

		// Token: 0x06001C7C RID: 7292
		public abstract bool IsPlayerKingdomAbdicationPossible(out List<TextObject> explanations);

		// Token: 0x06001C7D RID: 7293
		public abstract IEnumerable<CultureObject> GetAvailablePlayerKingdomCultures();
	}
}
