using System;
using System.Linq;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Quests
{
	// Token: 0x02000022 RID: 34
	public class QuestItemVM : ViewModel
	{
		// Token: 0x17000058 RID: 88
		// (get) Token: 0x060001F8 RID: 504 RVA: 0x000127A4 File Offset: 0x000109A4
		public QuestBase Quest { get; }

		// Token: 0x17000059 RID: 89
		// (get) Token: 0x060001F9 RID: 505 RVA: 0x000127AC File Offset: 0x000109AC
		public IssueBase Issue { get; }

		// Token: 0x1700005A RID: 90
		// (get) Token: 0x060001FA RID: 506 RVA: 0x000127B4 File Offset: 0x000109B4
		public JournalLogEntry QuestLogEntry { get; }

		// Token: 0x060001FB RID: 507 RVA: 0x000127BC File Offset: 0x000109BC
		public QuestItemVM(JournalLogEntry questLogEntry, Action<QuestItemVM> onSelection, QuestsVM.QuestCompletionType completion)
		{
			this._onSelection = onSelection;
			this.QuestLogEntry = questLogEntry;
			this.Stages = new MBBindingList<QuestStageVM>();
			this._completionType = completion;
			this.IsCompleted = this._completionType > QuestsVM.QuestCompletionType.Active;
			this.IsCompletedSuccessfully = this._completionType == QuestsVM.QuestCompletionType.Successful;
			this.CompletionTypeAsInt = (int)this._completionType;
			bool isRemainingDaysHidden;
			if (!this.IsCompleted)
			{
				QuestBase quest = this.Quest;
				CampaignTime? campaignTime = ((quest != null) ? new CampaignTime?(quest.QuestDueTime) : null);
				CampaignTime never = CampaignTime.Never;
				isRemainingDaysHidden = campaignTime != null && (campaignTime == null || campaignTime.GetValueOrDefault() == never);
			}
			else
			{
				isRemainingDaysHidden = true;
			}
			this.IsRemainingDaysHidden = isRemainingDaysHidden;
			this.IsQuestGiverHeroHidden = false;
			this.IsMainQuest = questLogEntry.IsSpecial;
			foreach (JournalLog log in questLogEntry.GetEntries())
			{
				this.PopulateQuestLog(log, false);
			}
			this.Name = questLogEntry.Title.ToString();
			this.QuestGiverHero = new HeroVM(questLogEntry.RelatedHero, false);
			this.UpdateIsUpdated();
			this.IsTracked = false;
			this.IsTrackable = false;
			this.RefreshValues();
		}

		// Token: 0x060001FC RID: 508 RVA: 0x00012908 File Offset: 0x00010B08
		public QuestItemVM(QuestBase quest, Action<QuestItemVM> onSelection)
		{
			this.Quest = quest;
			this._onSelection = onSelection;
			this.Stages = new MBBindingList<QuestStageVM>();
			this.CompletionTypeAsInt = 0;
			this.IsRemainingDaysHidden = !this.Quest.IsOngoing || this.Quest.IsRemainingTimeHidden;
			this.IsQuestGiverHeroHidden = this.Quest.QuestGiver == null;
			MBReadOnlyList<JournalLog> journalEntries = this.Quest.JournalEntries;
			for (int i = 0; i < journalEntries.Count; i++)
			{
				bool isLastStage = i == journalEntries.Count - 1;
				JournalLog log = journalEntries[i];
				this.PopulateQuestLog(log, isLastStage);
			}
			this.IsMainQuest = quest.IsSpecialQuest;
			if (!this.IsQuestGiverHeroHidden)
			{
				this.QuestGiverHero = new HeroVM(this.Quest.QuestGiver, false);
			}
			this.UpdateIsUpdated();
			this.IsTrackable = !this.Quest.IsFinalized;
			this.IsTracked = this.Quest.IsTrackEnabled;
			this.RefreshValues();
		}

		// Token: 0x060001FD RID: 509 RVA: 0x00012A08 File Offset: 0x00010C08
		public QuestItemVM(IssueBase issue, Action<QuestItemVM> onSelection)
		{
			this.Issue = issue;
			this._onSelection = onSelection;
			this.Stages = new MBBindingList<QuestStageVM>();
			this.IsCompleted = false;
			this.CompletionTypeAsInt = 0;
			this.IsRemainingDaysHidden = this.Issue.IsOngoingWithoutQuest;
			this.IsQuestGiverHeroHidden = false;
			this.UpdateRemainingTime(this.Issue.IssueDueTime);
			foreach (JournalLog log in issue.JournalEntries)
			{
				this.PopulateQuestLog(log, false);
			}
			this.Name = issue.Title.ToString();
			this.QuestGiverHero = new HeroVM(issue.IssueOwner, false);
			this.UpdateIsUpdated();
			this.IsTrackable = false;
		}

		// Token: 0x060001FE RID: 510 RVA: 0x00012AE4 File Offset: 0x00010CE4
		public override void RefreshValues()
		{
			base.RefreshValues();
			if (this.Quest != null)
			{
				this.Name = this.Quest.Title.ToString();
				this.UpdateRemainingTime(this.Quest.QuestDueTime);
			}
			else if (this.Issue != null)
			{
				this.Name = this.Issue.Title.ToString();
				this.UpdateRemainingTime(this.Issue.IssueDueTime);
			}
			else if (this.QuestLogEntry != null)
			{
				this.Name = this.QuestLogEntry.Title.ToString();
			}
			HeroVM questGiverHero = this.QuestGiverHero;
			if (questGiverHero != null)
			{
				questGiverHero.RefreshValues();
			}
			this.Stages.ApplyActionOnAllItems(delegate(QuestStageVM x)
			{
				x.RefreshValues();
			});
		}

		// Token: 0x060001FF RID: 511 RVA: 0x00012BB4 File Offset: 0x00010DB4
		private void UpdateRemainingTime(CampaignTime dueTime)
		{
			if (this.IsRemainingDaysHidden)
			{
				this.RemainingDays = 0;
			}
			else
			{
				this.RemainingDays = (int)(dueTime - CampaignTime.Now).ToDays;
			}
			GameTexts.SetVariable("DAY_IS_PLURAL", (this.RemainingDays > 1) ? 1 : 0);
			GameTexts.SetVariable("DAY", this.RemainingDays);
			if (dueTime.ToHours - CampaignTime.Now.ToHours < (double)CampaignTime.HoursInDay)
			{
				this.RemainingDaysText = GameTexts.FindText("str_less_than_a_day", null).ToString();
				this.RemainingDaysTextCombined = GameTexts.FindText("str_less_than_a_day", null).ToString();
				return;
			}
			this.RemainingDaysText = GameTexts.FindText("str_DAY_days_capital", null).ToString();
			this.RemainingDaysTextCombined = GameTexts.FindText("str_DAY_days", null).ToString();
		}

		// Token: 0x06000200 RID: 512 RVA: 0x00012C8C File Offset: 0x00010E8C
		private void PopulateQuestLog(JournalLog log, bool isLastStage)
		{
			string dateString = log.GetTimeText().ToString();
			if (log.Type != LogType.Text && log.Type != LogType.None)
			{
				int num = MathF.Max(log.Range, 0);
				int num2 = ((log.Type == LogType.TwoWayContinuous) ? log.CurrentProgress : MathF.Max(log.CurrentProgress, 0));
				TextObject textObject = new TextObject("{=Pdo7PpS3}{TASK_NAME} {CURRENT_PROGRESS}/{TARGET_PROGRESS}", null);
				textObject.SetTextVariable("TASK_NAME", log.TaskName);
				textObject.SetTextVariable("CURRENT_PROGRESS", num2);
				textObject.SetTextVariable("TARGET_PROGRESS", num);
				QuestStageTaskVM stageTask = new QuestStageTaskVM(textObject, num2, num, log.Type);
				this.Stages.Add(new QuestStageVM(log, dateString, isLastStage, new Action(this.UpdateIsUpdated), stageTask));
				return;
			}
			this.Stages.Add(new QuestStageVM(log, log.LogText.ToString(), dateString, isLastStage, new Action(this.UpdateIsUpdated)));
		}

		// Token: 0x06000201 RID: 513 RVA: 0x00012D77 File Offset: 0x00010F77
		public void UpdateIsUpdated()
		{
			this.IsUpdated = this.Stages.Any((QuestStageVM s) => s.IsNew);
		}

		// Token: 0x06000202 RID: 514 RVA: 0x00012DA9 File Offset: 0x00010FA9
		public void ExecuteSelection()
		{
			this._onSelection(this);
		}

		// Token: 0x06000203 RID: 515 RVA: 0x00012DB7 File Offset: 0x00010FB7
		public void ExecuteToggleQuestTrack()
		{
			if (this.Quest != null)
			{
				this.Quest.ToggleTrackedObjects();
				this.IsTracked = this.Quest.IsTrackEnabled;
			}
		}

		// Token: 0x1700005B RID: 91
		// (get) Token: 0x06000204 RID: 516 RVA: 0x00012DDD File Offset: 0x00010FDD
		// (set) Token: 0x06000205 RID: 517 RVA: 0x00012DE5 File Offset: 0x00010FE5
		[DataSourceProperty]
		public string Name
		{
			get
			{
				return this._name;
			}
			set
			{
				if (value != this._name)
				{
					this._name = value;
					base.OnPropertyChangedWithValue<string>(value, "Name");
				}
			}
		}

		// Token: 0x1700005C RID: 92
		// (get) Token: 0x06000206 RID: 518 RVA: 0x00012E08 File Offset: 0x00011008
		// (set) Token: 0x06000207 RID: 519 RVA: 0x00012E10 File Offset: 0x00011010
		[DataSourceProperty]
		public int CompletionTypeAsInt
		{
			get
			{
				return this._completionTypeAsInt;
			}
			set
			{
				if (value != this._completionTypeAsInt)
				{
					this._completionTypeAsInt = value;
					base.OnPropertyChangedWithValue(value, "CompletionTypeAsInt");
				}
			}
		}

		// Token: 0x1700005D RID: 93
		// (get) Token: 0x06000208 RID: 520 RVA: 0x00012E2E File Offset: 0x0001102E
		// (set) Token: 0x06000209 RID: 521 RVA: 0x00012E36 File Offset: 0x00011036
		[DataSourceProperty]
		public bool IsMainQuest
		{
			get
			{
				return this._isMainQuest;
			}
			set
			{
				if (value != this._isMainQuest)
				{
					this._isMainQuest = value;
					base.OnPropertyChangedWithValue(value, "IsMainQuest");
				}
			}
		}

		// Token: 0x1700005E RID: 94
		// (get) Token: 0x0600020A RID: 522 RVA: 0x00012E54 File Offset: 0x00011054
		// (set) Token: 0x0600020B RID: 523 RVA: 0x00012E5C File Offset: 0x0001105C
		[DataSourceProperty]
		public bool IsNavalQuest
		{
			get
			{
				return this._isNavalQuest;
			}
			set
			{
				if (value != this._isNavalQuest)
				{
					this._isNavalQuest = value;
					base.OnPropertyChangedWithValue(value, "IsNavalQuest");
				}
			}
		}

		// Token: 0x1700005F RID: 95
		// (get) Token: 0x0600020C RID: 524 RVA: 0x00012E7A File Offset: 0x0001107A
		// (set) Token: 0x0600020D RID: 525 RVA: 0x00012E82 File Offset: 0x00011082
		[DataSourceProperty]
		public bool IsCompletedSuccessfully
		{
			get
			{
				return this._isCompletedSuccessfully;
			}
			set
			{
				if (value != this._isCompletedSuccessfully)
				{
					this._isCompletedSuccessfully = value;
					base.OnPropertyChangedWithValue(value, "IsCompletedSuccessfully");
				}
			}
		}

		// Token: 0x17000060 RID: 96
		// (get) Token: 0x0600020E RID: 526 RVA: 0x00012EA0 File Offset: 0x000110A0
		// (set) Token: 0x0600020F RID: 527 RVA: 0x00012EA8 File Offset: 0x000110A8
		[DataSourceProperty]
		public bool IsCompleted
		{
			get
			{
				return this._isCompleted;
			}
			set
			{
				if (value != this._isCompleted)
				{
					this._isCompleted = value;
					base.OnPropertyChangedWithValue(value, "IsCompleted");
				}
			}
		}

		// Token: 0x17000061 RID: 97
		// (get) Token: 0x06000210 RID: 528 RVA: 0x00012EC6 File Offset: 0x000110C6
		// (set) Token: 0x06000211 RID: 529 RVA: 0x00012ECE File Offset: 0x000110CE
		[DataSourceProperty]
		public bool IsUpdated
		{
			get
			{
				return this._isUpdated;
			}
			set
			{
				if (value != this._isUpdated)
				{
					this._isUpdated = value;
					base.OnPropertyChangedWithValue(value, "IsUpdated");
				}
			}
		}

		// Token: 0x17000062 RID: 98
		// (get) Token: 0x06000212 RID: 530 RVA: 0x00012EEC File Offset: 0x000110EC
		// (set) Token: 0x06000213 RID: 531 RVA: 0x00012EF4 File Offset: 0x000110F4
		[DataSourceProperty]
		public bool IsSelected
		{
			get
			{
				return this._isSelected;
			}
			set
			{
				if (value != this._isSelected)
				{
					this._isSelected = value;
					base.OnPropertyChangedWithValue(value, "IsSelected");
				}
			}
		}

		// Token: 0x17000063 RID: 99
		// (get) Token: 0x06000214 RID: 532 RVA: 0x00012F12 File Offset: 0x00011112
		// (set) Token: 0x06000215 RID: 533 RVA: 0x00012F1A File Offset: 0x0001111A
		[DataSourceProperty]
		public bool IsRemainingDaysHidden
		{
			get
			{
				return this._isRemainingDaysHidden;
			}
			set
			{
				if (value != this._isRemainingDaysHidden)
				{
					this._isRemainingDaysHidden = value;
					base.OnPropertyChangedWithValue(value, "IsRemainingDaysHidden");
				}
			}
		}

		// Token: 0x17000064 RID: 100
		// (get) Token: 0x06000216 RID: 534 RVA: 0x00012F38 File Offset: 0x00011138
		// (set) Token: 0x06000217 RID: 535 RVA: 0x00012F40 File Offset: 0x00011140
		[DataSourceProperty]
		public bool IsTracked
		{
			get
			{
				return this._isTracked;
			}
			set
			{
				if (value != this._isTracked)
				{
					this._isTracked = value;
					base.OnPropertyChangedWithValue(value, "IsTracked");
				}
			}
		}

		// Token: 0x17000065 RID: 101
		// (get) Token: 0x06000218 RID: 536 RVA: 0x00012F5E File Offset: 0x0001115E
		// (set) Token: 0x06000219 RID: 537 RVA: 0x00012F66 File Offset: 0x00011166
		[DataSourceProperty]
		public bool IsTrackable
		{
			get
			{
				return this._isTrackable;
			}
			set
			{
				if (value != this._isTrackable)
				{
					this._isTrackable = value;
					base.OnPropertyChangedWithValue(value, "IsTrackable");
				}
			}
		}

		// Token: 0x17000066 RID: 102
		// (get) Token: 0x0600021A RID: 538 RVA: 0x00012F84 File Offset: 0x00011184
		// (set) Token: 0x0600021B RID: 539 RVA: 0x00012F8C File Offset: 0x0001118C
		[DataSourceProperty]
		public string RemainingDaysText
		{
			get
			{
				return this._remainingDaysText;
			}
			set
			{
				if (value != this._remainingDaysText)
				{
					this._remainingDaysText = value;
					base.OnPropertyChangedWithValue<string>(value, "RemainingDaysText");
				}
			}
		}

		// Token: 0x17000067 RID: 103
		// (get) Token: 0x0600021C RID: 540 RVA: 0x00012FAF File Offset: 0x000111AF
		// (set) Token: 0x0600021D RID: 541 RVA: 0x00012FB7 File Offset: 0x000111B7
		[DataSourceProperty]
		public string RemainingDaysTextCombined
		{
			get
			{
				return this._remainingDaysTextCombined;
			}
			set
			{
				if (value != this._remainingDaysTextCombined)
				{
					this._remainingDaysTextCombined = value;
					base.OnPropertyChangedWithValue<string>(value, "RemainingDaysTextCombined");
				}
			}
		}

		// Token: 0x17000068 RID: 104
		// (get) Token: 0x0600021E RID: 542 RVA: 0x00012FDA File Offset: 0x000111DA
		// (set) Token: 0x0600021F RID: 543 RVA: 0x00012FE2 File Offset: 0x000111E2
		[DataSourceProperty]
		public int RemainingDays
		{
			get
			{
				return this._remainingDays;
			}
			set
			{
				if (value != this._remainingDays)
				{
					this._remainingDays = value;
					base.OnPropertyChangedWithValue(value, "RemainingDays");
				}
			}
		}

		// Token: 0x17000069 RID: 105
		// (get) Token: 0x06000220 RID: 544 RVA: 0x00013000 File Offset: 0x00011200
		// (set) Token: 0x06000221 RID: 545 RVA: 0x00013008 File Offset: 0x00011208
		[DataSourceProperty]
		public HeroVM QuestGiverHero
		{
			get
			{
				return this._questGiverHero;
			}
			set
			{
				if (value != this._questGiverHero)
				{
					this._questGiverHero = value;
					base.OnPropertyChangedWithValue<HeroVM>(value, "QuestGiverHero");
				}
			}
		}

		// Token: 0x1700006A RID: 106
		// (get) Token: 0x06000222 RID: 546 RVA: 0x00013026 File Offset: 0x00011226
		// (set) Token: 0x06000223 RID: 547 RVA: 0x0001302E File Offset: 0x0001122E
		[DataSourceProperty]
		public bool IsQuestGiverHeroHidden
		{
			get
			{
				return this._isQuestGiverHeroHidden;
			}
			set
			{
				if (value != this._isQuestGiverHeroHidden)
				{
					this._isQuestGiverHeroHidden = value;
					base.OnPropertyChangedWithValue(value, "IsQuestGiverHeroHidden");
				}
			}
		}

		// Token: 0x1700006B RID: 107
		// (get) Token: 0x06000224 RID: 548 RVA: 0x0001304C File Offset: 0x0001124C
		// (set) Token: 0x06000225 RID: 549 RVA: 0x00013054 File Offset: 0x00011254
		[DataSourceProperty]
		public MBBindingList<QuestStageVM> Stages
		{
			get
			{
				return this._stages;
			}
			set
			{
				if (value != this._stages)
				{
					this._stages = value;
					base.OnPropertyChangedWithValue<MBBindingList<QuestStageVM>>(value, "Stages");
				}
			}
		}

		// Token: 0x040000E9 RID: 233
		private readonly Action<QuestItemVM> _onSelection;

		// Token: 0x040000EA RID: 234
		private QuestsVM.QuestCompletionType _completionType;

		// Token: 0x040000EB RID: 235
		private string _name;

		// Token: 0x040000EC RID: 236
		private string _remainingDaysText;

		// Token: 0x040000ED RID: 237
		private string _remainingDaysTextCombined;

		// Token: 0x040000EE RID: 238
		private int _remainingDays;

		// Token: 0x040000EF RID: 239
		private int _completionTypeAsInt;

		// Token: 0x040000F0 RID: 240
		private bool _isRemainingDaysHidden;

		// Token: 0x040000F1 RID: 241
		private bool _isUpdated;

		// Token: 0x040000F2 RID: 242
		private bool _isSelected;

		// Token: 0x040000F3 RID: 243
		private bool _isCompleted;

		// Token: 0x040000F4 RID: 244
		private bool _isCompletedSuccessfully;

		// Token: 0x040000F5 RID: 245
		private bool _isTracked;

		// Token: 0x040000F6 RID: 246
		private bool _isTrackable;

		// Token: 0x040000F7 RID: 247
		private bool _isMainQuest;

		// Token: 0x040000F8 RID: 248
		private bool _isNavalQuest;

		// Token: 0x040000F9 RID: 249
		private HeroVM _questGiverHero;

		// Token: 0x040000FA RID: 250
		private bool _isQuestGiverHeroHidden;

		// Token: 0x040000FB RID: 251
		private MBBindingList<QuestStageVM> _stages;
	}
}
