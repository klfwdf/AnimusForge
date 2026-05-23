using System;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.MapNotificationTypes;
using TaleWorlds.CampaignSystem.Party;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes
{
	// Token: 0x0200003F RID: 63
	public class ArmyCreationNotificationItemVM : MapNotificationItemBaseVM
	{
		// Token: 0x170001A6 RID: 422
		// (get) Token: 0x060005DD RID: 1501 RVA: 0x0001ECB3 File Offset: 0x0001CEB3
		public Army Army { get; }

		// Token: 0x060005DE RID: 1502 RVA: 0x0001ECBC File Offset: 0x0001CEBC
		public ArmyCreationNotificationItemVM(ArmyCreationMapNotification data)
			: base(data)
		{
			this.Army = data.CreatedArmy;
			base.NotificationIdentifier = "armycreation";
			this._onInspect = delegate()
			{
				Army army = this.Army;
				CampaignVec2? campaignVec;
				if (army == null)
				{
					campaignVec = null;
				}
				else
				{
					MobileParty leaderParty = army.LeaderParty;
					campaignVec = ((leaderParty != null) ? new CampaignVec2?(leaderParty.Position) : null);
				}
				base.GoToMapPosition(campaignVec ?? MobileParty.MainParty.Position);
			};
			CampaignEvents.OnPartyJoinedArmyEvent.AddNonSerializedListener(this, new Action<MobileParty>(this.OnPartyJoinedArmy));
			CampaignEvents.ArmyDispersed.AddNonSerializedListener(this, new Action<Army, Army.ArmyDispersionReason, bool>(this.OnArmyDispersed));
			CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, new Action<Clan, Kingdom, Kingdom, ChangeKingdomAction.ChangeKingdomActionDetail, bool>(this.OnClanChangedKingdom));
		}

		// Token: 0x060005DF RID: 1503 RVA: 0x0001ED3E File Offset: 0x0001CF3E
		private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification = true)
		{
			if (clan == MobileParty.MainParty.ActualClan && oldKingdom != newKingdom)
			{
				base.ExecuteRemove();
			}
		}

		// Token: 0x060005E0 RID: 1504 RVA: 0x0001ED57 File Offset: 0x0001CF57
		private void OnArmyDispersed(Army arg1, Army.ArmyDispersionReason arg2, bool isPlayersArmy)
		{
			if (arg1 == this.Army)
			{
				base.ExecuteRemove();
			}
		}

		// Token: 0x060005E1 RID: 1505 RVA: 0x0001ED68 File Offset: 0x0001CF68
		private void OnPartyJoinedArmy(MobileParty party)
		{
			if (party == MobileParty.MainParty && party.Army == this.Army)
			{
				base.ExecuteRemove();
			}
		}

		// Token: 0x060005E2 RID: 1506 RVA: 0x0001ED86 File Offset: 0x0001CF86
		public override void OnFinalize()
		{
			base.OnFinalize();
			CampaignEvents.OnPartyJoinedArmyEvent.ClearListeners(this);
			CampaignEvents.ArmyDispersed.ClearListeners(this);
			CampaignEvents.OnClanChangedKingdomEvent.ClearListeners(this);
		}
	}
}
