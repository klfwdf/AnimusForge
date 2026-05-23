using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001EF RID: 495
	public abstract class BattleCaptainModel : MBGameModel<BattleCaptainModel>
	{
		// Token: 0x06001EF3 RID: 7923
		public abstract float GetCaptainRatingForTroopUsages(Hero hero, TroopUsageFlags flag, out List<PerkObject> compatiblePerks);
	}
}
