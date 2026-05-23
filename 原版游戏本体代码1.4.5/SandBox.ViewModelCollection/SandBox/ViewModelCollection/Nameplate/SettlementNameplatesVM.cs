using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace SandBox.ViewModelCollection.Nameplate;

public class SettlementNameplatesVM : ViewModel
{
	private readonly Camera _mapCamera;

	private Vec3 _cachedCameraPosition;

	private readonly TWParallel.ParallelForAuxPredicate UpdateNameplateAuxMTPredicate;

	private readonly Action<CampaignVec2> _fastMoveCameraToPosition;

	private IEnumerable<Tuple<Settlement, GameEntity>> _allHideouts;

	private IEnumerable<Tuple<Settlement, GameEntity>> _allRetreats;

	private IEnumerable<Tuple<Settlement, GameEntity>> _allRegularSettlements;

	private MBList<SettlementNameplateVM> _allNameplates;

	private Dictionary<Settlement, SettlementNameplateVM> _allNameplatesBySettlements;

	private MBBindingList<SettlementNameplateVM> _smallNameplates;

	private MBBindingList<SettlementNameplateVM> _mediumNameplates;

	private MBBindingList<SettlementNameplateVM> _largeNameplates;

	public MBReadOnlyList<SettlementNameplateVM> AllNameplates => _allNameplates;

	[DataSourceProperty]
	public MBBindingList<SettlementNameplateVM> SmallNameplates
	{
		get
		{
			return _smallNameplates;
		}
		set
		{
			if (_smallNameplates != value)
			{
				_smallNameplates = value;
				OnPropertyChangedWithValue(value, "SmallNameplates");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<SettlementNameplateVM> MediumNameplates
	{
		get
		{
			return _mediumNameplates;
		}
		set
		{
			if (_mediumNameplates != value)
			{
				_mediumNameplates = value;
				OnPropertyChangedWithValue(value, "MediumNameplates");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<SettlementNameplateVM> LargeNameplates
	{
		get
		{
			return _largeNameplates;
		}
		set
		{
			if (_largeNameplates != value)
			{
				_largeNameplates = value;
				OnPropertyChangedWithValue(value, "LargeNameplates");
			}
		}
	}

	public SettlementNameplatesVM(Camera mapCamera, Action<CampaignVec2> fastMoveCameraToPosition)
	{
		_allNameplates = new MBList<SettlementNameplateVM>(400);
		_allNameplatesBySettlements = new Dictionary<Settlement, SettlementNameplateVM>(400);
		SmallNameplates = new MBBindingList<SettlementNameplateVM>();
		MediumNameplates = new MBBindingList<SettlementNameplateVM>();
		LargeNameplates = new MBBindingList<SettlementNameplateVM>();
		_mapCamera = mapCamera;
		_fastMoveCameraToPosition = fastMoveCameraToPosition;
		CampaignEvents.PartyVisibilityChangedEvent.AddNonSerializedListener(this, OnPartyBaseVisibilityChange);
		CampaignEvents.WarDeclared.AddNonSerializedListener(this, OnWarDeclared);
		CampaignEvents.MakePeace.AddNonSerializedListener(this, OnPeaceDeclared);
		CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanChangeKingdom);
		CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, OnSettlementOwnerChanged);
		CampaignEvents.OnSiegeEventStartedEvent.AddNonSerializedListener(this, OnSiegeEventStartedOnSettlement);
		CampaignEvents.OnSiegeEventEndedEvent.AddNonSerializedListener(this, OnSiegeEventEndedOnSettlement);
		CampaignEvents.RebelliousClanDisbandedAtSettlement.AddNonSerializedListener(this, OnRebelliousClanDisbandedAtSettlement);
		CampaignEvents.OnAllianceStartedEvent.AddNonSerializedListener(this, OnAllianceStarted);
		CampaignEvents.OnAllianceEndedEvent.AddNonSerializedListener(this, OnAllianceEnded);
		UpdateNameplateAuxMTPredicate = UpdateNameplateAuxMT;
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		CampaignEventDispatcher.Instance.RemoveListeners(this);
		for (int i = 0; i < _allNameplates.Count; i++)
		{
			_allNameplates[i].OnFinalize();
		}
		_allNameplates.Clear();
		_allNameplatesBySettlements.Clear();
		SmallNameplates.Clear();
		MediumNameplates.Clear();
		LargeNameplates.Clear();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		for (int i = 0; i < _allNameplates.Count; i++)
		{
			_allNameplates[i].RefreshValues();
		}
	}

	public void Initialize(IEnumerable<Tuple<Settlement, GameEntity>> settlements)
	{
		_allRegularSettlements = settlements.Where((Tuple<Settlement, GameEntity> x) => !x.Item1.IsHideout && !(x.Item1.SettlementComponent is RetirementSettlementComponent));
		_allHideouts = settlements.Where((Tuple<Settlement, GameEntity> x) => x.Item1.IsHideout && !(x.Item1.SettlementComponent is RetirementSettlementComponent));
		_allRetreats = settlements.Where((Tuple<Settlement, GameEntity> x) => !x.Item1.IsHideout && x.Item1.SettlementComponent is RetirementSettlementComponent);
		foreach (Tuple<Settlement, GameEntity> allRegularSettlement in _allRegularSettlements)
		{
			if (allRegularSettlement.Item1.IsVisible)
			{
				SettlementNameplateVM nameplate = new SettlementNameplateVM(allRegularSettlement.Item1, allRegularSettlement.Item2, _mapCamera, _fastMoveCameraToPosition);
				AddNameplate(nameplate);
			}
		}
		foreach (Tuple<Settlement, GameEntity> allHideout in _allHideouts)
		{
			if (allHideout.Item1.Hideout.IsSpotted)
			{
				SettlementNameplateVM nameplate2 = new SettlementNameplateVM(allHideout.Item1, allHideout.Item2, _mapCamera, _fastMoveCameraToPosition);
				AddNameplate(nameplate2);
			}
		}
		foreach (Tuple<Settlement, GameEntity> allRetreat in _allRetreats)
		{
			if (allRetreat.Item1.SettlementComponent is RetirementSettlementComponent retirementSettlementComponent)
			{
				if (retirementSettlementComponent.IsSpotted)
				{
					SettlementNameplateVM nameplate3 = new SettlementNameplateVM(allRetreat.Item1, allRetreat.Item2, _mapCamera, _fastMoveCameraToPosition);
					AddNameplate(nameplate3);
				}
			}
			else
			{
				Debug.FailedAssert("A seetlement which is IsRetreat doesn't have a retirement component.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.ViewModelCollection\\Nameplate\\SettlementNameplatesVM.cs", "Initialize", 120);
			}
		}
		for (int num = 0; num < _allNameplates.Count; num++)
		{
			SettlementNameplateVM settlementNameplateVM = _allNameplates[num];
			if (settlementNameplateVM.Settlement?.SiegeEvent != null)
			{
				settlementNameplateVM.OnSiegeEventStartedOnSettlement(settlementNameplateVM.Settlement?.SiegeEvent);
			}
			else if (settlementNameplateVM.Settlement.IsTown || settlementNameplateVM.Settlement.IsCastle)
			{
				Clan ownerClan = settlementNameplateVM.Settlement.OwnerClan;
				if (ownerClan != null && ownerClan.IsRebelClan)
				{
					settlementNameplateVM.OnRebelliousClanFormed(settlementNameplateVM.Settlement.OwnerClan);
				}
			}
		}
		RefreshRelationsOfNameplates();
	}

	private void AddNameplate(SettlementNameplateVM nameplate)
	{
		_allNameplates.Add(nameplate);
		_allNameplatesBySettlements[nameplate.Settlement] = nameplate;
		switch (nameplate.SettlementTypeEnum)
		{
		case SettlementNameplateVM.Type.Village:
			SmallNameplates.Add(nameplate);
			break;
		case SettlementNameplateVM.Type.Castle:
			MediumNameplates.Add(nameplate);
			break;
		case SettlementNameplateVM.Type.Town:
			LargeNameplates.Add(nameplate);
			break;
		}
	}

	private void RemoveNameplate(SettlementNameplateVM nameplate)
	{
		_allNameplates.Remove(nameplate);
		_allNameplatesBySettlements.Remove(nameplate.Settlement);
		SmallNameplates.Remove(nameplate);
		MediumNameplates.Remove(nameplate);
		LargeNameplates.Remove(nameplate);
	}

	private void UpdateNameplateAuxMT(int startInclusive, int endExclusive)
	{
		for (int i = startInclusive; i < endExclusive; i++)
		{
			_allNameplates[i].UpdateNameplateMT(_cachedCameraPosition);
		}
	}

	public void Update()
	{
		_cachedCameraPosition = _mapCamera.Position;
		TWParallel.For(0, _allNameplates.Count, UpdateNameplateAuxMTPredicate);
		for (int i = 0; i < _allNameplates.Count; i++)
		{
			_allNameplates[i].RefreshBindValues();
		}
	}

	private void OnSiegeEventStartedOnSettlement(SiegeEvent siegeEvent)
	{
		if (_allNameplatesBySettlements.TryGetValue(siegeEvent.BesiegedSettlement, out var value))
		{
			value.OnSiegeEventStartedOnSettlement(siegeEvent);
		}
	}

	private void OnSiegeEventEndedOnSettlement(SiegeEvent siegeEvent)
	{
		if (_allNameplatesBySettlements.TryGetValue(siegeEvent.BesiegedSettlement, out var value))
		{
			value.OnSiegeEventEndedOnSettlement(siegeEvent);
		}
	}

	private void OnMapEventStartedOnSettlement(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)
	{
		if (_allNameplatesBySettlements.TryGetValue(mapEvent.MapEventSettlement, out var value))
		{
			value.OnMapEventStartedOnSettlement(mapEvent);
		}
	}

	private void OnMapEventEndedOnSettlement(MapEvent mapEvent)
	{
		if (_allNameplatesBySettlements.TryGetValue(mapEvent.MapEventSettlement, out var value))
		{
			value.OnMapEventEndedOnSettlement();
		}
	}

	private void OnPartyBaseVisibilityChange(PartyBase party)
	{
		if (!party.IsSettlement)
		{
			return;
		}
		Tuple<Settlement, GameEntity> tuple = null;
		tuple = (party.Settlement.IsHideout ? _allHideouts.SingleOrDefault((Tuple<Settlement, GameEntity> h) => h.Item1.Hideout == party.Settlement.Hideout) : ((!(party.Settlement.SettlementComponent is RetirementSettlementComponent)) ? _allRegularSettlements.SingleOrDefault((Tuple<Settlement, GameEntity> h) => h.Item1 == party.Settlement) : _allRetreats.SingleOrDefault((Tuple<Settlement, GameEntity> h) => h.Item1.SettlementComponent as RetirementSettlementComponent == party.Settlement.SettlementComponent as RetirementSettlementComponent)));
		if (tuple != null)
		{
			SettlementNameplateVM value = null;
			if (tuple.Item1 != null)
			{
				_allNameplatesBySettlements.TryGetValue(tuple.Item1, out value);
			}
			if (party.IsVisible && value == null)
			{
				SettlementNameplateVM settlementNameplateVM = new SettlementNameplateVM(tuple.Item1, tuple.Item2, _mapCamera, _fastMoveCameraToPosition);
				AddNameplate(settlementNameplateVM);
				settlementNameplateVM.RefreshRelationStatus();
			}
			else if (!party.IsVisible && value != null)
			{
				RemoveNameplate(value);
			}
		}
	}

	private void OnPeaceDeclared(IFaction faction1, IFaction faction2, MakePeaceAction.MakePeaceDetail detail)
	{
		OnPeaceOrWarDeclared(faction1, faction2);
	}

	private void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail arg3)
	{
		OnPeaceOrWarDeclared(faction1, faction2);
	}

	private void OnPeaceOrWarDeclared(IFaction faction1, IFaction faction2)
	{
		if (faction1 == Hero.MainHero.MapFaction || faction1 == Hero.MainHero.Clan || faction2 == Hero.MainHero.MapFaction || faction2 == Hero.MainHero.Clan)
		{
			RefreshRelationsOfNameplates();
		}
	}

	private void OnClanChangeKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification)
	{
		RefreshRelationsOfNameplates();
	}

	private void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero previousOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
	{
		SettlementNameplateVM value = null;
		if (_allNameplatesBySettlements.TryGetValue(settlement, out value))
		{
			value.RefreshDynamicProperties(forceUpdate: true);
			value.RefreshRelationStatus();
			if (detail == ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail.ByRebellion)
			{
				value.OnRebelliousClanFormed(newOwner.Clan);
			}
			else if (previousOwner != null && previousOwner.IsRebel)
			{
				value.OnRebelliousClanDisbanded(previousOwner.Clan);
			}
		}
		for (int i = 0; i < settlement.BoundVillages.Count; i++)
		{
			Village village = settlement.BoundVillages[i];
			if (_allNameplatesBySettlements.TryGetValue(village.Settlement, out value))
			{
				value.RefreshDynamicProperties(forceUpdate: true);
				value.RefreshRelationStatus();
			}
		}
	}

	private void OnAllianceEnded(Kingdom kingdom1, Kingdom kingdom2)
	{
		OnAllianceStateChanged(kingdom1, kingdom2);
	}

	private void OnAllianceStarted(Kingdom kingdom1, Kingdom kingdom2)
	{
		OnAllianceStateChanged(kingdom1, kingdom2);
	}

	private void OnAllianceStateChanged(Kingdom kingdom1, Kingdom kingdom2)
	{
		if (kingdom1 == Hero.MainHero.MapFaction || kingdom2 == Hero.MainHero.MapFaction)
		{
			RefreshRelationsOfNameplates();
		}
	}

	public SettlementNameplateVM GetNameplateOfSettlement(Settlement settlement)
	{
		if (_allNameplatesBySettlements.TryGetValue(settlement, out var value))
		{
			return value;
		}
		return null;
	}

	public void OnRebelliousClanDisbandedAtSettlement(Settlement settlement, Clan clan)
	{
		if (_allNameplatesBySettlements.TryGetValue(settlement, out var value))
		{
			value.OnRebelliousClanDisbanded(clan);
		}
	}

	public void RefreshRelationsOfNameplates()
	{
		for (int i = 0; i < _allNameplates.Count; i++)
		{
			_allNameplates[i].RefreshRelationStatus();
		}
	}

	public void RefreshDynamicPropertiesOfNameplates(bool forceUpdate)
	{
		for (int i = 0; i < _allNameplates.Count; i++)
		{
			_allNameplates[i].RefreshDynamicProperties(forceUpdate);
		}
	}
}
