using System;
using System.Linq;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Quests;

public class QuestItemVM : ViewModel
{
	private readonly Action<QuestItemVM> _onSelection;

	private QuestsVM.QuestCompletionType _completionType;

	private string _name;

	private string _remainingDaysText;

	private string _remainingDaysTextCombined;

	private int _remainingDays;

	private int _completionTypeAsInt;

	private bool _isRemainingDaysHidden;

	private bool _isUpdated;

	private bool _isSelected;

	private bool _isCompleted;

	private bool _isCompletedSuccessfully;

	private bool _isTracked;

	private bool _isTrackable;

	private bool _isMainQuest;

	private bool _isNavalQuest;

	private HeroVM _questGiverHero;

	private bool _isQuestGiverHeroHidden;

	private MBBindingList<QuestStageVM> _stages;

	public QuestBase Quest { get; }

	public IssueBase Issue { get; }

	public JournalLogEntry QuestLogEntry { get; }

	[DataSourceProperty]
	public string Name
	{
		get
		{
			return _name;
		}
		set
		{
			if (value != _name)
			{
				_name = value;
				OnPropertyChangedWithValue(value, "Name");
			}
		}
	}

	[DataSourceProperty]
	public int CompletionTypeAsInt
	{
		get
		{
			return _completionTypeAsInt;
		}
		set
		{
			if (value != _completionTypeAsInt)
			{
				_completionTypeAsInt = value;
				OnPropertyChangedWithValue(value, "CompletionTypeAsInt");
			}
		}
	}

	[DataSourceProperty]
	public bool IsMainQuest
	{
		get
		{
			return _isMainQuest;
		}
		set
		{
			if (value != _isMainQuest)
			{
				_isMainQuest = value;
				OnPropertyChangedWithValue(value, "IsMainQuest");
			}
		}
	}

	[DataSourceProperty]
	public bool IsNavalQuest
	{
		get
		{
			return _isNavalQuest;
		}
		set
		{
			if (value != _isNavalQuest)
			{
				_isNavalQuest = value;
				OnPropertyChangedWithValue(value, "IsNavalQuest");
			}
		}
	}

	[DataSourceProperty]
	public bool IsCompletedSuccessfully
	{
		get
		{
			return _isCompletedSuccessfully;
		}
		set
		{
			if (value != _isCompletedSuccessfully)
			{
				_isCompletedSuccessfully = value;
				OnPropertyChangedWithValue(value, "IsCompletedSuccessfully");
			}
		}
	}

	[DataSourceProperty]
	public bool IsCompleted
	{
		get
		{
			return _isCompleted;
		}
		set
		{
			if (value != _isCompleted)
			{
				_isCompleted = value;
				OnPropertyChangedWithValue(value, "IsCompleted");
			}
		}
	}

	[DataSourceProperty]
	public bool IsUpdated
	{
		get
		{
			return _isUpdated;
		}
		set
		{
			if (value != _isUpdated)
			{
				_isUpdated = value;
				OnPropertyChangedWithValue(value, "IsUpdated");
			}
		}
	}

	[DataSourceProperty]
	public bool IsSelected
	{
		get
		{
			return _isSelected;
		}
		set
		{
			if (value != _isSelected)
			{
				_isSelected = value;
				OnPropertyChangedWithValue(value, "IsSelected");
			}
		}
	}

	[DataSourceProperty]
	public bool IsRemainingDaysHidden
	{
		get
		{
			return _isRemainingDaysHidden;
		}
		set
		{
			if (value != _isRemainingDaysHidden)
			{
				_isRemainingDaysHidden = value;
				OnPropertyChangedWithValue(value, "IsRemainingDaysHidden");
			}
		}
	}

	[DataSourceProperty]
	public bool IsTracked
	{
		get
		{
			return _isTracked;
		}
		set
		{
			if (value != _isTracked)
			{
				_isTracked = value;
				OnPropertyChangedWithValue(value, "IsTracked");
			}
		}
	}

	[DataSourceProperty]
	public bool IsTrackable
	{
		get
		{
			return _isTrackable;
		}
		set
		{
			if (value != _isTrackable)
			{
				_isTrackable = value;
				OnPropertyChangedWithValue(value, "IsTrackable");
			}
		}
	}

	[DataSourceProperty]
	public string RemainingDaysText
	{
		get
		{
			return _remainingDaysText;
		}
		set
		{
			if (value != _remainingDaysText)
			{
				_remainingDaysText = value;
				OnPropertyChangedWithValue(value, "RemainingDaysText");
			}
		}
	}

	[DataSourceProperty]
	public string RemainingDaysTextCombined
	{
		get
		{
			return _remainingDaysTextCombined;
		}
		set
		{
			if (value != _remainingDaysTextCombined)
			{
				_remainingDaysTextCombined = value;
				OnPropertyChangedWithValue(value, "RemainingDaysTextCombined");
			}
		}
	}

	[DataSourceProperty]
	public int RemainingDays
	{
		get
		{
			return _remainingDays;
		}
		set
		{
			if (value != _remainingDays)
			{
				_remainingDays = value;
				OnPropertyChangedWithValue(value, "RemainingDays");
			}
		}
	}

	[DataSourceProperty]
	public HeroVM QuestGiverHero
	{
		get
		{
			return _questGiverHero;
		}
		set
		{
			if (value != _questGiverHero)
			{
				_questGiverHero = value;
				OnPropertyChangedWithValue(value, "QuestGiverHero");
			}
		}
	}

	[DataSourceProperty]
	public bool IsQuestGiverHeroHidden
	{
		get
		{
			return _isQuestGiverHeroHidden;
		}
		set
		{
			if (value != _isQuestGiverHeroHidden)
			{
				_isQuestGiverHeroHidden = value;
				OnPropertyChangedWithValue(value, "IsQuestGiverHeroHidden");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<QuestStageVM> Stages
	{
		get
		{
			return _stages;
		}
		set
		{
			if (value != _stages)
			{
				_stages = value;
				OnPropertyChangedWithValue(value, "Stages");
			}
		}
	}

	public QuestItemVM(JournalLogEntry questLogEntry, Action<QuestItemVM> onSelection, QuestsVM.QuestCompletionType completion)
	{
		_onSelection = onSelection;
		QuestLogEntry = questLogEntry;
		Stages = new MBBindingList<QuestStageVM>();
		_completionType = completion;
		IsCompleted = _completionType != QuestsVM.QuestCompletionType.Active;
		IsCompletedSuccessfully = _completionType == QuestsVM.QuestCompletionType.Successful;
		CompletionTypeAsInt = (int)_completionType;
		IsRemainingDaysHidden = IsCompleted || Quest?.QuestDueTime == CampaignTime.Never;
		IsQuestGiverHeroHidden = false;
		IsMainQuest = questLogEntry.IsSpecial;
		foreach (JournalLog entry in questLogEntry.GetEntries())
		{
			PopulateQuestLog(entry, isLastStage: false);
		}
		Name = questLogEntry.Title.ToString();
		QuestGiverHero = new HeroVM(questLogEntry.RelatedHero);
		UpdateIsUpdated();
		IsTracked = false;
		IsTrackable = false;
		RefreshValues();
	}

	public QuestItemVM(QuestBase quest, Action<QuestItemVM> onSelection)
	{
		Quest = quest;
		_onSelection = onSelection;
		Stages = new MBBindingList<QuestStageVM>();
		CompletionTypeAsInt = 0;
		IsRemainingDaysHidden = !Quest.IsOngoing || Quest.IsRemainingTimeHidden;
		IsQuestGiverHeroHidden = Quest.QuestGiver == null;
		MBReadOnlyList<JournalLog> journalEntries = Quest.JournalEntries;
		for (int i = 0; i < journalEntries.Count; i++)
		{
			bool isLastStage = i == journalEntries.Count - 1;
			JournalLog log = journalEntries[i];
			PopulateQuestLog(log, isLastStage);
		}
		IsMainQuest = quest.IsSpecialQuest;
		if (!IsQuestGiverHeroHidden)
		{
			QuestGiverHero = new HeroVM(Quest.QuestGiver);
		}
		UpdateIsUpdated();
		IsTrackable = !Quest.IsFinalized;
		IsTracked = Quest.IsTrackEnabled;
		RefreshValues();
	}

	public QuestItemVM(IssueBase issue, Action<QuestItemVM> onSelection)
	{
		Issue = issue;
		_onSelection = onSelection;
		Stages = new MBBindingList<QuestStageVM>();
		IsCompleted = false;
		CompletionTypeAsInt = 0;
		IsRemainingDaysHidden = Issue.IsOngoingWithoutQuest;
		IsQuestGiverHeroHidden = false;
		UpdateRemainingTime(Issue.IssueDueTime);
		foreach (JournalLog journalEntry in issue.JournalEntries)
		{
			PopulateQuestLog(journalEntry, isLastStage: false);
		}
		Name = issue.Title.ToString();
		QuestGiverHero = new HeroVM(issue.IssueOwner);
		UpdateIsUpdated();
		IsTrackable = false;
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		if (Quest != null)
		{
			Name = Quest.Title.ToString();
			UpdateRemainingTime(Quest.QuestDueTime);
		}
		else if (Issue != null)
		{
			Name = Issue.Title.ToString();
			UpdateRemainingTime(Issue.IssueDueTime);
		}
		else if (QuestLogEntry != null)
		{
			Name = QuestLogEntry.Title.ToString();
		}
		QuestGiverHero?.RefreshValues();
		Stages.ApplyActionOnAllItems(delegate(QuestStageVM x)
		{
			x.RefreshValues();
		});
	}

	private void UpdateRemainingTime(CampaignTime dueTime)
	{
		if (IsRemainingDaysHidden)
		{
			RemainingDays = 0;
		}
		else
		{
			RemainingDays = (int)(dueTime - CampaignTime.Now).ToDays;
		}
		GameTexts.SetVariable("DAY_IS_PLURAL", (RemainingDays > 1) ? 1 : 0);
		GameTexts.SetVariable("DAY", RemainingDays);
		if (dueTime.ToHours - CampaignTime.Now.ToHours < (double)CampaignTime.HoursInDay)
		{
			RemainingDaysText = GameTexts.FindText("str_less_than_a_day").ToString();
			RemainingDaysTextCombined = GameTexts.FindText("str_less_than_a_day").ToString();
		}
		else
		{
			RemainingDaysText = GameTexts.FindText("str_DAY_days_capital").ToString();
			RemainingDaysTextCombined = GameTexts.FindText("str_DAY_days").ToString();
		}
	}

	private void PopulateQuestLog(JournalLog log, bool isLastStage)
	{
		string dateString = log.GetTimeText().ToString();
		if (log.Type != LogType.Text && log.Type != LogType.None)
		{
			int num = TaleWorlds.Library.MathF.Max(log.Range, 0);
			int num2 = ((log.Type == LogType.TwoWayContinuous) ? log.CurrentProgress : TaleWorlds.Library.MathF.Max(log.CurrentProgress, 0));
			TextObject textObject = new TextObject("{=Pdo7PpS3}{TASK_NAME} {CURRENT_PROGRESS}/{TARGET_PROGRESS}");
			textObject.SetTextVariable("TASK_NAME", log.TaskName);
			textObject.SetTextVariable("CURRENT_PROGRESS", num2);
			textObject.SetTextVariable("TARGET_PROGRESS", num);
			QuestStageTaskVM stageTask = new QuestStageTaskVM(textObject, num2, num, log.Type);
			Stages.Add(new QuestStageVM(log, dateString, isLastStage, UpdateIsUpdated, stageTask));
		}
		else
		{
			Stages.Add(new QuestStageVM(log, log.LogText.ToString(), dateString, isLastStage, UpdateIsUpdated));
		}
	}

	public void UpdateIsUpdated()
	{
		IsUpdated = Stages.Any((QuestStageVM s) => s.IsNew);
	}

	public void ExecuteSelection()
	{
		_onSelection(this);
	}

	public void ExecuteToggleQuestTrack()
	{
		if (Quest != null)
		{
			Quest.ToggleTrackedObjects();
			IsTracked = Quest.IsTrackEnabled;
		}
	}
}
