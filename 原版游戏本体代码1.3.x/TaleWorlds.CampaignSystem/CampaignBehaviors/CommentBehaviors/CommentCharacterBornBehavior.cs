using System;
using TaleWorlds.CampaignSystem.LogEntries;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors.CommentBehaviors
{
	// Token: 0x02000453 RID: 1107
	public class CommentCharacterBornBehavior : CampaignBehaviorBase
	{
		// Token: 0x06004746 RID: 18246 RVA: 0x0016590F File Offset: 0x00163B0F
		public override void RegisterEvents()
		{
			CampaignEvents.HeroCreated.AddNonSerializedListener(this, new Action<Hero, bool>(this.HeroCreated));
		}

		// Token: 0x06004747 RID: 18247 RVA: 0x00165928 File Offset: 0x00163B28
		private void HeroCreated(Hero hero, bool isBornNaturally)
		{
			if (isBornNaturally)
			{
				LogEntry.AddLogEntry(new CharacterBornLogEntry(hero));
			}
		}

		// Token: 0x06004748 RID: 18248 RVA: 0x00165938 File Offset: 0x00163B38
		public override void SyncData(IDataStore dataStore)
		{
		}
	}
}
