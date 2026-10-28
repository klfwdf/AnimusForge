using System;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.CharacterCreation;

public abstract class CharacterCreationStageBaseVM : ViewModel
{
	protected readonly CharacterCreationManager CharacterCreationManager;

	protected readonly Action _affirmativeAction;

	protected readonly Action _negativeAction;

	protected readonly TextObject _affirmativeActionText;

	protected readonly TextObject _negativeActionText;

	private string _title = "";

	private string _description = "";

	private string _selectionText = "";

	private string _nextStageText;

	private string _previousStageText;

	private int _totalStageCount = -1;

	private int _currentStageIndex = -1;

	private int _furthestIndex = -1;

	private bool _anyItemSelected;

	private bool _canAdvance;

	[DataSourceProperty]
	public string Title
	{
		get
		{
			return _title;
		}
		set
		{
			if (value != _title)
			{
				_title = value;
				OnPropertyChangedWithValue(value, "Title");
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
			if (value != _description)
			{
				_description = value;
				OnPropertyChangedWithValue(value, "Description");
			}
		}
	}

	[DataSourceProperty]
	public string SelectionText
	{
		get
		{
			return _selectionText;
		}
		set
		{
			if (value != _selectionText)
			{
				_selectionText = value;
				OnPropertyChangedWithValue(value, "SelectionText");
			}
		}
	}

	[DataSourceProperty]
	public string NextStageText
	{
		get
		{
			return _nextStageText;
		}
		set
		{
			if (value != _nextStageText)
			{
				_nextStageText = value;
				OnPropertyChangedWithValue(value, "NextStageText");
			}
		}
	}

	[DataSourceProperty]
	public string PreviousStageText
	{
		get
		{
			return _previousStageText;
		}
		set
		{
			if (value != _previousStageText)
			{
				_previousStageText = value;
				OnPropertyChangedWithValue(value, "PreviousStageText");
			}
		}
	}

	[DataSourceProperty]
	public int TotalStageCount
	{
		get
		{
			return _totalStageCount;
		}
		set
		{
			if (value != _totalStageCount)
			{
				_totalStageCount = value;
				OnPropertyChangedWithValue(value, "TotalStageCount");
			}
		}
	}

	[DataSourceProperty]
	public int FurthestIndex
	{
		get
		{
			return _furthestIndex;
		}
		set
		{
			if (value != _furthestIndex)
			{
				_furthestIndex = value;
				OnPropertyChangedWithValue(value, "FurthestIndex");
			}
		}
	}

	[DataSourceProperty]
	public int CurrentStageIndex
	{
		get
		{
			return _currentStageIndex;
		}
		set
		{
			if (value != _currentStageIndex)
			{
				_currentStageIndex = value;
				OnPropertyChangedWithValue(value, "CurrentStageIndex");
			}
		}
	}

	[DataSourceProperty]
	public bool AnyItemSelected
	{
		get
		{
			return _anyItemSelected;
		}
		set
		{
			if (value != _anyItemSelected)
			{
				_anyItemSelected = value;
				OnPropertyChangedWithValue(value, "AnyItemSelected");
			}
		}
	}

	[DataSourceProperty]
	public bool CanAdvance
	{
		get
		{
			return _canAdvance;
		}
		set
		{
			if (value != _canAdvance)
			{
				_canAdvance = value;
				OnPropertyChangedWithValue(value, "CanAdvance");
			}
		}
	}

	protected CharacterCreationStageBaseVM(CharacterCreationManager characterCreationManager, Action affirmativeAction, TextObject affirmativeActionText, Action negativeAction, TextObject negativeActionText)
	{
		CharacterCreationManager = characterCreationManager;
		_affirmativeAction = affirmativeAction;
		_negativeAction = negativeAction;
		_affirmativeActionText = affirmativeActionText;
		_negativeActionText = negativeActionText;
		NextStageText = _affirmativeActionText?.ToString();
		PreviousStageText = _negativeActionText?.ToString();
	}

	public abstract void OnNextStage();

	public abstract void OnPreviousStage();

	public abstract bool CanAdvanceToNextStage();
}
