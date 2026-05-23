using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Decisions;

public class DecisionOptionVM : ViewModel
{
	private readonly Action<DecisionOptionVM> _onSelect;

	private readonly Action<DecisionOptionVM> _onSupportStrengthChange;

	private readonly KingdomElection _kingdomDecisionMaker;

	private bool _hasKingChoosen;

	private MBBindingList<DecisionSupporterVM> _supportersOfThisOption;

	private HeroVM _sponsor;

	private bool _isOptionForAbstain;

	private bool _isPlayerSupporter;

	private bool _isSelected;

	private bool _canBeChosen;

	private bool _isKingsOutcome;

	private bool _isHighlightEnabled;

	private int _winPercentage = -1;

	private int _influenceCost;

	private int _initialPercentage = -99;

	private int _currentSupportWeightIndex;

	private string _name;

	private string _description;

	private string _winPercentageStr;

	private string _sponsorWeightImagePath;

	private Supporter.SupportWeights _currentSupportWeight;

	private string _supportOption1Text;

	private bool _isSupportOption1Enabled;

	private string _supportOption2Text;

	private bool _isSupportOption2Enabled;

	private string _supportOption3Text;

	private bool _isSupportOption3Enabled;

	private HintViewModel _optionHint;

	public DecisionOutcome Option { get; private set; }

	public KingdomDecision Decision { get; private set; }

	[DataSourceProperty]
	public HintViewModel OptionHint
	{
		get
		{
			return _optionHint;
		}
		set
		{
			if (value != _optionHint)
			{
				_optionHint = value;
				OnPropertyChangedWithValue(value, "OptionHint");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<DecisionSupporterVM> SupportersOfThisOption
	{
		get
		{
			return _supportersOfThisOption;
		}
		set
		{
			if (value != _supportersOfThisOption)
			{
				_supportersOfThisOption = value;
				OnPropertyChangedWithValue(value, "SupportersOfThisOption");
			}
		}
	}

	[DataSourceProperty]
	public HeroVM Sponsor
	{
		get
		{
			return _sponsor;
		}
		set
		{
			if (value != _sponsor)
			{
				_sponsor = value;
				OnPropertyChangedWithValue(value, "Sponsor");
			}
		}
	}

	[DataSourceProperty]
	public string Name
	{
		get
		{
			return _name;
		}
		set
		{
			if (value != _name)
			{
				_name = value;
				OnPropertyChangedWithValue(value, "Name");
			}
		}
	}

	[DataSourceProperty]
	public string SponsorWeightImagePath
	{
		get
		{
			return _sponsorWeightImagePath;
		}
		set
		{
			if (value != _sponsorWeightImagePath)
			{
				_sponsorWeightImagePath = value;
				OnPropertyChangedWithValue(value, "SponsorWeightImagePath");
			}
		}
	}

	[DataSourceProperty]
	public bool CanBeChosen
	{
		get
		{
			return _canBeChosen;
		}
		set
		{
			if (value != _canBeChosen)
			{
				_canBeChosen = value;
				OnPropertyChangedWithValue(value, "CanBeChosen");
			}
		}
	}

	[DataSourceProperty]
	public bool IsKingsOutcome
	{
		get
		{
			return _isKingsOutcome;
		}
		set
		{
			if (value != _isKingsOutcome)
			{
				_isKingsOutcome = value;
				OnPropertyChangedWithValue(value, "IsKingsOutcome");
			}
		}
	}

	[DataSourceProperty]
	public bool IsPlayerSupporter
	{
		get
		{
			return _isPlayerSupporter;
		}
		set
		{
			if (value != _isPlayerSupporter)
			{
				_isPlayerSupporter = value;
				OnPropertyChangedWithValue(value, "IsPlayerSupporter");
			}
		}
	}

	[DataSourceProperty]
	public bool IsHighlightEnabled
	{
		get
		{
			return _isHighlightEnabled;
		}
		set
		{
			if (value != _isHighlightEnabled)
			{
				_isHighlightEnabled = value;
				OnPropertyChangedWithValue(value, "IsHighlightEnabled");
			}
		}
	}

	[DataSourceProperty]
	public int WinPercentage
	{
		get
		{
			return _winPercentage;
		}
		set
		{
			if (value != _winPercentage)
			{
				_winPercentage = value;
				OnPropertyChangedWithValue(value, "WinPercentage");
				GameTexts.SetVariable("NUMBER", value);
				WinPercentageStr = GameTexts.FindText("str_NUMBER_percent").ToString();
			}
		}
	}

	[DataSourceProperty]
	public string WinPercentageStr
	{
		get
		{
			return _winPercentageStr;
		}
		set
		{
			if (value != _winPercentageStr)
			{
				_winPercentageStr = value;
				OnPropertyChangedWithValue(value, "WinPercentageStr");
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
	public int InitialPercentage
	{
		get
		{
			return _initialPercentage;
		}
		set
		{
			if (value != _initialPercentage)
			{
				_initialPercentage = value;
				OnPropertyChangedWithValue(value, "InitialPercentage");
			}
		}
	}

	[DataSourceProperty]
	public int InfluenceCost
	{
		get
		{
			return _influenceCost;
		}
		set
		{
			if (value != _influenceCost)
			{
				_influenceCost = value;
				OnPropertyChangedWithValue(value, "InfluenceCost");
			}
		}
	}

	[DataSourceProperty]
	public bool IsSelected
	{
		get
		{
			return _isSelected;
		}
		set
		{
			if (value != _isSelected)
			{
				_isSelected = value;
				OnPropertyChangedWithValue(value, "IsSelected");
			}
		}
	}

	[DataSourceProperty]
	public bool IsOptionForAbstain
	{
		get
		{
			return _isOptionForAbstain;
		}
		set
		{
			if (value != _isOptionForAbstain)
			{
				_isOptionForAbstain = value;
				OnPropertyChangedWithValue(value, "IsOptionForAbstain");
			}
		}
	}

	[DataSourceProperty]
	public Supporter.SupportWeights CurrentSupportWeight
	{
		get
		{
			return _currentSupportWeight;
		}
		set
		{
			if (value != _currentSupportWeight)
			{
				_currentSupportWeight = value;
				OnPropertyChanged("CurrentSupportWeight");
				CurrentSupportWeightIndex = (int)value;
			}
		}
	}

	[DataSourceProperty]
	public int CurrentSupportWeightIndex
	{
		get
		{
			return _currentSupportWeightIndex;
		}
		set
		{
			if (value != _currentSupportWeightIndex)
			{
				_currentSupportWeightIndex = value;
				OnPropertyChangedWithValue(value, "CurrentSupportWeightIndex");
			}
		}
	}

	[DataSourceProperty]
	public string SupportOption1Text
	{
		get
		{
			return _supportOption1Text;
		}
		set
		{
			if (value != _supportOption1Text)
			{
				_supportOption1Text = value;
				OnPropertyChangedWithValue(value, "SupportOption1Text");
			}
		}
	}

	[DataSourceProperty]
	public string SupportOption2Text
	{
		get
		{
			return _supportOption2Text;
		}
		set
		{
			if (value != _supportOption2Text)
			{
				_supportOption2Text = value;
				OnPropertyChangedWithValue(value, "SupportOption2Text");
			}
		}
	}

	[DataSourceProperty]
	public string SupportOption3Text
	{
		get
		{
			return _supportOption3Text;
		}
		set
		{
			if (value != _supportOption3Text)
			{
				_supportOption3Text = value;
				OnPropertyChangedWithValue(value, "SupportOption3Text");
			}
		}
	}

	[DataSourceProperty]
	public bool IsSupportOption1Enabled
	{
		get
		{
			return _isSupportOption1Enabled;
		}
		set
		{
			if (value != _isSupportOption1Enabled)
			{
				_isSupportOption1Enabled = value;
				OnPropertyChangedWithValue(value, "IsSupportOption1Enabled");
			}
		}
	}

	[DataSourceProperty]
	public bool IsSupportOption2Enabled
	{
		get
		{
			return _isSupportOption2Enabled;
		}
		set
		{
			if (value != _isSupportOption2Enabled)
			{
				_isSupportOption2Enabled = value;
				OnPropertyChangedWithValue(value, "IsSupportOption2Enabled");
			}
		}
	}

	[DataSourceProperty]
	public bool IsSupportOption3Enabled
	{
		get
		{
			return _isSupportOption3Enabled;
		}
		set
		{
			if (value != _isSupportOption3Enabled)
			{
				_isSupportOption3Enabled = value;
				OnPropertyChangedWithValue(value, "IsSupportOption3Enabled");
			}
		}
	}

	public DecisionOptionVM(DecisionOutcome option, KingdomDecision decision, KingdomElection kingdomDecisionMaker, Action<DecisionOptionVM> onSelect, Action<DecisionOptionVM> onSupportStrengthChange)
	{
		_onSelect = onSelect;
		_onSupportStrengthChange = onSupportStrengthChange;
		_kingdomDecisionMaker = kingdomDecisionMaker;
		Decision = decision;
		CurrentSupportWeight = Supporter.SupportWeights.Choose;
		OptionHint = new HintViewModel();
		IsPlayerSupporter = !_kingdomDecisionMaker.IsPlayerChooser;
		SupportersOfThisOption = new MBBindingList<DecisionSupporterVM>();
		Option = option;
		if (option != null)
		{
			if (option.SponsorClan?.Leader != null)
			{
				Sponsor = new HeroVM(option.SponsorClan.Leader);
			}
			List<Supporter> supporterList = option.SupporterList;
			if (supporterList != null && supporterList.Count > 0)
			{
				foreach (Supporter supporter in option.SupporterList)
				{
					if (supporter.SupportWeight > Supporter.SupportWeights.StayNeutral)
					{
						if (supporter.Clan != option.SponsorClan)
						{
							SupportersOfThisOption.Add(new DecisionSupporterVM(supporter.Name, supporter.ImagePath, supporter.Clan, supporter.SupportWeight));
						}
						else
						{
							SponsorWeightImagePath = DecisionSupporterVM.GetSupporterWeightImagePath(supporter.SupportWeight);
						}
					}
				}
			}
			IsOptionForAbstain = false;
		}
		else
		{
			IsOptionForAbstain = true;
		}
		RefreshValues();
		RefreshSupportOptionEnabled();
		RefreshCanChooseOption();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		if (Option != null)
		{
			Name = Option.GetDecisionTitle().ToString();
			Description = Option.GetDecisionDescription().ToString();
		}
		else
		{
			Name = GameTexts.FindText("str_abstain").ToString();
			Description = GameTexts.FindText(IsPlayerSupporter ? "str_kingdom_decision_abstain_desc" : "str_kingdom_decision_ruler_abstain_desc").ToString();
		}
		SupportersOfThisOption?.ApplyActionOnAllItems(delegate(DecisionSupporterVM x)
		{
			x.RefreshValues();
		});
	}

	private void ExecuteShowSupporterTooltip()
	{
		DecisionOutcome option = Option;
		if (option == null || option.SupporterList.Count <= 0)
		{
			return;
		}
		List<TooltipProperty> list = new List<TooltipProperty>();
		_kingdomDecisionMaker.DetermineOfficialSupport();
		foreach (Supporter supporter in Option.SupporterList)
		{
			if (supporter.SupportWeight > Supporter.SupportWeights.StayNeutral && !supporter.IsPlayer)
			{
				int influenceCost = Decision.GetInfluenceCost(Option, supporter.Clan, supporter.SupportWeight);
				GameTexts.SetVariable("AMOUNT", influenceCost);
				GameTexts.SetVariable("INFLUENCE_ICON", "{=!}<img src=\"General\\Icons\\Influence@2x\" extend=\"5\">");
				list.Add(new TooltipProperty(supporter.Name.ToString(), GameTexts.FindText("str_amount_with_influence_icon").ToString(), 0));
			}
		}
		InformationManager.ShowTooltip(typeof(List<TooltipProperty>), list);
	}

	private void ExecuteHideSupporterTooltip()
	{
		MBInformationManager.HideInformations();
	}

	private void RefreshSupportOptionEnabled()
	{
		int influenceCostOfOutcome = _kingdomDecisionMaker.GetInfluenceCostOfOutcome(Option, Clan.PlayerClan, Supporter.SupportWeights.SlightlyFavor);
		int influenceCostOfOutcome2 = _kingdomDecisionMaker.GetInfluenceCostOfOutcome(Option, Clan.PlayerClan, Supporter.SupportWeights.StronglyFavor);
		int influenceCostOfOutcome3 = _kingdomDecisionMaker.GetInfluenceCostOfOutcome(Option, Clan.PlayerClan, Supporter.SupportWeights.FullyPush);
		SupportOption1Text = influenceCostOfOutcome.ToString();
		SupportOption2Text = influenceCostOfOutcome2.ToString();
		SupportOption3Text = influenceCostOfOutcome3.ToString();
		IsSupportOption1Enabled = (float)influenceCostOfOutcome <= Clan.PlayerClan.Influence && !IsOptionForAbstain;
		IsSupportOption2Enabled = (float)influenceCostOfOutcome2 <= Clan.PlayerClan.Influence && !IsOptionForAbstain;
		IsSupportOption3Enabled = (float)influenceCostOfOutcome3 <= Clan.PlayerClan.Influence && !IsOptionForAbstain;
	}

	private void OnSupportStrengthChange(int index)
	{
		if (!IsOptionForAbstain)
		{
			switch (index)
			{
			case 0:
				CurrentSupportWeight = Supporter.SupportWeights.SlightlyFavor;
				break;
			case 1:
				CurrentSupportWeight = Supporter.SupportWeights.StronglyFavor;
				break;
			case 2:
				CurrentSupportWeight = Supporter.SupportWeights.FullyPush;
				break;
			}
			_kingdomDecisionMaker.OnPlayerSupport((!IsOptionForAbstain) ? Option : null, CurrentSupportWeight);
			_onSupportStrengthChange(this);
		}
	}

	public void AfterKingChooseOutcome()
	{
		_hasKingChoosen = true;
		RefreshCanChooseOption();
	}

	private void RefreshCanChooseOption()
	{
		if (_hasKingChoosen)
		{
			CanBeChosen = false;
			return;
		}
		if (IsOptionForAbstain)
		{
			CanBeChosen = true;
			return;
		}
		if (IsPlayerSupporter)
		{
			CanBeChosen = (float)_kingdomDecisionMaker.GetInfluenceCostOfOutcome(Option, Clan.PlayerClan, Supporter.SupportWeights.SlightlyFavor) <= Clan.PlayerClan.Influence;
		}
		else
		{
			int influenceCostOfOutcome = _kingdomDecisionMaker.GetInfluenceCostOfOutcome(Option, Clan.PlayerClan, Supporter.SupportWeights.Choose);
			CanBeChosen = (float)influenceCostOfOutcome <= Clan.PlayerClan.Influence || influenceCostOfOutcome == 0;
		}
		OptionHint.HintText = (CanBeChosen ? TextObject.GetEmpty() : new TextObject("{=Xmw93W6a}Not Enough Influence"));
	}

	private void ExecuteSelection()
	{
		_onSelect(this);
		Game.Current?.EventManager?.TriggerEvent(new PlayerSelectedAKingdomDecisionOptionEvent(Option));
	}
}
