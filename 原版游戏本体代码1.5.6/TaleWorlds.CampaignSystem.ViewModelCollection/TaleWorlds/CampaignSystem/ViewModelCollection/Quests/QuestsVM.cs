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

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Quests;

public class QuestsVM : ViewModel
{
	public enum QuestCompletionType
	{
		Active,
		Successful,
		UnSuccessful
	}

	private readonly Action _closeQuestsScreen;

	private readonly IViewDataTracker _viewDataTracker;

	private InputKeyItemVM _doneInputKey;

	private MBBindingList<QuestItemVM> _activeQuestsList;

	private MBBindingList<QuestItemVM> _oldQuestsList;

	private QuestItemVM _selectedQuest;

	private HeroVM _currentQuestGiverHero;

	private string _activeQuestsText;

	private string _oldQuestsText;

	private string _timeRemainingLbl;

	private string _currentQuestTitle;

	private bool _isCurrentQuestGiverHeroHidden;

	private string _questGiverText;

	private string _questTitleText;

	private string _doneLbl;

	private string _noActiveQuestText;

	private string _sortQuestsText;

	private bool _isThereAnyQuest;

	private MBBindingList<QuestStageVM> _currentQuestStages;

	private HintViewModel _timeRemainingHint;

	private HintViewModel _oldQuestsHint;

	private QuestItemSortControllerVM _activeQuestsSortController;

	private QuestItemSortControllerVM _oldQuestsSortController;

	private SelectorVM<SelectorItemVM> _sortSelector;

	[DataSourceProperty]
	public InputKeyItemVM DoneInputKey
	{
		get
		{
			return _doneInputKey;
		}
		set
		{
			if (value != _doneInputKey)
			{
				_doneInputKey = value;
				OnPropertyChangedWithValue(value, "DoneInputKey");
			}
		}
	}

	[DataSourceProperty]
	public QuestItemVM SelectedQuest
	{
		get
		{
			return _selectedQuest;
		}
		set
		{
			if (value != _selectedQuest)
			{
				_selectedQuest = value;
				OnPropertyChangedWithValue(value, "SelectedQuest");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<QuestItemVM> ActiveQuestsList
	{
		get
		{
			return _activeQuestsList;
		}
		set
		{
			if (value != _activeQuestsList)
			{
				_activeQuestsList = value;
				OnPropertyChangedWithValue(value, "ActiveQuestsList");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<QuestItemVM> OldQuestsList
	{
		get
		{
			return _oldQuestsList;
		}
		set
		{
			if (value != _oldQuestsList)
			{
				_oldQuestsList = value;
				OnPropertyChangedWithValue(value, "OldQuestsList");
			}
		}
	}

	[DataSourceProperty]
	public HeroVM CurrentQuestGiverHero
	{
		get
		{
			return _currentQuestGiverHero;
		}
		set
		{
			if (value != _currentQuestGiverHero)
			{
				_currentQuestGiverHero = value;
				OnPropertyChangedWithValue(value, "CurrentQuestGiverHero");
			}
		}
	}

	[DataSourceProperty]
	public string TimeRemainingLbl
	{
		get
		{
			return _timeRemainingLbl;
		}
		set
		{
			if (value != _timeRemainingLbl)
			{
				_timeRemainingLbl = value;
				OnPropertyChangedWithValue(value, "TimeRemainingLbl");
			}
		}
	}

	[DataSourceProperty]
	public bool IsThereAnyQuest
	{
		get
		{
			return _isThereAnyQuest;
		}
		set
		{
			if (value != _isThereAnyQuest)
			{
				_isThereAnyQuest = value;
				OnPropertyChangedWithValue(value, "IsThereAnyQuest");
			}
		}
	}

	[DataSourceProperty]
	public string NoActiveQuestText
	{
		get
		{
			return _noActiveQuestText;
		}
		set
		{
			if (value != _noActiveQuestText)
			{
				_noActiveQuestText = value;
				OnPropertyChangedWithValue(value, "NoActiveQuestText");
			}
		}
	}

	[DataSourceProperty]
	public string SortQuestsText
	{
		get
		{
			return _sortQuestsText;
		}
		set
		{
			if (value != _sortQuestsText)
			{
				_sortQuestsText = value;
				OnPropertyChangedWithValue(value, "SortQuestsText");
			}
		}
	}

	[DataSourceProperty]
	public string QuestGiverText
	{
		get
		{
			return _questGiverText;
		}
		set
		{
			if (value != _questGiverText)
			{
				_questGiverText = value;
				OnPropertyChangedWithValue(value, "QuestGiverText");
			}
		}
	}

	[DataSourceProperty]
	public string QuestTitleText
	{
		get
		{
			return _questTitleText;
		}
		set
		{
			if (value != _questTitleText)
			{
				_questTitleText = value;
				OnPropertyChangedWithValue(value, "QuestTitleText");
			}
		}
	}

	[DataSourceProperty]
	public string OldQuestsText
	{
		get
		{
			return _oldQuestsText;
		}
		set
		{
			if (value != _oldQuestsText)
			{
				_oldQuestsText = value;
				OnPropertyChangedWithValue(value, "OldQuestsText");
			}
		}
	}

	[DataSourceProperty]
	public string ActiveQuestsText
	{
		get
		{
			return _activeQuestsText;
		}
		set
		{
			if (value != _activeQuestsText)
			{
				_activeQuestsText = value;
				OnPropertyChangedWithValue(value, "ActiveQuestsText");
			}
		}
	}

	[DataSourceProperty]
	public string DoneLbl
	{
		get
		{
			return _doneLbl;
		}
		set
		{
			if (value != _doneLbl)
			{
				_doneLbl = value;
				OnPropertyChangedWithValue(value, "DoneLbl");
			}
		}
	}

	[DataSourceProperty]
	public string CurrentQuestTitle
	{
		get
		{
			return _currentQuestTitle;
		}
		set
		{
			if (value != _currentQuestTitle)
			{
				_currentQuestTitle = value;
				OnPropertyChangedWithValue(value, "CurrentQuestTitle");
			}
		}
	}

	[DataSourceProperty]
	public bool IsCurrentQuestGiverHeroHidden
	{
		get
		{
			return _isCurrentQuestGiverHeroHidden;
		}
		set
		{
			if (value != _isCurrentQuestGiverHeroHidden)
			{
				_isCurrentQuestGiverHeroHidden = value;
				OnPropertyChangedWithValue(value, "IsCurrentQuestGiverHeroHidden");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<QuestStageVM> CurrentQuestStages
	{
		get
		{
			return _currentQuestStages;
		}
		set
		{
			if (value != _currentQuestStages)
			{
				_currentQuestStages = value;
				OnPropertyChangedWithValue(value, "CurrentQuestStages");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel TimeRemainingHint
	{
		get
		{
			return _timeRemainingHint;
		}
		set
		{
			if (value != _timeRemainingHint)
			{
				_timeRemainingHint = value;
				OnPropertyChangedWithValue(value, "TimeRemainingHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel OldQuestsHint
	{
		get
		{
			return _oldQuestsHint;
		}
		set
		{
			if (value != _oldQuestsHint)
			{
				_oldQuestsHint = value;
				OnPropertyChangedWithValue(value, "OldQuestsHint");
			}
		}
	}

	[DataSourceProperty]
	public QuestItemSortControllerVM ActiveQuestsSortController
	{
		get
		{
			return _activeQuestsSortController;
		}
		set
		{
			if (value != _activeQuestsSortController)
			{
				_activeQuestsSortController = value;
				OnPropertyChangedWithValue(value, "ActiveQuestsSortController");
			}
		}
	}

	[DataSourceProperty]
	public QuestItemSortControllerVM OldQuestsSortController
	{
		get
		{
			return _oldQuestsSortController;
		}
		set
		{
			if (value != _oldQuestsSortController)
			{
				_oldQuestsSortController = value;
				OnPropertyChangedWithValue(value, "OldQuestsSortController");
			}
		}
	}

	[DataSourceProperty]
	public SelectorVM<SelectorItemVM> SortSelector
	{
		get
		{
			return _sortSelector;
		}
		set
		{
			if (value != _sortSelector)
			{
				_sortSelector = value;
				OnPropertyChangedWithValue(value, "SortSelector");
			}
		}
	}

	public QuestsVM(Action closeQuestsScreen)
	{
		_closeQuestsScreen = closeQuestsScreen;
		ActiveQuestsList = new MBBindingList<QuestItemVM>();
		OldQuestsList = new MBBindingList<QuestItemVM>();
		CurrentQuestStages = new MBBindingList<QuestStageVM>();
		_viewDataTracker = Campaign.Current.GetCampaignBehavior<IViewDataTracker>();
		QuestBase questSelection = _viewDataTracker.GetQuestSelection();
		foreach (QuestBase item3 in Campaign.Current.QuestManager.Quests.Where((QuestBase Q) => Q.IsOngoing))
		{
			QuestItemVM questItemVM = new QuestItemVM(item3, SetSelectedItem);
			if (questSelection != null && item3 == questSelection)
			{
				SetSelectedItem(questItemVM);
			}
			ActiveQuestsList.Add(questItemVM);
		}
		foreach (KeyValuePair<Hero, IssueBase> item4 in Campaign.Current.IssueManager.Issues.Where((KeyValuePair<Hero, IssueBase> i) => i.Value.IsSolvingWithAlternative))
		{
			QuestItemVM item = new QuestItemVM(item4.Value, SetSelectedItem);
			ActiveQuestsList.Add(item);
		}
		foreach (JournalLogEntry gameActionLog in Campaign.Current.LogEntryHistory.GetGameActionLogs((JournalLogEntry JournalLogEntry) => true))
		{
			if (gameActionLog.IsEnded())
			{
				QuestItemVM item2 = new QuestItemVM(gameActionLog, SetSelectedItem, (!gameActionLog.IsEndedUnsuccessfully()) ? QuestCompletionType.Successful : QuestCompletionType.UnSuccessful);
				OldQuestsList.Add(item2);
			}
		}
		Comparer<QuestItemVM> comparer = Comparer<QuestItemVM>.Create((QuestItemVM q1, QuestItemVM q2) => q1.IsMainQuest.CompareTo(q2.IsMainQuest));
		ActiveQuestsList.Sort(comparer);
		if (!OldQuestsList.Any((QuestItemVM q) => q.IsSelected) && !ActiveQuestsList.Any((QuestItemVM q) => q.IsSelected))
		{
			if (ActiveQuestsList.Count > 0)
			{
				SetSelectedItem(ActiveQuestsList.FirstOrDefault());
			}
			else if (OldQuestsList.Count > 0)
			{
				SetSelectedItem(OldQuestsList.FirstOrDefault());
			}
		}
		IsThereAnyQuest = TaleWorlds.Library.MathF.Max(ActiveQuestsList.Count, OldQuestsList.Count) > 0;
		List<TextObject> list = new List<TextObject>
		{
			new TextObject("{=7l0LGKRk}Date Started"),
			new TextObject("{=Y8EcVL1c}Last Updated"),
			new TextObject("{=BEXTcJaS}Time Due")
		};
		ActiveQuestsSortController = new QuestItemSortControllerVM(ref _activeQuestsList);
		OldQuestsSortController = new QuestItemSortControllerVM(ref _oldQuestsList);
		SortSelector = new SelectorVM<SelectorItemVM>(list, _viewDataTracker.GetQuestSortTypeSelection(), OnSortOptionChanged);
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		QuestGiverText = GameTexts.FindText("str_quest_given_by").ToString();
		TimeRemainingLbl = GameTexts.FindText("str_time_remaining").ToString();
		QuestTitleText = GameTexts.FindText("str_quests").ToString();
		NoActiveQuestText = GameTexts.FindText("str_no_active_quest").ToString();
		SortQuestsText = GameTexts.FindText("str_sort_quests").ToString();
		OldQuestsHint = new HintViewModel(GameTexts.FindText("str_old_quests_explanation"));
		DoneLbl = GameTexts.FindText("str_done").ToString();
		GameTexts.SetVariable("RANK", GameTexts.FindText("str_active_quests"));
		GameTexts.SetVariable("NUMBER", ActiveQuestsList.Count);
		ActiveQuestsText = GameTexts.FindText("str_RANK_with_NUM_between_parenthesis").ToString();
		GameTexts.SetVariable("RANK", GameTexts.FindText("str_old_quests"));
		GameTexts.SetVariable("NUMBER", OldQuestsList.Count);
		OldQuestsText = GameTexts.FindText("str_RANK_with_NUM_between_parenthesis").ToString();
		CurrentQuestStages.ApplyActionOnAllItems(delegate(QuestStageVM x)
		{
			x.RefreshValues();
		});
		ActiveQuestsList.ApplyActionOnAllItems(delegate(QuestItemVM x)
		{
			x.RefreshValues();
		});
		OldQuestsList.ApplyActionOnAllItems(delegate(QuestItemVM x)
		{
			x.RefreshValues();
		});
		SelectedQuest?.RefreshValues();
		CurrentQuestGiverHero?.RefreshValues();
	}

	private void SetSelectedItem(QuestItemVM quest)
	{
		if (SelectedQuest != quest)
		{
			CurrentQuestStages.Clear();
			if (SelectedQuest != null)
			{
				SelectedQuest.IsSelected = false;
				foreach (QuestStageVM stage in SelectedQuest.Stages)
				{
					stage.UpdateIsNew();
				}
				SelectedQuest.UpdateIsUpdated();
			}
			SelectedQuest = quest;
			if (SelectedQuest != null)
			{
				SelectedQuest.IsSelected = true;
				CurrentQuestGiverHero = SelectedQuest.QuestGiverHero;
				CurrentQuestTitle = SelectedQuest.Name;
				IsCurrentQuestGiverHeroHidden = SelectedQuest.IsQuestGiverHeroHidden;
				foreach (QuestStageVM stage2 in SelectedQuest.Stages)
				{
					CurrentQuestStages.Add(stage2);
				}
				foreach (QuestStageVM stage3 in SelectedQuest.Stages)
				{
					stage3.UpdateIsNew();
					_viewDataTracker.OnQuestLogExamined(stage3.Log);
				}
				SelectedQuest.IsUpdated = false;
			}
			else
			{
				CurrentQuestGiverHero = new HeroVM(null);
				CurrentQuestTitle = "";
				IsCurrentQuestGiverHeroHidden = true;
			}
		}
		_viewDataTracker.SetQuestSelection(quest.Quest);
		TimeRemainingHint = new HintViewModel(new TextObject("{=2nN1QuxZ}This quest will be failed unless completed in this time."));
	}

	public void ExecuteOpenQuestGiverEncyclopedia()
	{
		CurrentQuestGiverHero?.ExecuteLink();
	}

	public void ExecuteClose()
	{
		_closeQuestsScreen();
	}

	public void SetSelectedIssue(IssueBase issue)
	{
		foreach (QuestItemVM activeQuests in ActiveQuestsList)
		{
			if (activeQuests.Issue == issue)
			{
				SetSelectedItem(activeQuests);
			}
		}
	}

	public void SetSelectedQuest(QuestBase quest)
	{
		foreach (QuestItemVM activeQuests in ActiveQuestsList)
		{
			if (activeQuests.Quest == quest)
			{
				SetSelectedItem(activeQuests);
			}
		}
	}

	public void SetSelectedLog(JournalLogEntry log)
	{
		foreach (QuestItemVM oldQuests in OldQuestsList)
		{
			if (oldQuests.QuestLogEntry == log)
			{
				SetSelectedItem(oldQuests);
			}
		}
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		DoneInputKey?.OnFinalize();
	}

	private void OnSortOptionChanged(SelectorVM<SelectorItemVM> sortSelector)
	{
		_viewDataTracker.SetQuestSortTypeSelection(sortSelector.SelectedIndex);
		ActiveQuestsSortController.SortByOption((QuestItemSortControllerVM.QuestItemSortOption)sortSelector.SelectedIndex);
		OldQuestsSortController.SortByOption((QuestItemSortControllerVM.QuestItemSortOption)sortSelector.SelectedIndex);
	}

	public void SetDoneInputKey(HotKey hotKey)
	{
		DoneInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}
}
