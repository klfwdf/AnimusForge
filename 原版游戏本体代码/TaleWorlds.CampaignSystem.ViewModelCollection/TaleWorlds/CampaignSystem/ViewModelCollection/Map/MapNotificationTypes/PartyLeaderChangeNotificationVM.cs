using System;
using TaleWorlds.CampaignSystem.MapNotificationTypes;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes
{
	// Token: 0x0200004B RID: 75
	public class PartyLeaderChangeNotificationVM : MapNotificationItemBaseVM
	{
		// Token: 0x06000623 RID: 1571 RVA: 0x0001FA50 File Offset: 0x0001DC50
		public PartyLeaderChangeNotificationVM(PartyLeaderChangeNotification data)
			: base(data)
		{
			this._party = data.Party;
			base.NotificationIdentifier = "death";
			this._onInspect = delegate()
			{
				InformationManager.ShowInquiry(new InquiryData(this._decisionPopupTitleText.ToString(), this._partyLeaderChangePopupText.ToString(), true, true, GameTexts.FindText("str_yes", null).ToString(), GameTexts.FindText("str_no", null).ToString(), delegate()
				{
					INavigationHandler navigationHandler = base.NavigationHandler;
					if (navigationHandler == null)
					{
						return;
					}
					navigationHandler.OpenClan(this._party.Party);
				}, null, "", 0f, null, null, null), false, false);
				Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
				this._playerInspectedNotification = true;
				base.ExecuteRemove();
			};
			CampaignEvents.OnPartyLeaderChangeOfferCanceledEvent.AddNonSerializedListener(this, new Action<MobileParty>(this.OnPartyLeaderChangeOfferCanceled));
			CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, new Action<MobileParty, PartyBase>(this.OnMobilePartyDestroyed));
		}

		// Token: 0x06000624 RID: 1572 RVA: 0x0001FADD File Offset: 0x0001DCDD
		private void OnPartyLeaderChangeOfferCanceled(MobileParty party)
		{
			this.CheckAndExecuteRemove(party);
		}

		// Token: 0x06000625 RID: 1573 RVA: 0x0001FAE6 File Offset: 0x0001DCE6
		private void OnMobilePartyDestroyed(MobileParty party, PartyBase destroyerParty)
		{
			this.CheckAndExecuteRemove(party);
		}

		// Token: 0x06000626 RID: 1574 RVA: 0x0001FAF0 File Offset: 0x0001DCF0
		private void CheckAndExecuteRemove(MobileParty party)
		{
			if (Campaign.Current.CampaignInformationManager.InformationDataExists<PartyLeaderChangeNotification>((PartyLeaderChangeNotification x) => x.Party == party))
			{
				base.ExecuteRemove();
			}
		}

		// Token: 0x06000627 RID: 1575 RVA: 0x0001FB2D File Offset: 0x0001DD2D
		public override void OnFinalize()
		{
			base.OnFinalize();
			CampaignEventDispatcher.Instance.RemoveListeners(this);
			if (!this._playerInspectedNotification)
			{
				CampaignEventDispatcher.Instance.OnPartyLeaderChangeOfferCanceled(this._party);
			}
		}

		// Token: 0x0400029A RID: 666
		private bool _playerInspectedNotification;

		// Token: 0x0400029B RID: 667
		private readonly MobileParty _party;

		// Token: 0x0400029C RID: 668
		private TextObject _decisionPopupTitleText = new TextObject("{=nFl0ufe3}A party without a leader", null);

		// Token: 0x0400029D RID: 669
		private TextObject _partyLeaderChangePopupText = new TextObject("{=OMqHwpXF}One of your parties has lost its leader. It will disband after a day has passed. You can assign a new clan member to lead it, if you wish to keep the party.{newline}{newline}Do you want to assign a new leader?", null);
	}
}
