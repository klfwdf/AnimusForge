using System;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Quests;

public class QuestStageVM : ViewModel
{
	public readonly JournalLog Log;

	private readonly Action _onLogNotified;

	private readonly IViewDataTracker _viewDataTracker;

	private string _descriptionText;

	private string _dateText;

	private bool _hasATask;

	private bool _isNew;

	private bool _isTaskCompleted;

	private bool _isLastStage;

	private QuestStageTaskVM _stageTask;

	[DataSourceProperty]
	public string DateText
	{
		get
		{
			return _dateText;
		}
		set
		{
			if (value != _dateText)
			{
				_dateText = value;
				OnPropertyChangedWithValue(value, "DateText");
			}
		}
	}

	[DataSourceProperty]
	public string DescriptionText
	{
		get
		{
			return _descriptionText;
		}
		set
		{
			if (value != _descriptionText)
			{
				_descriptionText = value;
				OnPropertyChangedWithValue(value, "DescriptionText");
			}
		}
	}

	[DataSourceProperty]
	public bool HasATask
	{
		get
		{
			return _hasATask;
		}
		set
		{
			if (value != _hasATask)
			{
				_hasATask = value;
				OnPropertyChangedWithValue(value, "HasATask");
			}
		}
	}

	[DataSourceProperty]
	public bool IsNew
	{
		get
		{
			return _isNew;
		}
		set
		{
			if (value != _isNew)
			{
				_isNew = value;
				OnPropertyChangedWithValue(value, "IsNew");
			}
		}
	}

	[DataSourceProperty]
	public bool IsLastStage
	{
		get
		{
			return _isLastStage;
		}
		set
		{
			if (value != _isLastStage)
			{
				_isLastStage = value;
				OnPropertyChangedWithValue(value, "IsLastStage");
			}
		}
	}

	[DataSourceProperty]
	public bool IsTaskCompleted
	{
		get
		{
			return _isTaskCompleted;
		}
		set
		{
			if (value != _isTaskCompleted)
			{
				_isTaskCompleted = value;
				OnPropertyChangedWithValue(value, "IsTaskCompleted");
			}
		}
	}

	[DataSourceProperty]
	public QuestStageTaskVM StageTask
	{
		get
		{
			return _stageTask;
		}
		set
		{
			if (value != _stageTask)
			{
				_stageTask = value;
				OnPropertyChangedWithValue(value, "StageTask");
			}
		}
	}

	public QuestStageVM(JournalLog log, string dateString, bool isLastStage, Action onLogNotified, QuestStageTaskVM stageTask = null)
	{
		StageTask = new QuestStageTaskVM(TextObject.GetEmpty(), 0, 0, LogType.None);
		_onLogNotified = onLogNotified;
		string content = log.LogText.ToString();
		GameTexts.SetVariable("ENTRY", content);
		_viewDataTracker = Campaign.Current.GetCampaignBehavior<IViewDataTracker>();
		DateText = dateString;
		DescriptionText = log.LogText.ToString();
		IsLastStage = isLastStage;
		Log = log;
		UpdateIsNew();
		if (stageTask != null)
		{
			StageTask = stageTask;
			StageTask.IsValid = true;
			HasATask = true;
			IsTaskCompleted = StageTask.CurrentProgress == StageTask.TargetProgress;
		}
	}

	public QuestStageVM(JournalLog log, string description, string dateString, bool isLastStage, Action onLogNotified)
	{
		Log = log;
		StageTask = new QuestStageTaskVM(TextObject.GetEmpty(), 0, 0, LogType.None);
		_onLogNotified = onLogNotified;
		_viewDataTracker = Campaign.Current.GetCampaignBehavior<IViewDataTracker>();
		DateText = dateString;
		DescriptionText = description;
		IsLastStage = isLastStage;
		UpdateIsNew();
	}

	public void ExecuteResetUpdated()
	{
	}

	public void ExecuteLink(string link)
	{
		Campaign.Current.EncyclopediaManager.GoToLink(link);
	}

	public void UpdateIsNew()
	{
		if (Log != null)
		{
			IsNew = _viewDataTracker.UnExaminedQuestLogs.Any((JournalLog l) => l == Log);
		}
	}
}
