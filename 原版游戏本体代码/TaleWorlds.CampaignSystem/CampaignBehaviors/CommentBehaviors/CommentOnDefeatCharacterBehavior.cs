using System;
using TaleWorlds.CampaignSystem.LogEntries;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors.CommentBehaviors
{
	// Token: 0x0200045C RID: 1116
	public class CommentOnDefeatCharacterBehavior : CampaignBehaviorBase
	{
		// Token: 0x0600476B RID: 18283 RVA: 0x00165DDD File Offset: 0x00163FDD
		public override void RegisterEvents()
		{
			CampaignEvents.CharacterDefeated.AddNonSerializedListener(this, new Action<Hero, Hero>(this.OnCharacterDefeated));
		}

		// Token: 0x0600476C RID: 18284 RVA: 0x00165DF6 File Offset: 0x00163FF6
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x0600476D RID: 18285 RVA: 0x00165DF8 File Offset: 0x00163FF8
		private void OnCharacterDefeated(Hero winner, Hero loser)
		{
			LogEntry.AddLogEntry(new DefeatCharacterLogEntry(winner, loser));
		}
	}
}
