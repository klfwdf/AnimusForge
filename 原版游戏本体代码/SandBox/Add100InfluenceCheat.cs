using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Localization;

namespace SandBox
{
	// Token: 0x0200000D RID: 13
	public class Add100InfluenceCheat : GameplayCheatItem
	{
		// Token: 0x06000027 RID: 39 RVA: 0x0000385A File Offset: 0x00001A5A
		public override void ExecuteCheat()
		{
			ChangeClanInfluenceAction.Apply(Clan.PlayerClan, 100f);
		}

		// Token: 0x06000028 RID: 40 RVA: 0x0000386B File Offset: 0x00001A6B
		public override TextObject GetName()
		{
			return new TextObject("{=6TgRwB2Q}Add 100 Influence", null);
		}
	}
}
