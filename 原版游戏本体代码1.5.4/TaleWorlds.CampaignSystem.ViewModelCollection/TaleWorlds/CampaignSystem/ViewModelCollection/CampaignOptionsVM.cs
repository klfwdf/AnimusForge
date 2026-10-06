using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection;

public class CampaignOptionsVM : ViewModel
{
	private readonly Action _onClose;

	private string _titleText;

	private string _doneText;

	private string _resetTutorialText;

	private CampaignOptionsControllerVM _optionsController;

	[DataSourceProperty]
	public CampaignOptionsControllerVM OptionsController
	{
		get
		{
			return _optionsController;
		}
		set
		{
			if (value != _optionsController)
			{
				_optionsController = value;
				OnPropertyChangedWithValue(value, "OptionsController");
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
	public string DoneText
	{
		get
		{
			return _doneText;
		}
		set
		{
			if (value != _doneText)
			{
				_doneText = value;
				OnPropertyChangedWithValue(value, "DoneText");
			}
		}
	}

	[DataSourceProperty]
	public string ResetTutorialText
	{
		get
		{
			return _resetTutorialText;
		}
		set
		{
			if (value != _resetTutorialText)
			{
				_resetTutorialText = value;
				OnPropertyChangedWithValue(value, "ResetTutorialText");
			}
		}
	}

	public CampaignOptionsVM(Action onClose)
	{
		_onClose = onClose;
		MBBindingList<CampaignOptionItemVM> mBBindingList = new MBBindingList<CampaignOptionItemVM>();
		List<ICampaignOptionData> gameplayCampaignOptions = CampaignOptionsManager.GetGameplayCampaignOptions();
		for (int i = 0; i < gameplayCampaignOptions.Count; i++)
		{
			mBBindingList.Add(new CampaignOptionItemVM(gameplayCampaignOptions[i]));
		}
		OptionsController = new CampaignOptionsControllerVM(mBBindingList);
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		TitleText = new TextObject("{=PXT6aA4J}Campaign Options").ToString();
		DoneText = GameTexts.FindText("str_done").ToString();
		ResetTutorialText = new TextObject("{=oUz16Nav}Reset Tutorial").ToString();
		OptionsController.RefreshValues();
	}

	public void ExecuteDone()
	{
		_onClose?.Invoke();
	}
}
