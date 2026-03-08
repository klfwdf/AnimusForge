using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.MapNotificationTypes;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x02000443 RID: 1091
	public class TeleportationCampaignBehavior : CampaignBehaviorBase, ITeleportationCampaignBehavior, ICampaignBehavior
	{
		// Token: 0x17000E2B RID: 3627
		// (get) Token: 0x06004598 RID: 17816 RVA: 0x00159D85 File Offset: 0x00157F85
		private TextObject _partyLeaderChangeNotificationText
		{
			get
			{
				return new TextObject("{=QSaufZ9i}One of your parties has lost its leader. It will disband after a day has passed. You can assign a new clan member to lead it, if you wish to keep the party.", null);
			}
		}

		// Token: 0x06004599 RID: 17817 RVA: 0x00159D94 File Offset: 0x00157F94
		public override void RegisterEvents()
		{
			CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, new Action(this.HourlyTick));
			CampaignEvents.DailyTickPartyEvent.AddNonSerializedListener(this, new Action<MobileParty>(this.DailyTickParty));
			CampaignEvents.HeroComesOfAgeEvent.AddNonSerializedListener(this, new Action<Hero>(this.OnHeroComesOfAge));
			CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, new Action<MobileParty, PartyBase>(this.OnMobilePartyDestroyed));
			CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, new Action<Settlement, bool, Hero, Hero, Hero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail>(this.OnSettlementOwnerChanged));
			CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, new Action<Hero, Hero, KillCharacterAction.KillCharacterActionDetail, bool>(this.OnHeroKilled));
			CampaignEvents.OnPartyDisbandStartedEvent.AddNonSerializedListener(this, new Action<MobileParty>(this.OnPartyDisbandStarted));
			CampaignEvents.OnGovernorChangedEvent.AddNonSerializedListener(this, new Action<Town, Hero, Hero>(this.OnGovernorChanged));
			CampaignEvents.OnHeroTeleportationRequestedEvent.AddNonSerializedListener(this, new Action<Hero, Settlement, MobileParty, TeleportHeroAction.TeleportationDetail>(this.OnHeroTeleportationRequested));
			CampaignEvents.OnPartyDisbandedEvent.AddNonSerializedListener(this, new Action<MobileParty, Settlement>(this.OnPartyDisbanded));
			CampaignEvents.OnClanLeaderChangedEvent.AddNonSerializedListener(this, new Action<Hero, Hero>(this.OnClanLeaderChanged));
			CampaignEvents.HeroPrisonerTaken.AddNonSerializedListener(this, new Action<PartyBase, Hero>(this.HeroPrisonerTaken));
		}

		// Token: 0x0600459A RID: 17818 RVA: 0x00159EB5 File Offset: 0x001580B5
		public override void SyncData(IDataStore dataStore)
		{
			dataStore.SyncData<List<TeleportationCampaignBehavior.TeleportationData>>("_teleportationList", ref this._teleportationList);
		}

		// Token: 0x0600459B RID: 17819 RVA: 0x00159ECC File Offset: 0x001580CC
		public bool GetTargetOfTeleportingHero(Hero teleportingHero, out bool isGovernor, out bool isPartyLeader, out IMapPoint target)
		{
			isGovernor = false;
			isPartyLeader = false;
			target = null;
			for (int i = 0; i < this._teleportationList.Count; i++)
			{
				TeleportationCampaignBehavior.TeleportationData teleportationData = this._teleportationList[i];
				if (teleportationData.TeleportingHero == teleportingHero)
				{
					if (teleportationData.TargetSettlement != null)
					{
						isGovernor = teleportationData.IsGovernor;
						target = teleportationData.TargetSettlement;
						return true;
					}
					if (teleportationData.TargetParty != null)
					{
						isPartyLeader = teleportationData.IsPartyLeader;
						target = teleportationData.TargetParty;
						return true;
					}
				}
			}
			return false;
		}

		// Token: 0x0600459C RID: 17820 RVA: 0x00159F48 File Offset: 0x00158148
		public CampaignTime GetHeroArrivalTimeToDestination(Hero teleportingHero)
		{
			TeleportationCampaignBehavior.TeleportationData teleportationData = this._teleportationList.FirstOrDefaultQ((TeleportationCampaignBehavior.TeleportationData x) => x.TeleportingHero == teleportingHero);
			if (teleportationData != null)
			{
				return teleportationData.TeleportationTime;
			}
			return CampaignTime.Never;
		}

		// Token: 0x0600459D RID: 17821 RVA: 0x00159F8C File Offset: 0x0015818C
		private void HourlyTick()
		{
			for (int i = this._teleportationList.Count - 1; i >= 0; i--)
			{
				TeleportationCampaignBehavior.TeleportationData teleportationData = this._teleportationList[i];
				if (teleportationData.TeleportationTime.IsPast && Campaign.Current.Models.DelayedTeleportationModel.CanPerformImmediateTeleport(teleportationData.TeleportingHero, teleportationData.TargetParty, teleportationData.TargetSettlement))
				{
					TeleportationCampaignBehavior.TeleportationData data = teleportationData;
					this.RemoveTeleportationData(teleportationData, false, true);
					this.ApplyImmediateTeleport(data);
				}
			}
		}

		// Token: 0x0600459E RID: 17822 RVA: 0x0015A008 File Offset: 0x00158208
		private void DailyTickParty(MobileParty mobileParty)
		{
			if (mobileParty.IsActive && mobileParty.Army == null && mobileParty.MapEvent == null && mobileParty.LeaderHero != null && mobileParty.LeaderHero.IsNoncombatant && mobileParty.LeaderHero.DeathMark == KillCharacterAction.KillCharacterActionDetail.None && mobileParty.ActualClan != null && mobileParty.ActualClan != Clan.PlayerClan && mobileParty.ActualClan.Leader != mobileParty.LeaderHero && !mobileParty.IsInRaftState && (!mobileParty.IsCurrentlyAtSea || mobileParty.CurrentSettlement != null))
			{
				MBList<Hero> mblist = mobileParty.ActualClan.Heroes.WhereQ((Hero h) => h.IsActive && h.IsCommander && h.PartyBelongedTo == null).ToMBList<Hero>();
				if (!mblist.IsEmpty<Hero>())
				{
					Hero leaderHero = mobileParty.LeaderHero;
					mobileParty.RemovePartyLeader();
					MakeHeroFugitiveAction.Apply(leaderHero, false);
					TeleportHeroAction.ApplyDelayedTeleportToPartyAsPartyLeader(mblist.GetRandomElementInefficiently<Hero>(), mobileParty);
				}
			}
		}

		// Token: 0x0600459F RID: 17823 RVA: 0x0015A104 File Offset: 0x00158304
		private void OnHeroComesOfAge(Hero hero)
		{
			if (hero.Clan != Clan.PlayerClan && !hero.IsNoncombatant)
			{
				foreach (WarPartyComponent warPartyComponent in hero.Clan.WarPartyComponents)
				{
					MobileParty mobileParty = warPartyComponent.MobileParty;
					if (mobileParty != null && mobileParty.Army == null && mobileParty.MapEvent == null && mobileParty.LeaderHero != null && mobileParty.LeaderHero.IsNoncombatant && (!mobileParty.IsCurrentlyAtSea || mobileParty.CurrentSettlement != null))
					{
						Hero leaderHero = mobileParty.LeaderHero;
						mobileParty.RemovePartyLeader();
						MakeHeroFugitiveAction.Apply(leaderHero, false);
						TeleportHeroAction.ApplyDelayedTeleportToPartyAsPartyLeader(hero, warPartyComponent.Party.MobileParty);
						break;
					}
				}
			}
		}

		// Token: 0x060045A0 RID: 17824 RVA: 0x0015A1D4 File Offset: 0x001583D4
		private void OnMobilePartyDestroyed(MobileParty mobileParty, PartyBase destroyerParty)
		{
			for (int i = this._teleportationList.Count - 1; i >= 0; i--)
			{
				TeleportationCampaignBehavior.TeleportationData teleportationData = this._teleportationList[i];
				if (teleportationData.TargetParty != null && teleportationData.TargetParty == mobileParty)
				{
					this.RemoveTeleportationData(teleportationData, true, false);
				}
			}
		}

		// Token: 0x060045A1 RID: 17825 RVA: 0x0015A220 File Offset: 0x00158420
		private void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
		{
			for (int i = this._teleportationList.Count - 1; i >= 0; i--)
			{
				TeleportationCampaignBehavior.TeleportationData teleportationData = this._teleportationList[i];
				if (teleportationData.TargetSettlement != null && teleportationData.TargetSettlement == settlement && newOwner.Clan != teleportationData.TeleportingHero.Clan)
				{
					this.RemoveTeleportationData(teleportationData, true, true);
				}
			}
		}

		// Token: 0x060045A2 RID: 17826 RVA: 0x0015A280 File Offset: 0x00158480
		private void OnHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification = true)
		{
			for (int i = this._teleportationList.Count - 1; i >= 0; i--)
			{
				TeleportationCampaignBehavior.TeleportationData teleportationData = this._teleportationList[i];
				if (teleportationData.TeleportingHero == victim)
				{
					this.RemoveTeleportationData(teleportationData, true, true);
				}
			}
		}

		// Token: 0x060045A3 RID: 17827 RVA: 0x0015A2C4 File Offset: 0x001584C4
		private void OnPartyDisbandStarted(MobileParty disbandParty)
		{
			if (disbandParty.ActualClan == Clan.PlayerClan && disbandParty.LeaderHero == null && (disbandParty.IsLordParty || disbandParty.IsCaravan) && !disbandParty.IsCurrentlyAtSea)
			{
				Campaign.Current.CampaignInformationManager.NewMapNoticeAdded(new PartyLeaderChangeNotification(disbandParty, this._partyLeaderChangeNotificationText));
			}
			for (int i = this._teleportationList.Count - 1; i >= 0; i--)
			{
				TeleportationCampaignBehavior.TeleportationData teleportationData = this._teleportationList[i];
				if (teleportationData.TargetParty != null && teleportationData.TargetParty == disbandParty)
				{
					this.RemoveTeleportationData(teleportationData, true, false);
				}
			}
		}

		// Token: 0x060045A4 RID: 17828 RVA: 0x0015A358 File Offset: 0x00158558
		private void OnGovernorChanged(Town fortification, Hero oldGovernor, Hero newGovernor)
		{
			for (int i = this._teleportationList.Count - 1; i >= 0; i--)
			{
				TeleportationCampaignBehavior.TeleportationData teleportationData = this._teleportationList[i];
				if (teleportationData.TeleportingHero != newGovernor && teleportationData.IsGovernor && teleportationData.TargetSettlement.Town == fortification)
				{
					teleportationData.IsGovernor = false;
				}
			}
		}

		// Token: 0x060045A5 RID: 17829 RVA: 0x0015A3B0 File Offset: 0x001585B0
		private void OnHeroTeleportationRequested(Hero hero, Settlement targetSettlement, MobileParty targetParty, TeleportHeroAction.TeleportationDetail detail)
		{
			switch (detail)
			{
			case TeleportHeroAction.TeleportationDetail.ImmediateTeleportToSettlement:
				for (int i = this._teleportationList.Count - 1; i >= 0; i--)
				{
					TeleportationCampaignBehavior.TeleportationData teleportationData = this._teleportationList[i];
					if (hero == teleportationData.TeleportingHero && teleportationData.TargetSettlement == targetSettlement)
					{
						this.RemoveTeleportationData(teleportationData, true, false);
					}
				}
				break;
			case TeleportHeroAction.TeleportationDetail.ImmediateTeleportToParty:
			case TeleportHeroAction.TeleportationDetail.ImmediateTeleportToPartyAsPartyLeader:
				break;
			case TeleportHeroAction.TeleportationDetail.DelayedTeleportToSettlement:
			case TeleportHeroAction.TeleportationDetail.DelayedTeleportToSettlementAsGovernor:
				this._teleportationList.Add(new TeleportationCampaignBehavior.TeleportationData(hero, targetSettlement, detail == TeleportHeroAction.TeleportationDetail.DelayedTeleportToSettlementAsGovernor));
				return;
			case TeleportHeroAction.TeleportationDetail.DelayedTeleportToParty:
			case TeleportHeroAction.TeleportationDetail.DelayedTeleportToPartyAsPartyLeader:
				if (detail == TeleportHeroAction.TeleportationDetail.DelayedTeleportToPartyAsPartyLeader)
				{
					for (int j = this._teleportationList.Count - 1; j >= 0; j--)
					{
						TeleportationCampaignBehavior.TeleportationData teleportationData2 = this._teleportationList[j];
						if (teleportationData2.TargetParty == targetParty && teleportationData2.IsPartyLeader)
						{
							this.RemoveTeleportationData(teleportationData2, true, false);
						}
					}
				}
				this._teleportationList.Add(new TeleportationCampaignBehavior.TeleportationData(hero, targetParty, detail == TeleportHeroAction.TeleportationDetail.DelayedTeleportToPartyAsPartyLeader));
				return;
			default:
				return;
			}
		}

		// Token: 0x060045A6 RID: 17830 RVA: 0x0015A498 File Offset: 0x00158698
		private void OnPartyDisbanded(MobileParty disbandParty, Settlement relatedSettlement)
		{
			for (int i = this._teleportationList.Count - 1; i >= 0; i--)
			{
				TeleportationCampaignBehavior.TeleportationData teleportationData = this._teleportationList[i];
				if (teleportationData.TargetParty != null && teleportationData.TargetParty == disbandParty)
				{
					this.RemoveTeleportationData(teleportationData, true, true);
				}
			}
		}

		// Token: 0x060045A7 RID: 17831 RVA: 0x0015A4E4 File Offset: 0x001586E4
		private void OnClanLeaderChanged(Hero oldLeader, Hero newLeader)
		{
			for (int i = this._teleportationList.Count - 1; i >= 0; i--)
			{
				TeleportationCampaignBehavior.TeleportationData teleportationData = this._teleportationList[i];
				if (teleportationData.TeleportingHero == newLeader && !teleportationData.IsPartyLeader)
				{
					this.RemoveTeleportationData(teleportationData, true, true);
				}
			}
		}

		// Token: 0x060045A8 RID: 17832 RVA: 0x0015A530 File Offset: 0x00158730
		private void HeroPrisonerTaken(PartyBase capturer, Hero prisoner)
		{
			for (int i = this._teleportationList.Count - 1; i >= 0; i--)
			{
				TeleportationCampaignBehavior.TeleportationData teleportationData = this._teleportationList[i];
				if (teleportationData.TeleportingHero == prisoner)
				{
					this.RemoveTeleportationData(teleportationData, true, true);
				}
			}
		}

		// Token: 0x060045A9 RID: 17833 RVA: 0x0015A574 File Offset: 0x00158774
		private void RemoveTeleportationData(TeleportationCampaignBehavior.TeleportationData data, bool isCanceled, bool disbandTargetParty = true)
		{
			this._teleportationList.Remove(data);
			if (isCanceled)
			{
				if (data.TeleportingHero.IsTraveling && data.TeleportingHero.DeathMark == KillCharacterAction.KillCharacterActionDetail.None)
				{
					MakeHeroFugitiveAction.Apply(data.TeleportingHero, false);
				}
				if (data.TargetParty != null)
				{
					if (data.TargetParty.ActualClan == Clan.PlayerClan)
					{
						CampaignEventDispatcher.Instance.OnPartyLeaderChangeOfferCanceled(data.TargetParty);
					}
					if (disbandTargetParty && data.TargetParty.IsActive && data.IsPartyLeader)
					{
						IDisbandPartyCampaignBehavior behavior = Campaign.Current.CampaignBehaviorManager.GetBehavior<IDisbandPartyCampaignBehavior>();
						if (behavior != null && !behavior.IsPartyWaitingForDisband(data.TargetParty))
						{
							DisbandPartyAction.StartDisband(data.TargetParty);
						}
					}
				}
			}
		}

		// Token: 0x060045AA RID: 17834 RVA: 0x0015A628 File Offset: 0x00158828
		private void ApplyImmediateTeleport(TeleportationCampaignBehavior.TeleportationData data)
		{
			if (data.TargetSettlement == null)
			{
				if (data.TargetParty != null)
				{
					if (data.IsPartyLeader)
					{
						TeleportHeroAction.ApplyImmediateTeleportToPartyAsPartyLeader(data.TeleportingHero, data.TargetParty);
						return;
					}
					TeleportHeroAction.ApplyImmediateTeleportToParty(data.TeleportingHero, data.TargetParty);
				}
				return;
			}
			if (data.IsGovernor)
			{
				data.TargetSettlement.Town.Governor = data.TeleportingHero;
				TeleportHeroAction.ApplyImmediateTeleportToSettlement(data.TeleportingHero, data.TargetSettlement);
				return;
			}
			TeleportHeroAction.ApplyImmediateTeleportToSettlement(data.TeleportingHero, data.TargetSettlement);
		}

		// Token: 0x0400137E RID: 4990
		private List<TeleportationCampaignBehavior.TeleportationData> _teleportationList = new List<TeleportationCampaignBehavior.TeleportationData>();

		// Token: 0x02000846 RID: 2118
		public class TeleportationCampaignBehaviorTypeDefiner : SaveableTypeDefiner
		{
			// Token: 0x060066F9 RID: 26361 RVA: 0x001C30B8 File Offset: 0x001C12B8
			public TeleportationCampaignBehaviorTypeDefiner()
				: base(151000)
			{
			}

			// Token: 0x060066FA RID: 26362 RVA: 0x001C30C5 File Offset: 0x001C12C5
			protected override void DefineClassTypes()
			{
				base.AddClassDefinition(typeof(TeleportationCampaignBehavior.TeleportationData), 1, null);
			}

			// Token: 0x060066FB RID: 26363 RVA: 0x001C30D9 File Offset: 0x001C12D9
			protected override void DefineContainerDefinitions()
			{
				base.ConstructContainerDefinition(typeof(List<TeleportationCampaignBehavior.TeleportationData>));
			}
		}

		// Token: 0x02000847 RID: 2119
		internal class TeleportationData
		{
			// Token: 0x060066FC RID: 26364 RVA: 0x001C30EC File Offset: 0x001C12EC
			public TeleportationData(Hero teleportingHero, Settlement targetSettlement, bool isGovernor)
			{
				this.TeleportingHero = teleportingHero;
				this.TeleportationTime = CampaignTime.HoursFromNow(Campaign.Current.Models.DelayedTeleportationModel.GetTeleportationDelayAsHours(teleportingHero, targetSettlement.Party).ResultNumber);
				this.TargetSettlement = targetSettlement;
				this.IsGovernor = isGovernor;
				this.TargetParty = null;
				this.IsPartyLeader = false;
			}

			// Token: 0x060066FD RID: 26365 RVA: 0x001C3150 File Offset: 0x001C1350
			public TeleportationData(Hero teleportingHero, MobileParty targetParty, bool isPartyLeader)
			{
				this.TeleportingHero = teleportingHero;
				this.TeleportationTime = CampaignTime.HoursFromNow(Campaign.Current.Models.DelayedTeleportationModel.GetTeleportationDelayAsHours(teleportingHero, targetParty.Party).ResultNumber);
				this.TargetParty = targetParty;
				this.IsPartyLeader = isPartyLeader;
				this.TargetSettlement = null;
				this.IsGovernor = false;
			}

			// Token: 0x060066FE RID: 26366 RVA: 0x001C31B4 File Offset: 0x001C13B4
			internal static void AutoGeneratedStaticCollectObjectsTeleportationData(object o, List<object> collectedObjects)
			{
				((TeleportationCampaignBehavior.TeleportationData)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
			}

			// Token: 0x060066FF RID: 26367 RVA: 0x001C31C2 File Offset: 0x001C13C2
			protected virtual void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
			{
				collectedObjects.Add(this.TeleportingHero);
				CampaignTime.AutoGeneratedStaticCollectObjectsCampaignTime(this.TeleportationTime, collectedObjects);
				collectedObjects.Add(this.TargetSettlement);
				collectedObjects.Add(this.TargetParty);
			}

			// Token: 0x06006700 RID: 26368 RVA: 0x001C31F9 File Offset: 0x001C13F9
			internal static object AutoGeneratedGetMemberValueTeleportingHero(object o)
			{
				return ((TeleportationCampaignBehavior.TeleportationData)o).TeleportingHero;
			}

			// Token: 0x06006701 RID: 26369 RVA: 0x001C3206 File Offset: 0x001C1406
			internal static object AutoGeneratedGetMemberValueTeleportationTime(object o)
			{
				return ((TeleportationCampaignBehavior.TeleportationData)o).TeleportationTime;
			}

			// Token: 0x06006702 RID: 26370 RVA: 0x001C3218 File Offset: 0x001C1418
			internal static object AutoGeneratedGetMemberValueTargetSettlement(object o)
			{
				return ((TeleportationCampaignBehavior.TeleportationData)o).TargetSettlement;
			}

			// Token: 0x06006703 RID: 26371 RVA: 0x001C3225 File Offset: 0x001C1425
			internal static object AutoGeneratedGetMemberValueIsGovernor(object o)
			{
				return ((TeleportationCampaignBehavior.TeleportationData)o).IsGovernor;
			}

			// Token: 0x06006704 RID: 26372 RVA: 0x001C3237 File Offset: 0x001C1437
			internal static object AutoGeneratedGetMemberValueTargetParty(object o)
			{
				return ((TeleportationCampaignBehavior.TeleportationData)o).TargetParty;
			}

			// Token: 0x06006705 RID: 26373 RVA: 0x001C3244 File Offset: 0x001C1444
			internal static object AutoGeneratedGetMemberValueIsPartyLeader(object o)
			{
				return ((TeleportationCampaignBehavior.TeleportationData)o).IsPartyLeader;
			}

			// Token: 0x0400234F RID: 9039
			[SaveableField(1)]
			public Hero TeleportingHero;

			// Token: 0x04002350 RID: 9040
			[SaveableField(2)]
			public CampaignTime TeleportationTime;

			// Token: 0x04002351 RID: 9041
			[SaveableField(3)]
			public Settlement TargetSettlement;

			// Token: 0x04002352 RID: 9042
			[SaveableField(4)]
			public bool IsGovernor;

			// Token: 0x04002353 RID: 9043
			[SaveableField(5)]
			public MobileParty TargetParty;

			// Token: 0x04002354 RID: 9044
			[SaveableField(6)]
			public bool IsPartyLeader;
		}
	}
}
