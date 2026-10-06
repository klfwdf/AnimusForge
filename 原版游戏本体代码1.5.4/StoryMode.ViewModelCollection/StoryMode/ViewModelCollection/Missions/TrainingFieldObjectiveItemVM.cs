using StoryMode.Missions;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace StoryMode.ViewModelCollection.Missions;

public class TrainingFieldObjectiveItemVM : ViewModel
{
	private string _textObjectString;

	private TrainingFieldMissionController.MouseObjectives _currentMouseObjective;

	private TrainingFieldMissionController.ObjectivePerformingType _currentObjectivePerformingType;

	private bool _lastGamepadActive;

	private bool _hasBackground;

	private string _objectiveText;

	private string _arrowState;

	private bool _isCompleted;

	private bool _isActive;

	private bool _isBackgroundActive;

	private float _score;

	private MBBindingList<TrainingFieldObjectiveItemVM> _objectiveItems;

	private MBBindingList<TrainingObjectiveKeyVM> _objectiveKeys;

	[DataSourceProperty]
	public string ObjectiveText
	{
		get
		{
			return _objectiveText;
		}
		set
		{
			if (value != _objectiveText)
			{
				_objectiveText = value;
				OnPropertyChangedWithValue(value, "ObjectiveText");
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
	public bool IsActive
	{
		get
		{
			return _isActive;
		}
		set
		{
			if (value != _isActive)
			{
				_isActive = value;
				OnPropertyChangedWithValue(value, "IsActive");
			}
		}
	}

	[DataSourceProperty]
	public bool IsBackgroundActive
	{
		get
		{
			return _isBackgroundActive;
		}
		set
		{
			if (value != _isBackgroundActive)
			{
				_isBackgroundActive = value;
				OnPropertyChangedWithValue(value, "IsBackgroundActive");
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

	[DataSourceProperty]
	public MBBindingList<TrainingObjectiveKeyVM> ObjectiveKeys
	{
		get
		{
			return _objectiveKeys;
		}
		set
		{
			if (value != _objectiveKeys)
			{
				_objectiveKeys = value;
				OnPropertyChangedWithValue(value, "ObjectiveKeys");
			}
		}
	}

	[DataSourceProperty]
	public string ArrowState
	{
		get
		{
			return _arrowState;
		}
		set
		{
			if (value != _arrowState)
			{
				_arrowState = value;
				OnPropertyChangedWithValue(value, "ArrowState");
			}
		}
	}

	private TrainingFieldObjectiveItemVM()
	{
	}

	private TrainingFieldObjectiveItemVM(TrainingFieldMissionController.TutorialObjective objective)
	{
		_textObjectString = objective.GetNameString();
		_hasBackground = objective.HasBackground;
		IsCompleted = objective.IsFinished;
		IsActive = objective.IsActive;
		_score = objective.Score;
		ObjectiveItems = new MBBindingList<TrainingFieldObjectiveItemVM>();
		if (objective.SubTasks != null)
		{
			foreach (TrainingFieldMissionController.TutorialObjective subTask in objective.SubTasks)
			{
				ObjectiveItems.Add(CreateFromObjective(subTask));
			}
		}
		ObjectiveKeys = new MBBindingList<TrainingObjectiveKeyVM>();
		RefreshValues();
	}

	public void UpdateObjective(TrainingFieldMissionController.MouseObjectives currentMouseObjective, TrainingFieldMissionController.ObjectivePerformingType currentObjectivePerformingType)
	{
		if (_currentMouseObjective != currentMouseObjective || _currentObjectivePerformingType != currentObjectivePerformingType || _lastGamepadActive != Input.IsGamepadActive)
		{
			_currentMouseObjective = currentMouseObjective;
			_currentObjectivePerformingType = currentObjectivePerformingType;
			_lastGamepadActive = Input.IsGamepadActive;
			ObjectiveKeys.Clear();
			ResolveInput(_currentMouseObjective, _currentObjectivePerformingType, _lastGamepadActive);
		}
	}

	private void ResolveInput(TrainingFieldMissionController.MouseObjectives currentMouseObjective, TrainingFieldMissionController.ObjectivePerformingType currentObjectivePerformingType, bool isGamepadActive)
	{
		if (currentObjectivePerformingType == TrainingFieldMissionController.ObjectivePerformingType.None || currentMouseObjective == TrainingFieldMissionController.MouseObjectives.None)
		{
			IsBackgroundActive = false;
			return;
		}
		IsBackgroundActive = _hasBackground;
		bool flag = IsAttackMovement(_currentMouseObjective);
		TrainingObjectiveKeyVM.MovementTypes movementTypeOfObjective = GetMovementTypeOfObjective(_currentMouseObjective);
		ArrowState = "Default";
		if (isGamepadActive)
		{
			ObjectiveKeys.Add(new TrainingObjectiveKeyVM(new TrainingObjectiveKeyVM.KeyInput(flag ? 9 : 10, isCombatHotKey: true)));
			if (currentObjectivePerformingType != TrainingFieldMissionController.ObjectivePerformingType.AutoBlock)
			{
				ObjectiveKeys.Add(new TrainingObjectiveKeyVM(new TrainingObjectiveKeyVM.ControllerStickInput(movementTypeOfObjective, currentObjectivePerformingType != TrainingFieldMissionController.ObjectivePerformingType.ByLookDirection)));
				ArrowState = DecideArrowDirection(movementTypeOfObjective);
			}
			return;
		}
		TrainingObjectiveKeyVM.MouseClickTypes mouseClickType = ((!flag) ? TrainingObjectiveKeyVM.MouseClickTypes.Right : TrainingObjectiveKeyVM.MouseClickTypes.Left);
		switch (currentObjectivePerformingType)
		{
		case TrainingFieldMissionController.ObjectivePerformingType.ByLookDirection:
			ObjectiveKeys.Add(new TrainingObjectiveKeyVM(new TrainingObjectiveKeyVM.MouseAndClickInput(movementTypeOfObjective, mouseClickType)));
			ArrowState = DecideArrowDirection(movementTypeOfObjective);
			break;
		case TrainingFieldMissionController.ObjectivePerformingType.ByMovement:
			ObjectiveKeys.Add(new TrainingObjectiveKeyVM(new TrainingObjectiveKeyVM.KeyInput(GetKeyOfMovementType(movementTypeOfObjective), isCombatHotKey: false)));
			ObjectiveKeys.Add(new TrainingObjectiveKeyVM(new TrainingObjectiveKeyVM.MouseAndClickInput(TrainingObjectiveKeyVM.MovementTypes.None, mouseClickType)));
			break;
		case TrainingFieldMissionController.ObjectivePerformingType.AutoBlock:
			ObjectiveKeys.Add(new TrainingObjectiveKeyVM(new TrainingObjectiveKeyVM.MouseAndClickInput(TrainingObjectiveKeyVM.MovementTypes.None, mouseClickType)));
			break;
		}
	}

	private TrainingObjectiveKeyVM.MovementTypes GetMovementTypeOfObjective(TrainingFieldMissionController.MouseObjectives mouseObjective)
	{
		switch (mouseObjective)
		{
		case TrainingFieldMissionController.MouseObjectives.AttackRight:
		case TrainingFieldMissionController.MouseObjectives.DefendRight:
			return TrainingObjectiveKeyVM.MovementTypes.MoveRight;
		case TrainingFieldMissionController.MouseObjectives.AttackLeft:
		case TrainingFieldMissionController.MouseObjectives.DefendLeft:
			return TrainingObjectiveKeyVM.MovementTypes.MoveLeft;
		case TrainingFieldMissionController.MouseObjectives.AttackUp:
		case TrainingFieldMissionController.MouseObjectives.DefendUp:
			return TrainingObjectiveKeyVM.MovementTypes.MoveUp;
		case TrainingFieldMissionController.MouseObjectives.AttackDown:
		case TrainingFieldMissionController.MouseObjectives.DefendDown:
			return TrainingObjectiveKeyVM.MovementTypes.MoveDown;
		default:
			return TrainingObjectiveKeyVM.MovementTypes.None;
		}
	}

	private int GetKeyOfMovementType(TrainingObjectiveKeyVM.MovementTypes movementType)
	{
		return movementType switch
		{
			TrainingObjectiveKeyVM.MovementTypes.MoveRight => 3, 
			TrainingObjectiveKeyVM.MovementTypes.MoveLeft => 2, 
			TrainingObjectiveKeyVM.MovementTypes.MoveUp => 0, 
			TrainingObjectiveKeyVM.MovementTypes.MoveDown => 1, 
			_ => -1, 
		};
	}

	private bool IsAttackMovement(TrainingFieldMissionController.MouseObjectives mouseObjective)
	{
		switch (mouseObjective)
		{
		case TrainingFieldMissionController.MouseObjectives.AttackLeft:
		case TrainingFieldMissionController.MouseObjectives.AttackRight:
		case TrainingFieldMissionController.MouseObjectives.AttackUp:
		case TrainingFieldMissionController.MouseObjectives.AttackDown:
			return true;
		case TrainingFieldMissionController.MouseObjectives.DefendLeft:
		case TrainingFieldMissionController.MouseObjectives.DefendRight:
		case TrainingFieldMissionController.MouseObjectives.DefendUp:
		case TrainingFieldMissionController.MouseObjectives.DefendDown:
			return false;
		default:
			return false;
		}
	}

	private string DecideArrowDirection(TrainingObjectiveKeyVM.MovementTypes movement)
	{
		return movement switch
		{
			TrainingObjectiveKeyVM.MovementTypes.MoveUp => "Up", 
			TrainingObjectiveKeyVM.MovementTypes.MoveDown => "Down", 
			TrainingObjectiveKeyVM.MovementTypes.MoveRight => "Right", 
			TrainingObjectiveKeyVM.MovementTypes.MoveLeft => "Left", 
			_ => "Default", 
		};
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		if (_textObjectString != "")
		{
			ObjectiveText = _textObjectString;
			if (_score != 0f)
			{
				TextObject textObject = GameTexts.FindText("str_tutorial_time_score");
				textObject.SetTextVariable("TIME_SCORE", _score.ToString("0.0"));
				ObjectiveText += textObject.ToString();
			}
		}
	}

	public static TrainingFieldObjectiveItemVM CreateFromObjective(TrainingFieldMissionController.TutorialObjective objective)
	{
		return new TrainingFieldObjectiveItemVM(objective);
	}

	public static TrainingFieldObjectiveItemVM CreateDummy()
	{
		return new TrainingFieldObjectiveItemVM();
	}
}
