using System;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x02000198 RID: 408
	public abstract class DefectionModel : MBGameModel<DefaultDefectionModel>
	{
		// Token: 0x06001C32 RID: 7218
		public abstract bool CanHeroDefectToFaction(Hero hero, Kingdom kingdom);
	}
}
