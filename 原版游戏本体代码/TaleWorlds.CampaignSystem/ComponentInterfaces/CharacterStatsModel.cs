using System;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x02000183 RID: 387
	public abstract class CharacterStatsModel : MBGameModel<CharacterStatsModel>
	{
		// Token: 0x06001B98 RID: 7064
		public abstract ExplainedNumber MaxHitpoints(CharacterObject character, bool includeDescriptions = false);

		// Token: 0x06001B99 RID: 7065
		public abstract int GetTier(CharacterObject character);

		// Token: 0x170006F0 RID: 1776
		// (get) Token: 0x06001B9A RID: 7066
		public abstract int MaxCharacterTier { get; }

		// Token: 0x06001B9B RID: 7067
		public abstract int WoundedHitPointLimit(Hero hero);
	}
}
