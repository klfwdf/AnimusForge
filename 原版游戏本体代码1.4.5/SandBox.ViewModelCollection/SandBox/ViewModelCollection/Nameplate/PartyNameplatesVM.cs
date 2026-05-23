using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace SandBox.ViewModelCollection.Nameplate;

public class PartyNameplatesVM : ViewModel
{
	private class NameplateDistanceComparer : IComparer<PartyNameplateVM>
	{
		public int Compare(PartyNameplateVM x, PartyNameplateVM y)
		{
			return y.DistanceToCamera.CompareTo(x.DistanceToCamera);
		}
	}

	private class NameplatePool
	{
		private readonly List<PartyNameplateVM> _nameplates;

		private int _initialCapacity => 64;

		public NameplatePool()
		{
			_nameplates = new List<PartyNameplateVM>(_initialCapacity);
			for (int i = 0; i < _initialCapacity; i++)
			{
				_nameplates.Add(new PartyNameplateVM());
			}
		}

		public PartyNameplateVM Get()
		{
			PartyNameplateVM result;
			if (_nameplates.Count > 0)
			{
				result = _nameplates[_nameplates.Count - 1];
				_nameplates.RemoveAt(_nameplates.Count - 1);
			}
			else
			{
				result = new PartyNameplateVM();
			}
			return result;
		}

		public void Release(PartyNameplateVM nameplate)
		{
			_nameplates.Add(nameplate);
		}
	}

	private readonly Camera _mapCamera;

	private readonly Action _resetCamera;

	private readonly NameplateDistanceComparer _nameplateComparer;

	private readonly NameplatePool _nameplatePool;

	private readonly TWParallel.ParallelForAuxPredicate _updateNameplatesDelegate;

	private readonly Dictionary<MobileParty, PartyNameplateVM> _nameplatesByParty;

	private readonly List<MobileParty> _visibilityDirtyParties;

	private MBBindingList<PartyNameplateVM> _nameplates;

	private PartyPlayerNameplateVM _playerNameplate;

	[DataSourceProperty]
	public MBBindingList<PartyNameplateVM> Nameplates
	{
		get
		{
			return _nameplates;
		}
		set
		{
			if (_nameplates != value)
			{
				_nameplates = value;
				OnPropertyChangedWithValue(value, "Nameplates");
			}
		}
	}

	[DataSourceProperty]
	public PartyPlayerNameplateVM PlayerNameplate
	{
		get
		{
			return _playerNameplate;
		}
		set
		{
			if (_playerNameplate != value)
			{
				_playerNameplate = value;
				OnPropertyChangedWithValue(value, "PlayerNameplate");
			}
		}
	}

	public PartyNameplatesVM(Camera mapCamera, Action resetCamera)
	{
		Nameplates = new MBBindingList<PartyNameplateVM>();
		_visibilityDirtyParties = new List<MobileParty>();
		_nameplatesByParty = new Dictionary<MobileParty, PartyNameplateVM>();
		_nameplateComparer = new NameplateDistanceComparer();
		_nameplatePool = new NameplatePool();
		_mapCamera = mapCamera;
		_resetCamera = resetCamera;
		_updateNameplatesDelegate = UpdateNameplatesInRange;
		RegisterEvents();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Nameplates.ApplyActionOnAllItems(delegate(PartyNameplateVM x)
		{
			x.RefreshValues();
		});
		PlayerNameplate?.RefreshValues();
	}

	public void Initialize()
	{
		MBReadOnlyList<MobileParty> all = MobileParty.All;
		for (int i = 0; i < all.Count; i++)
		{
			MobileParty mobileParty = all[i];
			if (mobileParty.IsSpotted() && mobileParty.CurrentSettlement == null)
			{
				CreateNameplateFor(mobileParty);
			}
		}
	}

	private void CreateNameplateFor(MobileParty party)
	{
		if (party.IsMainParty)
		{
			if (PlayerNameplate != null)
			{
				PlayerNameplate.Clear();
			}
			else
			{
				PlayerNameplate = new PartyPlayerNameplateVM();
			}
			PlayerNameplate.InitializeWith(party, _mapCamera);
			PlayerNameplate.InitializePlayerNameplate(_resetCamera);
		}
		else
		{
			PartyNameplateVM partyNameplateVM = _nameplatePool.Get();
			partyNameplateVM.InitializeWith(party, _mapCamera);
			Nameplates.Add(partyNameplateVM);
			_nameplatesByParty[partyNameplateVM.Party] = partyNameplateVM;
		}
	}

	private void RemoveNameplate(PartyNameplateVM nameplate)
	{
		Nameplates.Remove(nameplate);
		_nameplatesByParty.Remove(nameplate.Party);
		_nameplatePool.Release(nameplate);
		nameplate.Clear();
	}

	private void OnClanChangeKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification)
	{
		for (int i = 0; i < Nameplates.Count; i++)
		{
			PartyNameplateVM partyNameplateVM = Nameplates[i];
			if (partyNameplateVM.Party.LeaderHero?.Clan == clan)
			{
				partyNameplateVM.RefreshDynamicProperties(forceUpdate: true);
			}
		}
		if (PlayerNameplate?.Party.LeaderHero?.Clan == clan)
		{
			PlayerNameplate.RefreshDynamicProperties(forceUpdate: true);
		}
	}

	private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
	{
		if (party != null)
		{
			if (_nameplatesByParty.TryGetValue(party, out var value))
			{
				RemoveNameplate(value);
			}
			else if (PlayerNameplate?.Party == party)
			{
				PlayerNameplate.Clear();
				PlayerNameplate = null;
			}
		}
	}

	private void OnSettlementLeft(MobileParty party, Settlement settlement)
	{
		if (party == null)
		{
			return;
		}
		if (party.Army != null && party.Army.LeaderParty == party)
		{
			for (int i = 0; i < party.Army.Parties.Count; i++)
			{
				MobileParty armyParty = party.Army.Parties[i];
				if (armyParty.IsSpotted() && Nameplates.All((PartyNameplateVM p) => p.Party != armyParty))
				{
					CreateNameplateFor(armyParty);
				}
			}
		}
		else if (party.IsSpotted() && !_nameplatesByParty.ContainsKey(party) && PlayerNameplate?.Party != party)
		{
			CreateNameplateFor(party);
		}
	}

	private void OnPartyVisibilityChanged(PartyBase party)
	{
		if (party?.MobileParty != null)
		{
			MobileParty mobileParty = party.MobileParty;
			_visibilityDirtyParties.Add(mobileParty);
		}
	}

	private void UpdateMobilePartyVisibility(MobileParty mobileParty)
	{
		PartyNameplateVM value;
		if (mobileParty.IsSpotted() && mobileParty.CurrentSettlement == null && Nameplates.All((PartyNameplateVM p) => p.Party != mobileParty))
		{
			CreateNameplateFor(mobileParty);
		}
		else if (PlayerNameplate != null && PlayerNameplate.Party == mobileParty && mobileParty.CurrentSettlement != null)
		{
			PlayerNameplate.Clear();
			PlayerNameplate = null;
		}
		else if ((!mobileParty.IsSpotted() || mobileParty.CurrentSettlement != null) && _nameplatesByParty.TryGetValue(mobileParty, out value))
		{
			RemoveNameplate(value);
		}
	}

	public void Update()
	{
		if (_visibilityDirtyParties.Count > 0)
		{
			for (int i = 0; i < _visibilityDirtyParties.Count; i++)
			{
				UpdateMobilePartyVisibility(_visibilityDirtyParties[i]);
			}
			_visibilityDirtyParties.Clear();
		}
		if (Nameplates.Count >= 32)
		{
			TWParallel.For(0, Nameplates.Count, _updateNameplatesDelegate);
		}
		else
		{
			UpdateNameplatesInRange(0, Nameplates.Count);
		}
		for (int j = 0; j < Nameplates.Count; j++)
		{
			Nameplates[j].RefreshBinding();
		}
		Nameplates.Sort(_nameplateComparer);
		if (PlayerNameplate != null)
		{
			PlayerNameplate.RefreshPosition();
			PlayerNameplate.DetermineIsVisibleOnMap();
			PlayerNameplate.RefreshDynamicProperties(forceUpdate: false);
			PlayerNameplate.RefreshBinding();
		}
	}

	private void UpdateNameplatesInRange(int beginInclusive, int endExclusive)
	{
		for (int i = beginInclusive; i < endExclusive; i++)
		{
			PartyNameplateVM partyNameplateVM = Nameplates[i];
			partyNameplateVM.RefreshPosition();
			partyNameplateVM.DetermineIsVisibleOnMap();
			partyNameplateVM.RefreshDynamicProperties(forceUpdate: false);
		}
	}

	private void OnPlayerCharacterChangedEvent(Hero oldPlayer, Hero newPlayer, MobileParty newMainParty, bool isMainPartyChanged)
	{
		if (PlayerNameplate != null)
		{
			PlayerNameplate.Clear();
		}
		else
		{
			PlayerNameplate = new PartyPlayerNameplateVM();
		}
		PlayerNameplate.InitializeWith(newMainParty, _mapCamera);
		PlayerNameplate.InitializePlayerNameplate(_resetCamera);
	}

	private void OnMobilePartyDestroyed(MobileParty destroyedParty, PartyBase destroyerParty)
	{
		if (destroyedParty != null)
		{
			if (_nameplatesByParty.TryGetValue(destroyedParty, out var value))
			{
				RemoveNameplate(value);
			}
			else if (PlayerNameplate?.Party == destroyedParty)
			{
				PlayerNameplate.Clear();
				PlayerNameplate = null;
			}
		}
	}

	private void OnGameOver()
	{
		if (PlayerNameplate != null)
		{
			PlayerNameplate.Clear();
			PlayerNameplate = null;
		}
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		Nameplates.ApplyActionOnAllItems(delegate(PartyNameplateVM n)
		{
			n.OnFinalize();
		});
		Nameplates.Clear();
		if (PlayerNameplate != null)
		{
			PlayerNameplate.Clear();
			PlayerNameplate = null;
		}
		UnregisterEvents();
	}

	private void RegisterEvents()
	{
		CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
		CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, OnSettlementLeft);
		CampaignEvents.PartyVisibilityChangedEvent.AddNonSerializedListener(this, OnPartyVisibilityChanged);
		CampaignEvents.OnPlayerCharacterChangedEvent.AddNonSerializedListener(this, OnPlayerCharacterChangedEvent);
		CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanChangeKingdom);
		CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, OnMobilePartyDestroyed);
		CampaignEvents.OnGameOverEvent.AddNonSerializedListener(this, OnGameOver);
	}

	private void UnregisterEvents()
	{
		CampaignEventDispatcher.Instance.RemoveListeners(this);
	}
}
