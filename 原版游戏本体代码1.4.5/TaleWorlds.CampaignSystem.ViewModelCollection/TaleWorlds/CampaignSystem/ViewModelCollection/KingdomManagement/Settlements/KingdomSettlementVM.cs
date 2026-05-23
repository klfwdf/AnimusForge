using System;
using System.Linq;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Settlements;

public class KingdomSettlementVM : KingdomCategoryVM
{
	private readonly Action<KingdomDecision> _forceDecision;

	private readonly Action<Settlement> _onGrantFief;

	private readonly Kingdom _kingdom;

	private KingdomDecision _currenItemsUnresolvedDecision;

	private MBBindingList<KingdomSettlementItemVM> _settlements;

	private KingdomSettlementItemVM _currentSelectedSettlement;

	private HintViewModel _annexHint;

	private string _ownerText;

	private string _nameText;

	private string _typeText;

	private string _prosperityText;

	private string _foodText;

	private string _garrisonText;

	private string _militiaText;

	private string _annexText;

	private string _clanText;

	private string _villagesText;

	private string _annexActionExplanationText;

	private string _proposeText;

	private string _defendersText;

	private int _annexCost;

	private bool _canAnnexCurrentSettlement;

	private bool _hasCost;

	private KingdomSettlementSortControllerVM _settlementSortController;

	[DataSourceProperty]
	public KingdomSettlementItemVM CurrentSelectedSettlement
	{
		get
		{
			return _currentSelectedSettlement;
		}
		set
		{
			if (value != _currentSelectedSettlement)
			{
				_currentSelectedSettlement = value;
				OnPropertyChangedWithValue(value, "CurrentSelectedSettlement");
			}
		}
	}

	[DataSourceProperty]
	public KingdomSettlementSortControllerVM SettlementSortController
	{
		get
		{
			return _settlementSortController;
		}
		set
		{
			if (value != _settlementSortController)
			{
				_settlementSortController = value;
				OnPropertyChangedWithValue(value, "SettlementSortController");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel AnnexHint
	{
		get
		{
			return _annexHint;
		}
		set
		{
			if (value != _annexHint)
			{
				_annexHint = value;
				OnPropertyChangedWithValue(value, "AnnexHint");
			}
		}
	}

	[DataSourceProperty]
	public string ProposeText
	{
		get
		{
			return _proposeText;
		}
		set
		{
			if (value != _proposeText)
			{
				_proposeText = value;
				OnPropertyChangedWithValue(value, "ProposeText");
			}
		}
	}

	[DataSourceProperty]
	public string AnnexActionExplanationText
	{
		get
		{
			return _annexActionExplanationText;
		}
		set
		{
			if (value != _annexActionExplanationText)
			{
				_annexActionExplanationText = value;
				OnPropertyChangedWithValue(value, "AnnexActionExplanationText");
			}
		}
	}

	[DataSourceProperty]
	public string ProsperityText
	{
		get
		{
			return _prosperityText;
		}
		set
		{
			if (value != _prosperityText)
			{
				_prosperityText = value;
				OnPropertyChangedWithValue(value, "ProsperityText");
			}
		}
	}

	[DataSourceProperty]
	public string VillagesText
	{
		get
		{
			return _villagesText;
		}
		set
		{
			if (value != _villagesText)
			{
				_villagesText = value;
				OnPropertyChangedWithValue(value, "VillagesText");
			}
		}
	}

	[DataSourceProperty]
	public string OwnerText
	{
		get
		{
			return _ownerText;
		}
		set
		{
			if (value != _ownerText)
			{
				_ownerText = value;
				OnPropertyChangedWithValue(value, "OwnerText");
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
	public string ClanText
	{
		get
		{
			return _clanText;
		}
		set
		{
			if (value != _clanText)
			{
				_clanText = value;
				OnPropertyChangedWithValue(value, "ClanText");
			}
		}
	}

	[DataSourceProperty]
	public string FoodText
	{
		get
		{
			return _foodText;
		}
		set
		{
			if (value != _foodText)
			{
				_foodText = value;
				OnPropertyChangedWithValue(value, "FoodText");
			}
		}
	}

	[DataSourceProperty]
	public string GarrisonText
	{
		get
		{
			return _garrisonText;
		}
		set
		{
			if (value != _garrisonText)
			{
				_garrisonText = value;
				OnPropertyChangedWithValue(value, "GarrisonText");
			}
		}
	}

	[DataSourceProperty]
	public string MilitiaText
	{
		get
		{
			return _militiaText;
		}
		set
		{
			if (value != _militiaText)
			{
				_militiaText = value;
				OnPropertyChangedWithValue(value, "MilitiaText");
			}
		}
	}

	[DataSourceProperty]
	public string AnnexText
	{
		get
		{
			return _annexText;
		}
		set
		{
			if (value != _annexText)
			{
				_annexText = value;
				OnPropertyChangedWithValue(value, "AnnexText");
			}
		}
	}

	[DataSourceProperty]
	public string TypeText
	{
		get
		{
			return _typeText;
		}
		set
		{
			if (value != _typeText)
			{
				_typeText = value;
				OnPropertyChangedWithValue(value, "TypeText");
			}
		}
	}

	[DataSourceProperty]
	public int AnnexCost
	{
		get
		{
			return _annexCost;
		}
		set
		{
			if (value != _annexCost)
			{
				_annexCost = value;
				OnPropertyChangedWithValue(value, "AnnexCost");
			}
		}
	}

	[DataSourceProperty]
	public string DefendersText
	{
		get
		{
			return _defendersText;
		}
		set
		{
			if (value != _defendersText)
			{
				_defendersText = value;
				OnPropertyChangedWithValue(value, "DefendersText");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<KingdomSettlementItemVM> Settlements
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
	public bool CanAnnexCurrentSettlement
	{
		get
		{
			return _canAnnexCurrentSettlement;
		}
		set
		{
			if (value != _canAnnexCurrentSettlement)
			{
				_canAnnexCurrentSettlement = value;
				OnPropertyChangedWithValue(value, "CanAnnexCurrentSettlement");
			}
		}
	}

	[DataSourceProperty]
	public bool HasCost
	{
		get
		{
			return _hasCost;
		}
		set
		{
			if (value != _hasCost)
			{
				_hasCost = value;
				OnPropertyChangedWithValue(value, "HasCost");
			}
		}
	}

	public KingdomSettlementVM(Action<KingdomDecision> forceDecision, Action<Settlement> onGrantFief)
	{
		_forceDecision = forceDecision;
		_onGrantFief = onGrantFief;
		_kingdom = Hero.MainHero.MapFaction as Kingdom;
		AnnexCost = Campaign.Current.Models.DiplomacyModel.GetInfluenceCostOfAnnexation(Clan.PlayerClan);
		AnnexHint = new HintViewModel();
		base.IsAcceptableItemSelected = false;
		Settlements = new MBBindingList<KingdomSettlementItemVM>();
		RefreshSettlementList();
		base.NotificationCount = 0;
		SettlementSortController = new KingdomSettlementSortControllerVM(Settlements);
		RefreshValues();
	}

	protected virtual KingdomSettlementItemVM CreateSettlementItemVM(Settlement settlement, Action<KingdomSettlementItemVM> onSelect)
	{
		return new KingdomSettlementItemVM(settlement, onSelect);
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		OwnerText = GameTexts.FindText("str_owner").ToString();
		NameText = GameTexts.FindText("str_scoreboard_header", "name").ToString();
		TypeText = GameTexts.FindText("str_sort_by_type_label").ToString();
		ProsperityText = GameTexts.FindText("str_prosperity_abbr").ToString();
		FoodText = GameTexts.FindText("str_inventory_category_tooltip", "6").ToString();
		GarrisonText = GameTexts.FindText("str_map_tooltip_garrison").ToString();
		MilitiaText = GameTexts.FindText("str_militia").ToString();
		ClanText = GameTexts.FindText("str_clans").ToString();
		VillagesText = GameTexts.FindText("str_villages").ToString();
		base.NoItemSelectedText = GameTexts.FindText("str_kingdom_no_settlement_selected").ToString();
		ProposeText = GameTexts.FindText("str_policy_propose").ToString();
		DefendersText = GameTexts.FindText("str_sort_by_defenders_label").ToString();
		base.CategoryNameText = new TextObject("{=qKUjgS6r}Settlement").ToString();
		Settlements.ApplyActionOnAllItems(delegate(KingdomSettlementItemVM x)
		{
			x.RefreshValues();
		});
		CurrentSelectedSettlement?.RefreshValues();
	}

	public void RefreshSettlementList()
	{
		Settlements.Clear();
		if (_kingdom != null)
		{
			foreach (Settlement item2 in _kingdom.Settlements.Where((Settlement S) => S.IsCastle || S.IsTown))
			{
				KingdomSettlementItemVM item = CreateSettlementItemVM(item2, OnSettlementSelection);
				Settlements.Add(item);
			}
		}
		if (Settlements.Count > 0)
		{
			SetCurrentSelectedSettlement(Settlements.FirstOrDefault());
		}
	}

	private void SetCurrentSelectedSettlement(KingdomSettlementItemVM settlementItem)
	{
		if (CurrentSelectedSettlement == settlementItem)
		{
			return;
		}
		if (CurrentSelectedSettlement != null)
		{
			CurrentSelectedSettlement.IsSelected = false;
		}
		CurrentSelectedSettlement = settlementItem;
		CurrentSelectedSettlement.IsSelected = true;
		if (settlementItem != null)
		{
			_currenItemsUnresolvedDecision = GetSettlementsAnyWaitingDecision(settlementItem.Settlement);
			if (_currenItemsUnresolvedDecision != null)
			{
				base.IsAcceptableItemSelected = true;
				AnnexCost = 0;
				AnnexText = GameTexts.FindText("str_resolve").ToString();
				AnnexActionExplanationText = GameTexts.FindText("str_resolve_explanation").ToString();
				AnnexHint.HintText = TextObject.GetEmpty();
			}
			else if (settlementItem.Owner.Hero == Hero.MainHero)
			{
				if (Hero.MainHero.IsKingdomLeader)
				{
					AnnexActionExplanationText = new TextObject("{=G2h0V10w}Gift this settlement to a clan in your kingdom.").ToString();
					AnnexText = new TextObject("{=sffGeQ1g}Gift").ToString();
				}
				else
				{
					AnnexActionExplanationText = new TextObject("{=1UbocG5B}Denounce your rights and responsibilities from this fief by giving it back to the realm.").ToString();
					AnnexText = new TextObject("{=U3ksQXD3}Give Away").ToString();
				}
				if (Hero.MainHero.IsPrisoner)
				{
					CanAnnexCurrentSettlement = false;
					HasCost = true;
					AnnexHint.HintText = GameTexts.FindText("str_action_disabled_reason_prisoner");
				}
				else if (!Campaign.Current.Models.DiplomacyModel.CanSettlementBeGifted(_currentSelectedSettlement.Settlement))
				{
					CanAnnexCurrentSettlement = false;
					HasCost = true;
					AnnexHint.HintText = GameTexts.FindText("str_cannot_annex_waiting_for_ruler_decision");
				}
				else if (PlayerEncounter.Current != null && PlayerEncounter.EncounterSettlement == null)
				{
					CanAnnexCurrentSettlement = false;
					HasCost = true;
					AnnexHint.HintText = GameTexts.FindText("str_action_disabled_reason_encounter");
				}
				else if (PlayerSiege.PlayerSiegeEvent != null)
				{
					CanAnnexCurrentSettlement = false;
					HasCost = true;
					AnnexHint.HintText = GameTexts.FindText("str_action_disabled_reason_siege");
				}
				else
				{
					CanAnnexCurrentSettlement = true;
					HasCost = false;
					AnnexHint.HintText = TextObject.GetEmpty();
				}
			}
			else
			{
				AnnexText = GameTexts.FindText("str_policy_propose").ToString();
				AnnexCost = Campaign.Current.Models.DiplomacyModel.GetInfluenceCostOfAnnexation(Clan.PlayerClan);
				AnnexActionExplanationText = GameTexts.FindText("str_annex_fief_action_explanation").SetTextVariable("SUPPORT", CalculateLikelihood(settlementItem.Settlement)).ToString();
				CanAnnexCurrentSettlement = GetCanAnnexSettlementWithReason(AnnexCost, out var disabledReason);
				AnnexHint.HintText = disabledReason;
				HasCost = true;
			}
		}
		base.IsAcceptableItemSelected = CurrentSelectedSettlement != null;
	}

	private bool GetCanAnnexSettlementWithReason(int annexCost, out TextObject disabledReason)
	{
		if (!CampaignUIHelper.GetMapScreenActionIsEnabledWithReason(out var disabledReason2))
		{
			disabledReason = disabledReason2;
			return false;
		}
		if (Hero.MainHero.Clan.Influence < (float)annexCost)
		{
			disabledReason = GameTexts.FindText("str_warning_you_dont_have_enough_influence");
			return false;
		}
		if (CurrentSelectedSettlement.Settlement.OwnerClan == _kingdom.RulingClan)
		{
			disabledReason = GameTexts.FindText("str_cannot_annex_ruling_clan_settlement");
			return false;
		}
		if (Clan.PlayerClan.IsUnderMercenaryService)
		{
			disabledReason = GameTexts.FindText("str_cannot_annex_while_mercenary");
			return false;
		}
		disabledReason = TextObject.GetEmpty();
		return true;
	}

	public void SelectSettlement(Settlement settlement)
	{
		foreach (KingdomSettlementItemVM settlement2 in Settlements)
		{
			if (settlement2.Settlement == settlement)
			{
				OnSettlementSelection(settlement2);
				break;
			}
		}
	}

	private void OnSettlementSelection(KingdomSettlementItemVM settlement)
	{
		if (_currentSelectedSettlement != settlement)
		{
			SetCurrentSelectedSettlement(settlement);
		}
	}

	private void ExecuteAnnex()
	{
		if (_currentSelectedSettlement == null)
		{
			return;
		}
		if (_currenItemsUnresolvedDecision != null)
		{
			_forceDecision(_currenItemsUnresolvedDecision);
			return;
		}
		Settlement settlement = _currentSelectedSettlement.Settlement;
		if (settlement.OwnerClan.Leader == Hero.MainHero)
		{
			_onGrantFief(settlement);
		}
		else if (Hero.MainHero.Clan.Influence >= (float)AnnexCost)
		{
			SettlementClaimantPreliminaryDecision settlementClaimantPreliminaryDecision = new SettlementClaimantPreliminaryDecision(Clan.PlayerClan, settlement);
			Clan.PlayerClan.Kingdom.AddDecision(settlementClaimantPreliminaryDecision);
			_forceDecision(settlementClaimantPreliminaryDecision);
		}
	}

	private KingdomDecision GetSettlementsAnyWaitingDecision(Settlement settlement)
	{
		KingdomDecision kingdomDecision = Clan.PlayerClan.Kingdom.UnresolvedDecisions.FirstOrDefault((KingdomDecision d) => d is SettlementClaimantDecision settlementClaimantDecision && settlementClaimantDecision.Settlement == settlement && !d.ShouldBeCancelled());
		KingdomDecision kingdomDecision2 = Clan.PlayerClan.Kingdom.UnresolvedDecisions.FirstOrDefault((KingdomDecision d) => d is SettlementClaimantPreliminaryDecision settlementClaimantPreliminaryDecision && settlementClaimantPreliminaryDecision.Settlement == settlement && !d.ShouldBeCancelled());
		if (kingdomDecision == null)
		{
			kingdomDecision = kingdomDecision2;
		}
		return kingdomDecision;
	}

	private static TextObject CalculateLikelihood(Settlement settlement)
	{
		SettlementClaimantPreliminaryDecision decision = new SettlementClaimantPreliminaryDecision(Clan.PlayerClan, settlement);
		return GameTexts.FindText("str_decision_outcome_support_status", KingdomElection.GetElectionOutcomeSupport(decision, Clan.PlayerClan).ToString());
	}
}
