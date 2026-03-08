using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Localization;

namespace SandBox
{
	// Token: 0x0200000E RID: 14
	public class Add100RenownCheat : GameplayCheatItem
	{
		// Token: 0x0600002A RID: 42 RVA: 0x00003880 File Offset: 0x00001A80
		public override void ExecuteCheat()
		{
			GainRenownAction.Apply(Hero.MainHero, 100f, true);
		}

		// Token: 0x0600002B RID: 43 RVA: 0x00003892 File Offset: 0x00001A92
		public override TextObject GetName()
		{
			return new TextObject("{=zXQwb3lj}Add 100 Renown", null);
		}
	}
}
