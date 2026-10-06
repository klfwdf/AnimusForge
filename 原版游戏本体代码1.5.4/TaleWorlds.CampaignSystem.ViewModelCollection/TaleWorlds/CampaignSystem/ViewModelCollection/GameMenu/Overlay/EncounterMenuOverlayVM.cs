using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Overlay;

[MenuOverlay("EncounterMenuOverlay")]
public class EncounterMenuOverlayVM : GameMenuOverlay
{
	private GameMenuPartyItemVM _defenderLeadingParty;

	private GameMenuPartyItemVM _attackerLeadingParty;

	private string _titleText;

	private BannerImageIdentifierVM _defenderPartyBanner;

	private BannerImageIdentifierVM _attackerPartyBanner;

	private MBBindingList<GameMenuPartyItemVM> _attackerPartyList;

	private MBBindingList<GameMenuPartyItemVM> _defenderPartyList;

	private string _attackerPartyMorale;

	private string _defenderPartyMorale;

	private int _attackerPartyCount;

	private int _defenderPartyCount;

	private int _defenderShipCount;

	private int _attackerShipCount;

	private string _attackerPartyFood;

	private string _defenderPartyFood;

	private string _defenderWallHitPoints;

	private string _defenderPartyCountLbl;

	private string _attackerPartyCountLbl;

	private bool _isNaval;

	private bool _isSiege;

	private PowerLevelComparer _powerComparer;

	private HintViewModel _attackerBannerHint;

	private HintViewModel _defenderBannerHint;

	private BasicTooltipViewModel _attackerTroopNumHint;

	private BasicTooltipViewModel _defenderTroopNumHint;

	private BasicTooltipViewModel _attackerShipNumHint;

	private BasicTooltipViewModel _defenderShipNumHint;

	private BasicTooltipViewModel _defenderWallHint;

	private BasicTooltipViewModel _defenderFoodHint;

	private BasicTooltipViewModel _attackerFoodHint;

	private BasicTooltipViewModel _attackerMoraleHint;

	private BasicTooltipViewModel _defenderMoraleHint;

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
	public BannerImageIdentifierVM DefenderPartyBanner
	{
		get
		{
			return _defenderPartyBanner;
		}
		set
		{
			if (value != _defenderPartyBanner)
			{
				_defenderPartyBanner = value;
				OnPropertyChangedWithValue(value, "DefenderPartyBanner");
			}
		}
	}

	[DataSourceProperty]
	public BannerImageIdentifierVM AttackerPartyBanner
	{
		get
		{
			return _attackerPartyBanner;
		}
		set
		{
			if (value != _attackerPartyBanner)
			{
				_attackerPartyBanner = value;
				OnPropertyChangedWithValue(value, "AttackerPartyBanner");
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
	public MBBindingList<GameMenuPartyItemVM> AttackerPartyList
	{
		get
		{
			return _attackerPartyList;
		}
		set
		{
			if (value != _attackerPartyList)
			{
				_attackerPartyList = value;
				OnPropertyChangedWithValue(value, "AttackerPartyList");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<GameMenuPartyItemVM> DefenderPartyList
	{
		get
		{
			return _defenderPartyList;
		}
		set
		{
			if (value != _defenderPartyList)
			{
				_defenderPartyList = value;
				OnPropertyChangedWithValue(value, "DefenderPartyList");
			}
		}
	}

	[DataSourceProperty]
	public string DefenderPartyMorale
	{
		get
		{
			return _defenderPartyMorale;
		}
		set
		{
			if (value != _defenderPartyMorale)
			{
				_defenderPartyMorale = value;
				OnPropertyChangedWithValue(value, "DefenderPartyMorale");
			}
		}
	}

	[DataSourceProperty]
	public string AttackerPartyMorale
	{
		get
		{
			return _attackerPartyMorale;
		}
		set
		{
			if (value != _attackerPartyMorale)
			{
				_attackerPartyMorale = value;
				OnPropertyChangedWithValue(value, "AttackerPartyMorale");
			}
		}
	}

	[DataSourceProperty]
	public int DefenderPartyCount
	{
		get
		{
			return _defenderPartyCount;
		}
		set
		{
			if (value != _defenderPartyCount)
			{
				_defenderPartyCount = value;
				OnPropertyChangedWithValue(value, "DefenderPartyCount");
			}
		}
	}

	[DataSourceProperty]
	public int AttackerPartyCount
	{
		get
		{
			return _attackerPartyCount;
		}
		set
		{
			if (value != _attackerPartyCount)
			{
				_attackerPartyCount = value;
				OnPropertyChangedWithValue(value, "AttackerPartyCount");
			}
		}
	}

	[DataSourceProperty]
	public int DefenderShipCount
	{
		get
		{
			return _defenderShipCount;
		}
		set
		{
			if (value != _defenderShipCount)
			{
				_defenderShipCount = value;
				OnPropertyChangedWithValue(value, "DefenderShipCount");
			}
		}
	}

	[DataSourceProperty]
	public int AttackerShipCount
	{
		get
		{
			return _attackerShipCount;
		}
		set
		{
			if (value != _attackerShipCount)
			{
				_attackerShipCount = value;
				OnPropertyChangedWithValue(value, "AttackerShipCount");
			}
		}
	}

	[DataSourceProperty]
	public string DefenderPartyFood
	{
		get
		{
			return _defenderPartyFood;
		}
		set
		{
			if (value != _defenderPartyFood)
			{
				_defenderPartyFood = value;
				OnPropertyChangedWithValue(value, "DefenderPartyFood");
			}
		}
	}

	[DataSourceProperty]
	public string AttackerPartyFood
	{
		get
		{
			return _attackerPartyFood;
		}
		set
		{
			if (value != _attackerPartyFood)
			{
				_attackerPartyFood = value;
				OnPropertyChangedWithValue(value, "AttackerPartyFood");
			}
		}
	}

	public string DefenderWallHitPoints
	{
		get
		{
			return _defenderWallHitPoints;
		}
		set
		{
			if (value != _defenderWallHitPoints)
			{
				_defenderWallHitPoints = value;
				OnPropertyChangedWithValue(value, "DefenderWallHitPoints");
			}
		}
	}

	[DataSourceProperty]
	public bool IsNaval
	{
		get
		{
			return _isNaval;
		}
		set
		{
			if (value != _isNaval)
			{
				_isNaval = value;
				OnPropertyChangedWithValue(value, "IsNaval");
			}
		}
	}

	[DataSourceProperty]
	public bool IsSiege
	{
		get
		{
			return _isSiege;
		}
		set
		{
			if (value != _isSiege)
			{
				_isSiege = value;
				OnPropertyChangedWithValue(value, "IsSiege");
			}
		}
	}

	[DataSourceProperty]
	public string DefenderPartyCountLbl
	{
		get
		{
			return _defenderPartyCountLbl;
		}
		set
		{
			if (value != _defenderPartyCountLbl)
			{
				_defenderPartyCountLbl = value;
				OnPropertyChangedWithValue(value, "DefenderPartyCountLbl");
			}
		}
	}

	[DataSourceProperty]
	public string AttackerPartyCountLbl
	{
		get
		{
			return _attackerPartyCountLbl;
		}
		set
		{
			if (value != _attackerPartyCountLbl)
			{
				_attackerPartyCountLbl = value;
				OnPropertyChangedWithValue(value, "AttackerPartyCountLbl");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel AttackerBannerHint
	{
		get
		{
			return _attackerBannerHint;
		}
		set
		{
			if (value != _attackerBannerHint)
			{
				_attackerBannerHint = value;
				OnPropertyChangedWithValue(value, "AttackerBannerHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel DefenderBannerHint
	{
		get
		{
			return _defenderBannerHint;
		}
		set
		{
			if (value != _defenderBannerHint)
			{
				_defenderBannerHint = value;
				OnPropertyChangedWithValue(value, "DefenderBannerHint");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel AttackerTroopNumHint
	{
		get
		{
			return _attackerTroopNumHint;
		}
		set
		{
			if (value != _attackerTroopNumHint)
			{
				_attackerTroopNumHint = value;
				OnPropertyChangedWithValue(value, "AttackerTroopNumHint");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel DefenderTroopNumHint
	{
		get
		{
			return _defenderTroopNumHint;
		}
		set
		{
			if (value != _defenderTroopNumHint)
			{
				_defenderTroopNumHint = value;
				OnPropertyChangedWithValue(value, "DefenderTroopNumHint");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel AttackerShipNumHint
	{
		get
		{
			return _attackerShipNumHint;
		}
		set
		{
			if (value != _attackerShipNumHint)
			{
				_attackerShipNumHint = value;
				OnPropertyChangedWithValue(value, "AttackerShipNumHint");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel DefenderShipNumHint
	{
		get
		{
			return _defenderShipNumHint;
		}
		set
		{
			if (value != _defenderShipNumHint)
			{
				_defenderShipNumHint = value;
				OnPropertyChangedWithValue(value, "DefenderShipNumHint");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel DefenderWallHint
	{
		get
		{
			return _defenderWallHint;
		}
		set
		{
			if (value != _defenderWallHint)
			{
				_defenderWallHint = value;
				OnPropertyChangedWithValue(value, "DefenderWallHint");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel DefenderFoodHint
	{
		get
		{
			return _defenderFoodHint;
		}
		set
		{
			if (value != _defenderFoodHint)
			{
				_defenderFoodHint = value;
				OnPropertyChangedWithValue(value, "DefenderFoodHint");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel AttackerFoodHint
	{
		get
		{
			return _attackerFoodHint;
		}
		set
		{
			if (value != _attackerFoodHint)
			{
				_attackerFoodHint = value;
				OnPropertyChangedWithValue(value, "AttackerFoodHint");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel AttackerMoraleHint
	{
		get
		{
			return _attackerMoraleHint;
		}
		set
		{
			if (value != _attackerMoraleHint)
			{
				_attackerMoraleHint = value;
				OnPropertyChangedWithValue(value, "AttackerMoraleHint");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel DefenderMoraleHint
	{
		get
		{
			return _defenderMoraleHint;
		}
		set
		{
			if (value != _defenderMoraleHint)
			{
				_defenderMoraleHint = value;
				OnPropertyChangedWithValue(value, "DefenderMoraleHint");
			}
		}
	}

	public EncounterMenuOverlayVM()
	{
		AttackerPartyList = new MBBindingList<GameMenuPartyItemVM>();
		DefenderPartyList = new MBBindingList<GameMenuPartyItemVM>();
		base.CurrentOverlayType = 1;
		AttackerMoraleHint = new BasicTooltipViewModel(() => GetEncounterSideMoraleTooltip(BattleSideEnum.Attacker));
		DefenderMoraleHint = new BasicTooltipViewModel(() => GetEncounterSideMoraleTooltip(BattleSideEnum.Defender));
		AttackerFoodHint = new BasicTooltipViewModel(() => GetEncounterSideFoodTooltip(BattleSideEnum.Attacker));
		DefenderFoodHint = new BasicTooltipViewModel(() => GetEncounterSideFoodTooltip(BattleSideEnum.Defender));
		AttackerTroopNumHint = new BasicTooltipViewModel(() => GetEncounterSideTroopsTooltip(BattleSideEnum.Attacker));
		DefenderTroopNumHint = new BasicTooltipViewModel(() => GetEncounterSideTroopsTooltip(BattleSideEnum.Defender));
		AttackerShipNumHint = new BasicTooltipViewModel(() => GetEncounterSideShipsTooltip(BattleSideEnum.Attacker));
		DefenderShipNumHint = new BasicTooltipViewModel(() => GetEncounterSideShipsTooltip(BattleSideEnum.Defender));
		DefenderWallHint = new BasicTooltipViewModel();
		base.IsInitializationOver = false;
		UpdateLists();
		UpdateProperties();
		base.IsInitializationOver = true;
		RefreshValues();
	}

	private void SetAttackerAndDefenderParties(out bool attackerChanged, out bool defenderChanged)
	{
		attackerChanged = false;
		defenderChanged = false;
		if (MobileParty.MainParty.MapEvent != null)
		{
			PartyBase leaderParty = MobileParty.MainParty.MapEvent.GetLeaderParty(BattleSideEnum.Attacker);
			if (leaderParty.IsSettlement)
			{
				if (_attackerLeadingParty == null || _attackerLeadingParty.Party != leaderParty)
				{
					attackerChanged = true;
					_attackerLeadingParty = new GameMenuPartyItemVM(ExecuteOnSetAsActiveContextMenuItem, leaderParty.Settlement);
				}
			}
			else if (_attackerLeadingParty == null || _attackerLeadingParty.Party != leaderParty)
			{
				attackerChanged = true;
				_attackerLeadingParty = new GameMenuPartyItemVM(ExecuteOnSetAsActiveContextMenuItem, leaderParty, canShowQuest: false);
			}
			PartyBase leaderParty2 = MobileParty.MainParty.MapEvent.GetLeaderParty(BattleSideEnum.Defender);
			if (leaderParty2.IsSettlement)
			{
				if (_defenderLeadingParty == null || _defenderLeadingParty.Party != leaderParty2)
				{
					defenderChanged = true;
					_defenderLeadingParty = new GameMenuPartyItemVM(ExecuteOnSetAsActiveContextMenuItem, leaderParty2.Settlement);
				}
			}
			else if (_defenderLeadingParty == null || _defenderLeadingParty.Party != leaderParty2)
			{
				defenderChanged = true;
				_defenderLeadingParty = new GameMenuPartyItemVM(ExecuteOnSetAsActiveContextMenuItem, leaderParty2, canShowQuest: false);
			}
			return;
		}
		Settlement settlement = Settlement.CurrentSettlement ?? PlayerSiege.PlayerSiegeEvent.BesiegedSettlement;
		SiegeEvent siegeEvent = settlement.SiegeEvent;
		if (siegeEvent != null)
		{
			if (_defenderLeadingParty == null || _defenderLeadingParty.Settlement != settlement)
			{
				defenderChanged = true;
				_defenderLeadingParty = new GameMenuPartyItemVM(ExecuteOnSetAsActiveContextMenuItem, settlement);
			}
			if (_attackerLeadingParty == null || _attackerLeadingParty.Party != siegeEvent.BesiegerCamp.LeaderParty.Party)
			{
				attackerChanged = true;
				_attackerLeadingParty = new GameMenuPartyItemVM(ExecuteOnSetAsActiveContextMenuItem, siegeEvent.BesiegerCamp.LeaderParty.Party, canShowQuest: false);
			}
			DefenderWallHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetSiegeWallTooltip(settlement.Town.GetWallLevel(), TaleWorlds.Library.MathF.Ceiling(settlement.SettlementTotalWallHitPoints)));
		}
		else
		{
			Debug.FailedAssert("Encounter overlay is open but MapEvent AND SiegeEvent is null", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\GameMenu\\Overlay\\EncounterMenuOverlayVM.cs", "SetAttackerAndDefenderParties", 121);
		}
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		AttackerBannerHint = new HintViewModel(GameTexts.FindText("str_attacker_banner"));
		DefenderBannerHint = new HintViewModel(GameTexts.FindText("str_defender_banner"));
		base.ContextList.Add(new StringItemWithEnabledAndHintVM(base.ExecuteTroopAction, GameTexts.FindText("str_menu_overlay_context_list", MenuOverlayContextList.Encyclopedia.ToString()).ToString(), enabled: true, MenuOverlayContextList.Encyclopedia));
		AttackerPartyList.ApplyActionOnAllItems(delegate(GameMenuPartyItemVM x)
		{
			x.RefreshValues();
		});
		DefenderPartyList.ApplyActionOnAllItems(delegate(GameMenuPartyItemVM x)
		{
			x.RefreshValues();
		});
	}

	public override void OnFrameTick(float dt)
	{
		base.OnFrameTick(dt);
		if (MobileParty.MainParty.MapEvent != null && AttackerPartyList.Count + DefenderPartyList.Count != MobileParty.MainParty.MapEvent.InvolvedParties.Count())
		{
			UpdateLists();
		}
		AttackerPartyList.ApplyActionOnAllItems(delegate(GameMenuPartyItemVM ap)
		{
			ap.RefreshCounts();
		});
		DefenderPartyList.ApplyActionOnAllItems(delegate(GameMenuPartyItemVM dp)
		{
			dp.RefreshCounts();
		});
	}

	public override void Refresh()
	{
		base.IsInitializationOver = false;
		UpdateLists();
		UpdateProperties();
		base.IsInitializationOver = true;
	}

	private void UpdateProperties()
	{
		if (!IsSiege)
		{
			return;
		}
		bool flag = _defenderLeadingParty?.Settlement != null;
		float num = 0f;
		float num2 = 0f;
		if (flag)
		{
			(int, int) townFoodAndMarketStocks = TownHelpers.GetTownFoodAndMarketStocks((flag ? _defenderLeadingParty : _attackerLeadingParty).Settlement.Town);
			num2 = (float)(townFoodAndMarketStocks.Item1 + townFoodAndMarketStocks.Item2) / (0f - _defenderLeadingParty.Settlement.Town.FoodChangeWithoutMarketStocks);
		}
		foreach (GameMenuPartyItemVM defenderParty in DefenderPartyList)
		{
			num += defenderParty.Party.MobileParty.Morale;
			if (!flag)
			{
				num2 += defenderParty.Party.MobileParty.Food / (0f - defenderParty.Party.MobileParty.FoodChange);
			}
		}
		num /= (float)DefenderPartyList.Count;
		if (!flag)
		{
			num2 /= (float)DefenderPartyList.Count;
		}
		num2 = Math.Max((int)Math.Ceiling(num2), 0);
		MBTextManager.SetTextVariable("DAY_NUM", num2.ToString());
		MBTextManager.SetTextVariable("PLURAL", (num2 > 1f) ? 1 : 0);
		DefenderPartyFood = GameTexts.FindText("str_party_food_left").ToString();
		DefenderPartyMorale = num.ToString("0.0");
		num = 0f;
		num2 = 0f;
		if (!flag)
		{
			if (_attackerLeadingParty.Settlement != null)
			{
				num2 = _attackerLeadingParty.Settlement.Town.FoodStocks / _attackerLeadingParty.Settlement.Town.FoodChangeWithoutMarketStocks;
			}
			else if (_attackerLeadingParty.Party.MobileParty.CurrentSettlement != null)
			{
				num2 = _attackerLeadingParty.Party.MobileParty.CurrentSettlement.Town.FoodStocks / _attackerLeadingParty.Party.MobileParty.CurrentSettlement.Town.FoodChangeWithoutMarketStocks;
			}
			else if (Settlement.CurrentSettlement?.SiegeEvent != null)
			{
				num2 = Settlement.CurrentSettlement.Town.FoodStocks / Settlement.CurrentSettlement.Town.FoodChangeWithoutMarketStocks;
			}
			else
			{
				Debug.FailedAssert("There are no settlements involved in the siege", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\GameMenu\\Overlay\\EncounterMenuOverlayVM.cs", "UpdateProperties", 217);
			}
		}
		else if (Settlement.CurrentSettlement?.SiegeEvent != null)
		{
			num2 = Settlement.CurrentSettlement.Town.FoodStocks / Settlement.CurrentSettlement.Town.FoodChangeWithoutMarketStocks;
		}
		foreach (GameMenuPartyItemVM attackerParty in AttackerPartyList)
		{
			num += attackerParty.Party.MobileParty.Morale;
			if (flag)
			{
				num2 += attackerParty.Party.MobileParty.Food / (0f - attackerParty.Party.MobileParty.FoodChange);
			}
		}
		num /= (float)AttackerPartyList.Count;
		if (flag)
		{
			num2 /= (float)AttackerPartyList.Count;
		}
		num2 = Math.Max((int)Math.Ceiling(num2), 0);
		MBTextManager.SetTextVariable("DAY_NUM", num2.ToString());
		MBTextManager.SetTextVariable("PLURAL", (num2 > 1f) ? 1 : 0);
		AttackerPartyFood = GameTexts.FindText("str_party_food_left").ToString();
		AttackerPartyMorale = num.ToString("0.0");
		Settlement settlement = Settlement.CurrentSettlement ?? PlayerSiege.PlayerSiegeEvent?.BesiegedSettlement;
		if (settlement != null)
		{
			DefenderWallHitPoints = TaleWorlds.Library.MathF.Ceiling(settlement.SettlementTotalWallHitPoints).ToString();
		}
	}

	private void UpdateLists()
	{
		if (MobileParty.MainParty.MapEvent == null && (Settlement.CurrentSettlement ?? PlayerSiege.PlayerSiegeEvent?.BesiegedSettlement) == null)
		{
			return;
		}
		SetAttackerAndDefenderParties(out var attackerChanged, out var defenderChanged);
		if (_defenderLeadingParty != null && defenderChanged)
		{
			int num = DefenderPartyList.FindIndex((GameMenuPartyItemVM x) => x.Party == _defenderLeadingParty.Party);
			if (num != -1)
			{
				DefenderPartyList.RemoveAt(num);
			}
			DefenderPartyList.Insert(0, _defenderLeadingParty);
		}
		if (_attackerLeadingParty != null && attackerChanged)
		{
			int num2 = AttackerPartyList.FindIndex((GameMenuPartyItemVM x) => x.Party == _attackerLeadingParty.Party);
			if (num2 != -1)
			{
				AttackerPartyList.RemoveAt(num2);
			}
			AttackerPartyList.Insert(0, _attackerLeadingParty);
		}
		List<PartyBase> list = new List<PartyBase>();
		List<PartyBase> list2 = new List<PartyBase>();
		List<int> list3 = new List<int>();
		List<int> list4 = new List<int>();
		List<PartyBase> list5 = new List<PartyBase>();
		if (MobileParty.MainParty.MapEvent != null)
		{
			list5.AddRange(MobileParty.MainParty.MapEvent.InvolvedParties);
			IsSiege = false;
			IsNaval = MobileParty.MainParty.MapEvent.IsNavalMapEvent;
		}
		else
		{
			Settlement settlement = Settlement.CurrentSettlement ?? PlayerSiege.PlayerSiegeEvent.BesiegedSettlement;
			if (settlement.SiegeEvent == null)
			{
				PowerComparer = new PowerLevelComparer(1.0, 1.0);
				return;
			}
			SiegeEvent siegeEvent = settlement.SiegeEvent;
			list5.AddRange(siegeEvent.GetInvolvedPartiesForEventType(MapEvent.BattleTypes.Siege));
			IsSiege = true;
			IsNaval = false;
		}
		foreach (PartyBase item in list5)
		{
			bool flag = ((MobileParty.MainParty.MapEvent == null) ? (Settlement.CurrentSettlement ?? PlayerSiege.PlayerSiegeEvent.BesiegedSettlement).SiegeEvent.GetSiegeEventSide(BattleSideEnum.Defender).HasInvolvedPartyForEventType(item) : (item.Side == BattleSideEnum.Defender));
			List<PartyBase> list6 = (flag ? list2 : list);
			List<int> list7 = (flag ? list4 : list3);
			if (item.IsActive && item.MemberRoster.Count > 0)
			{
				int numberOfHealthyMembers = item.NumberOfHealthyMembers;
				int num3;
				for (num3 = 0; num3 < list7.Count && numberOfHealthyMembers <= list7[num3]; num3++)
				{
				}
				list7.Add(item.NumberOfHealthyMembers);
				list6.Insert(num3, item);
			}
		}
		float num4 = list2.Sum((PartyBase party) => party.CalculateCurrentStrength());
		float num5 = list.Sum((PartyBase party) => party.CalculateCurrentStrength());
		if (list5.AnyQ((PartyBase p) => p.MobileParty?.IsInfoHidden ?? false))
		{
			num4 = 1f;
			num5 = 0f;
		}
		if (PowerComparer == null)
		{
			PowerComparer = new PowerLevelComparer(num4, num5);
		}
		else
		{
			PowerComparer.Update(num4, num5, num4, num5);
		}
		List<PartyBase> list8 = list.OrderByDescending((PartyBase p) => p.NumberOfAllMembers).ToList();
		List<PartyBase> list9 = AttackerPartyList.Select((GameMenuPartyItemVM enemy) => enemy.Party).ToList();
		List<PartyBase> list10 = list8.Except(list9).ToList();
		list10.Remove(_attackerLeadingParty.Party);
		foreach (PartyBase item2 in list9.Except(list8).ToList())
		{
			for (int num6 = AttackerPartyList.Count - 1; num6 >= 0; num6--)
			{
				if (AttackerPartyList[num6].Party == item2)
				{
					AttackerPartyList.RemoveAt(num6);
				}
			}
		}
		if (IsSiege)
		{
			list10 = list10.Where((PartyBase x) => x.MemberRoster.TotalHealthyCount > 0).ToList();
		}
		foreach (PartyBase item3 in list10)
		{
			AttackerPartyList.Add(new GameMenuPartyItemVM(ExecuteOnSetAsActiveContextMenuItem, item3, canShowQuest: false));
		}
		List<PartyBase> list11 = list2.OrderByDescending((PartyBase p) => p.NumberOfAllMembers).ToList();
		List<PartyBase> list12 = DefenderPartyList.Select((GameMenuPartyItemVM ally) => ally.Party).ToList();
		List<PartyBase> list13 = list11.Except(list12).ToList();
		list13.Remove(_defenderLeadingParty.Party);
		foreach (PartyBase item4 in list12.Except(list11).ToList())
		{
			for (int num7 = DefenderPartyList.Count - 1; num7 >= 0; num7--)
			{
				if (DefenderPartyList[num7].Party == item4)
				{
					DefenderPartyList.RemoveAt(num7);
				}
			}
		}
		if (IsSiege)
		{
			list13 = list13.Where((PartyBase x) => x.MemberRoster.TotalHealthyCount > 0).ToList();
		}
		foreach (PartyBase item5 in list13)
		{
			DefenderPartyList.Add(new GameMenuPartyItemVM(ExecuteOnSetAsActiveContextMenuItem, item5, canShowQuest: false));
		}
		foreach (GameMenuPartyItemVM defenderParty in DefenderPartyList)
		{
			defenderParty.RefreshProperties();
		}
		foreach (GameMenuPartyItemVM attackerParty in AttackerPartyList)
		{
			attackerParty.RefreshProperties();
		}
		DefenderPartyCount = DefenderPartyList.Sum((GameMenuPartyItemVM p) => p?.Party?.NumberOfHealthyMembers ?? 0);
		DefenderPartyCountLbl = (DefenderPartyList.AnyQ((GameMenuPartyItemVM p) => p.Party.MobileParty?.IsInfoHidden ?? false) ? "?" : DefenderPartyCount.ToString());
		DefenderShipCount = DefenderPartyList.Sum((GameMenuPartyItemVM p) => p?.Party?.Ships.Count ?? 0);
		AttackerPartyCount = AttackerPartyList.Sum((GameMenuPartyItemVM p) => p?.Party?.NumberOfHealthyMembers ?? 0);
		AttackerPartyCountLbl = (AttackerPartyList.AnyQ((GameMenuPartyItemVM p) => p.Party.MobileParty?.IsInfoHidden ?? false) ? "?" : AttackerPartyCount.ToString());
		AttackerShipCount = AttackerPartyList.Sum((GameMenuPartyItemVM p) => p?.Party?.Ships.Count ?? 0);
		if (MobileParty.MainParty.MapEvent != null)
		{
			PartyBase leaderParty = MobileParty.MainParty.MapEvent.GetLeaderParty(BattleSideEnum.Attacker);
			PartyBase leaderParty2 = MobileParty.MainParty.MapEvent.GetLeaderParty(BattleSideEnum.Defender);
			if (_attackerLeadingParty.Party == leaderParty2 || _defenderLeadingParty.Party == leaderParty)
			{
				GameMenuPartyItemVM attackerLeadingParty = _attackerLeadingParty;
				_attackerLeadingParty = _defenderLeadingParty;
				_defenderLeadingParty = attackerLeadingParty;
			}
		}
		string titleText;
		if (!IsSiege)
		{
			string text = (TitleText = GameTexts.FindText("str_battle").ToString());
			titleText = text;
		}
		else
		{
			titleText = GameTexts.FindText("str_siege").ToString();
		}
		TitleText = titleText;
		IFaction faction = ((_defenderLeadingParty.Party == null) ? _defenderLeadingParty.Settlement.MapFaction : _defenderLeadingParty.Party.MapFaction);
		IFaction faction2 = ((_attackerLeadingParty.Party == null) ? _attackerLeadingParty.Settlement.MapFaction : _attackerLeadingParty.Party.MapFaction);
		Banner banner = ((_defenderLeadingParty.Party == null) ? _defenderLeadingParty.Settlement.OwnerClan.Banner : _defenderLeadingParty.Party.Banner);
		Banner banner2 = ((_attackerLeadingParty.Party == null) ? _attackerLeadingParty.Settlement.OwnerClan.Banner : _attackerLeadingParty.Party.Banner);
		DefenderPartyBanner = new BannerImageIdentifierVM(banner, nineGrid: true);
		AttackerPartyBanner = new BannerImageIdentifierVM(banner2, nineGrid: true);
		string defenderColor = ((faction == null || !(faction is Kingdom)) ? Color.FromUint(faction?.Banner?.GetPrimaryColor() ?? Color.White.ToUnsignedInteger()).ToString() : Color.FromUint(((Kingdom)faction).PrimaryBannerColor).ToString());
		string attackerColor = ((faction2 == null || !(faction2 is Kingdom)) ? Color.FromUint(faction2?.Banner?.GetPrimaryColor() ?? Color.White.ToUnsignedInteger()).ToString() : Color.FromUint(((Kingdom)faction2).PrimaryBannerColor).ToString());
		PowerComparer.SetColors(defenderColor, attackerColor);
	}

	private List<TooltipProperty> GetEncounterSideFoodTooltip(BattleSideEnum side)
	{
		List<TooltipProperty> list = new List<TooltipProperty>();
		bool flag = _defenderLeadingParty?.Settlement != null;
		bool flag2 = (flag && flag && side == BattleSideEnum.Defender) || (!flag && side == BattleSideEnum.Attacker);
		if (IsSiege && flag2)
		{
			list.Add(new TooltipProperty(new TextObject("{=OSsSBHKe}Settlement's Food").ToString(), "", 0, onlyShowWhenExtended: false, TooltipProperty.TooltipPropertyFlags.Title));
			Town town = (flag ? _defenderLeadingParty : _attackerLeadingParty)?.Settlement?.Town;
			float foodChange = town?.FoodChangeWithoutMarketStocks ?? 0f;
			(int, int) townFoodAndMarketStocks = TownHelpers.GetTownFoodAndMarketStocks(town);
			list.Add(new TooltipProperty(new TextObject("{=EkFDvG7z}Settlement Food Stocks").ToString(), townFoodAndMarketStocks.Item1.ToString("F0"), 0));
			if (townFoodAndMarketStocks.Item2 != 0)
			{
				list.Add(new TooltipProperty(new TextObject("{=HTtWslIx}Market Food Stocks").ToString(), townFoodAndMarketStocks.Item2.ToString("F0"), 0));
			}
			list.Add(new TooltipProperty(new TextObject("{=laznt9ZK}Settlement Food Change").ToString(), foodChange.ToString("F2"), 0));
			list.Add(new TooltipProperty("", string.Empty, 0, onlyShowWhenExtended: false, TooltipProperty.TooltipPropertyFlags.RundownSeperator));
			list.Add(new TooltipProperty(new TextObject("{=DNXD37JL}Settlement's Days Until Food Runs Out").ToString(), CampaignUIHelper.GetDaysUntilNoFood(townFoodAndMarketStocks.Item1 + townFoodAndMarketStocks.Item2, foodChange), 0));
			if (town?.Settlement != null && SettlementHelper.IsGarrisonStarving(town.Settlement))
			{
				list.Add(new TooltipProperty(new TextObject("{=0rmpC7jf}The Garrison is Starving").ToString(), string.Empty, 0));
			}
		}
		else
		{
			list.Add(new TooltipProperty(new TextObject("{=Q8dhryRX}Parties' Food").ToString(), "", 0, onlyShowWhenExtended: false, TooltipProperty.TooltipPropertyFlags.Title));
			MBBindingList<GameMenuPartyItemVM> mBBindingList = ((side == BattleSideEnum.Attacker) ? AttackerPartyList : DefenderPartyList);
			double num = 0.0;
			foreach (GameMenuPartyItemVM item in mBBindingList)
			{
				float val = item.Party.MobileParty.Food / (0f - item.Party.MobileParty.FoodChange);
				num += (double)Math.Max(val, 0f);
				string daysUntilNoFood = CampaignUIHelper.GetDaysUntilNoFood(item.Party.MobileParty.Food, item.Party.MobileParty.FoodChange);
				list.Add(new TooltipProperty(item.Party.MobileParty.Name.ToString(), daysUntilNoFood, 0));
			}
			list.Add(new TooltipProperty("", string.Empty, 0, onlyShowWhenExtended: false, TooltipProperty.TooltipPropertyFlags.RundownSeperator));
			list.Add(new TooltipProperty(new TextObject("{=rwKBR4NE}Average Days Until Food Runs Out").ToString(), TaleWorlds.Library.MathF.Ceiling(num / (double)mBBindingList.Count).ToString(), 0));
		}
		return list;
	}

	private List<TooltipProperty> GetEncounterSideTroopsTooltip(BattleSideEnum side)
	{
		List<TooltipProperty> list = new List<TooltipProperty>();
		MBBindingList<GameMenuPartyItemVM> sideList = ((side == BattleSideEnum.Attacker) ? AttackerPartyList : DefenderPartyList);
		TroopRoster troopRoster = TroopRoster.CreateDummyTroopRoster();
		foreach (GameMenuPartyItemVM item in sideList)
		{
			for (int i = 0; i < item.Party.MemberRoster.Count; i++)
			{
				TroopRosterElement elementCopyAtIndex = item.Party.MemberRoster.GetElementCopyAtIndex(i);
				troopRoster.AddToCounts(elementCopyAtIndex.Character, elementCopyAtIndex.Number, insertAtFront: false, elementCopyAtIndex.WoundedNumber);
			}
		}
		Func<TroopRoster> getTempRoster = delegate
		{
			TroopRoster troopRoster2 = TroopRoster.CreateDummyTroopRoster();
			foreach (GameMenuPartyItemVM item2 in sideList)
			{
				for (int j = 0; j < item2.Party.MemberRoster.Count; j++)
				{
					TroopRosterElement elementCopyAtIndex4 = item2.Party.MemberRoster.GetElementCopyAtIndex(j);
					troopRoster2.AddToCounts(elementCopyAtIndex4.Character, elementCopyAtIndex4.Number, insertAtFront: false, elementCopyAtIndex4.WoundedNumber);
				}
			}
			return troopRoster2;
		};
		Dictionary<FormationClass, Tuple<int, int>> dictionary = new Dictionary<FormationClass, Tuple<int, int>>();
		for (int num = 0; num < troopRoster.Count; num++)
		{
			TroopRosterElement elementCopyAtIndex2 = troopRoster.GetElementCopyAtIndex(num);
			if (dictionary.ContainsKey(elementCopyAtIndex2.Character.DefaultFormationClass))
			{
				Tuple<int, int> tuple = dictionary[elementCopyAtIndex2.Character.DefaultFormationClass];
				dictionary[elementCopyAtIndex2.Character.DefaultFormationClass] = new Tuple<int, int>(tuple.Item1 + elementCopyAtIndex2.Number - elementCopyAtIndex2.WoundedNumber, tuple.Item2 + elementCopyAtIndex2.WoundedNumber);
			}
			else
			{
				dictionary.Add(elementCopyAtIndex2.Character.DefaultFormationClass, new Tuple<int, int>(elementCopyAtIndex2.Number - elementCopyAtIndex2.WoundedNumber, elementCopyAtIndex2.WoundedNumber));
			}
		}
		foreach (KeyValuePair<FormationClass, Tuple<int, int>> item3 in dictionary.OrderBy((KeyValuePair<FormationClass, Tuple<int, int>> x) => x.Key))
		{
			TextObject textObject = new TextObject("{=Dqydb21E} {PARTY_SIZE}");
			textObject.SetTextVariable("PARTY_SIZE", PartyBaseHelper.GetPartySizeText(item3.Value.Item1, item3.Value.Item2, isInspected: true));
			TextObject textObject2 = GameTexts.FindText("str_troop_type_name", item3.Key.GetName());
			list.Add(new TooltipProperty(textObject2.ToString(), textObject.ToString(), 0));
		}
		list.Add(new TooltipProperty(string.Empty, string.Empty, -1, onlyShowWhenExtended: true));
		list.Add(new TooltipProperty(GameTexts.FindText("str_troop_types").ToString(), " ", 0, onlyShowWhenExtended: true));
		list.Add(new TooltipProperty("", "", 0, onlyShowWhenExtended: true, TooltipProperty.TooltipPropertyFlags.DefaultSeperator));
		for (int num2 = 0; num2 < troopRoster.Count; num2++)
		{
			TroopRosterElement elementCopyAtIndex3 = troopRoster.GetElementCopyAtIndex(num2);
			if (!elementCopyAtIndex3.Character.IsHero)
			{
				continue;
			}
			CharacterObject hero = elementCopyAtIndex3.Character;
			list.Add(new TooltipProperty(elementCopyAtIndex3.Character.Name.ToString(), delegate
			{
				TroopRoster troopRoster2 = ((getTempRoster != null) ? getTempRoster() : troopRoster);
				int num4 = troopRoster2.FindIndexOfTroop(hero);
				if (num4 == -1)
				{
					return string.Empty;
				}
				TroopRosterElement elementCopyAtIndex4 = troopRoster2.GetElementCopyAtIndex(num4);
				TextObject textObject3 = GameTexts.FindText("str_NUMBER_percent");
				textObject3.SetTextVariable("NUMBER", elementCopyAtIndex4.Character.HeroObject.HitPoints * 100 / elementCopyAtIndex4.Character.MaxHitPoints());
				return textObject3.ToString();
			}, 0, onlyShowWhenExtended: true));
		}
		for (int num3 = 0; num3 < troopRoster.Count; num3++)
		{
			int index = num3;
			CharacterObject character = troopRoster.GetElementCopyAtIndex(index).Character;
			if (character.IsHero)
			{
				continue;
			}
			list.Add(new TooltipProperty(character.Name.ToString(), delegate
			{
				TroopRoster troopRoster2 = ((getTempRoster != null) ? getTempRoster() : troopRoster);
				int num4 = troopRoster2.FindIndexOfTroop(character);
				if (num4 != -1)
				{
					if (num4 > troopRoster2.Count)
					{
						return string.Empty;
					}
					TroopRosterElement elementCopyAtIndex4 = troopRoster2.GetElementCopyAtIndex(num4);
					if (elementCopyAtIndex4.Character == null)
					{
						return string.Empty;
					}
					CharacterObject character2 = elementCopyAtIndex4.Character;
					if (character2 != null && !character2.IsHero)
					{
						TextObject textObject3 = new TextObject("{=!}{PARTY_SIZE}");
						textObject3.SetTextVariable("PARTY_SIZE", PartyBaseHelper.GetPartySizeText(elementCopyAtIndex4.Number - elementCopyAtIndex4.WoundedNumber, elementCopyAtIndex4.WoundedNumber, isInspected: true));
						return textObject3.ToString();
					}
				}
				return string.Empty;
			}, 0, onlyShowWhenExtended: true));
		}
		return list;
	}

	private List<TooltipProperty> GetEncounterSideShipsTooltip(BattleSideEnum side)
	{
		Dictionary<ShipHull.ShipType, int> dictionary = new Dictionary<ShipHull.ShipType, int>();
		Dictionary<ShipHull, int> dictionary2 = new Dictionary<ShipHull, int>();
		int num = 0;
		List<TooltipProperty> list = new List<TooltipProperty>();
		foreach (GameMenuPartyItemVM item in (side == BattleSideEnum.Attacker) ? AttackerPartyList : DefenderPartyList)
		{
			if (item.Party.MobileParty.Ships.Count <= 0)
			{
				continue;
			}
			num += item.Party.MobileParty.Ships.Count;
			for (int i = 0; i < item.Party.MobileParty.Ships.Count; i++)
			{
				Ship ship = item.Party.MobileParty.Ships[i];
				ShipHull shipHull = ship.ShipHull;
				ShipHull.ShipType type = ship.ShipHull.Type;
				if (dictionary.ContainsKey(type))
				{
					dictionary[type]++;
				}
				else
				{
					dictionary[type] = 1;
				}
				if (dictionary2.ContainsKey(shipHull))
				{
					dictionary2[shipHull]++;
				}
				else
				{
					dictionary2[shipHull] = 1;
				}
			}
		}
		list.Add(new TooltipProperty("", "", 0, onlyShowWhenExtended: false, TooltipProperty.TooltipPropertyFlags.RundownSeperator));
		foreach (KeyValuePair<ShipHull.ShipType, int> item2 in dictionary.OrderBy((KeyValuePair<ShipHull.ShipType, int> x) => x.Key))
		{
			TextObject textObject = new TextObject("{=Dqydb21E} {PARTY_SIZE}");
			textObject.SetTextVariable("PARTY_SIZE", item2.Value);
			TextObject textObject2 = GameTexts.FindText("str_ship_type", item2.Key.ToString().ToLower());
			list.Add(new TooltipProperty(textObject2.ToString(), textObject.ToString(), 0));
		}
		list.Add(new TooltipProperty(string.Empty, string.Empty, -1, onlyShowWhenExtended: true));
		list.Add(new TooltipProperty(GameTexts.FindText("str_ship_types").ToString(), " ", 0, onlyShowWhenExtended: true));
		list.Add(new TooltipProperty("", "", 0, onlyShowWhenExtended: true, TooltipProperty.TooltipPropertyFlags.DefaultSeperator));
		foreach (KeyValuePair<ShipHull, int> item3 in dictionary2)
		{
			ShipHull key = item3.Key;
			int value = item3.Value;
			list.Add(new TooltipProperty(key.Name.ToString(), value.ToString(), 0, onlyShowWhenExtended: true));
		}
		return list;
	}

	private List<TooltipProperty> GetEncounterSideMoraleTooltip(BattleSideEnum side)
	{
		List<TooltipProperty> list = new List<TooltipProperty>();
		list.Add(new TooltipProperty(new TextObject("{=QBB0KQ2Z}Parties' Average Morale").ToString(), "", 0, onlyShowWhenExtended: false, TooltipProperty.TooltipPropertyFlags.Title));
		MBBindingList<GameMenuPartyItemVM> mBBindingList = ((side == BattleSideEnum.Attacker) ? AttackerPartyList : DefenderPartyList);
		double num = 0.0;
		foreach (GameMenuPartyItemVM item in mBBindingList)
		{
			list.Add(new TooltipProperty(item.Party.MobileParty.Name.ToString(), item.Party.MobileParty.Morale.ToString("0.0"), 0));
			num += (double)item.Party.MobileParty.Morale;
		}
		list.Add(new TooltipProperty("", string.Empty, 0, onlyShowWhenExtended: false, TooltipProperty.TooltipPropertyFlags.RundownSeperator));
		list.Add(new TooltipProperty(new TextObject("{=eoVW9z54}Average Morale").ToString(), (num / (double)mBBindingList.Count).ToString("0.0"), 0));
		return list;
	}
}
