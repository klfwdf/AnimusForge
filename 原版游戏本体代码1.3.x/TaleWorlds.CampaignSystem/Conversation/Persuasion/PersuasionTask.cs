using System;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.Conversation.Persuasion
{
	// Token: 0x020002A1 RID: 673
	public class PersuasionTask
	{
		// Token: 0x060023D8 RID: 9176 RVA: 0x00099C6F File Offset: 0x00097E6F
		public PersuasionTask(int reservationType)
		{
			this.Options = new MBList<PersuasionOptionArgs>();
			this.ReservationType = reservationType;
		}

		// Token: 0x060023D9 RID: 9177 RVA: 0x00099C89 File Offset: 0x00097E89
		public void AddOptionToTask(PersuasionOptionArgs option)
		{
			this.Options.Add(option);
		}

		// Token: 0x060023DA RID: 9178 RVA: 0x00099C98 File Offset: 0x00097E98
		public void BlockAllOptions()
		{
			foreach (PersuasionOptionArgs persuasionOptionArgs in this.Options)
			{
				persuasionOptionArgs.BlockTheOption(true);
			}
		}

		// Token: 0x060023DB RID: 9179 RVA: 0x00099CEC File Offset: 0x00097EEC
		public void UnblockAllOptions()
		{
			foreach (PersuasionOptionArgs persuasionOptionArgs in this.Options)
			{
				persuasionOptionArgs.BlockTheOption(false);
			}
		}

		// Token: 0x060023DC RID: 9180 RVA: 0x00099D40 File Offset: 0x00097F40
		public void ApplyEffects(float moveToNextStageChance, float blockRandomOptionChance)
		{
			if (moveToNextStageChance > MBRandom.RandomFloat)
			{
				this.BlockAllOptions();
				return;
			}
			if (blockRandomOptionChance > MBRandom.RandomFloat)
			{
				PersuasionOptionArgs randomElementWithPredicate = this.Options.GetRandomElementWithPredicate((PersuasionOptionArgs x) => !x.IsBlocked);
				if (randomElementWithPredicate == null)
				{
					return;
				}
				randomElementWithPredicate.BlockTheOption(true);
			}
		}

		// Token: 0x04000AD9 RID: 2777
		public readonly MBList<PersuasionOptionArgs> Options;

		// Token: 0x04000ADA RID: 2778
		public TextObject SpokenLine;

		// Token: 0x04000ADB RID: 2779
		public TextObject ImmediateFailLine;

		// Token: 0x04000ADC RID: 2780
		public TextObject FinalFailLine;

		// Token: 0x04000ADD RID: 2781
		public TextObject TryLaterLine;

		// Token: 0x04000ADE RID: 2782
		public readonly int ReservationType;
	}
}
