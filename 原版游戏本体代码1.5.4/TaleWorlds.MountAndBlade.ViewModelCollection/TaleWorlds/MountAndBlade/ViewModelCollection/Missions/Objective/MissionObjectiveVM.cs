using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Missions.MissionLogics;
using TaleWorlds.MountAndBlade.Missions.Objectives;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.Missions.Objective;

public class MissionObjectiveVM : ViewModel
{
	private MissionObjective _currentObjective;

	private BasicCharacterObject _currentObjectiveGiver;

	private bool _isCurrentObjectiveDirty;

	private string _title;

	private string _description;

	private string _progressText;

	private string _objectiveGiverName;

	private bool _hasObjectiveGiver;

	private bool _isEnabled;

	private bool _hasTitle;

	private bool _hasDescription;

	private bool _hasProgress;

	private int _currentProgress;

	private int _requiredProgress;

	private CharacterImageIdentifierVM _objectiveGiverIdentifier;

	private MissionObjectiveMarkersVM _markers;

	[DataSourceProperty]
	public string Title
	{
		get
		{
			return _title;
		}
		set
		{
			if (_title != value)
			{
				_title = value;
				OnPropertyChangedWithValue(value, "Title");
				HasTitle = !string.IsNullOrEmpty(value);
			}
		}
	}

	[DataSourceProperty]
	public string Description
	{
		get
		{
			return _description;
		}
		set
		{
			if (_description != value)
			{
				_description = value;
				OnPropertyChangedWithValue(value, "Description");
				HasDescription = !string.IsNullOrEmpty(value);
			}
		}
	}

	[DataSourceProperty]
	public string ProgressText
	{
		get
		{
			return _progressText;
		}
		set
		{
			if (_progressText != value)
			{
				_progressText = value;
				OnPropertyChangedWithValue(value, "ProgressText");
			}
		}
	}

	[DataSourceProperty]
	public string ObjectiveGiverName
	{
		get
		{
			return _objectiveGiverName;
		}
		set
		{
			if (_objectiveGiverName != value)
			{
				_objectiveGiverName = value;
				OnPropertyChangedWithValue(value, "ObjectiveGiverName");
			}
		}
	}

	[DataSourceProperty]
	public bool HasObjectiveGiver
	{
		get
		{
			return _hasObjectiveGiver;
		}
		set
		{
			if (_hasObjectiveGiver != value)
			{
				_hasObjectiveGiver = value;
				OnPropertyChangedWithValue(value, "HasObjectiveGiver");
			}
		}
	}

	[DataSourceProperty]
	public bool IsEnabled
	{
		get
		{
			return _isEnabled;
		}
		set
		{
			if (_isEnabled != value)
			{
				_isEnabled = value;
				OnPropertyChangedWithValue(value, "IsEnabled");
				Markers.IsEnabled = value;
			}
		}
	}

	[DataSourceProperty]
	public bool HasTitle
	{
		get
		{
			return _hasTitle;
		}
		set
		{
			if (_hasTitle != value)
			{
				_hasTitle = value;
				OnPropertyChangedWithValue(value, "HasTitle");
			}
		}
	}

	[DataSourceProperty]
	public bool HasDescription
	{
		get
		{
			return _hasDescription;
		}
		set
		{
			if (_hasDescription != value)
			{
				_hasDescription = value;
				OnPropertyChangedWithValue(value, "HasDescription");
			}
		}
	}

	[DataSourceProperty]
	public bool HasProgress
	{
		get
		{
			return _hasProgress;
		}
		set
		{
			if (_hasProgress != value)
			{
				_hasProgress = value;
				OnPropertyChangedWithValue(value, "HasProgress");
			}
		}
	}

	[DataSourceProperty]
	public int CurrentProgress
	{
		get
		{
			return _currentProgress;
		}
		set
		{
			if (_currentProgress != value)
			{
				_currentProgress = value;
				OnPropertyChangedWithValue(value, "CurrentProgress");
			}
		}
	}

	[DataSourceProperty]
	public int RequiredProgress
	{
		get
		{
			return _requiredProgress;
		}
		set
		{
			if (_requiredProgress != value)
			{
				_requiredProgress = value;
				OnPropertyChangedWithValue(value, "RequiredProgress");
			}
		}
	}

	[DataSourceProperty]
	public CharacterImageIdentifierVM ObjectiveGiverIdentifier
	{
		get
		{
			return _objectiveGiverIdentifier;
		}
		set
		{
			if (_objectiveGiverIdentifier != value)
			{
				_objectiveGiverIdentifier = value;
				OnPropertyChangedWithValue(value, "ObjectiveGiverIdentifier");
			}
		}
	}

	[DataSourceProperty]
	public MissionObjectiveMarkersVM Markers
	{
		get
		{
			return _markers;
		}
		set
		{
			if (_markers != value)
			{
				_markers = value;
				OnPropertyChangedWithValue(value, "Markers");
			}
		}
	}

	public MissionObjectiveVM(MissionObjectiveLogic objectiveLogic, Camera missionCamera)
	{
		Markers = new MissionObjectiveMarkersVM(objectiveLogic, missionCamera);
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		if (_currentObjective != null)
		{
			Title = _currentObjective.Name.ToString();
			Description = _currentObjective.Description.ToString();
		}
		Markers.RefreshValues();
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		if (_currentObjective != null)
		{
			_currentObjective.OnUpdated -= OnObjectiveUpdated;
		}
		ObjectiveGiverIdentifier?.OnFinalize();
		Markers.OnFinalize();
	}

	public void UpdateObjective(MissionObjective objective)
	{
		if (_currentObjective != null)
		{
			_currentObjective.OnUpdated -= OnObjectiveUpdated;
		}
		_currentObjective = objective;
		if (_currentObjective != null)
		{
			_currentObjective.OnUpdated += OnObjectiveUpdated;
		}
		RefreshCurrentObjectiveData();
	}

	public void Tick(float dt)
	{
		if (_isCurrentObjectiveDirty)
		{
			RefreshCurrentObjectiveData();
			_isCurrentObjectiveDirty = false;
		}
		Markers.Tick(dt);
		if (_currentObjective != null)
		{
			MissionObjectiveProgressInfo progressInfo = _currentObjective.GetCurrentProgress();
			if (IsProgressDirty(in progressInfo))
			{
				HasProgress = progressInfo.HasProgress;
				CurrentProgress = progressInfo.CurrentProgressAmount;
				RequiredProgress = progressInfo.RequiredProgressAmount;
				ProgressText = GameTexts.FindText("str_LEFT_over_RIGHT_no_space").SetTextVariable("LEFT", CurrentProgress).SetTextVariable("RIGHT", RequiredProgress)
					.ToString();
			}
		}
		UpdateObjectiveGiver();
	}

	private void RefreshCurrentObjectiveData()
	{
		Markers.UpdateObjective(_currentObjective);
		IsEnabled = _currentObjective != null;
		RefreshValues();
	}

	private void UpdateObjectiveGiver()
	{
		if (_currentObjectiveGiver != _currentObjective?.ObjectiveGiver)
		{
			_currentObjectiveGiver = _currentObjective?.ObjectiveGiver;
			HasObjectiveGiver = _currentObjectiveGiver != null;
			ObjectiveGiverIdentifier?.OnFinalize();
			if (HasObjectiveGiver)
			{
				ObjectiveGiverIdentifier = new CharacterImageIdentifierVM(CharacterCode.CreateFrom(_currentObjectiveGiver));
			}
			ObjectiveGiverName = _currentObjectiveGiver?.Name?.ToString() ?? string.Empty;
		}
	}

	private bool IsProgressDirty(in MissionObjectiveProgressInfo progressInfo)
	{
		if (progressInfo.HasProgress == HasProgress && progressInfo.CurrentProgressAmount == CurrentProgress)
		{
			return progressInfo.RequiredProgressAmount != RequiredProgress;
		}
		return true;
	}

	private void OnObjectiveUpdated()
	{
		_isCurrentObjectiveDirty = true;
	}
}
