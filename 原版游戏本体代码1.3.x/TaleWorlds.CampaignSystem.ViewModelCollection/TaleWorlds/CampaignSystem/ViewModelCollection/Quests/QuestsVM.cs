using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.CampaignSystem.ViewModelCollection.Input;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Quests
{
	// Token: 0x02000026 RID: 38
	public class QuestsVM : ViewModel
	{
		// Token: 0x06000257 RID: 599 RVA: 0x00013578 File Offset: 0x00011778
		public QuestsVM(Action closeQuestsScreen)
		{
			this._closeQuestsScreen = closeQuestsScreen;
			this.ActiveQuestsList = new MBBindingList<QuestItemVM>();
			this.OldQuestsList = new MBBindingList<QuestItemVM>();
			this.CurrentQuestStages = new MBBindingList<QuestStageVM>();
			this._viewDataTracker = Campaign.Current.GetCampaignBehavior<IViewDataTracker>();
			QuestBase questSelection = this._viewDataTracker.GetQuestSelection();
			foreach (QuestBase questBase in from Q in Campaign.Current.QuestManager.Quests
				where Q.IsOngoing
				select Q)
			{
				QuestItemVM questItemVM = new QuestItemVM(questBase, new Action<QuestItemVM>(this.SetSelectedItem));
				if (questSelection != null && questBase == questSelection)
				{
					this.SetSelectedItem(questItemVM);
				}
				this.ActiveQuestsList.Add(questItemVM);
			}
			foreach (KeyValuePair<Hero, IssueBase> keyValuePair in from i in Campaign.Current.IssueManager.Issues
				where i.Value.IsSolvingWithAlternative
				select i)
			{
				QuestItemVM item = new QuestItemVM(keyValuePair.Value, new Action<QuestItemVM>(this.SetSelectedItem));
				this.ActiveQuestsList.Add(item);
			}
			foreach (JournalLogEntry journalLogEntry in Campaign.Current.LogEntryHistory.GetGameActionLogs<JournalLogEntry>((JournalLogEntry JournalLogEntry) => true))
			{
				if (journalLogEntry.IsEnded())
				{
					QuestItemVM item2 = new QuestItemVM(journalLogEntry, new Action<QuestItemVM>(this.SetSelectedItem), journalLogEntry.IsEndedUnsuccessfully() ? QuestsVM.QuestCompletionType.UnSuccessful : QuestsVM.QuestCompletionType.Successful);
					this.OldQuestsList.Add(item2);
				}
			}
			Comparer<QuestItemVM> comparer = Comparer<QuestItemVM>.Create((QuestItemVM q1, QuestItemVM q2) => q1.IsMainQuest.CompareTo(q2.IsMainQuest));
			this.ActiveQuestsList.Sort(comparer);
			if (!this.OldQuestsList.Any((QuestItemVM q) => q.IsSelected))
			{
				if (!this.ActiveQuestsList.Any((QuestItemVM q) => q.IsSelected))
				{
					if (this.ActiveQuestsList.Count > 0)
					{
						this.SetSelectedItem(this.ActiveQuestsList.FirstOrDefault<QuestItemVM>());
					}
					else if (this.OldQuestsList.Count > 0)
					{
						this.SetSelectedItem(this.OldQuestsList.FirstOrDefault<QuestItemVM>());
					}
				}
			}
			this.IsThereAnyQuest = MathF.Max(this.ActiveQuestsList.Count, this.OldQuestsList.Count) > 0;
			List<TextObject> list = new List<TextObject>
			{
				new TextObject("{=7l0LGKRk}Date Started", null),
				new TextObject("{=Y8EcVL1c}Last Updated", null),
				new TextObject("{=BEXTcJaS}Time Due", null)
			};
			this.ActiveQuestsSortController = new QuestItemSortControllerVM(ref this._activeQuestsList);
			this.OldQuestsSortController = new QuestItemSortControllerVM(ref this._oldQuestsList);
			this.SortSelector = new SelectorVM<SelectorItemVM>(list, this._viewDataTracker.GetQuestSortTypeSelection(), new Action<SelectorVM<SelectorItemVM>>(this.OnSortOptionChanged));
			this.RefreshValues();
		}

		// Token: 0x06000258 RID: 600 RVA: 0x00013904 File Offset: 0x00011B04
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.QuestGiverText = GameTexts.FindText("str_quest_given_by", null).ToString();
			this.TimeRemainingLbl = GameTexts.FindText("str_time_remaining", null).ToString();
			this.QuestTitleText = GameTexts.FindText("str_quests", null).ToString();
			this.NoActiveQuestText = GameTexts.FindText("str_no_active_quest", null).ToString();
			this.SortQuestsText = GameTexts.FindText("str_sort_quests", null).ToString();
			this.OldQuestsHint = new HintViewModel(GameTexts.FindText("str_old_quests_explanation", null), null);
			this.DoneLbl = GameTexts.FindText("str_done", null).ToString();
			GameTexts.SetVariable("RANK", GameTexts.FindText("str_active_quests", null));
			GameTexts.SetVariable("NUMBER", this.ActiveQuestsList.Count);
			this.ActiveQuestsText = GameTexts.FindText("str_RANK_with_NUM_between_parenthesis", null).ToString();
			GameTexts.SetVariable("RANK", GameTexts.FindText("str_old_quests", null));
			GameTexts.SetVariable("NUMBER", this.OldQuestsList.Count);
			this.OldQuestsText = GameTexts.FindText("str_RANK_with_NUM_between_parenthesis", null).ToString();
			this.CurrentQuestStages.ApplyActionOnAllItems(delegate(QuestStageVM x)
			{
				x.RefreshValues();
			});
			this.ActiveQuestsList.ApplyActionOnAllItems(delegate(QuestItemVM x)
			{
				x.RefreshValues();
			});
			this.OldQuestsList.ApplyActionOnAllItems(delegate(QuestItemVM x)
			{
				x.RefreshValues();
			});
			QuestItemVM selectedQuest = this.SelectedQuest;
			if (selectedQuest != null)
			{
				selectedQuest.RefreshValues();
			}
			HeroVM currentQuestGiverHero = this.CurrentQuestGiverHero;
			if (currentQuestGiverHero == null)
			{
				return;
			}
			currentQuestGiverHero.RefreshValues();
		}

		// Token: 0x06000259 RID: 601 RVA: 0x00013AD4 File Offset: 0x00011CD4
		private void SetSelectedItem(QuestItemVM quest)
		{
			if (this.SelectedQuest != quest)
			{
				this.CurrentQuestStages.Clear();
				if (this.SelectedQuest != null)
				{
					this.SelectedQuest.IsSelected = false;
					foreach (QuestStageVM questStageVM in this.SelectedQuest.Stages)
					{
						questStageVM.UpdateIsNew();
					}
					this.SelectedQuest.UpdateIsUpdated();
				}
				this.SelectedQuest = quest;
				if (this.SelectedQuest != null)
				{
					this.SelectedQuest.IsSelected = true;
					this.CurrentQuestGiverHero = this.SelectedQuest.QuestGiverHero;
					this.CurrentQuestTitle = this.SelectedQuest.Name;
					this.IsCurrentQuestGiverHeroHidden = this.SelectedQuest.IsQuestGiverHeroHidden;
					foreach (QuestStageVM item in this.SelectedQuest.Stages)
					{
						this.CurrentQuestStages.Add(item);
					}
					foreach (QuestStageVM questStageVM2 in this.SelectedQuest.Stages)
					{
						questStageVM2.UpdateIsNew();
						this._viewDataTracker.OnQuestLogExamined(questStageVM2.Log);
					}
					this.SelectedQuest.IsUpdated = false;
				}
				else
				{
					this.CurrentQuestGiverHero = new HeroVM(null, false);
					this.CurrentQuestTitle = "";
					this.IsCurrentQuestGiverHeroHidden = true;
				}
			}
			this._viewDataTracker.SetQuestSelection(quest.Quest);
			this.TimeRemainingHint = new HintViewModel(new TextObject("{=2nN1QuxZ}This quest will be failed unless completed in this time.", null), null);
		}

		// Token: 0x0600025A RID: 602 RVA: 0x00013C98 File Offset: 0x00011E98
		public void ExecuteOpenQuestGiverEncyclopedia()
		{
			HeroVM currentQuestGiverHero = this.CurrentQuestGiverHero;
			if (currentQuestGiverHero == null)
			{
				return;
			}
			currentQuestGiverHero.ExecuteLink();
		}

		// Token: 0x0600025B RID: 603 RVA: 0x00013CAA File Offset: 0x00011EAA
		public void ExecuteClose()
		{
			this._closeQuestsScreen();
		}

		// Token: 0x0600025C RID: 604 RVA: 0x00013CB8 File Offset: 0x00011EB8
		public void SetSelectedIssue(IssueBase issue)
		{
			foreach (QuestItemVM questItemVM in this.ActiveQuestsList)
			{
				if (questItemVM.Issue == issue)
				{
					this.SetSelectedItem(questItemVM);
				}
			}
		}

		// Token: 0x0600025D RID: 605 RVA: 0x00013D10 File Offset: 0x00011F10
		public void SetSelectedQuest(QuestBase quest)
		{
			foreach (QuestItemVM questItemVM in this.ActiveQuestsList)
			{
				if (questItemVM.Quest == quest)
				{
					this.SetSelectedItem(questItemVM);
				}
			}
		}

		// Token: 0x0600025E RID: 606 RVA: 0x00013D68 File Offset: 0x00011F68
		public void SetSelectedLog(JournalLogEntry log)
		{
			foreach (QuestItemVM questItemVM in this.OldQuestsList)
			{
				if (questItemVM.QuestLogEntry == log)
				{
					this.SetSelectedItem(questItemVM);
				}
			}
		}

		// Token: 0x0600025F RID: 607 RVA: 0x00013DC0 File Offset: 0x00011FC0
		public override void OnFinalize()
		{
			base.OnFinalize();
			InputKeyItemVM doneInputKey = this.DoneInputKey;
			if (doneInputKey == null)
			{
				return;
			}
			doneInputKey.OnFinalize();
		}

		// Token: 0x06000260 RID: 608 RVA: 0x00013DD8 File Offset: 0x00011FD8
		private void OnSortOptionChanged(SelectorVM<SelectorItemVM> sortSelector)
		{
			this._viewDataTracker.SetQuestSortTypeSelection(sortSelector.SelectedIndex);
			this.ActiveQuestsSortController.SortByOption((QuestItemSortControllerVM.QuestItemSortOption)sortSelector.SelectedIndex);
			this.OldQuestsSortController.SortByOption((QuestItemSortControllerVM.QuestItemSortOption)sortSelector.SelectedIndex);
		}

		// Token: 0x06000261 RID: 609 RVA: 0x00013E0D File Offset: 0x0001200D
		public void SetDoneInputKey(HotKey hotKey)
		{
			this.DoneInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, true);
		}

		// Token: 0x1700007F RID: 127
		// (get) Token: 0x06000262 RID: 610 RVA: 0x00013E1C File Offset: 0x0001201C
		// (set) Token: 0x06000263 RID: 611 RVA: 0x00013E24 File Offset: 0x00012024
		[DataSourceProperty]
		public InputKeyItemVM DoneInputKey
		{
			get
			{
				return this._doneInputKey;
			}
			set
			{
				if (value != this._doneInputKey)
				{
					this._doneInputKey = value;
					base.OnPropertyChangedWithValue<InputKeyItemVM>(value, "DoneInputKey");
				}
			}
		}

		// Token: 0x17000080 RID: 128
		// (get) Token: 0x06000264 RID: 612 RVA: 0x00013E42 File Offset: 0x00012042
		// (set) Token: 0x06000265 RID: 613 RVA: 0x00013E4A File Offset: 0x0001204A
		[DataSourceProperty]
		public QuestItemVM SelectedQuest
		{
			get
			{
				return this._selectedQuest;
			}
			set
			{
				if (value != this._selectedQuest)
				{
					this._selectedQuest = value;
					base.OnPropertyChangedWithValue<QuestItemVM>(value, "SelectedQuest");
				}
			}
		}

		// Token: 0x17000081 RID: 129
		// (get) Token: 0x06000266 RID: 614 RVA: 0x00013E68 File Offset: 0x00012068
		// (set) Token: 0x06000267 RID: 615 RVA: 0x00013E70 File Offset: 0x00012070
		[DataSourceProperty]
		public MBBindingList<QuestItemVM> ActiveQuestsList
		{
			get
			{
				return this._activeQuestsList;
			}
			set
			{
				if (value != this._activeQuestsList)
				{
					this._activeQuestsList = value;
					base.OnPropertyChangedWithValue<MBBindingList<QuestItemVM>>(value, "ActiveQuestsList");
				}
			}
		}

		// Token: 0x17000082 RID: 130
		// (get) Token: 0x06000268 RID: 616 RVA: 0x00013E8E File Offset: 0x0001208E
		// (set) Token: 0x06000269 RID: 617 RVA: 0x00013E96 File Offset: 0x00012096
		[DataSourceProperty]
		public MBBindingList<QuestItemVM> OldQuestsList
		{
			get
			{
				return this._oldQuestsList;
			}
			set
			{
				if (value != this._oldQuestsList)
				{
					this._oldQuestsList = value;
					base.OnPropertyChangedWithValue<MBBindingList<QuestItemVM>>(value, "OldQuestsList");
				}
			}
		}

		// Token: 0x17000083 RID: 131
		// (get) Token: 0x0600026A RID: 618 RVA: 0x00013EB4 File Offset: 0x000120B4
		// (set) Token: 0x0600026B RID: 619 RVA: 0x00013EBC File Offset: 0x000120BC
		[DataSourceProperty]
		public HeroVM CurrentQuestGiverHero
		{
			get
			{
				return this._currentQuestGiverHero;
			}
			set
			{
				if (value != this._currentQuestGiverHero)
				{
					this._currentQuestGiverHero = value;
					base.OnPropertyChangedWithValue<HeroVM>(value, "CurrentQuestGiverHero");
				}
			}
		}

		// Token: 0x17000084 RID: 132
		// (get) Token: 0x0600026C RID: 620 RVA: 0x00013EDA File Offset: 0x000120DA
		// (set) Token: 0x0600026D RID: 621 RVA: 0x00013EE2 File Offset: 0x000120E2
		[DataSourceProperty]
		public string TimeRemainingLbl
		{
			get
			{
				return this._timeRemainingLbl;
			}
			set
			{
				if (value != this._timeRemainingLbl)
				{
					this._timeRemainingLbl = value;
					base.OnPropertyChangedWithValue<string>(value, "TimeRemainingLbl");
				}
			}
		}

		// Token: 0x17000085 RID: 133
		// (get) Token: 0x0600026E RID: 622 RVA: 0x00013F05 File Offset: 0x00012105
		// (set) Token: 0x0600026F RID: 623 RVA: 0x00013F0D File Offset: 0x0001210D
		[DataSourceProperty]
		public bool IsThereAnyQuest
		{
			get
			{
				return this._isThereAnyQuest;
			}
			set
			{
				if (value != this._isThereAnyQuest)
				{
					this._isThereAnyQuest = value;
					base.OnPropertyChangedWithValue(value, "IsThereAnyQuest");
				}
			}
		}

		// Token: 0x17000086 RID: 134
		// (get) Token: 0x06000270 RID: 624 RVA: 0x00013F2B File Offset: 0x0001212B
		// (set) Token: 0x06000271 RID: 625 RVA: 0x00013F33 File Offset: 0x00012133
		[DataSourceProperty]
		public string NoActiveQuestText
		{
			get
			{
				return this._noActiveQuestText;
			}
			set
			{
				if (value != this._noActiveQuestText)
				{
					this._noActiveQuestText = value;
					base.OnPropertyChangedWithValue<string>(value, "NoActiveQuestText");
				}
			}
		}

		// Token: 0x17000087 RID: 135
		// (get) Token: 0x06000272 RID: 626 RVA: 0x00013F56 File Offset: 0x00012156
		// (set) Token: 0x06000273 RID: 627 RVA: 0x00013F5E File Offset: 0x0001215E
		[DataSourceProperty]
		public string SortQuestsText
		{
			get
			{
				return this._sortQuestsText;
			}
			set
			{
				if (value != this._sortQuestsText)
				{
					this._sortQuestsText = value;
					base.OnPropertyChangedWithValue<string>(value, "SortQuestsText");
				}
			}
		}

		// Token: 0x17000088 RID: 136
		// (get) Token: 0x06000274 RID: 628 RVA: 0x00013F81 File Offset: 0x00012181
		// (set) Token: 0x06000275 RID: 629 RVA: 0x00013F89 File Offset: 0x00012189
		[DataSourceProperty]
		public string QuestGiverText
		{
			get
			{
				return this._questGiverText;
			}
			set
			{
				if (value != this._questGiverText)
				{
					this._questGiverText = value;
					base.OnPropertyChangedWithValue<string>(value, "QuestGiverText");
				}
			}
		}

		// Token: 0x17000089 RID: 137
		// (get) Token: 0x06000276 RID: 630 RVA: 0x00013FAC File Offset: 0x000121AC
		// (set) Token: 0x06000277 RID: 631 RVA: 0x00013FB4 File Offset: 0x000121B4
		[DataSourceProperty]
		public string QuestTitleText
		{
			get
			{
				return this._questTitleText;
			}
			set
			{
				if (value != this._questTitleText)
				{
					this._questTitleText = value;
					base.OnPropertyChangedWithValue<string>(value, "QuestTitleText");
				}
			}
		}

		// Token: 0x1700008A RID: 138
		// (get) Token: 0x06000278 RID: 632 RVA: 0x00013FD7 File Offset: 0x000121D7
		// (set) Token: 0x06000279 RID: 633 RVA: 0x00013FDF File Offset: 0x000121DF
		[DataSourceProperty]
		public string OldQuestsText
		{
			get
			{
				return this._oldQuestsText;
			}
			set
			{
				if (value != this._oldQuestsText)
				{
					this._oldQuestsText = value;
					base.OnPropertyChangedWithValue<string>(value, "OldQuestsText");
				}
			}
		}

		// Token: 0x1700008B RID: 139
		// (get) Token: 0x0600027A RID: 634 RVA: 0x00014002 File Offset: 0x00012202
		// (set) Token: 0x0600027B RID: 635 RVA: 0x0001400A File Offset: 0x0001220A
		[DataSourceProperty]
		public string ActiveQuestsText
		{
			get
			{
				return this._activeQuestsText;
			}
			set
			{
				if (value != this._activeQuestsText)
				{
					this._activeQuestsText = value;
					base.OnPropertyChangedWithValue<string>(value, "ActiveQuestsText");
				}
			}
		}

		// Token: 0x1700008C RID: 140
		// (get) Token: 0x0600027C RID: 636 RVA: 0x0001402D File Offset: 0x0001222D
		// (set) Token: 0x0600027D RID: 637 RVA: 0x00014035 File Offset: 0x00012235
		[DataSourceProperty]
		public string DoneLbl
		{
			get
			{
				return this._doneLbl;
			}
			set
			{
				if (value != this._doneLbl)
				{
					this._doneLbl = value;
					base.OnPropertyChangedWithValue<string>(value, "DoneLbl");
				}
			}
		}

		// Token: 0x1700008D RID: 141
		// (get) Token: 0x0600027E RID: 638 RVA: 0x00014058 File Offset: 0x00012258
		// (set) Token: 0x0600027F RID: 639 RVA: 0x00014060 File Offset: 0x00012260
		[DataSourceProperty]
		public string CurrentQuestTitle
		{
			get
			{
				return this._currentQuestTitle;
			}
			set
			{
				if (value != this._currentQuestTitle)
				{
					this._currentQuestTitle = value;
					base.OnPropertyChangedWithValue<string>(value, "CurrentQuestTitle");
				}
			}
		}

		// Token: 0x1700008E RID: 142
		// (get) Token: 0x06000280 RID: 640 RVA: 0x00014083 File Offset: 0x00012283
		// (set) Token: 0x06000281 RID: 641 RVA: 0x0001408B File Offset: 0x0001228B
		[DataSourceProperty]
		public bool IsCurrentQuestGiverHeroHidden
		{
			get
			{
				return this._isCurrentQuestGiverHeroHidden;
			}
			set
			{
				if (value != this._isCurrentQuestGiverHeroHidden)
				{
					this._isCurrentQuestGiverHeroHidden = value;
					base.OnPropertyChangedWithValue(value, "IsCurrentQuestGiverHeroHidden");
				}
			}
		}

		// Token: 0x1700008F RID: 143
		// (get) Token: 0x06000282 RID: 642 RVA: 0x000140A9 File Offset: 0x000122A9
		// (set) Token: 0x06000283 RID: 643 RVA: 0x000140B1 File Offset: 0x000122B1
		[DataSourceProperty]
		public MBBindingList<QuestStageVM> CurrentQuestStages
		{
			get
			{
				return this._currentQuestStages;
			}
			set
			{
				if (value != this._currentQuestStages)
				{
					this._currentQuestStages = value;
					base.OnPropertyChangedWithValue<MBBindingList<QuestStageVM>>(value, "CurrentQuestStages");
				}
			}
		}

		// Token: 0x17000090 RID: 144
		// (get) Token: 0x06000284 RID: 644 RVA: 0x000140CF File Offset: 0x000122CF
		// (set) Token: 0x06000285 RID: 645 RVA: 0x000140D7 File Offset: 0x000122D7
		[DataSourceProperty]
		public HintViewModel TimeRemainingHint
		{
			get
			{
				return this._timeRemainingHint;
			}
			set
			{
				if (value != this._timeRemainingHint)
				{
					this._timeRemainingHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "TimeRemainingHint");
				}
			}
		}

		// Token: 0x17000091 RID: 145
		// (get) Token: 0x06000286 RID: 646 RVA: 0x000140F5 File Offset: 0x000122F5
		// (set) Token: 0x06000287 RID: 647 RVA: 0x000140FD File Offset: 0x000122FD
		[DataSourceProperty]
		public HintViewModel OldQuestsHint
		{
			get
			{
				return this._oldQuestsHint;
			}
			set
			{
				if (value != this._oldQuestsHint)
				{
					this._oldQuestsHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "OldQuestsHint");
				}
			}
		}

		// Token: 0x17000092 RID: 146
		// (get) Token: 0x06000288 RID: 648 RVA: 0x0001411B File Offset: 0x0001231B
		// (set) Token: 0x06000289 RID: 649 RVA: 0x00014123 File Offset: 0x00012323
		[DataSourceProperty]
		public QuestItemSortControllerVM ActiveQuestsSortController
		{
			get
			{
				return this._activeQuestsSortController;
			}
			set
			{
				if (value != this._activeQuestsSortController)
				{
					this._activeQuestsSortController = value;
					base.OnPropertyChangedWithValue<QuestItemSortControllerVM>(value, "ActiveQuestsSortController");
				}
			}
		}

		// Token: 0x17000093 RID: 147
		// (get) Token: 0x0600028A RID: 650 RVA: 0x00014141 File Offset: 0x00012341
		// (set) Token: 0x0600028B RID: 651 RVA: 0x00014149 File Offset: 0x00012349
		[DataSourceProperty]
		public QuestItemSortControllerVM OldQuestsSortController
		{
			get
			{
				return this._oldQuestsSortController;
			}
			set
			{
				if (value != this._oldQuestsSortController)
				{
					this._oldQuestsSortController = value;
					base.OnPropertyChangedWithValue<QuestItemSortControllerVM>(value, "OldQuestsSortController");
				}
			}
		}

		// Token: 0x17000094 RID: 148
		// (get) Token: 0x0600028C RID: 652 RVA: 0x00014167 File Offset: 0x00012367
		// (set) Token: 0x0600028D RID: 653 RVA: 0x0001416F File Offset: 0x0001236F
		[DataSourceProperty]
		public SelectorVM<SelectorItemVM> SortSelector
		{
			get
			{
				return this._sortSelector;
			}
			set
			{
				if (value != this._sortSelector)
				{
					this._sortSelector = value;
					base.OnPropertyChangedWithValue<SelectorVM<SelectorItemVM>>(value, "SortSelector");
				}
			}
		}

		// Token: 0x04000112 RID: 274
		private readonly Action _closeQuestsScreen;

		// Token: 0x04000113 RID: 275
		private readonly IViewDataTracker _viewDataTracker;

		// Token: 0x04000114 RID: 276
		private InputKeyItemVM _doneInputKey;

		// Token: 0x04000115 RID: 277
		private MBBindingList<QuestItemVM> _activeQuestsList;

		// Token: 0x04000116 RID: 278
		private MBBindingList<QuestItemVM> _oldQuestsList;

		// Token: 0x04000117 RID: 279
		private QuestItemVM _selectedQuest;

		// Token: 0x04000118 RID: 280
		private HeroVM _currentQuestGiverHero;

		// Token: 0x04000119 RID: 281
		private string _activeQuestsText;

		// Token: 0x0400011A RID: 282
		private string _oldQuestsText;

		// Token: 0x0400011B RID: 283
		private string _timeRemainingLbl;

		// Token: 0x0400011C RID: 284
		private string _currentQuestTitle;

		// Token: 0x0400011D RID: 285
		private bool _isCurrentQuestGiverHeroHidden;

		// Token: 0x0400011E RID: 286
		private string _questGiverText;

		// Token: 0x0400011F RID: 287
		private string _questTitleText;

		// Token: 0x04000120 RID: 288
		private string _doneLbl;

		// Token: 0x04000121 RID: 289
		private string _noActiveQuestText;

		// Token: 0x04000122 RID: 290
		private string _sortQuestsText;

		// Token: 0x04000123 RID: 291
		private bool _isThereAnyQuest;

		// Token: 0x04000124 RID: 292
		private MBBindingList<QuestStageVM> _currentQuestStages;

		// Token: 0x04000125 RID: 293
		private HintViewModel _timeRemainingHint;

		// Token: 0x04000126 RID: 294
		private HintViewModel _oldQuestsHint;

		// Token: 0x04000127 RID: 295
		private QuestItemSortControllerVM _activeQuestsSortController;

		// Token: 0x04000128 RID: 296
		private QuestItemSortControllerVM _oldQuestsSortController;

		// Token: 0x04000129 RID: 297
		private SelectorVM<SelectorItemVM> _sortSelector;

		// Token: 0x02000192 RID: 402
		public enum QuestCompletionType
		{
			// Token: 0x04001073 RID: 4211
			Active,
			// Token: 0x04001074 RID: 4212
			Successful,
			// Token: 0x04001075 RID: 4213
			UnSuccessful
		}
	}
}
