using System;
using System.Collections.Generic;
using Helpers;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Conversation.Persuasion;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Conversation;

public class MissionConversationVM : ViewModel
{
	private readonly ConversationManager _conversationManager;

	private readonly bool _isLinksDisabled;

	private static bool _isCurrentlyPlayerSpeaking;

	private bool _isProcessingOption;

	private BasicCharacterObject _currentDialogCharacter;

	private Func<string> _getContinueInputText;

	private MBBindingList<ConversationItemVM> _answerList;

	private string _dialogText;

	private string _currentCharacterNameLbl;

	private string _continueText;

	private string _relationText;

	private string _persuasionText;

	private bool _isLoadingOver;

	private string _moreOptionText;

	private string _goldText;

	private ConversationAggressivePartyItemVM _defenderLeader;

	private ConversationAggressivePartyItemVM _attackerLeader;

	private MBBindingList<ConversationAggressivePartyItemVM> _defenderParties;

	private MBBindingList<ConversationAggressivePartyItemVM> _attackerParties;

	private BannerImageIdentifierVM _conversedHeroBanner;

	private bool _isAggressive;

	private bool _isRelationEnabled;

	private bool _isBannerEnabled;

	private bool _isPersuading;

	private bool _isCurrentCharacterValidInEncyclopedia;

	private int _selectedSide;

	private int _relation;

	private int _minRelation;

	private int _maxRelation;

	private PowerLevelComparer _powerComparer;

	private ConversationItemVM _currentSelectedAnswer;

	private PersuasionVM _persuasion;

	private HintViewModel _relationHint;

	private HintViewModel _factionHint;

	private HintViewModel _goldHint;

	public bool SelectedAnOptionOrLinkThisFrame { get; set; }

	[DataSourceProperty]
	public PersuasionVM Persuasion
	{
		get
		{
			return _persuasion;
		}
		set
		{
			if (value != _persuasion)
			{
				_persuasion = value;
				OnPropertyChangedWithValue(value, "Persuasion");
			}
		}
	}

	[DataSourceProperty]
	public PowerLevelComparer PowerComparer
	{
		get
		{
			return _powerComparer;
		}
		set
		{
			if (value != _powerComparer)
			{
				_powerComparer = value;
				OnPropertyChangedWithValue(value, "PowerComparer");
			}
		}
	}

	[DataSourceProperty]
	public int Relation
	{
		get
		{
			return _relation;
		}
		set
		{
			if (_relation != value)
			{
				_relation = value;
				OnPropertyChangedWithValue(value, "Relation");
			}
		}
	}

	[DataSourceProperty]
	public int MinRelation
	{
		get
		{
			return _minRelation;
		}
		set
		{
			if (_minRelation != value)
			{
				_minRelation = value;
				OnPropertyChangedWithValue(value, "MinRelation");
			}
		}
	}

	[DataSourceProperty]
	public int MaxRelation
	{
		get
		{
			return _maxRelation;
		}
		set
		{
			if (_maxRelation != value)
			{
				_maxRelation = value;
				OnPropertyChangedWithValue(value, "MaxRelation");
			}
		}
	}

	[DataSourceProperty]
	public ConversationAggressivePartyItemVM DefenderLeader
	{
		get
		{
			return _defenderLeader;
		}
		set
		{
			if (value != _defenderLeader)
			{
				_defenderLeader = value;
				OnPropertyChangedWithValue(value, "DefenderLeader");
			}
		}
	}

	[DataSourceProperty]
	public ConversationAggressivePartyItemVM AttackerLeader
	{
		get
		{
			return _attackerLeader;
		}
		set
		{
			if (value != _attackerLeader)
			{
				_attackerLeader = value;
				OnPropertyChangedWithValue(value, "AttackerLeader");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<ConversationAggressivePartyItemVM> AttackerParties
	{
		get
		{
			return _attackerParties;
		}
		set
		{
			if (value != _attackerParties)
			{
				_attackerParties = value;
				OnPropertyChangedWithValue(value, "AttackerParties");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<ConversationAggressivePartyItemVM> DefenderParties
	{
		get
		{
			return _defenderParties;
		}
		set
		{
			if (value != _defenderParties)
			{
				_defenderParties = value;
				OnPropertyChangedWithValue(value, "DefenderParties");
			}
		}
	}

	[DataSourceProperty]
	public string MoreOptionText
	{
		get
		{
			return _moreOptionText;
		}
		set
		{
			if (_moreOptionText != value)
			{
				_moreOptionText = value;
				OnPropertyChangedWithValue(value, "MoreOptionText");
			}
		}
	}

	[DataSourceProperty]
	public string GoldText
	{
		get
		{
			return _goldText;
		}
		set
		{
			if (_goldText != value)
			{
				_goldText = value;
				OnPropertyChangedWithValue(value, "GoldText");
			}
		}
	}

	[DataSourceProperty]
	public string PersuasionText
	{
		get
		{
			return _persuasionText;
		}
		set
		{
			if (_persuasionText != value)
			{
				_persuasionText = value;
				OnPropertyChangedWithValue(value, "PersuasionText");
			}
		}
	}

	[DataSourceProperty]
	public bool IsCurrentCharacterValidInEncyclopedia
	{
		get
		{
			return _isCurrentCharacterValidInEncyclopedia;
		}
		set
		{
			if (_isCurrentCharacterValidInEncyclopedia != value)
			{
				_isCurrentCharacterValidInEncyclopedia = value;
				OnPropertyChangedWithValue(value, "IsCurrentCharacterValidInEncyclopedia");
			}
		}
	}

	[DataSourceProperty]
	public bool IsLoadingOver
	{
		get
		{
			return _isLoadingOver;
		}
		set
		{
			if (_isLoadingOver != value)
			{
				_isLoadingOver = value;
				OnPropertyChangedWithValue(value, "IsLoadingOver");
			}
		}
	}

	[DataSourceProperty]
	public bool IsPersuading
	{
		get
		{
			return _isPersuading;
		}
		set
		{
			if (_isPersuading != value)
			{
				_isPersuading = value;
				OnPropertyChangedWithValue(value, "IsPersuading");
			}
		}
	}

	[DataSourceProperty]
	public string ContinueText
	{
		get
		{
			return _continueText;
		}
		set
		{
			if (_continueText != value)
			{
				_continueText = value;
				OnPropertyChangedWithValue(value, "ContinueText");
			}
		}
	}

	[DataSourceProperty]
	public string CurrentCharacterNameLbl
	{
		get
		{
			return _currentCharacterNameLbl;
		}
		set
		{
			if (_currentCharacterNameLbl != value)
			{
				_currentCharacterNameLbl = value;
				OnPropertyChangedWithValue(value, "CurrentCharacterNameLbl");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<ConversationItemVM> AnswerList
	{
		get
		{
			return _answerList;
		}
		set
		{
			if (_answerList != value)
			{
				_answerList = value;
				OnPropertyChangedWithValue(value, "AnswerList");
			}
		}
	}

	[DataSourceProperty]
	public string DialogText
	{
		get
		{
			return _dialogText;
		}
		set
		{
			if (_dialogText != value)
			{
				_dialogText = value;
				OnPropertyChangedWithValue(value, "DialogText");
			}
		}
	}

	[DataSourceProperty]
	public bool IsAggressive
	{
		get
		{
			return _isAggressive;
		}
		set
		{
			if (value != _isAggressive)
			{
				_isAggressive = value;
				OnPropertyChangedWithValue(value, "IsAggressive");
				OnAgressiveStateUpdated();
			}
		}
	}

	[DataSourceProperty]
	public int SelectedSide
	{
		get
		{
			return _selectedSide;
		}
		set
		{
			if (value != _selectedSide)
			{
				_selectedSide = value;
				OnPropertyChangedWithValue(value, "SelectedSide");
			}
		}
	}

	[DataSourceProperty]
	public string RelationText
	{
		get
		{
			return _relationText;
		}
		set
		{
			if (_relationText != value)
			{
				_relationText = value;
				OnPropertyChangedWithValue(value, "RelationText");
			}
		}
	}

	[DataSourceProperty]
	public bool IsRelationEnabled
	{
		get
		{
			return _isRelationEnabled;
		}
		set
		{
			if (value != _isRelationEnabled)
			{
				_isRelationEnabled = value;
				OnPropertyChangedWithValue(value, "IsRelationEnabled");
			}
		}
	}

	[DataSourceProperty]
	public bool IsBannerEnabled
	{
		get
		{
			return _isBannerEnabled;
		}
		set
		{
			if (value != _isBannerEnabled)
			{
				_isBannerEnabled = value;
				OnPropertyChangedWithValue(value, "IsBannerEnabled");
			}
		}
	}

	[DataSourceProperty]
	public ConversationItemVM CurrentSelectedAnswer
	{
		get
		{
			return _currentSelectedAnswer;
		}
		set
		{
			if (_currentSelectedAnswer != value)
			{
				_currentSelectedAnswer = value;
				OnPropertyChangedWithValue(value, "CurrentSelectedAnswer");
			}
		}
	}

	[DataSourceProperty]
	public BannerImageIdentifierVM ConversedHeroBanner
	{
		get
		{
			return _conversedHeroBanner;
		}
		set
		{
			if (_conversedHeroBanner != value)
			{
				_conversedHeroBanner = value;
				OnPropertyChangedWithValue(value, "ConversedHeroBanner");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel RelationHint
	{
		get
		{
			return _relationHint;
		}
		set
		{
			if (_relationHint != value)
			{
				_relationHint = value;
				OnPropertyChangedWithValue(value, "RelationHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel FactionHint
	{
		get
		{
			return _factionHint;
		}
		set
		{
			if (_factionHint != value)
			{
				_factionHint = value;
				OnPropertyChangedWithValue(value, "FactionHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel GoldHint
	{
		get
		{
			return _goldHint;
		}
		set
		{
			if (_goldHint != value)
			{
				_goldHint = value;
				OnPropertyChangedWithValue(value, "GoldHint");
			}
		}
	}

	public MissionConversationVM(Func<string> getContinueInputText, bool isLinksDisabled = false)
	{
		AnswerList = new MBBindingList<ConversationItemVM>();
		AttackerParties = new MBBindingList<ConversationAggressivePartyItemVM>();
		DefenderParties = new MBBindingList<ConversationAggressivePartyItemVM>();
		_conversationManager = Campaign.Current.ConversationManager;
		_getContinueInputText = getContinueInputText;
		_isLinksDisabled = isLinksDisabled;
		TaleWorlds.InputSystem.Input.OnGamepadActiveStateChanged = (Action)Delegate.Combine(TaleWorlds.InputSystem.Input.OnGamepadActiveStateChanged, new Action(RefreshValues));
		CampaignEvents.PersuasionProgressCommittedEvent.AddNonSerializedListener(this, OnPersuasionProgress);
		Persuasion = new PersuasionVM(_conversationManager);
		if (_conversationManager.SpeakerAgent != null && (CharacterObject)_conversationManager.SpeakerAgent.Character != null && ((CharacterObject)_conversationManager.SpeakerAgent.Character).IsHero && _conversationManager.SpeakerAgent.Character != CharacterObject.PlayerCharacter)
		{
			Hero heroObject = ((CharacterObject)_conversationManager.SpeakerAgent.Character).HeroObject;
			Relation = (int)heroObject.GetRelationWithPlayer();
		}
		IsAggressive = Campaign.Current.CurrentConversationContext == ConversationContext.PartyEncounter && _conversationManager.ConversationParty != null && FactionManager.IsAtWarAgainstFaction(_conversationManager.ConversationParty.MapFaction, Hero.MainHero.MapFaction);
		OnAgressiveStateUpdated();
		ExecuteSetCurrentAnswer(null);
		RefreshValues();
	}

	private void OnPersuasionProgress(Tuple<PersuasionOptionArgs, PersuasionOptionResult> result)
	{
		Persuasion?.OnPersuasionProgress(result);
		AnswerList.ApplyActionOnAllItems(delegate(ConversationItemVM a)
		{
			a.OnPersuasionProgress(result);
		});
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		ContinueText = _getContinueInputText();
		MoreOptionText = GameTexts.FindText("str_more_brackets").ToString();
		PersuasionText = GameTexts.FindText("str_persuasion").ToString();
		RelationHint = new HintViewModel(GameTexts.FindText("str_tooltip_label_relation"));
		GoldHint = new HintViewModel(new TextObject("{=o5G8A8ZH}Your Denars"));
		_answerList.ApplyActionOnAllItems(delegate(ConversationItemVM x)
		{
			x.RefreshValues();
		});
		_defenderParties.ApplyActionOnAllItems(delegate(ConversationAggressivePartyItemVM x)
		{
			x.RefreshValues();
		});
		_attackerParties.ApplyActionOnAllItems(delegate(ConversationAggressivePartyItemVM x)
		{
			x.RefreshValues();
		});
		_defenderLeader.RefreshValues();
		_attackerLeader.RefreshValues();
		_currentSelectedAnswer.RefreshValues();
	}

	public void Tick(float dt)
	{
		IsAggressive = Campaign.Current.CurrentConversationContext == ConversationContext.PartyEncounter && _conversationManager.ConversationParty != null && FactionManager.IsAtWarAgainstFaction(_conversationManager.ConversationParty.MapFaction, Hero.MainHero.MapFaction);
	}

	private void OnAgressiveStateUpdated()
	{
		if (IsAggressive)
		{
			List<MobileParty> list = new List<MobileParty>();
			List<MobileParty> list2 = new List<MobileParty>();
			DefenderParties.Clear();
			AttackerParties.Clear();
			MobileParty conversationParty = _conversationManager.ConversationParty;
			MobileParty mainParty = MobileParty.MainParty;
			if (PlayerEncounter.PlayerIsAttacker)
			{
				list2.Add(mainParty);
				list.Add(conversationParty);
				PlayerEncounter.Current.FindAllNpcPartiesWhoWillJoinEvent(list2, list);
			}
			else
			{
				list2.Add(conversationParty);
				list.Add(mainParty);
				PlayerEncounter.Current.FindAllNpcPartiesWhoWillJoinEvent(list, list2);
			}
			AttackerLeader = new ConversationAggressivePartyItemVM(PlayerEncounter.PlayerIsAttacker ? mainParty : conversationParty);
			DefenderLeader = new ConversationAggressivePartyItemVM(PlayerEncounter.PlayerIsAttacker ? conversationParty : mainParty);
			double num = 0.0;
			double num2 = 0.0;
			num += (double)DefenderLeader.Party.Party.CalculateCurrentStrength();
			num2 += (double)AttackerLeader.Party.Party.CalculateCurrentStrength();
			foreach (MobileParty item in list)
			{
				if (item != conversationParty && item != mainParty)
				{
					num += (double)item.Party.CalculateCurrentStrength();
					DefenderParties.Add(new ConversationAggressivePartyItemVM(item));
				}
			}
			foreach (MobileParty item2 in list2)
			{
				if (item2 != conversationParty && item2 != mainParty)
				{
					num2 += (double)item2.Party.CalculateCurrentStrength();
					AttackerParties.Add(new ConversationAggressivePartyItemVM(item2));
				}
			}
			string defenderColor = ((DefenderLeader.Party.MapFaction == null || !(DefenderLeader.Party.MapFaction is Kingdom)) ? Color.FromUint(DefenderLeader.Party.MapFaction.Banner.GetPrimaryColor()).ToString() : Color.FromUint(((Kingdom)DefenderLeader.Party.MapFaction).PrimaryBannerColor).ToString());
			string attackerColor = ((AttackerLeader.Party.MapFaction == null || !(AttackerLeader.Party.MapFaction is Kingdom)) ? Color.FromUint(AttackerLeader.Party.MapFaction.Banner.GetPrimaryColor()).ToString() : Color.FromUint(((Kingdom)AttackerLeader.Party.MapFaction).PrimaryBannerColor).ToString());
			if (list2.AnyQ((MobileParty p) => p.IsInfoHidden) || list.AnyQ((MobileParty p) => p.IsInfoHidden))
			{
				if (PlayerEncounter.PlayerIsAttacker)
				{
					num2 = 0.0;
					num = 1.0;
				}
				else
				{
					num2 = 1.0;
					num = 0.0;
				}
			}
			PowerComparer = new PowerLevelComparer(num, num2);
			PowerComparer.SetColors(defenderColor, attackerColor);
		}
		else
		{
			DefenderLeader = new ConversationAggressivePartyItemVM(null);
			AttackerLeader = new ConversationAggressivePartyItemVM(null);
		}
	}

	public void OnConversationContinue()
	{
		if (!ConversationManager.GetPersuasionIsActive() || (ConversationManager.GetPersuasionIsActive() && !IsPersuading) || (_conversationManager.CurOptions?.Count ?? 0) <= 1)
		{
			Refresh();
		}
	}

	public void ExecuteLink(string link)
	{
		if (!_isLinksDisabled)
		{
			Campaign.Current.EncyclopediaManager.GoToLink(link);
		}
	}

	public void ExecuteConversedHeroLink()
	{
		if (!_isLinksDisabled && _currentDialogCharacter is CharacterObject characterObject)
		{
			Campaign.Current.EncyclopediaManager.GoToLink(characterObject.HeroObject?.EncyclopediaLink ?? characterObject.EncyclopediaLink);
			SelectedAnOptionOrLinkThisFrame = true;
		}
	}

	public void Refresh()
	{
		ExecuteCloseTooltip();
		_isProcessingOption = false;
		IsLoadingOver = false;
		IReadOnlyList<IAgent> conversationAgents = _conversationManager.ConversationAgents;
		if (conversationAgents != null && conversationAgents.Count > 0)
		{
			_currentDialogCharacter = _conversationManager.SpeakerAgent.Character;
			CurrentCharacterNameLbl = _currentDialogCharacter.Name.ToString();
			IsCurrentCharacterValidInEncyclopedia = false;
			if (((CharacterObject)_currentDialogCharacter).IsHero && _currentDialogCharacter != CharacterObject.PlayerCharacter)
			{
				MinRelation = Campaign.Current.Models.DiplomacyModel.MinRelationLimit;
				MaxRelation = Campaign.Current.Models.DiplomacyModel.MaxRelationLimit;
				Hero heroObject = ((CharacterObject)_currentDialogCharacter).HeroObject;
				if (heroObject.IsLord && !heroObject.IsMinorFactionHero && heroObject.Clan?.Leader == heroObject && heroObject.Clan?.Kingdom != null)
				{
					string stringId = heroObject.MapFaction.Culture.StringId;
					if (GameTexts.TryGetText("str_faction_noble_name_with_title", out var textObject, stringId))
					{
						if (heroObject.Clan.Kingdom.Leader == heroObject)
						{
							textObject = GameTexts.FindText("str_faction_ruler_name_with_title", stringId);
						}
						StringHelpers.SetCharacterProperties("RULER", (CharacterObject)_currentDialogCharacter);
						CurrentCharacterNameLbl = textObject.ToString();
					}
				}
				IsRelationEnabled = true;
				Relation = Hero.MainHero.GetRelation(heroObject);
				GameTexts.SetVariable("NUM", Relation.ToString());
				if (Relation > 0)
				{
					RelationText = "+" + Relation;
				}
				else if (Relation < 0)
				{
					RelationText = "-" + TaleWorlds.Library.MathF.Abs(Relation);
				}
				else
				{
					RelationText = Relation.ToString();
				}
				if (heroObject.Clan == null)
				{
					ConversedHeroBanner = new BannerImageIdentifierVM(null);
					IsRelationEnabled = false;
					IsBannerEnabled = false;
				}
				else
				{
					ConversedHeroBanner = ((heroObject != null) ? new BannerImageIdentifierVM(heroObject.ClanBanner) : new BannerImageIdentifierVM(null));
					TextObject hintText = ((heroObject != null) ? heroObject.Clan.Name : TextObject.GetEmpty());
					FactionHint = new HintViewModel(hintText);
					IsBannerEnabled = true;
				}
				IsCurrentCharacterValidInEncyclopedia = Campaign.Current.EncyclopediaManager.GetPageOf(typeof(Hero)).IsValidEncyclopediaItem(heroObject);
			}
			else
			{
				ConversedHeroBanner = new BannerImageIdentifierVM(null);
				IsRelationEnabled = false;
				IsBannerEnabled = false;
				IsCurrentCharacterValidInEncyclopedia = Campaign.Current.EncyclopediaManager.GetPageOf(typeof(CharacterObject)).IsValidEncyclopediaItem((CharacterObject)_conversationManager.SpeakerAgent.Character);
			}
		}
		DialogText = _conversationManager.CurrentSentenceText;
		AnswerList.Clear();
		_isCurrentlyPlayerSpeaking = _currentDialogCharacter == Hero.MainHero.CharacterObject;
		_conversationManager.GetPlayerSentenceOptions();
		int num = _conversationManager.CurOptions?.Count ?? 0;
		if (num > 0 && !_isCurrentlyPlayerSpeaking)
		{
			for (int i = 0; i < num; i++)
			{
				AnswerList.Add(new ConversationItemVM(OnSelectOption, OnReadyToContinue, ExecuteSetCurrentAnswer, i));
			}
		}
		GoldText = CampaignUIHelper.GetAbbreviatedValueTextFromValue(Hero.MainHero.Gold);
		IsPersuading = ConversationManager.GetPersuasionIsActive();
		if (IsPersuading)
		{
			CurrentSelectedAnswer = new ConversationItemVM();
		}
		IsLoadingOver = true;
		Persuasion?.RefreshPersusasion();
	}

	private void OnReadyToContinue()
	{
		Refresh();
	}

	private void ExecuteDefenderTooltip()
	{
		if (PlayerEncounter.PlayerIsDefender)
		{
			InformationManager.ShowTooltip(typeof(List<MobileParty>), 0);
		}
		else
		{
			InformationManager.ShowTooltip(typeof(List<MobileParty>), 1);
		}
	}

	public void ExecuteCloseTooltip()
	{
		MBInformationManager.HideInformations();
	}

	public void ExecuteHeroTooltip()
	{
		CharacterObject characterObject = (CharacterObject)_currentDialogCharacter;
		if (characterObject != null && characterObject.IsHero)
		{
			InformationManager.ShowTooltip(typeof(Hero), characterObject.HeroObject, true);
		}
	}

	private void ExecuteAttackerTooltip()
	{
		if (PlayerEncounter.PlayerIsAttacker)
		{
			InformationManager.ShowTooltip(typeof(List<MobileParty>), 0);
		}
		else
		{
			InformationManager.ShowTooltip(typeof(List<MobileParty>), 1);
		}
	}

	private void ExecuteHeroInfo()
	{
		if (_conversationManager.ListenerAgent.Character == Hero.MainHero.CharacterObject)
		{
			Campaign.Current.EncyclopediaManager.GoToLink(Hero.MainHero.EncyclopediaLink);
		}
		else if (CharacterObject.OneToOneConversationCharacter.IsHero)
		{
			Campaign.Current.EncyclopediaManager.GoToLink(CharacterObject.OneToOneConversationCharacter.HeroObject.EncyclopediaLink);
		}
		else
		{
			Campaign.Current.EncyclopediaManager.GoToLink(CharacterObject.OneToOneConversationCharacter.EncyclopediaLink);
		}
	}

	private void OnSelectOption(int optionIndex)
	{
		if (!_isProcessingOption)
		{
			_isProcessingOption = true;
			_conversationManager.DoOption(optionIndex);
			Persuasion?.RefreshPersusasion();
			SelectedAnOptionOrLinkThisFrame = true;
		}
	}

	public void ExecuteFinalizeSelection()
	{
		Refresh();
	}

	public void ExecuteContinue()
	{
		Debug.Print("ExecuteContinue");
		_conversationManager.ContinueConversation();
		_isProcessingOption = false;
	}

	private void ExecuteSetCurrentAnswer(ConversationItemVM _answer)
	{
		Persuasion.SetCurrentOption(_answer?.PersuasionItem);
		if (_answer != null)
		{
			CurrentSelectedAnswer = _answer;
		}
		else
		{
			CurrentSelectedAnswer = new ConversationItemVM();
		}
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		CampaignEvents.PersuasionProgressCommittedEvent.ClearListeners(this);
		TaleWorlds.InputSystem.Input.OnGamepadActiveStateChanged = (Action)Delegate.Remove(TaleWorlds.InputSystem.Input.OnGamepadActiveStateChanged, new Action(RefreshValues));
		Persuasion?.OnFinalize();
	}
}
