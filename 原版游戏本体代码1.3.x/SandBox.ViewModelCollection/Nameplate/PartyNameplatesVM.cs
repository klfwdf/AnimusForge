using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace SandBox.ViewModelCollection.Nameplate
{
	// Token: 0x02000019 RID: 25
	public class PartyNameplatesVM : ViewModel
	{
		// Token: 0x06000252 RID: 594 RVA: 0x00009DFC File Offset: 0x00007FFC
		public PartyNameplatesVM(Camera mapCamera, Action resetCamera)
		{
			this.Nameplates = new MBBindingList<PartyNameplateVM>();
			this._visibilityDirtyParties = new List<MobileParty>();
			this._nameplatesByParty = new Dictionary<MobileParty, PartyNameplateVM>();
			this._nameplateComparer = new PartyNameplatesVM.NameplateDistanceComparer();
			this._nameplatePool = new PartyNameplatesVM.NameplatePool();
			this._mapCamera = mapCamera;
			this._resetCamera = resetCamera;
			this._updateNameplatesDelegate = new TWParallel.ParallelForAuxPredicate(this.UpdateNameplatesInRange);
			this.RegisterEvents();
		}

		// Token: 0x06000253 RID: 595 RVA: 0x00009E6C File Offset: 0x0000806C
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.Nameplates.ApplyActionOnAllItems(delegate(PartyNameplateVM x)
			{
				x.RefreshValues();
			});
			PartyPlayerNameplateVM playerNameplate = this.PlayerNameplate;
			if (playerNameplate == null)
			{
				return;
			}
			playerNameplate.RefreshValues();
		}

		// Token: 0x06000254 RID: 596 RVA: 0x00009EBC File Offset: 0x000080BC
		public void Initialize()
		{
			MBReadOnlyList<MobileParty> all = MobileParty.All;
			for (int i = 0; i < all.Count; i++)
			{
				MobileParty mobileParty = all[i];
				if (mobileParty.IsSpotted() && mobileParty.CurrentSettlement == null)
				{
					this.CreateNameplateFor(mobileParty);
				}
			}
		}

		// Token: 0x06000255 RID: 597 RVA: 0x00009F00 File Offset: 0x00008100
		private void CreateNameplateFor(MobileParty party)
		{
			if (party.IsMainParty)
			{
				if (this.PlayerNameplate != null)
				{
					this.PlayerNameplate.Clear();
				}
				else
				{
					this.PlayerNameplate = new PartyPlayerNameplateVM();
				}
				this.PlayerNameplate.InitializeWith(party, this._mapCamera);
				this.PlayerNameplate.InitializePlayerNameplate(this._resetCamera);
				return;
			}
			PartyNameplateVM partyNameplateVM = this._nameplatePool.Get();
			partyNameplateVM.InitializeWith(party, this._mapCamera);
			this.Nameplates.Add(partyNameplateVM);
			this._nameplatesByParty[partyNameplateVM.Party] = partyNameplateVM;
		}

		// Token: 0x06000256 RID: 598 RVA: 0x00009F90 File Offset: 0x00008190
		private void RemoveNameplate(PartyNameplateVM nameplate)
		{
			this.Nameplates.Remove(nameplate);
			this._nameplatesByParty.Remove(nameplate.Party);
			this._nameplatePool.Release(nameplate);
			nameplate.Clear();
		}

		// Token: 0x06000257 RID: 599 RVA: 0x00009FC4 File Offset: 0x000081C4
		private void OnClanChangeKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification)
		{
			for (int i = 0; i < this.Nameplates.Count; i++)
			{
				PartyNameplateVM partyNameplateVM = this.Nameplates[i];
				Hero leaderHero = partyNameplateVM.Party.LeaderHero;
				if (((leaderHero != null) ? leaderHero.Clan : null) == clan)
				{
					partyNameplateVM.RefreshDynamicProperties(true);
				}
			}
			PartyPlayerNameplateVM playerNameplate = this.PlayerNameplate;
			Clan clan2;
			if (playerNameplate == null)
			{
				clan2 = null;
			}
			else
			{
				Hero leaderHero2 = playerNameplate.Party.LeaderHero;
				clan2 = ((leaderHero2 != null) ? leaderHero2.Clan : null);
			}
			if (clan2 == clan)
			{
				this.PlayerNameplate.RefreshDynamicProperties(true);
			}
		}

		// Token: 0x06000258 RID: 600 RVA: 0x0000A048 File Offset: 0x00008248
		private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
		{
			if (party == null)
			{
				return;
			}
			PartyNameplateVM nameplate;
			if (this._nameplatesByParty.TryGetValue(party, out nameplate))
			{
				this.RemoveNameplate(nameplate);
				return;
			}
			PartyPlayerNameplateVM playerNameplate = this.PlayerNameplate;
			if (((playerNameplate != null) ? playerNameplate.Party : null) == party)
			{
				this.PlayerNameplate.Clear();
				this.PlayerNameplate = null;
			}
		}

		// Token: 0x06000259 RID: 601 RVA: 0x0000A098 File Offset: 0x00008298
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
					if (armyParty.IsSpotted() && this.Nameplates.All((PartyNameplateVM p) => p.Party != armyParty))
					{
						this.CreateNameplateFor(armyParty);
					}
				}
				return;
			}
			if (party.IsSpotted() && !this._nameplatesByParty.ContainsKey(party))
			{
				PartyPlayerNameplateVM playerNameplate = this.PlayerNameplate;
				if (((playerNameplate != null) ? playerNameplate.Party : null) != party)
				{
					this.CreateNameplateFor(party);
				}
			}
		}

		// Token: 0x0600025A RID: 602 RVA: 0x0000A15C File Offset: 0x0000835C
		private void OnPartyVisibilityChanged(PartyBase party)
		{
			if (((party != null) ? party.MobileParty : null) == null)
			{
				return;
			}
			MobileParty mobileParty = party.MobileParty;
			this._visibilityDirtyParties.Add(mobileParty);
		}

		// Token: 0x0600025B RID: 603 RVA: 0x0000A18C File Offset: 0x0000838C
		private void UpdateMobilePartyVisibility(MobileParty mobileParty)
		{
			if (mobileParty.IsSpotted() && mobileParty.CurrentSettlement == null && this.Nameplates.All((PartyNameplateVM p) => p.Party != mobileParty))
			{
				this.CreateNameplateFor(mobileParty);
				return;
			}
			if (this.PlayerNameplate != null && this.PlayerNameplate.Party == mobileParty && mobileParty.CurrentSettlement != null)
			{
				this.PlayerNameplate.Clear();
				this.PlayerNameplate = null;
				return;
			}
			PartyNameplateVM nameplate;
			if ((!mobileParty.IsSpotted() || mobileParty.CurrentSettlement != null) && this._nameplatesByParty.TryGetValue(mobileParty, out nameplate))
			{
				this.RemoveNameplate(nameplate);
			}
		}

		// Token: 0x0600025C RID: 604 RVA: 0x0000A258 File Offset: 0x00008458
		public void Update()
		{
			if (this._visibilityDirtyParties.Count > 0)
			{
				for (int i = 0; i < this._visibilityDirtyParties.Count; i++)
				{
					this.UpdateMobilePartyVisibility(this._visibilityDirtyParties[i]);
				}
				this._visibilityDirtyParties.Clear();
			}
			if (this.Nameplates.Count >= 32)
			{
				TWParallel.For(0, this.Nameplates.Count, this._updateNameplatesDelegate, 16);
			}
			else
			{
				this.UpdateNameplatesInRange(0, this.Nameplates.Count);
			}
			for (int j = 0; j < this.Nameplates.Count; j++)
			{
				this.Nameplates[j].RefreshBinding();
			}
			this.Nameplates.Sort(this._nameplateComparer);
			if (this.PlayerNameplate != null)
			{
				this.PlayerNameplate.RefreshPosition();
				this.PlayerNameplate.DetermineIsVisibleOnMap();
				this.PlayerNameplate.RefreshDynamicProperties(false);
				this.PlayerNameplate.RefreshBinding();
			}
		}

		// Token: 0x0600025D RID: 605 RVA: 0x0000A350 File Offset: 0x00008550
		private void UpdateNameplatesInRange(int beginInclusive, int endExclusive)
		{
			for (int i = beginInclusive; i < endExclusive; i++)
			{
				PartyNameplateVM partyNameplateVM = this.Nameplates[i];
				partyNameplateVM.RefreshPosition();
				partyNameplateVM.DetermineIsVisibleOnMap();
				partyNameplateVM.RefreshDynamicProperties(false);
			}
		}

		// Token: 0x0600025E RID: 606 RVA: 0x0000A388 File Offset: 0x00008588
		private void OnPlayerCharacterChangedEvent(Hero oldPlayer, Hero newPlayer, MobileParty newMainParty, bool isMainPartyChanged)
		{
			if (this.PlayerNameplate != null)
			{
				this.PlayerNameplate.Clear();
			}
			else
			{
				this.PlayerNameplate = new PartyPlayerNameplateVM();
			}
			this.PlayerNameplate.InitializeWith(newMainParty, this._mapCamera);
			this.PlayerNameplate.InitializePlayerNameplate(this._resetCamera);
		}

		// Token: 0x0600025F RID: 607 RVA: 0x0000A3D8 File Offset: 0x000085D8
		private void OnMobilePartyDestroyed(MobileParty destroyedParty, PartyBase destroyerParty)
		{
			if (destroyedParty == null)
			{
				return;
			}
			PartyNameplateVM nameplate;
			if (this._nameplatesByParty.TryGetValue(destroyedParty, out nameplate))
			{
				this.RemoveNameplate(nameplate);
				return;
			}
			PartyPlayerNameplateVM playerNameplate = this.PlayerNameplate;
			if (((playerNameplate != null) ? playerNameplate.Party : null) == destroyedParty)
			{
				this.PlayerNameplate.Clear();
				this.PlayerNameplate = null;
			}
		}

		// Token: 0x06000260 RID: 608 RVA: 0x0000A428 File Offset: 0x00008628
		private void OnGameOver()
		{
			if (this.PlayerNameplate != null)
			{
				this.PlayerNameplate.Clear();
				this.PlayerNameplate = null;
			}
		}

		// Token: 0x06000261 RID: 609 RVA: 0x0000A444 File Offset: 0x00008644
		public override void OnFinalize()
		{
			base.OnFinalize();
			this.Nameplates.ApplyActionOnAllItems(delegate(PartyNameplateVM n)
			{
				n.OnFinalize();
			});
			this.Nameplates.Clear();
			if (this.PlayerNameplate != null)
			{
				this.PlayerNameplate.Clear();
				this.PlayerNameplate = null;
			}
			this.UnregisterEvents();
		}

		// Token: 0x06000262 RID: 610 RVA: 0x0000A4AC File Offset: 0x000086AC
		private void RegisterEvents()
		{
			CampaignEvents.SettlementEntered.AddNonSerializedListener(this, new Action<MobileParty, Settlement, Hero>(this.OnSettlementEntered));
			CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, new Action<MobileParty, Settlement>(this.OnSettlementLeft));
			CampaignEvents.PartyVisibilityChangedEvent.AddNonSerializedListener(this, new Action<PartyBase>(this.OnPartyVisibilityChanged));
			CampaignEvents.OnPlayerCharacterChangedEvent.AddNonSerializedListener(this, new Action<Hero, Hero, MobileParty, bool>(this.OnPlayerCharacterChangedEvent));
			CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, new Action<Clan, Kingdom, Kingdom, ChangeKingdomAction.ChangeKingdomActionDetail, bool>(this.OnClanChangeKingdom));
			CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, new Action<MobileParty, PartyBase>(this.OnMobilePartyDestroyed));
			CampaignEvents.OnGameOverEvent.AddNonSerializedListener(this, new Action(this.OnGameOver));
		}

		// Token: 0x06000263 RID: 611 RVA: 0x0000A55A File Offset: 0x0000875A
		private void UnregisterEvents()
		{
			CampaignEventDispatcher.Instance.RemoveListeners(this);
		}

		// Token: 0x170000CC RID: 204
		// (get) Token: 0x06000264 RID: 612 RVA: 0x0000A567 File Offset: 0x00008767
		// (set) Token: 0x06000265 RID: 613 RVA: 0x0000A56F File Offset: 0x0000876F
		[DataSourceProperty]
		public MBBindingList<PartyNameplateVM> Nameplates
		{
			get
			{
				return this._nameplates;
			}
			set
			{
				if (this._nameplates != value)
				{
					this._nameplates = value;
					base.OnPropertyChangedWithValue<MBBindingList<PartyNameplateVM>>(value, "Nameplates");
				}
			}
		}

		// Token: 0x170000CD RID: 205
		// (get) Token: 0x06000266 RID: 614 RVA: 0x0000A58D File Offset: 0x0000878D
		// (set) Token: 0x06000267 RID: 615 RVA: 0x0000A595 File Offset: 0x00008795
		[DataSourceProperty]
		public PartyPlayerNameplateVM PlayerNameplate
		{
			get
			{
				return this._playerNameplate;
			}
			set
			{
				if (this._playerNameplate != value)
				{
					this._playerNameplate = value;
					base.OnPropertyChangedWithValue<PartyPlayerNameplateVM>(value, "PlayerNameplate");
				}
			}
		}

		// Token: 0x0400010C RID: 268
		private readonly Camera _mapCamera;

		// Token: 0x0400010D RID: 269
		private readonly Action _resetCamera;

		// Token: 0x0400010E RID: 270
		private readonly PartyNameplatesVM.NameplateDistanceComparer _nameplateComparer;

		// Token: 0x0400010F RID: 271
		private readonly PartyNameplatesVM.NameplatePool _nameplatePool;

		// Token: 0x04000110 RID: 272
		private readonly TWParallel.ParallelForAuxPredicate _updateNameplatesDelegate;

		// Token: 0x04000111 RID: 273
		private readonly Dictionary<MobileParty, PartyNameplateVM> _nameplatesByParty;

		// Token: 0x04000112 RID: 274
		private readonly List<MobileParty> _visibilityDirtyParties;

		// Token: 0x04000113 RID: 275
		private MBBindingList<PartyNameplateVM> _nameplates;

		// Token: 0x04000114 RID: 276
		private PartyPlayerNameplateVM _playerNameplate;

		// Token: 0x0200007F RID: 127
		private class NameplateDistanceComparer : IComparer<PartyNameplateVM>
		{
			// Token: 0x06000654 RID: 1620 RVA: 0x0001660C File Offset: 0x0001480C
			public int Compare(PartyNameplateVM x, PartyNameplateVM y)
			{
				return y.DistanceToCamera.CompareTo(x.DistanceToCamera);
			}
		}

		// Token: 0x02000080 RID: 128
		private class NameplatePool
		{
			// Token: 0x170001C8 RID: 456
			// (get) Token: 0x06000656 RID: 1622 RVA: 0x00016635 File Offset: 0x00014835
			private int _initialCapacity
			{
				get
				{
					return 64;
				}
			}

			// Token: 0x06000657 RID: 1623 RVA: 0x0001663C File Offset: 0x0001483C
			public NameplatePool()
			{
				this._nameplates = new List<PartyNameplateVM>(this._initialCapacity);
				for (int i = 0; i < this._initialCapacity; i++)
				{
					this._nameplates.Add(new PartyNameplateVM());
				}
			}

			// Token: 0x06000658 RID: 1624 RVA: 0x00016684 File Offset: 0x00014884
			public PartyNameplateVM Get()
			{
				PartyNameplateVM result;
				if (this._nameplates.Count > 0)
				{
					result = this._nameplates[this._nameplates.Count - 1];
					this._nameplates.RemoveAt(this._nameplates.Count - 1);
				}
				else
				{
					result = new PartyNameplateVM();
				}
				return result;
			}

			// Token: 0x06000659 RID: 1625 RVA: 0x000166D9 File Offset: 0x000148D9
			public void Release(PartyNameplateVM nameplate)
			{
				this._nameplates.Add(nameplate);
			}

			// Token: 0x04000367 RID: 871
			private readonly List<PartyNameplateVM> _nameplates;
		}
	}
}
