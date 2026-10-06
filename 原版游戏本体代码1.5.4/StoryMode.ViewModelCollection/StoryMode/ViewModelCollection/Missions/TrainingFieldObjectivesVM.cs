using System.Collections.Generic;
using StoryMode.Missions;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace StoryMode.ViewModelCollection.Missions;

public class TrainingFieldObjectivesVM : ViewModel
{
	private TrainingFieldObjectiveItemVM _dummyObjective;

	private string _leaveAnyTimeText;

	private string _currentObjectiveExplanationText;

	private string _timerText;

	private TrainingFieldObjectiveItemVM _activeObjective;

	private MBBindingList<TrainingFieldObjectiveItemVM> _objectiveItems;

	[DataSourceProperty]
	public string LeaveAnyTimeText
	{
		get
		{
			return _leaveAnyTimeText;
		}
		set
		{
			if (value != _leaveAnyTimeText)
			{
				_leaveAnyTimeText = value;
				OnPropertyChangedWithValue(value, "LeaveAnyTimeText");
			}
		}
	}

	[DataSourceProperty]
	public string CurrentObjectiveExplanationText
	{
		get
		{
			return _currentObjectiveExplanationText;
		}
		set
		{
			if (value != _currentObjectiveExplanationText)
			{
				_currentObjectiveExplanationText = value;
				OnPropertyChangedWithValue(value, "CurrentObjectiveExplanationText");
			}
		}
	}

	[DataSourceProperty]
	public string TimerText
	{
		get
		{
			return _timerText;
		}
		set
		{
			if (value != _timerText)
			{
				_timerText = value;
				OnPropertyChangedWithValue(value, "TimerText");
			}
		}
	}

	[DataSourceProperty]
	public TrainingFieldObjectiveItemVM ActiveObjective
	{
		get
		{
			return _activeObjective;
		}
		set
		{
			if (value != _activeObjective)
			{
				_activeObjective = value;
				OnPropertyChangedWithValue(value, "ActiveObjective");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<TrainingFieldObjectiveItemVM> ObjectiveItems
	{
		get
		{
			return _objectiveItems;
		}
		set
		{
			if (value != _objectiveItems)
			{
				_objectiveItems = value;
				OnPropertyChangedWithValue(value, "ObjectiveItems");
			}
		}
	}

	public TrainingFieldObjectivesVM()
	{
		ObjectiveItems = new MBBindingList<TrainingFieldObjectiveItemVM>();
		_dummyObjective = TrainingFieldObjectiveItemVM.CreateDummy();
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		string keyHyperlinkText = HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("Generic", 4));
		GameTexts.SetVariable("LEAVE_KEY", keyHyperlinkText);
		GameTexts.SetVariable("newline", "\n");
		LeaveAnyTimeText = GameTexts.FindText("str_leave_training_field").ToString();
		ObjectiveItems.ApplyActionOnAllItems(delegate(TrainingFieldObjectiveItemVM o)
		{
			o.RefreshValues();
		});
	}

	public void UpdateObjectivesWith(List<TrainingFieldMissionController.TutorialObjective> objectives)
	{
		ObjectiveItems.Clear();
		foreach (TrainingFieldMissionController.TutorialObjective objective in objectives)
		{
			TrainingFieldObjectiveItemVM trainingFieldObjectiveItemVM = TrainingFieldObjectiveItemVM.CreateFromObjective(objective);
			ObjectiveItems.Add(trainingFieldObjectiveItemVM);
			if (objective.IsActive)
			{
				ActiveObjective = trainingFieldObjectiveItemVM;
			}
		}
	}

	public void UpdateCurrentObjectiveExplanationText(TextObject currentObjectiveText)
	{
		if (ActiveObjective == null)
		{
			ActiveObjective = _dummyObjective;
		}
		CurrentObjectiveExplanationText = currentObjectiveText?.ToString() ?? "";
	}

	public void UpdateCurrentMouseObjective(TrainingFieldMissionController.MouseObjectives currentMouseObjective, TrainingFieldMissionController.ObjectivePerformingType currentObjectivePerformingType)
	{
		ActiveObjective?.UpdateObjective(currentMouseObjective, currentObjectivePerformingType);
	}

	public void UpdateTimerText(string timerText)
	{
		TimerText = timerText;
	}
}
