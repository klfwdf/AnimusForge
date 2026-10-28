using System;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SandBox.ViewModelCollection.Tutorial;

public class TutorialItemVM : ViewModel
{
	public enum ItemPlacements
	{
		Left,
		Right,
		Top,
		Bottom,
		TopLeft,
		TopRight,
		BottomLeft,
		BottomRight,
		Center
	}

	private const string ControllerIdentificationModifier = "_controller";

	private string _tutorialTypeId;

	private Action _onFinishTutorial;

	private string _titleText;

	private string _descriptionText;

	private ImageIdentifierVM _centerImage;

	private string _soundId;

	private string _stepCountText;

	private string _tutorialsEnabledText;

	private string _tutorialTitleText;

	private bool _isEnabled;

	private bool _requiresMouse;

	private HintViewModel _disableCurrentTutorialHint;

	private HintViewModel _disableAllTutorialsHint;

	private bool _areTutorialsEnabled;

	public Action<bool> SetIsActive { get; private set; }

	[DataSourceProperty]
	public HintViewModel DisableCurrentTutorialHint
	{
		get
		{
			return _disableCurrentTutorialHint;
		}
		set
		{
			if (value != _disableCurrentTutorialHint)
			{
				_disableCurrentTutorialHint = value;
				OnPropertyChangedWithValue(value, "DisableCurrentTutorialHint");
			}
		}
	}

	[DataSourceProperty]
	public bool AreTutorialsEnabled
	{
		get
		{
			return _areTutorialsEnabled;
		}
		set
		{
			if (value != _areTutorialsEnabled)
			{
				_areTutorialsEnabled = value;
				OnPropertyChangedWithValue(value, "AreTutorialsEnabled");
				BannerlordConfig.EnableTutorialHints = value;
			}
		}
	}

	[DataSourceProperty]
	public string TutorialsEnabledText
	{
		get
		{
			return _tutorialsEnabledText;
		}
		set
		{
			if (value != _tutorialsEnabledText)
			{
				_tutorialsEnabledText = value;
				OnPropertyChangedWithValue(value, "TutorialsEnabledText");
			}
		}
	}

	[DataSourceProperty]
	public string TutorialTitleText
	{
		get
		{
			return _tutorialTitleText;
		}
		set
		{
			if (value != _tutorialTitleText)
			{
				_tutorialTitleText = value;
				OnPropertyChangedWithValue(value, "TutorialTitleText");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel DisableAllTutorialsHint
	{
		get
		{
			return _disableAllTutorialsHint;
		}
		set
		{
			if (value != _disableAllTutorialsHint)
			{
				_disableAllTutorialsHint = value;
				OnPropertyChangedWithValue(value, "DisableAllTutorialsHint");
			}
		}
	}

	[DataSourceProperty]
	public string TitleText
	{
		get
		{
			return _titleText;
		}
		set
		{
			if (value != _titleText)
			{
				_titleText = value;
				OnPropertyChangedWithValue(value, "TitleText");
			}
		}
	}

	[DataSourceProperty]
	public string StepCountText
	{
		get
		{
			return _stepCountText;
		}
		set
		{
			if (value != _stepCountText)
			{
				_stepCountText = value;
				OnPropertyChangedWithValue(value, "StepCountText");
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
			if (value != _isEnabled)
			{
				_isEnabled = value;
				OnPropertyChangedWithValue(value, "IsEnabled");
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
	public string SoundId
	{
		get
		{
			return _soundId;
		}
		set
		{
			if (value != _soundId)
			{
				_soundId = value;
				OnPropertyChanged("SoundId");
			}
		}
	}

	[DataSourceProperty]
	public ImageIdentifierVM CenterImage
	{
		get
		{
			return _centerImage;
		}
		set
		{
			if (value != _centerImage)
			{
				_centerImage = value;
				OnPropertyChanged("CenterImage");
			}
		}
	}

	[DataSourceProperty]
	public bool RequiresMouse
	{
		get
		{
			return _requiresMouse;
		}
		set
		{
			if (value != _requiresMouse)
			{
				_requiresMouse = value;
				OnPropertyChanged("RequiresMouse");
			}
		}
	}

	public TutorialItemVM()
	{
		IsEnabled = false;
	}

	public void Init(string tutorialTypeId, bool requiresMouse, Action onFinishTutorial)
	{
		IsEnabled = false;
		StepCountText = "DISABLED";
		RequiresMouse = requiresMouse;
		IsEnabled = true;
		_onFinishTutorial = onFinishTutorial;
		_tutorialTypeId = tutorialTypeId;
		AreTutorialsEnabled = BannerlordConfig.EnableTutorialHints;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		DisableCurrentTutorialHint = new HintViewModel(GameTexts.FindText("str_disable_current_tutorial_step"));
		DisableAllTutorialsHint = new HintViewModel(GameTexts.FindText("str_disable_all_tutorials"));
		TutorialsEnabledText = GameTexts.FindText("str_tutorials_enabled").ToString();
		TutorialTitleText = GameTexts.FindText("str_initial_menu_option", "Tutorial").ToString();
		TitleText = GameTexts.FindText("str_campaign_tutorial_title", _tutorialTypeId).ToString();
		if (TaleWorlds.InputSystem.Input.IsGamepadActive && GameTexts.TryGetText("str_campaign_tutorial_description", out var textObject, _tutorialTypeId + "_controller"))
		{
			DescriptionText = textObject.ToString();
		}
		else
		{
			DescriptionText = GameTexts.FindText("str_campaign_tutorial_description", _tutorialTypeId).ToString();
		}
	}

	public void CloseTutorialPanel()
	{
		IsEnabled = false;
	}

	private void ExecuteFinishTutorial()
	{
		_onFinishTutorial();
	}

	private void ExecuteToggleDisableAllTutorials()
	{
		AreTutorialsEnabled = !AreTutorialsEnabled;
	}
}
