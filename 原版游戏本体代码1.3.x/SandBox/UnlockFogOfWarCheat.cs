using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;

namespace SandBox
{
	// Token: 0x02000018 RID: 24
	public class UnlockFogOfWarCheat : GameplayCheatItem
	{
		// Token: 0x06000048 RID: 72 RVA: 0x00003ED8 File Offset: 0x000020D8
		public override void ExecuteCheat()
		{
			foreach (Hero hero in Hero.AllAliveHeroes)
			{
				hero.IsKnownToPlayer = true;
			}
		}

		// Token: 0x06000049 RID: 73 RVA: 0x00003F28 File Offset: 0x00002128
		public override TextObject GetName()
		{
			return new TextObject("{=jPtG0Pu1}Unlock Fog of War", null);
		}
	}
}
