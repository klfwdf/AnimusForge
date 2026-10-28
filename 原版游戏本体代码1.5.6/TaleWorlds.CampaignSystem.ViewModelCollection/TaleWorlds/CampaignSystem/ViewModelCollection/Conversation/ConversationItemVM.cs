using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Conversation.Persuasion;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Conversation;

public class ConversationItemVM : ViewModel
{
	public Action<int> ActionWihIntIndex;

	public Action<ConversationItemVM> _setCurrentAnswer;

	public int Index;

	private Action _onReadyToContinue;

	private bool _hasPersuasion;

	private bool _isSpecial;

	private string _itemText;

	private int _iconType;

	private bool _isEnabled;

	private PersuasionOptionVM _persuasionItem;

	private HintViewModel _optionHint;

	private ConversationSentenceOption _option
	{
		get
		{
			List<ConversationSentenceOption> curOptions = Campaign.Current.ConversationManager.CurOptions;
			if (curOptions == null || curOptions.Count <= 0)
			{
				return default(ConversationSentenceOption);
			}
			return Campaign.Current.ConversationManager.CurOptions[Index];
		}
	}

	[DataSourceProperty]
	public PersuasionOptionVM PersuasionItem
	{
		get
		{
			return _persuasionItem;
		}
		set
		{
			if (_persuasionItem != value)
			{
				_persuasionItem = value;
				OnPropertyChangedWithValue(value, "PersuasionItem");
			}
		}
	}

	[DataSourceProperty]
	public bool HasPersuasion
	{
		get
		{
			return _hasPersuasion;
		}
		set
		{
			if (_hasPersuasion != value)
			{
				_hasPersuasion = value;
				OnPropertyChangedWithValue(value, "HasPersuasion");
			}
		}
	}

	[DataSourceProperty]
	public int IconType
	{
		get
		{
			return _iconType;
		}
		set
		{
			if (_iconType != value)
			{
				_iconType = value;
				OnPropertyChangedWithValue(value, "IconType");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel OptionHint
	{
		get
		{
			return _optionHint;
		}
		set
		{
			if (_optionHint != value)
			{
				_optionHint = value;
				OnPropertyChangedWithValue(value, "OptionHint");
			}
		}
	}

	[DataSourceProperty]
	public string ItemText
	{
		get
		{
			return _itemText;
		}
		set
		{
			if (_itemText != value)
			{
				_itemText = value;
				OnPropertyChangedWithValue(value, "ItemText");
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
			}
		}
	}

	[DataSourceProperty]
	public bool IsSpecial
	{
		get
		{
			return _isSpecial;
		}
		set
		{
			if (_isSpecial != value)
			{
				_isSpecial = value;
				OnPropertyChangedWithValue(value, "IsSpecial");
			}
		}
	}

	public ConversationItemVM(Action<int> action, Action onReadyToContinue, Action<ConversationItemVM> setCurrentAnswer, int index)
	{
		ActionWihIntIndex = action;
		Index = index;
		_onReadyToContinue = onReadyToContinue;
		IsEnabled = _option.IsClickable;
		HasPersuasion = _option.HasPersuasion;
		_setCurrentAnswer = setCurrentAnswer;
		PersuasionItem = new PersuasionOptionVM(Campaign.Current.ConversationManager, index, OnReadyToContinue);
		IsSpecial = _option.IsSpecial;
		RefreshValues();
	}

	private void OnReadyToContinue()
	{
		_onReadyToContinue.DynamicInvokeWithLog();
	}

	public ConversationItemVM()
	{
		Index = 0;
		ItemText = "";
		IsEnabled = false;
		OptionHint = new HintViewModel();
		HasPersuasion = false;
		_setCurrentAnswer = null;
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		string text = _option.Text?.ToString() ?? "";
		OptionHint = new HintViewModel((_option.HintText != null) ? _option.HintText : TextObject.GetEmpty());
		PersuasionItem?.RefreshValues();
		if (PersuasionItem != null)
		{
			string persuasionAdditionalText = PersuasionItem.GetPersuasionAdditionalText();
			if (!string.IsNullOrEmpty(persuasionAdditionalText))
			{
				GameTexts.SetVariable("STR1", text);
				GameTexts.SetVariable("STR2", persuasionAdditionalText);
				text = GameTexts.FindText("str_STR1_space_STR2").ToString();
			}
		}
		ItemText = text;
	}

	public void ExecuteAction()
	{
		ActionWihIntIndex?.Invoke(Index);
	}

	public void SetCurrentAnswer()
	{
		_setCurrentAnswer?.Invoke(this);
	}

	public void ResetCurrentAnswer()
	{
		_setCurrentAnswer(null);
	}

	internal void OnPersuasionProgress(Tuple<PersuasionOptionArgs, PersuasionOptionResult> result)
	{
		PersuasionItem?.OnPersuasionProgress(result);
	}
}
