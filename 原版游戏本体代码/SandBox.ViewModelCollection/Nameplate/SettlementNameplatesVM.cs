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

namespace SandBox.ViewModelCollection.Nameplate
{
	// Token: 0x02000020 RID: 32
	public class SettlementNameplatesVM : ViewModel
	{
		// Token: 0x170000EF RID: 239
		// (get) Token: 0x060002E4 RID: 740 RVA: 0x0000C603 File Offset: 0x0000A803
		public MBReadOnlyList<SettlementNameplateVM> AllNameplates
		{
			get
			{
				return this._allNameplates;
			}
		}

		// Token: 0x060002E5 RID: 741 RVA: 0x0000C60C File Offset: 0x0000A80C
		public SettlementNameplatesVM(Camera mapCamera, Action<CampaignVec2> fastMoveCameraToPosition)
		{
			this._allNameplates = new MBList<SettlementNameplateVM>(400);
			this._allNameplatesBySettlements = new Dictionary<Settlement, SettlementNameplateVM>(400);
			this.SmallNameplates = new MBBindingList<SettlementNameplateVM>();
			this.MediumNameplates = new MBBindingList<SettlementNameplateVM>();
			this.LargeNameplates = new MBBindingList<SettlementNameplateVM>();
			this._mapCamera = mapCamera;
			this._fastMoveCameraToPosition = fastMoveCameraToPosition;
			CampaignEvents.PartyVisibilityChangedEvent.AddNonSerializedListener(this, new Action<PartyBase>(this.OnPartyBaseVisibilityChange));
			CampaignEvents.WarDeclared.AddNonSerializedListener(this, new Action<IFaction, IFaction, DeclareWarAction.DeclareWarDetail>(this.OnWarDeclared));
			CampaignEvents.MakePeace.AddNonSerializedListener(this, new Action<IFaction, IFaction, MakePeaceAction.MakePeaceDetail>(this.OnPeaceDeclared));
			CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, new Action<Clan, Kingdom, Kingdom, ChangeKingdomAction.ChangeKingdomActionDetail, bool>(this.OnClanChangeKingdom));
			CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, new Action<Settlement, bool, Hero, Hero, Hero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail>(this.OnSettlementOwnerChanged));
			CampaignEvents.OnSiegeEventStartedEvent.AddNonSerializedListener(this, new Action<SiegeEvent>(this.OnSiegeEventStartedOnSettlement));
			CampaignEvents.OnSiegeEventEndedEvent.AddNonSerializedListener(this, new Action<SiegeEvent>(this.OnSiegeEventEndedOnSettlement));
			CampaignEvents.RebelliousClanDisbandedAtSettlement.AddNonSerializedListener(this, new Action<Settlement, Clan>(this.OnRebelliousClanDisbandedAtSettlement));
			this.UpdateNameplateAuxMTPredicate = new TWParallel.ParallelForAuxPredicate(this.UpdateNameplateAuxMT);
		}

		// Token: 0x060002E6 RID: 742 RVA: 0x0000C738 File Offset: 0x0000A938
		public override void OnFinalize()
		{
			base.OnFinalize();
			CampaignEventDispatcher.Instance.RemoveListeners(this);
			for (int i = 0; i < this._allNameplates.Count; i++)
			{
				this._allNameplates[i].OnFinalize();
			}
			this._allNameplates.Clear();
			this._allNameplatesBySettlements.Clear();
			this.SmallNameplates.Clear();
			this.MediumNameplates.Clear();
			this.LargeNameplates.Clear();
		}

		// Token: 0x060002E7 RID: 743 RVA: 0x0000C7B4 File Offset: 0x0000A9B4
		public override void RefreshValues()
		{
			base.RefreshValues();
			for (int i = 0; i < this._allNameplates.Count; i++)
			{
				this._allNameplates[i].RefreshValues();
			}
		}

		// Token: 0x060002E8 RID: 744 RVA: 0x0000C7F0 File Offset: 0x0000A9F0
		public void Initialize(IEnumerable<Tuple<Settlement, GameEntity>> settlements)
		{
			this._allRegularSettlements = from x in settlements
				where !x.Item1.IsHideout && !(x.Item1.SettlementComponent is RetirementSettlementComponent)
				select x;
			this._allHideouts = from x in settlements
				where x.Item1.IsHideout && !(x.Item1.SettlementComponent is RetirementSettlementComponent)
				select x;
			this._allRetreats = from x in settlements
				where !x.Item1.IsHideout && x.Item1.SettlementComponent is RetirementSettlementComponent
				select x;
			foreach (Tuple<Settlement, GameEntity> tuple in this._allRegularSettlements)
			{
				if (tuple.Item1.IsVisible)
				{
					SettlementNameplateVM nameplate = new SettlementNameplateVM(tuple.Item1, tuple.Item2, this._mapCamera, this._fastMoveCameraToPosition);
					this.AddNameplate(nameplate);
				}
			}
			foreach (Tuple<Settlement, GameEntity> tuple2 in this._allHideouts)
			{
				if (tuple2.Item1.Hideout.IsSpotted)
				{
					SettlementNameplateVM nameplate2 = new SettlementNameplateVM(tuple2.Item1, tuple2.Item2, this._mapCamera, this._fastMoveCameraToPosition);
					this.AddNameplate(nameplate2);
				}
			}
			foreach (Tuple<Settlement, GameEntity> tuple3 in this._allRetreats)
			{
				RetirementSettlementComponent retirementSettlementComponent;
				if ((retirementSettlementComponent = tuple3.Item1.SettlementComponent as RetirementSettlementComponent) != null)
				{
					if (retirementSettlementComponent.IsSpotted)
					{
						SettlementNameplateVM nameplate3 = new SettlementNameplateVM(tuple3.Item1, tuple3.Item2, this._mapCamera, this._fastMoveCameraToPosition);
						this.AddNameplate(nameplate3);
					}
				}
				else
				{
					Debug.FailedAssert("A seetlement which is IsRetreat doesn't have a retirement component.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.ViewModelCollection\\Nameplate\\SettlementNameplatesVM.cs", "Initialize", 118);
				}
			}
			for (int i = 0; i < this._allNameplates.Count; i++)
			{
				SettlementNameplateVM settlementNameplateVM = this._allNameplates[i];
				Settlement settlement = settlementNameplateVM.Settlement;
				if (((settlement != null) ? settlement.SiegeEvent : null) != null)
				{
					SettlementNameplateVM settlementNameplateVM2 = settlementNameplateVM;
					Settlement settlement2 = settlementNameplateVM.Settlement;
					settlementNameplateVM2.OnSiegeEventStartedOnSettlement((settlement2 != null) ? settlement2.SiegeEvent : null);
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
			this.RefreshRelationsOfNameplates();
		}

		// Token: 0x060002E9 RID: 745 RVA: 0x0000CA9C File Offset: 0x0000AC9C
		private void AddNameplate(SettlementNameplateVM nameplate)
		{
			this._allNameplates.Add(nameplate);
			this._allNameplatesBySettlements[nameplate.Settlement] = nameplate;
			switch (nameplate.SettlementTypeEnum)
			{
			case SettlementNameplateVM.Type.Village:
				this.SmallNameplates.Add(nameplate);
				return;
			case SettlementNameplateVM.Type.Castle:
				this.MediumNameplates.Add(nameplate);
				return;
			case SettlementNameplateVM.Type.Town:
				this.LargeNameplates.Add(nameplate);
				return;
			default:
				return;
			}
		}

		// Token: 0x060002EA RID: 746 RVA: 0x0000CB08 File Offset: 0x0000AD08
		private void RemoveNameplate(SettlementNameplateVM nameplate)
		{
			this._allNameplates.Remove(nameplate);
			this._allNameplatesBySettlements.Remove(nameplate.Settlement);
			this.SmallNameplates.Remove(nameplate);
			this.MediumNameplates.Remove(nameplate);
			this.LargeNameplates.Remove(nameplate);
		}

		// Token: 0x060002EB RID: 747 RVA: 0x0000CB5C File Offset: 0x0000AD5C
		private void UpdateNameplateAuxMT(int startInclusive, int endExclusive)
		{
			for (int i = startInclusive; i < endExclusive; i++)
			{
				this._allNameplates[i].UpdateNameplateMT(this._cachedCameraPosition);
			}
		}

		// Token: 0x060002EC RID: 748 RVA: 0x0000CB8C File Offset: 0x0000AD8C
		public void Update()
		{
			this._cachedCameraPosition = this._mapCamera.Position;
			TWParallel.For(0, this._allNameplates.Count, this.UpdateNameplateAuxMTPredicate, 16);
			for (int i = 0; i < this._allNameplates.Count; i++)
			{
				this._allNameplates[i].RefreshBindValues();
			}
		}

		// Token: 0x060002ED RID: 749 RVA: 0x0000CBEC File Offset: 0x0000ADEC
		private void OnSiegeEventStartedOnSettlement(SiegeEvent siegeEvent)
		{
			SettlementNameplateVM settlementNameplateVM;
			if (this._allNameplatesBySettlements.TryGetValue(siegeEvent.BesiegedSettlement, out settlementNameplateVM))
			{
				settlementNameplateVM.OnSiegeEventStartedOnSettlement(siegeEvent);
			}
		}

		// Token: 0x060002EE RID: 750 RVA: 0x0000CC18 File Offset: 0x0000AE18
		private void OnSiegeEventEndedOnSettlement(SiegeEvent siegeEvent)
		{
			SettlementNameplateVM settlementNameplateVM;
			if (this._allNameplatesBySettlements.TryGetValue(siegeEvent.BesiegedSettlement, out settlementNameplateVM))
			{
				settlementNameplateVM.OnSiegeEventEndedOnSettlement(siegeEvent);
			}
		}

		// Token: 0x060002EF RID: 751 RVA: 0x0000CC44 File Offset: 0x0000AE44
		private void OnMapEventStartedOnSettlement(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)
		{
			SettlementNameplateVM settlementNameplateVM;
			if (this._allNameplatesBySettlements.TryGetValue(mapEvent.MapEventSettlement, out settlementNameplateVM))
			{
				settlementNameplateVM.OnMapEventStartedOnSettlement(mapEvent);
			}
		}

		// Token: 0x060002F0 RID: 752 RVA: 0x0000CC70 File Offset: 0x0000AE70
		private void OnMapEventEndedOnSettlement(MapEvent mapEvent)
		{
			SettlementNameplateVM settlementNameplateVM;
			if (this._allNameplatesBySettlements.TryGetValue(mapEvent.MapEventSettlement, out settlementNameplateVM))
			{
				settlementNameplateVM.OnMapEventEndedOnSettlement();
			}
		}

		// Token: 0x060002F1 RID: 753 RVA: 0x0000CC98 File Offset: 0x0000AE98
		private void OnPartyBaseVisibilityChange(PartyBase party)
		{
			if (party.IsSettlement)
			{
				Tuple<Settlement, GameEntity> tuple;
				if (party.Settlement.IsHideout)
				{
					tuple = this._allHideouts.SingleOrDefault((Tuple<Settlement, GameEntity> h) => h.Item1.Hideout == party.Settlement.Hideout);
				}
				else if (party.Settlement.SettlementComponent is RetirementSettlementComponent)
				{
					tuple = this._allRetreats.SingleOrDefault((Tuple<Settlement, GameEntity> h) => h.Item1.SettlementComponent as RetirementSettlementComponent == party.Settlement.SettlementComponent as RetirementSettlementComponent);
				}
				else
				{
					tuple = this._allRegularSettlements.SingleOrDefault((Tuple<Settlement, GameEntity> h) => h.Item1 == party.Settlement);
				}
				if (tuple != null)
				{
					SettlementNameplateVM settlementNameplateVM = null;
					if (tuple.Item1 != null)
					{
						this._allNameplatesBySettlements.TryGetValue(tuple.Item1, out settlementNameplateVM);
					}
					if (party.IsVisible && settlementNameplateVM == null)
					{
						SettlementNameplateVM settlementNameplateVM2 = new SettlementNameplateVM(tuple.Item1, tuple.Item2, this._mapCamera, this._fastMoveCameraToPosition);
						this.AddNameplate(settlementNameplateVM2);
						settlementNameplateVM2.RefreshRelationStatus();
						return;
					}
					if (!party.IsVisible && settlementNameplateVM != null)
					{
						this.RemoveNameplate(settlementNameplateVM);
					}
				}
			}
		}

		// Token: 0x060002F2 RID: 754 RVA: 0x0000CDAD File Offset: 0x0000AFAD
		private void OnPeaceDeclared(IFaction faction1, IFaction faction2, MakePeaceAction.MakePeaceDetail detail)
		{
			this.OnPeaceOrWarDeclared(faction1, faction2);
		}

		// Token: 0x060002F3 RID: 755 RVA: 0x0000CDB7 File Offset: 0x0000AFB7
		private void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail arg3)
		{
			this.OnPeaceOrWarDeclared(faction1, faction2);
		}

		// Token: 0x060002F4 RID: 756 RVA: 0x0000CDC1 File Offset: 0x0000AFC1
		private void OnPeaceOrWarDeclared(IFaction faction1, IFaction faction2)
		{
			if (faction1 == Hero.MainHero.MapFaction || faction1 == Hero.MainHero.Clan || faction2 == Hero.MainHero.MapFaction || faction2 == Hero.MainHero.Clan)
			{
				this.RefreshRelationsOfNameplates();
			}
		}

		// Token: 0x060002F5 RID: 757 RVA: 0x0000CDFD File Offset: 0x0000AFFD
		private void OnClanChangeKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification)
		{
			this.RefreshRelationsOfNameplates();
		}

		// Token: 0x060002F6 RID: 758 RVA: 0x0000CE08 File Offset: 0x0000B008
		private void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero previousOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
		{
			SettlementNameplateVM settlementNameplateVM = null;
			if (this._allNameplatesBySettlements.TryGetValue(settlement, out settlementNameplateVM))
			{
				settlementNameplateVM.RefreshDynamicProperties(true);
				settlementNameplateVM.RefreshRelationStatus();
				if (detail == ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail.ByRebellion)
				{
					settlementNameplateVM.OnRebelliousClanFormed(newOwner.Clan);
				}
				else if (previousOwner != null && previousOwner.IsRebel)
				{
					settlementNameplateVM.OnRebelliousClanDisbanded(previousOwner.Clan);
				}
			}
			for (int i = 0; i < settlement.BoundVillages.Count; i++)
			{
				Village village = settlement.BoundVillages[i];
				if (this._allNameplatesBySettlements.TryGetValue(village.Settlement, out settlementNameplateVM))
				{
					settlementNameplateVM.RefreshDynamicProperties(true);
					settlementNameplateVM.RefreshRelationStatus();
				}
			}
		}

		// Token: 0x060002F7 RID: 759 RVA: 0x0000CEA8 File Offset: 0x0000B0A8
		public SettlementNameplateVM GetNameplateOfSettlement(Settlement settlement)
		{
			SettlementNameplateVM result;
			if (this._allNameplatesBySettlements.TryGetValue(settlement, out result))
			{
				return result;
			}
			return null;
		}

		// Token: 0x060002F8 RID: 760 RVA: 0x0000CEC8 File Offset: 0x0000B0C8
		public void OnRebelliousClanDisbandedAtSettlement(Settlement settlement, Clan clan)
		{
			SettlementNameplateVM settlementNameplateVM;
			if (this._allNameplatesBySettlements.TryGetValue(settlement, out settlementNameplateVM))
			{
				settlementNameplateVM.OnRebelliousClanDisbanded(clan);
			}
		}

		// Token: 0x060002F9 RID: 761 RVA: 0x0000CEEC File Offset: 0x0000B0EC
		public void RefreshRelationsOfNameplates()
		{
			for (int i = 0; i < this._allNameplates.Count; i++)
			{
				this._allNameplates[i].RefreshRelationStatus();
			}
		}

		// Token: 0x060002FA RID: 762 RVA: 0x0000CF20 File Offset: 0x0000B120
		public void RefreshDynamicPropertiesOfNameplates(bool forceUpdate)
		{
			for (int i = 0; i < this._allNameplates.Count; i++)
			{
				this._allNameplates[i].RefreshDynamicProperties(forceUpdate);
			}
		}

		// Token: 0x170000F0 RID: 240
		// (get) Token: 0x060002FB RID: 763 RVA: 0x0000CF55 File Offset: 0x0000B155
		// (set) Token: 0x060002FC RID: 764 RVA: 0x0000CF5D File Offset: 0x0000B15D
		[DataSourceProperty]
		public MBBindingList<SettlementNameplateVM> SmallNameplates
		{
			get
			{
				return this._smallNameplates;
			}
			set
			{
				if (this._smallNameplates != value)
				{
					this._smallNameplates = value;
					base.OnPropertyChangedWithValue<MBBindingList<SettlementNameplateVM>>(value, "SmallNameplates");
				}
			}
		}

		// Token: 0x170000F1 RID: 241
		// (get) Token: 0x060002FD RID: 765 RVA: 0x0000CF7B File Offset: 0x0000B17B
		// (set) Token: 0x060002FE RID: 766 RVA: 0x0000CF83 File Offset: 0x0000B183
		[DataSourceProperty]
		public MBBindingList<SettlementNameplateVM> MediumNameplates
		{
			get
			{
				return this._mediumNameplates;
			}
			set
			{
				if (this._mediumNameplates != value)
				{
					this._mediumNameplates = value;
					base.OnPropertyChangedWithValue<MBBindingList<SettlementNameplateVM>>(value, "MediumNameplates");
				}
			}
		}

		// Token: 0x170000F2 RID: 242
		// (get) Token: 0x060002FF RID: 767 RVA: 0x0000CFA1 File Offset: 0x0000B1A1
		// (set) Token: 0x06000300 RID: 768 RVA: 0x0000CFA9 File Offset: 0x0000B1A9
		[DataSourceProperty]
		public MBBindingList<SettlementNameplateVM> LargeNameplates
		{
			get
			{
				return this._largeNameplates;
			}
			set
			{
				if (this._largeNameplates != value)
				{
					this._largeNameplates = value;
					base.OnPropertyChangedWithValue<MBBindingList<SettlementNameplateVM>>(value, "LargeNameplates");
				}
			}
		}

		// Token: 0x04000167 RID: 359
		private readonly Camera _mapCamera;

		// Token: 0x04000168 RID: 360
		private Vec3 _cachedCameraPosition;

		// Token: 0x04000169 RID: 361
		private readonly TWParallel.ParallelForAuxPredicate UpdateNameplateAuxMTPredicate;

		// Token: 0x0400016A RID: 362
		private readonly Action<CampaignVec2> _fastMoveCameraToPosition;

		// Token: 0x0400016B RID: 363
		private IEnumerable<Tuple<Settlement, GameEntity>> _allHideouts;

		// Token: 0x0400016C RID: 364
		private IEnumerable<Tuple<Settlement, GameEntity>> _allRetreats;

		// Token: 0x0400016D RID: 365
		private IEnumerable<Tuple<Settlement, GameEntity>> _allRegularSettlements;

		// Token: 0x0400016E RID: 366
		private MBList<SettlementNameplateVM> _allNameplates;

		// Token: 0x0400016F RID: 367
		private Dictionary<Settlement, SettlementNameplateVM> _allNameplatesBySettlements;

		// Token: 0x04000170 RID: 368
		private MBBindingList<SettlementNameplateVM> _smallNameplates;

		// Token: 0x04000171 RID: 369
		private MBBindingList<SettlementNameplateVM> _mediumNameplates;

		// Token: 0x04000172 RID: 370
		private MBBindingList<SettlementNameplateVM> _largeNameplates;
	}
}
