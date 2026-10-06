using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement.Categories;

public class ClanFiefsVM : ViewModel
{
	private readonly Clan _clan;

	private readonly Action _onRefresh;

	private readonly Action<ClanCardSelectionInfo> _openCardSelectionPopup;

	private readonly ITeleportationCampaignBehavior _teleportationBehavior;

	private readonly TextObject _noGovernorTextSource = new TextObject("{=zLFsnaqR}No Governor");

	private MBBindingList<ClanSettlementItemVM> _settlements;

	private MBBindingList<ClanSettlementItemVM> _castles;

	private ClanSettlementItemVM _currentSelectedFief;

	private bool _isSelected;

	private string _nameText;

	private string _taxText;

	private string _governorText;

	private string _profitText;

	private string _townsText;

	private string _castlesText;

	private string _noFiefsText;

	private string _noGovernorText;

	private bool _isAnyValidFiefSelected;

	private bool _canChangeGovernorOfCurrentFief;

	private HintViewModel _governorActionHint;

	private string _governorActionText;

	private ClanFiefsSortControllerVM _sortController;

	[DataSourceProperty]
	public string GovernorActionText
	{
		get
		{
			return _governorActionText;
		}
		set
		{
			if (value != _governorActionText)
			{
				_governorActionText = value;
				OnPropertyChangedWithValue(value, "GovernorActionText");
			}
		}
	}

	[DataSourceProperty]
	public bool CanChangeGovernorOfCurrentFief
	{
		get
		{
			return _canChangeGovernorOfCurrentFief;
		}
		set
		{
			if (value != _canChangeGovernorOfCurrentFief)
			{
				_canChangeGovernorOfCurrentFief = value;
				OnPropertyChangedWithValue(value, "CanChangeGovernorOfCurrentFief");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel GovernorActionHint
	{
		get
		{
			return _governorActionHint;
		}
		set
		{
			if (value != _governorActionHint)
			{
				_governorActionHint = value;
				OnPropertyChangedWithValue(value, "GovernorActionHint");
			}
		}
	}

	[DataSourceProperty]
	public bool IsAnyValidFiefSelected
	{
		get
		{
			return _isAnyValidFiefSelected;
		}
		set
		{
			if (value != _isAnyValidFiefSelected)
			{
				_isAnyValidFiefSelected = value;
				OnPropertyChangedWithValue(value, "IsAnyValidFiefSelected");
			}
		}
	}

	[DataSourceProperty]
	public string NameText
	{
		get
		{
			return _nameText;
		}
		set
		{
			if (value != _nameText)
			{
				_nameText = value;
				OnPropertyChangedWithValue(value, "NameText");
			}
		}
	}

	[DataSourceProperty]
	public string TaxText
	{
		get
		{
			return _taxText;
		}
		set
		{
			if (value != _taxText)
			{
				_taxText = value;
				OnPropertyChangedWithValue(value, "TaxText");
			}
		}
	}

	[DataSourceProperty]
	public string GovernorText
	{
		get
		{
			return _governorText;
		}
		set
		{
			if (value != _governorText)
			{
				_governorText = value;
				OnPropertyChangedWithValue(value, "GovernorText");
			}
		}
	}

	[DataSourceProperty]
	public string ProfitText
	{
		get
		{
			return _profitText;
		}
		set
		{
			if (value != _profitText)
			{
				_profitText = value;
				OnPropertyChangedWithValue(value, "ProfitText");
			}
		}
	}

	[DataSourceProperty]
	public string TownsText
	{
		get
		{
			return _townsText;
		}
		set
		{
			if (value != _townsText)
			{
				_townsText = value;
				OnPropertyChangedWithValue(value, "TownsText");
			}
		}
	}

	[DataSourceProperty]
	public string CastlesText
	{
		get
		{
			return _castlesText;
		}
		set
		{
			if (value != _castlesText)
			{
				_castlesText = value;
				OnPropertyChangedWithValue(value, "CastlesText");
			}
		}
	}

	[DataSourceProperty]
	public string NoFiefsText
	{
		get
		{
			return _noFiefsText;
		}
		set
		{
			if (value != _noFiefsText)
			{
				_noFiefsText = value;
				OnPropertyChangedWithValue(value, "NoFiefsText");
			}
		}
	}

	[DataSourceProperty]
	public string NoGovernorText
	{
		get
		{
			return _noGovernorText;
		}
		set
		{
			if (value != _noGovernorText)
			{
				_noGovernorText = value;
				OnPropertyChangedWithValue(value, "NoGovernorText");
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
	public MBBindingList<ClanSettlementItemVM> Settlements
	{
		get
		{
			return _settlements;
		}
		set
		{
			if (value != _settlements)
			{
				_settlements = value;
				OnPropertyChangedWithValue(value, "Settlements");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<ClanSettlementItemVM> Castles
	{
		get
		{
			return _castles;
		}
		set
		{
			if (value != _castles)
			{
				_castles = value;
				OnPropertyChangedWithValue(value, "Castles");
			}
		}
	}

	[DataSourceProperty]
	public ClanSettlementItemVM CurrentSelectedFief
	{
		get
		{
			return _currentSelectedFief;
		}
		set
		{
			if (value != _currentSelectedFief)
			{
				_currentSelectedFief = value;
				OnPropertyChangedWithValue(value, "CurrentSelectedFief");
				IsAnyValidFiefSelected = value != null;
			}
		}
	}

	[DataSourceProperty]
	public ClanFiefsSortControllerVM SortController
	{
		get
		{
			return _sortController;
		}
		set
		{
			if (value != _sortController)
			{
				_sortController = value;
				OnPropertyChangedWithValue(value, "SortController");
			}
		}
	}

	public ClanFiefsVM(Action onRefresh, Action<ClanCardSelectionInfo> openCardSelectionPopup)
	{
		_onRefresh = onRefresh;
		_clan = Hero.MainHero.Clan;
		_openCardSelectionPopup = openCardSelectionPopup;
		_teleportationBehavior = Campaign.Current.GetCampaignBehavior<ITeleportationCampaignBehavior>();
		Settlements = new MBBindingList<ClanSettlementItemVM>();
		Castles = new MBBindingList<ClanSettlementItemVM>();
		List<MBBindingList<ClanSettlementItemVM>> listsToControl = new List<MBBindingList<ClanSettlementItemVM>> { Settlements, Castles };
		SortController = new ClanFiefsSortControllerVM(listsToControl);
		RefreshAllLists();
		RefreshValues();
	}

	protected virtual ClanSettlementItemVM CreateSettlementItem(Settlement settlement, Action<ClanSettlementItemVM> onSelection, Action onShowSendMembers, ITeleportationCampaignBehavior teleportationBehavior)
	{
		return new ClanSettlementItemVM(settlement, onSelection, onShowSendMembers, teleportationBehavior);
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		TaxText = GameTexts.FindText("str_tax").ToString();
		GovernorText = GameTexts.FindText("str_notable_governor").ToString();
		ProfitText = GameTexts.FindText("str_profit").ToString();
		NameText = GameTexts.FindText("str_sort_by_name_label").ToString();
		NoFiefsText = GameTexts.FindText("str_clan_no_fiefs").ToString();
		NoGovernorText = _noGovernorTextSource.ToString();
		Settlements.ApplyActionOnAllItems(delegate(ClanSettlementItemVM x)
		{
			x.RefreshValues();
		});
		Castles.ApplyActionOnAllItems(delegate(ClanSettlementItemVM x)
		{
			x.RefreshValues();
		});
		CurrentSelectedFief?.RefreshValues();
		SortController.RefreshValues();
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
	}

	public void RefreshAllLists()
	{
		Settlements.Clear();
		Castles.Clear();
		SortController.ResetAllStates();
		foreach (Settlement settlement in _clan.Settlements)
		{
			if (settlement.IsTown)
			{
				Settlements.Add(CreateSettlementItem(settlement, OnFiefSelection, OnShowSendMembers, _teleportationBehavior));
			}
			else if (settlement.IsCastle)
			{
				Castles.Add(CreateSettlementItem(settlement, OnFiefSelection, OnShowSendMembers, _teleportationBehavior));
			}
		}
		GameTexts.SetVariable("RANK", GameTexts.FindText("str_towns"));
		GameTexts.SetVariable("NUMBER", Settlements.Count);
		TownsText = GameTexts.FindText("str_RANK_with_NUM_between_parenthesis").ToString();
		GameTexts.SetVariable("RANK", GameTexts.FindText("str_castles"));
		GameTexts.SetVariable("NUMBER", Castles.Count);
		CastlesText = GameTexts.FindText("str_RANK_with_NUM_between_parenthesis").ToString();
		OnFiefSelection(GetDefaultMember());
	}

	private ClanSettlementItemVM GetDefaultMember()
	{
		if (!Settlements.IsEmpty())
		{
			return Settlements.FirstOrDefault();
		}
		return Castles.FirstOrDefault();
	}

	public void SelectFief(Settlement settlement)
	{
		foreach (ClanSettlementItemVM settlement2 in Settlements)
		{
			if (settlement2.Settlement == settlement)
			{
				OnFiefSelection(settlement2);
				break;
			}
		}
	}

	private void OnFiefSelection(ClanSettlementItemVM fief)
	{
		if (CurrentSelectedFief != null)
		{
			CurrentSelectedFief.IsSelected = false;
		}
		CurrentSelectedFief = fief;
		CanChangeGovernorOfCurrentFief = GetCanChangeGovernor(out var disabledReason);
		GovernorActionHint = new HintViewModel(disabledReason);
		if (fief != null)
		{
			fief.IsSelected = true;
			GovernorActionText = (fief.HasGovernor ? GameTexts.FindText("str_clan_change_governor").ToString() : GameTexts.FindText("str_clan_assign_governor").ToString());
		}
	}

	private bool GetCanChangeGovernor(out TextObject disabledReason)
	{
		if (!CampaignUIHelper.GetMapScreenActionIsEnabledWithReason(out var disabledReason2))
		{
			disabledReason = disabledReason2;
			return false;
		}
		ClanSettlementItemVM currentSelectedFief = CurrentSelectedFief;
		if (currentSelectedFief != null && currentSelectedFief.Governor?.Hero?.IsTraveling == true)
		{
			disabledReason = new TextObject("{=qbqimqMb}{GOVERNOR.NAME} is on the way to be the new governor of {SETTLEMENT_NAME}");
			if (CurrentSelectedFief.Governor.Hero.CharacterObject != null)
			{
				StringHelpers.SetCharacterProperties("GOVERNOR", CurrentSelectedFief.Governor.Hero.CharacterObject, disabledReason);
			}
			disabledReason.SetTextVariable("SETTLEMENT_NAME", CurrentSelectedFief.Settlement?.Name?.ToString() ?? string.Empty);
			return false;
		}
		if (CurrentSelectedFief?.Settlement.Town == null)
		{
			disabledReason = TextObject.GetEmpty();
			return false;
		}
		disabledReason = TextObject.GetEmpty();
		return true;
	}

	public void ExecuteAssignGovernor()
	{
		if (CurrentSelectedFief?.Settlement?.Town != null)
		{
			ClanCardSelectionInfo obj = new ClanCardSelectionInfo(GameTexts.FindText("str_clan_assign_governor").CopyTextObject(), GetGovernorCandidates(), OnGovernorSelectionOver, isMultiSelection: false);
			_openCardSelectionPopup?.Invoke(obj);
		}
	}

	private IEnumerable<ClanCardSelectionItemInfo> GetGovernorCandidates()
	{
		yield return new ClanCardSelectionItemInfo(_noGovernorTextSource.CopyTextObject(), isDisabled: false, null, null);
		foreach (Hero item in _clan.Heroes.Where((Hero h) => !h.IsDisabled).Union(_clan.Companions))
		{
			if ((item.IsActive || item.IsTraveling) && !item.IsChild && item != Hero.MainHero && item != CurrentSelectedFief.Governor?.Hero && item.CanBeGovernorOrHavePartyRole())
			{
				TextObject explanation;
				bool flag = FactionHelper.IsMainClanMemberAvailableForSendingSettlementAsGovernor(item, GetSettlementOfGovernor(item), out explanation);
				SkillObject charm = DefaultSkills.Charm;
				int skillValue = item.GetSkillValue(charm);
				CharacterImageIdentifier image = new CharacterImageIdentifier(CampaignUIHelper.GetCharacterCode(item.CharacterObject));
				yield return new ClanCardSelectionItemInfo(item, item.Name, image, CardSelectionItemSpriteType.Skill, charm.StringId.ToLower(), skillValue.ToString(), GetGovernorCandidateProperties(item), !flag, explanation, null);
			}
		}
	}

	private IEnumerable<ClanCardSelectionItemPropertyInfo> GetGovernorCandidateProperties(Hero hero)
	{
		GameTexts.SetVariable("newline", "\n");
		TextObject teleportationDelayText = CampaignUIHelper.GetTeleportationDelayText(hero, CurrentSelectedFief.Settlement.Party);
		yield return new ClanCardSelectionItemPropertyInfo(teleportationDelayText);
		(TextObject, TextObject) governorEngineeringSkillEffectForHero = PerkHelper.GetGovernorEngineeringSkillEffectForHero(hero);
		yield return new ClanCardSelectionItemPropertyInfo(new TextObject("{=J8ddrAOf}Governor Effects"), governorEngineeringSkillEffectForHero.Item2);
		List<PerkObject> governorPerksForHero = PerkHelper.GetGovernorPerksForHero(hero);
		TextObject perksPropertyText = new TextObject("{=oSfsqBwJ}No perks");
		int addedPerkCount = 0;
		foreach (PerkObject item in governorPerksForHero)
		{
			bool num = item.PrimaryRole == PartyRole.Governor;
			bool flag = item.SecondaryRole == PartyRole.Governor;
			if (num)
			{
				TextObject perkText = ClanCardSelectionItemPropertyInfo.CreateLabeledValueText(item.Name, item.PrimaryDescription);
				SetPerksPropertyText(perkText, ref perksPropertyText, ref addedPerkCount);
			}
			if (flag)
			{
				TextObject perkText2 = ClanCardSelectionItemPropertyInfo.CreateLabeledValueText(item.Name, item.SecondaryDescription);
				SetPerksPropertyText(perkText2, ref perksPropertyText, ref addedPerkCount);
			}
		}
		yield return new ClanCardSelectionItemPropertyInfo(GameTexts.FindText("str_clan_governor_perks"), perksPropertyText);
	}

	private void SetPerksPropertyText(TextObject perkText, ref TextObject perksPropertyText, ref int addedPerkCount)
	{
		if (addedPerkCount == 0)
		{
			perksPropertyText = perkText;
		}
		else
		{
			TextObject textObject = GameTexts.FindText("str_string_newline_newline_string");
			textObject.SetTextVariable("STR1", perksPropertyText);
			textObject.SetTextVariable("STR2", perkText);
			perksPropertyText = textObject;
		}
		addedPerkCount++;
	}

	private void OnGovernorSelectionOver(List<object> selectedItems, Action closePopup)
	{
		if (selectedItems.Count == 1)
		{
			Hero hero = CurrentSelectedFief?.Governor?.Hero;
			Hero newGovernor = selectedItems.FirstOrDefault() as Hero;
			bool isRemoveGovernor = newGovernor == null;
			if (!isRemoveGovernor || hero != null)
			{
				(TextObject, TextObject) governorSelectionConfirmationPopupTexts = CampaignUIHelper.GetGovernorSelectionConfirmationPopupTexts(hero, newGovernor, CurrentSelectedFief.Settlement);
				InformationManager.ShowInquiry(new InquiryData(governorSelectionConfirmationPopupTexts.Item1.ToString(), governorSelectionConfirmationPopupTexts.Item2.ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, GameTexts.FindText("str_yes").ToString(), GameTexts.FindText("str_no").ToString(), delegate
				{
					closePopup?.Invoke();
					if (isRemoveGovernor)
					{
						ChangeGovernorAction.RemoveGovernorOfIfExists(CurrentSelectedFief.Settlement.Town);
					}
					else
					{
						ChangeGovernorAction.Apply(CurrentSelectedFief.Settlement.Town, newGovernor);
					}
					_onRefresh?.Invoke();
				}, null));
			}
			else
			{
				closePopup?.Invoke();
			}
		}
		else
		{
			closePopup?.Invoke();
		}
	}

	private Settlement GetSettlementOfGovernor(Hero hero)
	{
		foreach (ClanSettlementItemVM settlement in Settlements)
		{
			if (settlement?.Governor?.Hero == hero)
			{
				return settlement.Settlement;
			}
		}
		foreach (ClanSettlementItemVM castle in Castles)
		{
			if (castle?.Governor?.Hero == hero)
			{
				return castle.Settlement;
			}
		}
		return null;
	}

	private void OnShowSendMembers()
	{
		Settlement settlement = CurrentSelectedFief?.Settlement;
		if (settlement != null)
		{
			TextObject textObject = GameTexts.FindText("str_send_members");
			textObject.SetTextVariable("SETTLEMENT_NAME", settlement.Name);
			ClanCardSelectionInfo obj = new ClanCardSelectionInfo(textObject, GetSendMembersCandidates(), OnSendMembersSelectionOver, isMultiSelection: true);
			_openCardSelectionPopup?.Invoke(obj);
		}
	}

	private IEnumerable<ClanCardSelectionItemInfo> GetSendMembersCandidates()
	{
		foreach (Hero item in _clan.Heroes.Where((Hero h) => !h.IsDisabled).Union(_clan.Companions))
		{
			if ((item.IsActive || item.IsTraveling) && (item.CurrentSettlement != CurrentSelectedFief.Settlement || item.PartyBelongedTo != null) && !item.IsChild && item != Hero.MainHero)
			{
				TextObject explanation;
				bool flag = FactionHelper.IsMainClanMemberAvailableForSendingSettlement(item, CurrentSelectedFief.Settlement, out explanation);
				SkillObject charm = DefaultSkills.Charm;
				int skillValue = item.GetSkillValue(charm);
				CharacterImageIdentifier image = new CharacterImageIdentifier(CampaignUIHelper.GetCharacterCode(item.CharacterObject));
				yield return new ClanCardSelectionItemInfo(item, item.Name, image, CardSelectionItemSpriteType.Skill, charm.StringId.ToLower(), skillValue.ToString(), GetSendMembersCandidateProperties(item), !flag, explanation, null);
			}
		}
	}

	private IEnumerable<ClanCardSelectionItemPropertyInfo> GetSendMembersCandidateProperties(Hero hero)
	{
		TextObject teleportationDelayText = CampaignUIHelper.GetTeleportationDelayText(hero, CurrentSelectedFief.Settlement.Party);
		yield return new ClanCardSelectionItemPropertyInfo(teleportationDelayText);
		TextObject textObject = new TextObject("{=otaUtXMX}+{AMOUNT} relation chance with notables per day.");
		int emissaryRelationBonusForMainClan = Campaign.Current.Models.EmissaryModel.EmissaryRelationBonusForMainClan;
		textObject.SetTextVariable("AMOUNT", emissaryRelationBonusForMainClan);
		yield return new ClanCardSelectionItemPropertyInfo(textObject);
	}

	private void OnSendMembersSelectionOver(List<object> selectedItems, Action closePopup)
	{
		if (selectedItems.Count > 0)
		{
			MBTextManager.SetTextVariable("SETTLEMENT_NAME", CurrentSelectedFief?.Settlement?.Name?.ToString() ?? string.Empty);
			InformationManager.ShowInquiry(new InquiryData(GameTexts.FindText("str_send_members").ToString(), GameTexts.FindText("str_send_members_inquiry").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, GameTexts.FindText("str_yes").ToString(), GameTexts.FindText("str_no").ToString(), delegate
			{
				closePopup?.Invoke();
				foreach (object selectedItem in selectedItems)
				{
					if (selectedItem is Hero heroToBeMoved)
					{
						TeleportHeroAction.ApplyDelayedTeleportToSettlement(heroToBeMoved, CurrentSelectedFief.Settlement);
					}
				}
				_onRefresh?.Invoke();
			}, null));
		}
		else
		{
			closePopup?.Invoke();
		}
	}
}
