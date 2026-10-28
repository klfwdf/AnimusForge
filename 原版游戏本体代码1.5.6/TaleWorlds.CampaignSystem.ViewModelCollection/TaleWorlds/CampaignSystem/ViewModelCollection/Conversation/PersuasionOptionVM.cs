using System;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Conversation.Persuasion;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Conversation;

public class PersuasionOptionVM : ViewModel
{
	private const int _minSkillValueForPositive = 50;

	private readonly ConversationManager _manager;

	private readonly PersuasionOptionArgs _args;

	private readonly Action _onReadyToContinue;

	private readonly int _index;

	private int _critFailChance;

	private int _failChance;

	private int _successChance;

	private int _critSuccessChance;

	private bool _isPersuasionResultReady;

	private int _persuasionResultIndex = -1;

	private bool _isABlockingOption;

	private bool _isAProgressingOption;

	private string _critFailChanceText;

	private string _failChanceText;

	private string _successChanceText;

	private string _critSuccessChanceText;

	private BasicTooltipViewModel _critFailHint;

	private BasicTooltipViewModel _failHint;

	private BasicTooltipViewModel _successHint;

	private BasicTooltipViewModel _critSuccessHint;

	private HintViewModel _progressingOptionHint;

	private HintViewModel _blockingOptionHint;

	private ConversationSentenceOption _option => _manager.CurOptions[_index];

	[DataSourceProperty]
	public bool IsPersuasionResultReady
	{
		get
		{
			return _isPersuasionResultReady;
		}
		set
		{
			if (_isPersuasionResultReady != value)
			{
				_isPersuasionResultReady = value;
				OnPropertyChangedWithValue(value, "IsPersuasionResultReady");
			}
		}
	}

	[DataSourceProperty]
	public bool IsABlockingOption
	{
		get
		{
			return _isABlockingOption;
		}
		set
		{
			if (_isABlockingOption != value)
			{
				_isABlockingOption = value;
				OnPropertyChangedWithValue(value, "IsABlockingOption");
			}
		}
	}

	[DataSourceProperty]
	public bool IsAProgressingOption
	{
		get
		{
			return _isAProgressingOption;
		}
		set
		{
			if (_isAProgressingOption != value)
			{
				_isAProgressingOption = value;
				OnPropertyChangedWithValue(value, "IsAProgressingOption");
			}
		}
	}

	[DataSourceProperty]
	public int SuccessChance
	{
		get
		{
			return _successChance;
		}
		set
		{
			if (_successChance != value)
			{
				_successChance = value;
				OnPropertyChangedWithValue(value, "SuccessChance");
			}
		}
	}

	[DataSourceProperty]
	public int PersuasionResultIndex
	{
		get
		{
			return _persuasionResultIndex;
		}
		set
		{
			if (_persuasionResultIndex != value)
			{
				_persuasionResultIndex = value;
				OnPropertyChangedWithValue(value, "PersuasionResultIndex");
			}
		}
	}

	[DataSourceProperty]
	public int FailChance
	{
		get
		{
			return _failChance;
		}
		set
		{
			if (_failChance != value)
			{
				_failChance = value;
				OnPropertyChangedWithValue(value, "FailChance");
			}
		}
	}

	[DataSourceProperty]
	public int CritSuccessChance
	{
		get
		{
			return _critSuccessChance;
		}
		set
		{
			if (_critSuccessChance != value)
			{
				_critSuccessChance = value;
				OnPropertyChangedWithValue(value, "CritSuccessChance");
			}
		}
	}

	[DataSourceProperty]
	public int CritFailChance
	{
		get
		{
			return _critFailChance;
		}
		set
		{
			if (_critFailChance != value)
			{
				_critFailChance = value;
				OnPropertyChangedWithValue(value, "CritFailChance");
			}
		}
	}

	[DataSourceProperty]
	public string FailChanceText
	{
		get
		{
			return _failChanceText;
		}
		set
		{
			if (_failChanceText != value)
			{
				_failChanceText = value;
				OnPropertyChangedWithValue(value, "FailChanceText");
			}
		}
	}

	[DataSourceProperty]
	public string CritFailChanceText
	{
		get
		{
			return _critFailChanceText;
		}
		set
		{
			if (_critFailChanceText != value)
			{
				_critFailChanceText = value;
				OnPropertyChangedWithValue(value, "CritFailChanceText");
			}
		}
	}

	[DataSourceProperty]
	public string SuccessChanceText
	{
		get
		{
			return _successChanceText;
		}
		set
		{
			if (_successChanceText != value)
			{
				_successChanceText = value;
				OnPropertyChangedWithValue(value, "SuccessChanceText");
			}
		}
	}

	[DataSourceProperty]
	public string CritSuccessChanceText
	{
		get
		{
			return _critSuccessChanceText;
		}
		set
		{
			if (_critSuccessChanceText != value)
			{
				_critSuccessChanceText = value;
				OnPropertyChangedWithValue(value, "CritSuccessChanceText");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel CritFailHint
	{
		get
		{
			return _critFailHint;
		}
		set
		{
			if (_critFailHint != value)
			{
				_critFailHint = value;
				OnPropertyChangedWithValue(value, "CritFailHint");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel FailHint
	{
		get
		{
			return _failHint;
		}
		set
		{
			if (_failHint != value)
			{
				_failHint = value;
				OnPropertyChangedWithValue(value, "FailHint");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel SuccessHint
	{
		get
		{
			return _successHint;
		}
		set
		{
			if (_successHint != value)
			{
				_successHint = value;
				OnPropertyChangedWithValue(value, "SuccessHint");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel CritSuccessHint
	{
		get
		{
			return _critSuccessHint;
		}
		set
		{
			if (_critSuccessHint != value)
			{
				_critSuccessHint = value;
				OnPropertyChangedWithValue(value, "CritSuccessHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel BlockingOptionHint
	{
		get
		{
			return _blockingOptionHint;
		}
		set
		{
			if (_blockingOptionHint != value)
			{
				_blockingOptionHint = value;
				OnPropertyChangedWithValue(value, "BlockingOptionHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel ProgressingOptionHint
	{
		get
		{
			return _progressingOptionHint;
		}
		set
		{
			if (_progressingOptionHint != value)
			{
				_progressingOptionHint = value;
				OnPropertyChangedWithValue(value, "ProgressingOptionHint");
			}
		}
	}

	public PersuasionOptionVM(ConversationManager manager, int index, Action onReadyToContinue)
	{
		_index = index;
		_manager = manager;
		_onReadyToContinue = onReadyToContinue;
		if (ConversationManager.GetPersuasionIsActive() && _option.HasPersuasion)
		{
			_manager.GetPersuasionChances(_option, out var successChance, out var critSuccessChance, out var critFailChance, out var failChance);
			CritFailChance = (int)(critFailChance * 100f);
			FailChance = (int)(failChance * 100f);
			SuccessChance = (int)(successChance * 100f);
			CritSuccessChance = (int)(critSuccessChance * 100f);
			_args = _option.PersuationOptionArgs;
		}
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		if (ConversationManager.GetPersuasionIsActive() && _option.HasPersuasion)
		{
			GameTexts.SetVariable("NUMBER", CritFailChance);
			CritFailChanceText = GameTexts.FindText("str_NUMBER_percent").ToString();
			GameTexts.SetVariable("NUMBER", FailChance);
			FailChanceText = GameTexts.FindText("str_NUMBER_percent").ToString();
			GameTexts.SetVariable("NUMBER", SuccessChance);
			SuccessChanceText = GameTexts.FindText("str_NUMBER_percent").ToString();
			GameTexts.SetVariable("NUMBER", CritSuccessChance);
			CritSuccessChanceText = GameTexts.FindText("str_NUMBER_percent").ToString();
			CritFailHint = new BasicTooltipViewModel(delegate
			{
				GameTexts.SetVariable("LEFT", GameTexts.FindText("str_persuasion_critical_fail"));
				GameTexts.SetVariable("NUMBER", CritFailChance);
				GameTexts.SetVariable("RIGHT", GameTexts.FindText("str_NUMBER_percent"));
				return GameTexts.FindText("str_LEFT_colon_RIGHT_wSpaceAfterColon").ToString();
			});
			FailHint = new BasicTooltipViewModel(delegate
			{
				GameTexts.SetVariable("LEFT", GameTexts.FindText("str_persuasion_fail"));
				GameTexts.SetVariable("NUMBER", FailChance);
				GameTexts.SetVariable("RIGHT", GameTexts.FindText("str_NUMBER_percent"));
				return GameTexts.FindText("str_LEFT_colon_RIGHT_wSpaceAfterColon").ToString();
			});
			SuccessHint = new BasicTooltipViewModel(delegate
			{
				GameTexts.SetVariable("LEFT", GameTexts.FindText("str_persuasion_success"));
				GameTexts.SetVariable("NUMBER", SuccessChance);
				GameTexts.SetVariable("RIGHT", GameTexts.FindText("str_NUMBER_percent"));
				return GameTexts.FindText("str_LEFT_colon_RIGHT_wSpaceAfterColon").ToString();
			});
			CritSuccessHint = new BasicTooltipViewModel(delegate
			{
				GameTexts.SetVariable("LEFT", GameTexts.FindText("str_persuasion_critical_success"));
				GameTexts.SetVariable("NUMBER", CritSuccessChance);
				GameTexts.SetVariable("RIGHT", GameTexts.FindText("str_NUMBER_percent"));
				return GameTexts.FindText("str_LEFT_colon_RIGHT_wSpaceAfterColon").ToString();
			});
			ProgressingOptionHint = new HintViewModel(GameTexts.FindText("str_persuasion_progressing_hint"));
			BlockingOptionHint = new HintViewModel(GameTexts.FindText("str_persuasion_blocking_hint"));
			IsABlockingOption = _args.CanBlockOtherOption;
			IsAProgressingOption = _args.CanMoveToTheNextReservation;
		}
	}

	internal void OnPersuasionProgress(Tuple<PersuasionOptionArgs, PersuasionOptionResult> result)
	{
		IsPersuasionResultReady = true;
		if (result.Item1 == _args)
		{
			PersuasionResultIndex = (int)result.Item2;
		}
	}

	public string GetPersuasionAdditionalText()
	{
		string text = null;
		if (_args != null)
		{
			if (_args.SkillUsed != null)
			{
				text = ((Hero.MainHero.GetSkillValue(_args.SkillUsed) <= 50) ? "<a style=\"Conversation.Persuasion.Neutral\"><b>{TEXT}</b></a>" : "<a style=\"Conversation.Persuasion.Positive\"><b>{TEXT}</b></a>").Replace("{TEXT}", _args.SkillUsed.Name.ToString());
			}
			if (_args.TraitUsed != null && !_args.TraitUsed.IsHidden)
			{
				string text2 = null;
				int traitLevel = Hero.MainHero.GetTraitLevel(_args.TraitUsed);
				text2 = ((traitLevel != 0) ? (((traitLevel > 0 && _args.TraitEffect == TraitEffect.Positive) || (traitLevel < 0 && _args.TraitEffect == TraitEffect.Negative)) ? "<a style=\"Conversation.Persuasion.Positive\"><b>{TEXT}</b></a>" : "<a style=\"Conversation.Persuasion.Negative\"><b>{TEXT}</b></a>") : "<a style=\"Conversation.Persuasion.Neutral\"><b>{TEXT}</b></a>");
				text2 = text2.Replace("{TEXT}", _args.TraitUsed.Name.ToString());
				if (text != null)
				{
					GameTexts.SetVariable("LEFT", text);
					GameTexts.SetVariable("RIGHT", text2);
					text = GameTexts.FindText("str_LEFT_comma_RIGHT").ToString();
				}
				else
				{
					text = text2;
				}
			}
			if (_args.TraitCorrelation != null)
			{
				Tuple<TraitObject, int>[] traitCorrelation = _args.TraitCorrelation;
				foreach (Tuple<TraitObject, int> tuple in traitCorrelation)
				{
					if (tuple.Item2 != 0 && _args.TraitUsed != tuple.Item1 && !tuple.Item1.IsHidden)
					{
						string text3 = null;
						int traitLevel2 = Hero.MainHero.GetTraitLevel(tuple.Item1);
						text3 = ((traitLevel2 != 0) ? ((traitLevel2 * tuple.Item2 > 0) ? "<a style=\"Conversation.Persuasion.Positive\"><b>{TEXT}</b></a>" : "<a style=\"Conversation.Persuasion.Negative\"><b>{TEXT}</b></a>") : "<a style=\"Conversation.Persuasion.Neutral\"><b>{TEXT}</b></a>");
						text3 = text3.Replace("{TEXT}", tuple.Item1.Name.ToString());
						if (text != null)
						{
							GameTexts.SetVariable("LEFT", text);
							GameTexts.SetVariable("RIGHT", text3);
							text = GameTexts.FindText("str_LEFT_comma_RIGHT").ToString();
						}
						else
						{
							text = text3;
						}
					}
				}
			}
		}
		if (!string.IsNullOrEmpty(text))
		{
			GameTexts.SetVariable("STR", text);
			return GameTexts.FindText("str_STR_in_parentheses").ToString();
		}
		return string.Empty;
	}

	public void ExecuteReadyToContinue()
	{
		_onReadyToContinue?.DynamicInvokeWithLog();
	}
}
