using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement.Categories;

public class ClanPartiesVM : ViewModel
{
	private Action _onExpenseChange;

	private Action<Hero> _openPartyAsManage;

	private Action<ClanCardSelectionInfo> _openCardSelectionPopup;

	private readonly IDisbandPartyCampaignBehavior _disbandBehavior;

	private readonly ITeleportationCampaignBehavior _teleportationBehavior;

	private readonly Action _onRefresh;

	private readonly Clan _faction;

	private readonly IEnumerable<SkillObject> _leaderAssignmentRelevantSkills = new List<SkillObject>
	{
		DefaultSkills.Engineering,
		DefaultSkills.Steward,
		DefaultSkills.Scouting,
		DefaultSkills.Medicine
	};

	private MBBindingList<ClanPartyItemVM> _parties;

	private MBBindingList<ClanPartyItemVM> _garrisons;

	private MBBindingList<ClanPartyItemVM> _caravans;

	private ClanPartyItemVM _currentSelectedParty;

	private HintViewModel _createNewPartyActionHint;

	private bool _canCreateNewParty;

	private bool _isSelected;

	private string _nameText;

	private string _moraleText;

	private string _locationText;

	private string _sizeText;

	private string _createNewPartyText;

	private string _partiesText;

	private string _caravansText;

	private string _garrisonsText;

	private bool _isAnyValidPartySelected;

	private ClanPartiesSortControllerVM _sortController;

	public int TotalExpense { get; private set; }

	public int TotalIncome { get; private set; }

	[DataSourceProperty]
	public HintViewModel CreateNewPartyActionHint
	{
		get
		{
			return _createNewPartyActionHint;
		}
		set
		{
			if (value != _createNewPartyActionHint)
			{
				_createNewPartyActionHint = value;
				OnPropertyChangedWithValue(value, "CreateNewPartyActionHint");
			}
		}
	}

	[DataSourceProperty]
	public bool IsAnyValidPartySelected
	{
		get
		{
			return _isAnyValidPartySelected;
		}
		set
		{
			if (value != _isAnyValidPartySelected)
			{
				_isAnyValidPartySelected = value;
				OnPropertyChangedWithValue(value, "IsAnyValidPartySelected");
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
	public string CaravansText
	{
		get
		{
			return _caravansText;
		}
		set
		{
			if (value != _caravansText)
			{
				_caravansText = value;
				OnPropertyChangedWithValue(value, "CaravansText");
			}
		}
	}

	[DataSourceProperty]
	public string GarrisonsText
	{
		get
		{
			return _garrisonsText;
		}
		set
		{
			if (value != _garrisonsText)
			{
				_garrisonsText = value;
				OnPropertyChangedWithValue(value, "GarrisonsText");
			}
		}
	}

	[DataSourceProperty]
	public string PartiesText
	{
		get
		{
			return _partiesText;
		}
		set
		{
			if (value != _partiesText)
			{
				_partiesText = value;
				OnPropertyChangedWithValue(value, "PartiesText");
			}
		}
	}

	[DataSourceProperty]
	public string MoraleText
	{
		get
		{
			return _moraleText;
		}
		set
		{
			if (value != _moraleText)
			{
				_moraleText = value;
				OnPropertyChangedWithValue(value, "MoraleText");
			}
		}
	}

	[DataSourceProperty]
	public string LocationText
	{
		get
		{
			return _locationText;
		}
		set
		{
			if (value != _locationText)
			{
				_locationText = value;
				OnPropertyChangedWithValue(value, "LocationText");
			}
		}
	}

	[DataSourceProperty]
	public string CreateNewPartyText
	{
		get
		{
			return _createNewPartyText;
		}
		set
		{
			if (value != _createNewPartyText)
			{
				_createNewPartyText = value;
				OnPropertyChangedWithValue(value, "CreateNewPartyText");
			}
		}
	}

	[DataSourceProperty]
	public string SizeText
	{
		get
		{
			return _sizeText;
		}
		set
		{
			if (value != _sizeText)
			{
				_sizeText = value;
				OnPropertyChangedWithValue(value, "SizeText");
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
	public bool CanCreateNewParty
	{
		get
		{
			return _canCreateNewParty;
		}
		set
		{
			if (value != _canCreateNewParty)
			{
				_canCreateNewParty = value;
				OnPropertyChangedWithValue(value, "CanCreateNewParty");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<ClanPartyItemVM> Parties
	{
		get
		{
			return _parties;
		}
		set
		{
			if (value != _parties)
			{
				_parties = value;
				OnPropertyChangedWithValue(value, "Parties");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<ClanPartyItemVM> Caravans
	{
		get
		{
			return _caravans;
		}
		set
		{
			if (value != _caravans)
			{
				_caravans = value;
				OnPropertyChangedWithValue(value, "Caravans");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<ClanPartyItemVM> Garrisons
	{
		get
		{
			return _garrisons;
		}
		set
		{
			if (value != _garrisons)
			{
				_garrisons = value;
				OnPropertyChangedWithValue(value, "Garrisons");
			}
		}
	}

	[DataSourceProperty]
	public ClanPartyItemVM CurrentSelectedParty
	{
		get
		{
			return _currentSelectedParty;
		}
		set
		{
			if (value != _currentSelectedParty)
			{
				_currentSelectedParty = value;
				OnPropertyChangedWithValue(value, "CurrentSelectedParty");
				IsAnyValidPartySelected = value != null;
			}
		}
	}

	[DataSourceProperty]
	public ClanPartiesSortControllerVM SortController
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

	public ClanPartiesVM(Action onExpenseChange, Action<Hero> openPartyAsManage, Action onRefresh, Action<ClanCardSelectionInfo> openCardSelectionPopup)
	{
		_onExpenseChange = onExpenseChange;
		_onRefresh = onRefresh;
		_disbandBehavior = Campaign.Current.GetCampaignBehavior<IDisbandPartyCampaignBehavior>();
		_teleportationBehavior = Campaign.Current.GetCampaignBehavior<ITeleportationCampaignBehavior>();
		_openPartyAsManage = openPartyAsManage;
		_openCardSelectionPopup = openCardSelectionPopup;
		_faction = Hero.MainHero.Clan;
		Parties = new MBBindingList<ClanPartyItemVM>();
		Garrisons = new MBBindingList<ClanPartyItemVM>();
		Caravans = new MBBindingList<ClanPartyItemVM>();
		MBBindingList<MBBindingList<ClanPartyItemVM>> listsToControl = new MBBindingList<MBBindingList<ClanPartyItemVM>> { Parties, Garrisons, Caravans };
		SortController = new ClanPartiesSortControllerVM(listsToControl);
		CreateNewPartyActionHint = new HintViewModel();
		RefreshPartiesList();
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		SizeText = GameTexts.FindText("str_clan_party_size").ToString();
		MoraleText = GameTexts.FindText("str_morale").ToString();
		LocationText = GameTexts.FindText("str_tooltip_label_location").ToString();
		NameText = GameTexts.FindText("str_sort_by_name_label").ToString();
		CreateNewPartyText = GameTexts.FindText("str_clan_create_new_party").ToString();
		GarrisonsText = GameTexts.FindText("str_clan_garrisons").ToString();
		CaravansText = GameTexts.FindText("str_clan_caravans").ToString();
		RefreshPartiesList();
		Parties.ApplyActionOnAllItems(delegate(ClanPartyItemVM x)
		{
			x.RefreshValues();
		});
		Garrisons.ApplyActionOnAllItems(delegate(ClanPartyItemVM x)
		{
			x.RefreshValues();
		});
		Caravans.ApplyActionOnAllItems(delegate(ClanPartyItemVM x)
		{
			x.RefreshValues();
		});
		SortController.RefreshValues();
	}

	public void RefreshTotalExpense()
	{
		TotalExpense = (from p in Parties.Union(Garrisons).Union(Caravans)
			where p.ShouldPartyHaveExpense
			select p)?.Sum((ClanPartyItemVM p) => p.Expense) ?? 0;
		TotalIncome = Caravans.Sum((ClanPartyItemVM p) => p.Income);
	}

	public void RefreshPartiesList()
	{
		Parties.Clear();
		Garrisons.Clear();
		Caravans.Clear();
		SortController.ResetAllStates();
		foreach (WarPartyComponent warPartyComponent in _faction.WarPartyComponents)
		{
			if (warPartyComponent.MobileParty == MobileParty.MainParty)
			{
				Parties.Insert(0, new ClanPartyItemVM(warPartyComponent.Party, OnPartySelection, OnAnyExpenseChange, OnShowChangeLeaderPopup, ClanPartyItemVM.ClanPartyType.Main, _disbandBehavior, _teleportationBehavior));
			}
			else
			{
				Parties.Add(new ClanPartyItemVM(warPartyComponent.Party, OnPartySelection, OnAnyExpenseChange, OnShowChangeLeaderPopup, ClanPartyItemVM.ClanPartyType.Member, _disbandBehavior, _teleportationBehavior));
			}
		}
		foreach (CaravanPartyComponent party in _faction.Heroes.SelectMany((Hero h) => h.OwnedCaravans))
		{
			if (!Caravans.Any((ClanPartyItemVM c) => c.Party.MobileParty == party.MobileParty))
			{
				Caravans.Add(new ClanPartyItemVM(party.Party, OnPartySelection, OnAnyExpenseChange, OnShowChangeLeaderPopup, ClanPartyItemVM.ClanPartyType.Caravan, _disbandBehavior, _teleportationBehavior));
			}
		}
		foreach (MobileParty garrison in from s in _faction.Settlements
			where s.Town != null
			select s.Town.GarrisonParty)
		{
			if (garrison != null && !Garrisons.Any((ClanPartyItemVM c) => c.Party == garrison.Party))
			{
				Garrisons.Add(new ClanPartyItemVM(garrison.Party, OnPartySelection, OnAnyExpenseChange, OnShowChangeLeaderPopup, ClanPartyItemVM.ClanPartyType.Garrison, _disbandBehavior, _teleportationBehavior));
			}
		}
		int count = _faction.WarPartyComponents.Count;
		_faction.Heroes.Where((Hero h) => !h.IsDisabled).Union(_faction.Companions).Any((Hero h) => h.IsActive && h.PartyBelongedToAsPrisoner == null && !h.IsChild && h.CanLeadParty() && (h.PartyBelongedTo == null || h.PartyBelongedTo.LeaderHero != h));
		CanCreateNewParty = GetCanCreateNewParty(out var disabledReason);
		CreateNewPartyActionHint.HintText = disabledReason;
		GameTexts.SetVariable("CURRENT", count);
		GameTexts.SetVariable("LIMIT", _faction.WarPartyLimit);
		PartiesText = GameTexts.FindText("str_clan_parties").ToString();
		GameTexts.SetVariable("CURRENT", Caravans.Count);
		CaravansText = GameTexts.FindText("str_clan_caravans").ToString();
		GameTexts.SetVariable("CURRENT", Garrisons.Count);
		GarrisonsText = GameTexts.FindText("str_clan_garrisons").ToString();
		OnPartySelection(GetDefaultMember());
	}

	private bool GetCanCreateNewParty(out TextObject disabledReason)
	{
		IEnumerable<Hero> source = from h in _faction.Heroes.Where((Hero h) => !h.IsDisabled).Union(_faction.Companions)
			where h.IsActive && h.PartyBelongedToAsPrisoner == null && !h.IsChild && h.CanLeadParty() && (h.PartyBelongedTo == null || h.PartyBelongedTo.LeaderHero != h)
			select h;
		bool flag = !source.IsEmpty();
		int partyGoldLowerThreshold = Campaign.Current.Models.ClanFinanceModel.PartyGoldLowerThreshold;
		bool flag2 = source.Any((Hero h) => Hero.MainHero.Gold > partyGoldLowerThreshold - h.Gold);
		if (!CampaignUIHelper.GetMapScreenActionIsEnabledWithReason(out var disabledReason2))
		{
			disabledReason = disabledReason2;
			return false;
		}
		if (MobileParty.MainParty.IsCurrentlyAtSea || MobileParty.MainParty.IsInRaftState)
		{
			disabledReason = GameTexts.FindText("str_cannot_perform_action_while_sailing");
			return false;
		}
		if (_faction.WarPartyLimit - _faction.WarPartyComponents.Count <= 0)
		{
			disabledReason = GameTexts.FindText("str_clan_doesnt_have_empty_party_slots");
			return false;
		}
		if (!flag)
		{
			disabledReason = GameTexts.FindText("str_clan_doesnt_have_available_heroes");
			return false;
		}
		if (!flag2)
		{
			disabledReason = new TextObject("{=VSUqbvbE}You don't have enough gold to create a new party.");
			return false;
		}
		disabledReason = TextObject.GetEmpty();
		return true;
	}

	private void OnAnyExpenseChange()
	{
		RefreshTotalExpense();
		_onExpenseChange();
	}

	private ClanPartyItemVM GetDefaultMember()
	{
		return Parties.FirstOrDefault();
	}

	public void ExecuteCreateNewParty()
	{
		if (CanCreateNewParty)
		{
			if (GetNewPartyLeaderCandidates().Any())
			{
				OnShowNewPartyPopup();
			}
			else
			{
				MBInformationManager.AddQuickInformation(new TextObject("{=qZvNIVGV}There is no one available in your clan who can lead a party right now."));
			}
		}
	}

	public void SelectParty(PartyBase party)
	{
		foreach (ClanPartyItemVM party2 in Parties)
		{
			if (party2.Party == party)
			{
				OnPartySelection(party2);
				break;
			}
		}
		foreach (ClanPartyItemVM caravan in Caravans)
		{
			if (caravan.Party == party)
			{
				OnPartySelection(caravan);
				break;
			}
		}
	}

	private void OnPartySelection(ClanPartyItemVM party)
	{
		if (CurrentSelectedParty != null)
		{
			CurrentSelectedParty.IsSelected = false;
		}
		CurrentSelectedParty = party;
		if (party != null)
		{
			party.IsSelected = true;
		}
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		Parties.ApplyActionOnAllItems(delegate(ClanPartyItemVM p)
		{
			p.OnFinalize();
		});
		Garrisons.ApplyActionOnAllItems(delegate(ClanPartyItemVM p)
		{
			p.OnFinalize();
		});
		Caravans.ApplyActionOnAllItems(delegate(ClanPartyItemVM p)
		{
			p.OnFinalize();
		});
	}

	public void OnShowNewPartyPopup()
	{
		ClanCardSelectionInfo obj = new ClanCardSelectionInfo(new TextObject("{=0Q4Xo2BQ}Select the Leader of the New Party"), GetNewPartyLeaderCandidates(), OnNewPartyCreationOver, isMultiSelection: false);
		_openCardSelectionPopup?.Invoke(obj);
	}

	private IEnumerable<ClanCardSelectionItemInfo> GetNewPartyLeaderCandidates()
	{
		int partyGoldLowerThreshold = Campaign.Current.Models.ClanFinanceModel.PartyGoldLowerThreshold;
		foreach (Hero item in _faction.Heroes.Where((Hero h) => !h.IsDisabled).Union(_faction.Companions))
		{
			if ((item.IsActive || item.IsReleased || item.IsFugitive) && !item.IsChild && item != Hero.MainHero && item.CanBeGovernorOrHavePartyRole())
			{
				bool flag = false;
				TextObject textObject = TextObject.GetEmpty();
				if (item.PartyBelongedToAsPrisoner != null)
				{
					textObject = new TextObject("{=vOojEcIf}You cannot assign a prisoner member as a new party leader");
				}
				else if (item.IsReleased)
				{
					textObject = new TextObject("{=OhNYkblK}This hero has just escaped from captors and will be available after some time.");
				}
				else if (item.PartyBelongedTo != null && item.PartyBelongedTo.LeaderHero == item)
				{
					textObject = new TextObject("{=aFYwbosi}This hero is already leading a party.");
				}
				else if (item.PartyBelongedTo != null && item.PartyBelongedTo.LeaderHero != Hero.MainHero)
				{
					textObject = new TextObject("{=FjJi1DJb}This hero is already a part of an another party.");
				}
				else if (item.GovernorOf != null)
				{
					textObject = new TextObject("{=Hz8XO8wk}Governors cannot lead a mobile party and be a governor at the same time.");
				}
				else if (item.HeroState == Hero.CharacterStates.Disabled)
				{
					textObject = new TextObject("{=slzfQzl3}This hero is lost");
				}
				else if (item.HeroState == Hero.CharacterStates.Fugitive)
				{
					textObject = new TextObject("{=dD3kRDHi}This hero is a fugitive and running from their captors. They will be available after some time.");
				}
				else if (partyGoldLowerThreshold - item.Gold > Hero.MainHero.Gold)
				{
					textObject = new TextObject("{=xpCdwmlX}You don't have enough gold to make {HERO.NAME} a party leader.");
					textObject.SetCharacterProperties("HERO", item.CharacterObject);
				}
				else if (item.PartyBelongedTo != null && item.PartyBelongedTo.IsCurrentlyAtSea)
				{
					textObject = new TextObject("{=1ELK1UbN}{HERO.NAME} is currently sailing.");
					textObject.SetCharacterProperties("HERO", item.CharacterObject);
				}
				else
				{
					flag = true;
				}
				yield return new ClanCardSelectionItemInfo(item, item.Name, new CharacterImageIdentifier(CampaignUIHelper.GetCharacterCode(item.CharacterObject)), CardSelectionItemSpriteType.None, null, null, GetNewPartyLeaderCandidateProperties(item), !flag, textObject, null);
			}
		}
	}

	private IEnumerable<ClanCardSelectionItemPropertyInfo> GetNewPartyLeaderCandidateProperties(Hero hero)
	{
		yield return new ClanCardSelectionItemPropertyInfo(TextObject.GetEmpty());
		TextObject textObject = new TextObject("{=hwrQqWir}No Skills");
		int num = 0;
		foreach (SkillObject leaderAssignmentRelevantSkill in _leaderAssignmentRelevantSkills)
		{
			TextObject textObject2 = new TextObject("{=!}{SKILL_VALUE}");
			textObject2.SetTextVariable("SKILL_VALUE", hero.GetSkillValue(leaderAssignmentRelevantSkill));
			TextObject textObject3 = ClanCardSelectionItemPropertyInfo.CreateLabeledValueText(leaderAssignmentRelevantSkill.Name, textObject2);
			if (num == 0)
			{
				textObject = textObject3;
			}
			else
			{
				TextObject textObject4 = GameTexts.FindText("str_string_newline_newline_string");
				textObject4.SetTextVariable("STR1", textObject);
				textObject4.SetTextVariable("STR2", textObject3);
				textObject = textObject4;
			}
			num++;
		}
		yield return new ClanCardSelectionItemPropertyInfo(GameTexts.FindText("str_skills"), textObject);
	}

	private void OnNewPartyCreationOver(List<object> selectedItems, Action closePopup)
	{
		if (selectedItems.Count == 1)
		{
			Hero newLeader = selectedItems.FirstOrDefault() as Hero;
			int partyGoldLowerThreshold = Campaign.Current.Models.ClanFinanceModel.PartyGoldLowerThreshold;
			if (newLeader.Gold < partyGoldLowerThreshold)
			{
				string titleText = new TextObject("{=DAYoD0aW}Create Party").ToString();
				string text = new TextObject("{=fRz2DJf4}Creating the party will cost you {PARTY_COST}{GOLD_ICON}. Are you sure?").SetTextVariable("PARTY_COST", partyGoldLowerThreshold - newLeader.Gold).SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">").ToString();
				InformationManager.ShowInquiry(new InquiryData(titleText, text, isAffirmativeOptionShown: true, isNegativeOptionShown: true, new TextObject("{=aeouhelq}Yes").ToString(), new TextObject("{=3CpNUnVl}Cancel").ToString(), delegate
				{
					closePopup?.Invoke();
					CreateNewClanParty(newLeader, partyGoldLowerThreshold);
				}, null));
			}
			else
			{
				closePopup?.Invoke();
				CreateNewClanParty(newLeader, partyGoldLowerThreshold);
			}
		}
		else
		{
			closePopup?.Invoke();
		}
	}

	private void CreateNewClanParty(Hero newLeader, int partyGoldLowerThreshold)
	{
		if (newLeader.PartyBelongedTo == MobileParty.MainParty)
		{
			_openPartyAsManage(newLeader);
			return;
		}
		MobileParty mobileParty = MobilePartyHelper.CreateNewClanMobileParty(newLeader, _faction);
		if (newLeader.Gold < partyGoldLowerThreshold)
		{
			GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, newLeader, partyGoldLowerThreshold - newLeader.Gold);
		}
		mobileParty.SetMoveModeHold();
		_onRefresh();
	}

	public void OnShowChangeLeaderPopup()
	{
		if (CurrentSelectedParty?.Party?.MobileParty != null)
		{
			ClanCardSelectionInfo obj = new ClanCardSelectionInfo(GameTexts.FindText("str_change_party_leader"), GetChangeLeaderCandidates(), OnChangeLeaderOver, isMultiSelection: false);
			_openCardSelectionPopup?.Invoke(obj);
		}
	}

	private IEnumerable<ClanCardSelectionItemInfo> GetChangeLeaderCandidates()
	{
		TextObject cannotDisbandReason;
		bool canDisbandParty = GetCanDisbandParty(out cannotDisbandReason);
		yield return new ClanCardSelectionItemInfo(GameTexts.FindText("str_disband_party"), !canDisbandParty, cannotDisbandReason, null);
		foreach (Hero item in _faction.Heroes.Where((Hero h) => !h.IsDisabled).Union(_faction.Companions))
		{
			if ((item.IsActive || item.IsReleased || item.IsFugitive || item.IsTraveling) && !item.IsChild && item != Hero.MainHero && item.CanLeadParty() && item != CurrentSelectedParty.LeaderMember?.HeroObject)
			{
				TextObject explanation;
				bool flag = FactionHelper.IsMainClanMemberAvailableForPartyLeaderChange(item, isSend: true, CurrentSelectedParty.Party.MobileParty, out explanation);
				CharacterImageIdentifier image = new CharacterImageIdentifier(CampaignUIHelper.GetCharacterCode(item.CharacterObject));
				yield return new ClanCardSelectionItemInfo(item, item.Name, image, CardSelectionItemSpriteType.None, null, null, GetChangeLeaderCandidateProperties(item), !flag, explanation, null);
			}
		}
	}

	private IEnumerable<ClanCardSelectionItemPropertyInfo> GetChangeLeaderCandidateProperties(Hero hero)
	{
		TextObject teleportationDelayText = CampaignUIHelper.GetTeleportationDelayText(hero, CurrentSelectedParty.Party);
		yield return new ClanCardSelectionItemPropertyInfo(teleportationDelayText);
		TextObject textObject = new TextObject("{=hwrQqWir}No Skills");
		int num = 0;
		foreach (SkillObject leaderAssignmentRelevantSkill in _leaderAssignmentRelevantSkills)
		{
			TextObject textObject2 = new TextObject("{=!}{SKILL_VALUE}");
			textObject2.SetTextVariable("SKILL_VALUE", hero.GetSkillValue(leaderAssignmentRelevantSkill));
			TextObject textObject3 = ClanCardSelectionItemPropertyInfo.CreateLabeledValueText(leaderAssignmentRelevantSkill.Name, textObject2);
			if (num == 0)
			{
				textObject = textObject3;
			}
			else
			{
				TextObject textObject4 = GameTexts.FindText("str_string_newline_newline_string");
				textObject4.SetTextVariable("STR1", textObject);
				textObject4.SetTextVariable("STR2", textObject3);
				textObject = textObject4;
			}
			num++;
		}
		yield return new ClanCardSelectionItemPropertyInfo(GameTexts.FindText("str_skills"), textObject);
	}

	private void OnChangeLeaderOver(List<object> selectedItems, Action closePopup)
	{
		if (selectedItems.Count == 1)
		{
			Hero newLeader = selectedItems.FirstOrDefault() as Hero;
			bool isDisband = newLeader == null;
			int partyGoldLowerThreshold = Campaign.Current.Models.ClanFinanceModel.PartyGoldLowerThreshold;
			PartyBase partyBase = CurrentSelectedParty?.Party;
			MobileParty mobileParty = partyBase?.MobileParty;
			DelayedTeleportationModel delayedTeleportationModel = Campaign.Current.Models.DelayedTeleportationModel;
			int num = ((!isDisband && mobileParty != null) ? ((int)Math.Ceiling(delayedTeleportationModel.GetTeleportationDelayAsHours(newLeader, mobileParty.Party).ResultNumber)) : 0);
			MBTextManager.SetTextVariable("TRAVEL_DURATION", CampaignUIHelper.GetHoursAndDaysTextFromHourValue(num).ToString());
			if (newLeader?.CharacterObject != null)
			{
				StringHelpers.SetCharacterProperties("LEADER", newLeader.CharacterObject);
				MBTextManager.SetTextVariable("PARTY_COST", partyGoldLowerThreshold - newLeader.Gold);
				MBTextManager.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				MBTextManager.SetTextVariable("DOES_LEADER_NEED_GOLD", (partyGoldLowerThreshold > newLeader.Gold) ? 1 : 0);
			}
			if (isDisband && partyBase != null && partyBase.Ships.Count > 0)
			{
				MBTextManager.SetTextVariable("DOES_DISBANDING_PARTY_HAVE_SHIP", true);
			}
			TextObject textObject = GameTexts.FindText(isDisband ? "str_disband_party" : "str_change_clan_party_leader");
			InformationManager.ShowInquiry(new InquiryData(text: GameTexts.FindText(isDisband ? "str_disband_party_inquiry" : ((num == 0) ? "str_change_clan_party_leader_instantly_inquiry" : "str_change_clan_party_leader_inquiry")).ToString(), titleText: textObject.ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, affirmativeText: GameTexts.FindText("str_yes").ToString(), negativeText: GameTexts.FindText("str_no").ToString(), affirmativeAction: delegate
			{
				closePopup?.Invoke();
				OnPartyLeaderChanged(newLeader);
				if (isDisband)
				{
					OnDisbandCurrentParty();
				}
				else if (newLeader.Gold < partyGoldLowerThreshold)
				{
					GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, newLeader, partyGoldLowerThreshold - newLeader.Gold);
				}
				_onRefresh?.Invoke();
			}, negativeAction: null));
		}
		else
		{
			closePopup?.Invoke();
		}
	}

	private void OnPartyLeaderChanged(Hero newLeader)
	{
		if (CurrentSelectedParty?.Party?.LeaderHero != null)
		{
			if (newLeader == null)
			{
				Hero leaderHero = CurrentSelectedParty.Party.LeaderHero;
				CurrentSelectedParty.Party.MobileParty.RemovePartyLeader();
				MakeHeroFugitiveAction.Apply(leaderHero);
			}
			else
			{
				TeleportHeroAction.ApplyDelayedTeleportToParty(CurrentSelectedParty.Party.LeaderHero, MobileParty.MainParty);
			}
		}
		if (newLeader != null)
		{
			TeleportHeroAction.ApplyDelayedTeleportToPartyAsPartyLeader(newLeader, CurrentSelectedParty.Party.MobileParty);
		}
	}

	private void OnDisbandCurrentParty()
	{
		DisbandPartyAction.StartDisband(CurrentSelectedParty.Party.MobileParty);
	}

	private bool GetCanDisbandParty(out TextObject cannotDisbandReason)
	{
		bool result = false;
		cannotDisbandReason = TextObject.GetEmpty();
		MobileParty mobileParty = CurrentSelectedParty?.Party?.MobileParty;
		if (mobileParty != null)
		{
			if (!CampaignUIHelper.GetMapScreenActionIsEnabledWithReason(out var disabledReason))
			{
				cannotDisbandReason = disabledReason;
			}
			else if (mobileParty.IsMilitia)
			{
				cannotDisbandReason = GameTexts.FindText("str_cannot_disband_milita_party");
			}
			else if (mobileParty.IsGarrison)
			{
				cannotDisbandReason = GameTexts.FindText("str_cannot_disband_garrison_party");
			}
			else if (mobileParty.IsMainParty)
			{
				cannotDisbandReason = GameTexts.FindText("str_cannot_disband_main_party");
			}
			else if (CurrentSelectedParty.IsDisbanding)
			{
				cannotDisbandReason = GameTexts.FindText("str_cannot_disband_already_disbanding_party");
			}
			else if (mobileParty.MapEvent != null || mobileParty.SiegeEvent != null)
			{
				cannotDisbandReason = GameTexts.FindText("str_cannot_disband_during_battle");
			}
			else
			{
				result = true;
			}
		}
		return result;
	}
}
