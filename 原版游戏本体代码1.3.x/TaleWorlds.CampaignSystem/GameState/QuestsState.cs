using System;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.GameState
{
	// Token: 0x0200039F RID: 927
	public class QuestsState : GameState
	{
		// Token: 0x17000C8A RID: 3210
		// (get) Token: 0x06003503 RID: 13571 RVA: 0x000D69B3 File Offset: 0x000D4BB3
		// (set) Token: 0x06003504 RID: 13572 RVA: 0x000D69BB File Offset: 0x000D4BBB
		public IssueBase InitialSelectedIssue { get; private set; }

		// Token: 0x17000C8B RID: 3211
		// (get) Token: 0x06003505 RID: 13573 RVA: 0x000D69C4 File Offset: 0x000D4BC4
		// (set) Token: 0x06003506 RID: 13574 RVA: 0x000D69CC File Offset: 0x000D4BCC
		public QuestBase InitialSelectedQuest { get; private set; }

		// Token: 0x17000C8C RID: 3212
		// (get) Token: 0x06003507 RID: 13575 RVA: 0x000D69D5 File Offset: 0x000D4BD5
		// (set) Token: 0x06003508 RID: 13576 RVA: 0x000D69DD File Offset: 0x000D4BDD
		public JournalLogEntry InitialSelectedLog { get; private set; }

		// Token: 0x17000C8D RID: 3213
		// (get) Token: 0x06003509 RID: 13577 RVA: 0x000D69E6 File Offset: 0x000D4BE6
		public override bool IsMenuState
		{
			get
			{
				return true;
			}
		}

		// Token: 0x17000C8E RID: 3214
		// (get) Token: 0x0600350A RID: 13578 RVA: 0x000D69E9 File Offset: 0x000D4BE9
		// (set) Token: 0x0600350B RID: 13579 RVA: 0x000D69F1 File Offset: 0x000D4BF1
		public IQuestsStateHandler Handler
		{
			get
			{
				return this._handler;
			}
			set
			{
				this._handler = value;
			}
		}

		// Token: 0x0600350C RID: 13580 RVA: 0x000D69FA File Offset: 0x000D4BFA
		public QuestsState()
		{
		}

		// Token: 0x0600350D RID: 13581 RVA: 0x000D6A02 File Offset: 0x000D4C02
		public QuestsState(IssueBase initialSelectedIssue)
		{
			this.InitialSelectedIssue = initialSelectedIssue;
		}

		// Token: 0x0600350E RID: 13582 RVA: 0x000D6A11 File Offset: 0x000D4C11
		public QuestsState(QuestBase initialSelectedQuest)
		{
			this.InitialSelectedQuest = initialSelectedQuest;
		}

		// Token: 0x0600350F RID: 13583 RVA: 0x000D6A20 File Offset: 0x000D4C20
		public QuestsState(JournalLogEntry initialSelectedLog)
		{
			this.InitialSelectedLog = initialSelectedLog;
		}

		// Token: 0x04000F19 RID: 3865
		private IQuestsStateHandler _handler;
	}
}
