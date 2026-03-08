using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Localization;

namespace SandBox
{
	// Token: 0x0200000C RID: 12
	public class Add1000GoldCheat : GameplayCheatItem
	{
		// Token: 0x06000024 RID: 36 RVA: 0x00003832 File Offset: 0x00001A32
		public override void ExecuteCheat()
		{
			GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, 1000, true);
		}

		// Token: 0x06000025 RID: 37 RVA: 0x00003845 File Offset: 0x00001A45
		public override TextObject GetName()
		{
			return new TextObject("{=KLbeF6gf}Add 1000 Gold", null);
		}
	}
}
