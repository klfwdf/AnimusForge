using System;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Conversation.Persuasion;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Conversation;

public class PersuasionVM : ViewModel
{
	internal const string PositiveText = "<a style=\"Conversation.Persuasion.Positive\"><b>{TEXT}</b></a>";

	internal const string NegativeText = "<a style=\"Conversation.Persuasion.Negative\"><b>{TEXT}</b></a>";

	internal const string NeutralText = "<a style=\"Conversation.Persuasion.Neutral\"><b>{TEXT}</b></a>";

	private ConversationManager _manager;

	private MBBindingList<BoolItemWithActionVM> _persuasionProgress;

	private bool _isPersuasionActive;

	private int _currentCritFailChance;

	private int _currentFailChance;

	private int _currentSuccessChance;

	private int _currentCritSuccessChance;

	private string _progressText;

	private PersuasionOptionVM _currentPersuasionOption;

	private BasicTooltipViewModel _persuasionHint;

	[DataSourceProperty]
	public BasicTooltipViewModel PersuasionHint
	{
		get
		{
			return _persuasionHint;
		}
		set
		{
			if (_persuasionHint != value)
			{
				_persuasionHint = value;
				OnPropertyChangedWithValue(value, "PersuasionHint");
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
	public MBBindingList<BoolItemWithActionVM> PersuasionProgress
	{
		get
		{
			return _persuasionProgress;
		}
		set
		{
			if (value != _persuasionProgress)
			{
				_persuasionProgress = value;
				OnPropertyChangedWithValue(value, "PersuasionProgress");
			}
		}
	}

	[DataSourceProperty]
	public bool IsPersuasionActive
	{
		get
		{
			return _isPersuasionActive;
		}
		set
		{
			if (value != _isPersuasionActive)
			{
				if (value)
				{
					RefreshChangeValues();
				}
				_isPersuasionActive = value;
				OnPropertyChangedWithValue(value, "IsPersuasionActive");
			}
		}
	}

	[DataSourceProperty]
	public int CurrentSuccessChance
	{
		get
		{
			return _currentSuccessChance;
		}
		set
		{
			if (_currentSuccessChance != value)
			{
				_currentSuccessChance = value;
				OnPropertyChangedWithValue(value, "CurrentSuccessChance");
			}
		}
	}

	[DataSourceProperty]
	public PersuasionOptionVM CurrentPersuasionOption
	{
		get
		{
			return _currentPersuasionOption;
		}
		set
		{
			if (_currentPersuasionOption != value)
			{
				_currentPersuasionOption = value;
				OnPropertyChangedWithValue(value, "CurrentPersuasionOption");
			}
		}
	}

	[DataSourceProperty]
	public int CurrentFailChance
	{
		get
		{
			return _currentFailChance;
		}
		set
		{
			if (_currentFailChance != value)
			{
				_currentFailChance = value;
				OnPropertyChangedWithValue(value, "CurrentFailChance");
			}
		}
	}

	[DataSourceProperty]
	public int CurrentCritSuccessChance
	{
		get
		{
			return _currentCritSuccessChance;
		}
		set
		{
			if (_currentCritSuccessChance != value)
			{
				_currentCritSuccessChance = value;
				OnPropertyChangedWithValue(value, "CurrentCritSuccessChance");
			}
		}
	}

	[DataSourceProperty]
	public int CurrentCritFailChance
	{
		get
		{
			return _currentCritFailChance;
		}
		set
		{
			if (_currentCritFailChance != value)
			{
				_currentCritFailChance = value;
				OnPropertyChangedWithValue(value, "CurrentCritFailChance");
			}
		}
	}

	public PersuasionVM(ConversationManager manager)
	{
		PersuasionProgress = new MBBindingList<BoolItemWithActionVM>();
		_manager = manager;
	}

	public void OnPersuasionProgress(Tuple<PersuasionOptionArgs, PersuasionOptionResult> selectedOption)
	{
		ProgressText = "";
		string newValue = null;
		string text = null;
		switch (selectedOption.Item2)
		{
		case PersuasionOptionResult.CriticalFailure:
			newValue = new TextObject("{=ocSW4WA2}Critical Fail!").ToString();
			text = "<a style=\"Conversation.Persuasion.Negative\"><b>{TEXT}</b></a>";
			break;
		case PersuasionOptionResult.CriticalSuccess:
			newValue = new TextObject("{=4U9EnZt5}Critical Success!").ToString();
			text = "<a style=\"Conversation.Persuasion.Positive\"><b>{TEXT}</b></a>";
			break;
		case PersuasionOptionResult.Success:
			newValue = new TextObject("{=3F0y3ugx}Success!").ToString();
			text = "<a style=\"Conversation.Persuasion.Positive\"><b>{TEXT}</b></a>";
			break;
		case PersuasionOptionResult.Failure:
		case PersuasionOptionResult.Miss:
			newValue = new TextObject("{=JYOcl7Ox}Ineffective!").ToString();
			text = "<a style=\"Conversation.Persuasion.Neutral\"><b>{TEXT}</b></a>";
			break;
		}
		ProgressText = text.Replace("{TEXT}", newValue);
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		CurrentPersuasionOption?.RefreshValues();
	}

	public void SetCurrentOption(PersuasionOptionVM option)
	{
		if (CurrentPersuasionOption != option)
		{
			CurrentPersuasionOption = option;
		}
	}

	public void RefreshPersusasion()
	{
		CurrentCritFailChance = 0;
		CurrentFailChance = 0;
		CurrentCritSuccessChance = 0;
		CurrentSuccessChance = 0;
		IsPersuasionActive = ConversationManager.GetPersuasionIsActive();
		PersuasionProgress.Clear();
		PersuasionHint = new BasicTooltipViewModel();
		if (IsPersuasionActive)
		{
			int num = (int)ConversationManager.GetPersuasionProgress();
			int num2 = (int)ConversationManager.GetPersuasionGoalValue();
			for (int i = 1; i <= num2; i++)
			{
				bool isActive = i <= num;
				PersuasionProgress.Add(new BoolItemWithActionVM(null, isActive, null));
			}
			if (CurrentPersuasionOption != null)
			{
				CurrentCritFailChance = _currentPersuasionOption.CritFailChance;
				CurrentFailChance = _currentPersuasionOption.FailChance;
				CurrentCritSuccessChance = _currentPersuasionOption.CritSuccessChance;
				CurrentSuccessChance = _currentPersuasionOption.SuccessChance;
			}
			PersuasionHint = new BasicTooltipViewModel(() => GetPersuasionTooltip());
		}
	}

	private string GetPersuasionTooltip()
	{
		if (ConversationManager.GetPersuasionIsActive())
		{
			GameTexts.SetVariable("CURRENT_PROGRESS", (int)ConversationManager.GetPersuasionProgress());
			GameTexts.SetVariable("TARGET_PROGRESS", (int)ConversationManager.GetPersuasionGoalValue());
			return GameTexts.FindText("str_persuasion_tooltip").ToString();
		}
		return "";
	}

	private void RefreshChangeValues()
	{
		_manager.GetPersuasionChanceValues(out var _, out var _, out var _);
	}
}
