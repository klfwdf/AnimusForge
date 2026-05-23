using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.ViewModelCollection.Input;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Core.ViewModelCollection.Tutorial;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Decisions.ItemTypes;

public class DecisionItemBaseVM : ViewModel
{
	protected enum DecisionTypes
	{
		Default,
		Settlement,
		ExpelClan,
		Policy,
		DeclareWar,
		MakePeace,
		KingSelection,
		StartAlliance,
		AcceptCallToWarAgreement,
		ProposeCallToWarAgreement,
		Trade
	}

	protected readonly KingdomDecision _decision;

	private readonly Action _onDecisionOver;

	private DecisionOptionVM _currentSelectedOption;

	private bool _finalSelectionDone;

	private bool _isDecisionOptionsHighlightEnabled;

	private string _decisionOptionsHighlightID = "DecisionOptions";

	private string _latestTutorialElementID;

	private InputKeyItemVM _doneInputKey;

	private int _decisionType;

	private bool _isActive;

	private bool _isPlayerSupporter;

	private bool _canEndDecision;

	private bool _isKingsDecisionOver;

	private int _currentStageIndex = -1;

	private string _titleText;

	private string _doneText;

	private string _descriptionText;

	private string _influenceCostText;

	private string _totalInfluenceText;

	private string _increaseRelationText;

	private HintViewModel _endDecisionHint;

	private MBBindingList<DecisionOptionVM> _decisionOptionsList;

	public KingdomElection KingdomDecisionMaker { get; private set; }

	private float _currentInfluenceCost
	{
		get
		{
			if (_currentSelectedOption != null && !_currentSelectedOption.IsOptionForAbstain)
			{
				if (!IsPlayerSupporter)
				{
					return Campaign.Current.Models.ClanPoliticsModel.GetInfluenceRequiredToOverrideKingdomDecision(TaleWorlds.Core.Extensions.MaxBy(KingdomDecisionMaker.PossibleOutcomes, (DecisionOutcome o) => o.WinChance), _currentSelectedOption.Option, _decision);
				}
				if (_currentSelectedOption.CurrentSupportWeight != Supporter.SupportWeights.Choose)
				{
					return KingdomDecisionMaker.GetInfluenceCostOfOutcome(_currentSelectedOption.Option, Clan.PlayerClan, _currentSelectedOption.CurrentSupportWeight);
				}
			}
			return 0f;
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM DoneInputKey
	{
		get
		{
			return _doneInputKey;
		}
		set
		{
			if (value != _doneInputKey)
			{
				_doneInputKey = value;
				OnPropertyChangedWithValue(value, "DoneInputKey");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel EndDecisionHint
	{
		get
		{
			return _endDecisionHint;
		}
		set
		{
			if (value != _endDecisionHint)
			{
				_endDecisionHint = value;
				OnPropertyChangedWithValue(value, "EndDecisionHint");
			}
		}
	}

	[DataSourceProperty]
	public int DecisionType
	{
		get
		{
			return _decisionType;
		}
		set
		{
			if (value != _decisionType)
			{
				_decisionType = value;
				OnPropertyChangedWithValue(value, "DecisionType");
			}
		}
	}

	[DataSourceProperty]
	public string TotalInfluenceText
	{
		get
		{
			return _totalInfluenceText;
		}
		set
		{
			if (value != _totalInfluenceText)
			{
				_totalInfluenceText = value;
				OnPropertyChangedWithValue(value, "TotalInfluenceText");
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
	public bool CanEndDecision
	{
		get
		{
			return _canEndDecision;
		}
		set
		{
			if (value != _canEndDecision)
			{
				_canEndDecision = value;
				OnPropertyChangedWithValue(value, "CanEndDecision");
			}
		}
	}

	[DataSourceProperty]
	public bool IsKingsDecisionOver
	{
		get
		{
			return _isKingsDecisionOver;
		}
		set
		{
			if (value != _isKingsDecisionOver)
			{
				_isKingsDecisionOver = value;
				OnPropertyChangedWithValue(value, "IsKingsDecisionOver");
			}
		}
	}

	[DataSourceProperty]
	public string RelationChangeText
	{
		get
		{
			return _increaseRelationText;
		}
		set
		{
			if (value != _increaseRelationText)
			{
				_increaseRelationText = value;
				OnPropertyChangedWithValue(value, "RelationChangeText");
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
	public string InfluenceCostText
	{
		get
		{
			return _influenceCostText;
		}
		set
		{
			if (value != _influenceCostText)
			{
				_influenceCostText = value;
				OnPropertyChangedWithValue(value, "InfluenceCostText");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<DecisionOptionVM> DecisionOptionsList
	{
		get
		{
			return _decisionOptionsList;
		}
		set
		{
			if (value != _decisionOptionsList)
			{
				_decisionOptionsList = value;
				OnPropertyChangedWithValue(value, "DecisionOptionsList");
			}
		}
	}

	public DecisionItemBaseVM(KingdomDecision decision, Action onDecisionOver)
	{
		_decision = decision;
		_onDecisionOver = onDecisionOver;
		DecisionType = 0;
		DecisionOptionsList = new MBBindingList<DecisionOptionVM>();
		EndDecisionHint = new HintViewModel();
		CampaignEvents.KingdomDecisionConcluded.AddNonSerializedListener(this, OnKingdomDecisionConcluded);
		RefreshValues();
		InitValues();
		Game.Current?.EventManager?.RegisterEvent<TutorialNotificationElementChangeEvent>(OnTutorialNotificationElementIDChange);
	}

	private void OnKingdomDecisionConcluded(KingdomDecision decision, DecisionOutcome outcome, bool isPlayerInvolved)
	{
		if (decision != _decision)
		{
			return;
		}
		IsKingsDecisionOver = true;
		CurrentStageIndex = 1;
		foreach (DecisionOptionVM decisionOptions in DecisionOptionsList)
		{
			if (decisionOptions.Option == outcome)
			{
				decisionOptions.IsKingsOutcome = true;
			}
			decisionOptions.AfterKingChooseOutcome();
		}
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		DoneText = GameTexts.FindText("str_done").ToString();
		GameTexts.SetVariable("TOTAL_INFLUENCE", TaleWorlds.Library.MathF.Round(Hero.MainHero.Clan.Influence));
		TotalInfluenceText = GameTexts.FindText("str_total_influence").ToString();
		RefreshInfluenceCost();
		DecisionOptionsList?.ApplyActionOnAllItems(delegate(DecisionOptionVM x)
		{
			x.RefreshValues();
		});
	}

	protected virtual void InitValues()
	{
		DecisionOptionsList.Clear();
		KingdomDecisionMaker = new KingdomElection(_decision);
		KingdomDecisionMaker.StartElection();
		CurrentStageIndex = (KingdomDecisionMaker.IsPlayerChooser ? 1 : 0);
		IsPlayerSupporter = !KingdomDecisionMaker.IsPlayerChooser;
		KingdomDecisionMaker.DetermineOfficialSupport();
		foreach (DecisionOutcome possibleOutcome in KingdomDecisionMaker.PossibleOutcomes)
		{
			DecisionOptionVM item = new DecisionOptionVM(possibleOutcome, _decision, KingdomDecisionMaker, OnChangeVote, OnSupportStrengthChange)
			{
				WinPercentage = TaleWorlds.Library.MathF.Round(possibleOutcome.WinChance * 100f),
				InitialPercentage = TaleWorlds.Library.MathF.Round(possibleOutcome.WinChance * 100f)
			};
			DecisionOptionsList.Add(item);
		}
		DecisionOptionVM item2 = new DecisionOptionVM(null, null, KingdomDecisionMaker, OnChangeVote, OnSupportStrengthChange);
		DecisionOptionsList.Add(item2);
		TitleText = KingdomDecisionMaker.GetTitle().ToString();
		DescriptionText = KingdomDecisionMaker.GetDescription().ToString();
		RefreshInfluenceCost();
		RefreshCanEndDecision();
		RefreshRelationChangeText();
		IsActive = true;
	}

	private void OnChangeVote(DecisionOptionVM target)
	{
		if (_currentSelectedOption != target)
		{
			if (_currentSelectedOption != null)
			{
				_currentSelectedOption.IsSelected = false;
			}
			_currentSelectedOption = target;
			_currentSelectedOption.IsSelected = true;
			if (_currentSelectedOption.IsOptionForAbstain && !IsPlayerSupporter)
			{
				KingdomDecisionMaker.OnPlayerAbstainedAsRuler();
			}
			else
			{
				KingdomDecisionMaker.OnPlayerSupport(_currentSelectedOption.Option, _currentSelectedOption.CurrentSupportWeight);
			}
			RefreshWinPercentages();
			RefreshInfluenceCost();
			RefreshCanEndDecision();
			RefreshRelationChangeText();
		}
	}

	private void OnSupportStrengthChange(DecisionOptionVM option)
	{
		RefreshWinPercentages();
		RefreshCanEndDecision();
		RefreshRelationChangeText();
		RefreshInfluenceCost();
	}

	private void RefreshWinPercentages()
	{
		KingdomDecisionMaker.DetermineOfficialSupport();
		foreach (DecisionOutcome option in KingdomDecisionMaker.PossibleOutcomes)
		{
			DecisionOptionVM decisionOptionVM = DecisionOptionsList.FirstOrDefault((DecisionOptionVM c) => c.Option == option);
			if (decisionOptionVM == null)
			{
				Debug.FailedAssert("Couldn't find option to update win chance for!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\KingdomManagement\\Decisions\\ItemTypes\\DecisionItemBaseVM.cs", "RefreshWinPercentages", 190);
			}
			else
			{
				decisionOptionVM.WinPercentage = (int)TaleWorlds.Library.MathF.Round(option.WinChance * 100f, 2);
			}
		}
		int num = DecisionOptionsList.Where((DecisionOptionVM d) => !d.IsOptionForAbstain).Sum((DecisionOptionVM d) => d.WinPercentage);
		if (num == 100)
		{
			return;
		}
		int num2 = 100 - num;
		List<DecisionOptionVM> list = DecisionOptionsList.Where((DecisionOptionVM opt) => opt.Sponsor != null).ToList();
		int num3 = list.Select((DecisionOptionVM opt) => opt.WinPercentage).Sum();
		if (num3 == 0)
		{
			int num4 = num2 / list.Count;
			foreach (DecisionOptionVM item in list)
			{
				item.WinPercentage += num4;
			}
			list[0].WinPercentage += num2 - num4 * list.Count;
			return;
		}
		int num5 = 0;
		foreach (DecisionOptionVM item2 in list.Where((DecisionOptionVM opt) => opt.WinPercentage > 0).ToList())
		{
			int num6 = TaleWorlds.Library.MathF.Floor((float)num2 * ((float)item2.WinPercentage / (float)num3));
			item2.WinPercentage += num6;
			num5 += num6;
		}
		list[0].WinPercentage += num2 - num5;
	}

	private void RefreshInfluenceCost()
	{
		if (_currentInfluenceCost > 0f)
		{
			GameTexts.SetVariable("AMOUNT", _currentInfluenceCost);
			GameTexts.SetVariable("INFLUENCE_ICON", "{=!}<img src=\"General\\Icons\\Influence@2x\" extend=\"5\">");
			InfluenceCostText = GameTexts.FindText(IsPlayerSupporter ? "str_decision_influence_cost" : "str_decision_ruler_influence_cost").ToString();
		}
		else
		{
			InfluenceCostText = "";
		}
	}

	private void RefreshRelationChangeText()
	{
		RelationChangeText = "";
		DecisionOptionVM currentSelectedOption = _currentSelectedOption;
		if (currentSelectedOption == null || currentSelectedOption.IsOptionForAbstain)
		{
			return;
		}
		DecisionOptionVM currentSelectedOption2 = _currentSelectedOption;
		if (currentSelectedOption2 != null && currentSelectedOption2.CurrentSupportWeight == Supporter.SupportWeights.Choose)
		{
			return;
		}
		foreach (DecisionOptionVM decisionOptions in DecisionOptionsList)
		{
			if (decisionOptions.Option?.SponsorClan != null && decisionOptions.Option.SponsorClan != Clan.PlayerClan)
			{
				bool num = _currentSelectedOption == decisionOptions;
				GameTexts.SetVariable("HERO_NAME", decisionOptions.Option.SponsorClan.Leader.EncyclopediaLinkWithName);
				string text = (num ? GameTexts.FindText("str_decision_relation_increase").ToString() : GameTexts.FindText("str_decision_relation_decrease").ToString());
				if (string.IsNullOrEmpty(RelationChangeText))
				{
					RelationChangeText = text;
					continue;
				}
				GameTexts.SetVariable("newline", "\n");
				GameTexts.SetVariable("STR1", RelationChangeText);
				GameTexts.SetVariable("STR2", text);
				RelationChangeText = GameTexts.FindText("str_string_newline_string").ToString();
			}
		}
	}

	private void RefreshCanEndDecision()
	{
		bool flag = _currentSelectedOption != null && (!IsPlayerSupporter || _currentSelectedOption.CurrentSupportWeight != Supporter.SupportWeights.Choose);
		bool flag2 = _currentInfluenceCost <= Clan.PlayerClan.Influence || _currentInfluenceCost == 0f;
		bool flag3 = _currentSelectedOption?.IsOptionForAbstain ?? false;
		CanEndDecision = !_finalSelectionDone && (flag3 || (flag && flag2));
		if (CanEndDecision)
		{
			EndDecisionHint.HintText = TextObject.GetEmpty();
		}
		else if (!flag)
		{
			if (IsPlayerSupporter)
			{
				EndDecisionHint.HintText = GameTexts.FindText("str_decision_need_to_select_an_option_and_support");
			}
			else
			{
				EndDecisionHint.HintText = GameTexts.FindText("str_decision_need_to_select_an_outcome");
			}
		}
		else if (!flag2)
		{
			EndDecisionHint.HintText = GameTexts.FindText("str_decision_not_enough_influence");
		}
	}

	protected void ExecuteLink(string link)
	{
		Campaign.Current.EncyclopediaManager.GoToLink(link);
	}

	protected void ExecuteShowStageTooltip()
	{
		if (IsPlayerSupporter)
		{
			if (CurrentStageIndex == 0)
			{
				MBInformationManager.ShowHint(GameTexts.FindText("str_decision_first_stage_player_supporter").ToString());
			}
			else
			{
				MBInformationManager.ShowHint(GameTexts.FindText("str_decision_second_stage_player_supporter").ToString());
			}
		}
		else
		{
			MBInformationManager.ShowHint(GameTexts.FindText("str_decision_second_stage_player_decider").ToString());
		}
	}

	protected void ExecuteHideStageTooltip()
	{
		MBInformationManager.HideInformations();
	}

	public void ExecuteFinalSelection()
	{
		if (CanEndDecision)
		{
			KingdomDecisionMaker.ApplySelection();
			_finalSelectionDone = true;
			RefreshCanEndDecision();
		}
	}

	protected void ExecuteDone()
	{
		TextObject chosenOutcomeText = KingdomDecisionMaker.GetChosenOutcomeText();
		IsActive = false;
		InformationManager.ShowInquiry(new InquiryData(GameTexts.FindText("str_decision_outcome").ToString(), chosenOutcomeText.ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: false, GameTexts.FindText("str_ok").ToString(), "", delegate
		{
			_onDecisionOver();
		}, null));
		CampaignEvents.KingdomDecisionConcluded.ClearListeners(this);
		_currentSelectedOption = null;
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		Game.Current?.EventManager?.UnregisterEvent<TutorialNotificationElementChangeEvent>(OnTutorialNotificationElementIDChange);
	}

	private void OnTutorialNotificationElementIDChange(TutorialNotificationElementChangeEvent obj)
	{
		if (_latestTutorialElementID != obj.NewNotificationElementID)
		{
			_latestTutorialElementID = obj.NewNotificationElementID;
			if (_isDecisionOptionsHighlightEnabled && _latestTutorialElementID != _decisionOptionsHighlightID)
			{
				SetOptionsHighlight(state: false);
				_isDecisionOptionsHighlightEnabled = false;
			}
			else if (!_isDecisionOptionsHighlightEnabled && _latestTutorialElementID == _decisionOptionsHighlightID)
			{
				SetOptionsHighlight(state: true);
				_isDecisionOptionsHighlightEnabled = true;
			}
		}
	}

	private void SetOptionsHighlight(bool state)
	{
		for (int i = 0; i < DecisionOptionsList.Count; i++)
		{
			DecisionOptionVM decisionOptionVM = DecisionOptionsList[i];
			if (decisionOptionVM.CanBeChosen)
			{
				decisionOptionVM.IsHighlightEnabled = state;
			}
		}
	}

	public void SetDoneInputKey(InputKeyItemVM inputKeyItemVM)
	{
		DoneInputKey = inputKeyItemVM;
	}
}
