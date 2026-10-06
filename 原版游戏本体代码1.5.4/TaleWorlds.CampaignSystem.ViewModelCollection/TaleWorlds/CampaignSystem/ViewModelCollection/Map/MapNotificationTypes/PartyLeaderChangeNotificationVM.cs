using TaleWorlds.CampaignSystem.MapNotificationTypes;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes;

public class PartyLeaderChangeNotificationVM : MapNotificationItemBaseVM
{
	private bool _playerInspectedNotification;

	private readonly MobileParty _party;

	private TextObject _decisionPopupTitleText = new TextObject("{=nFl0ufe3}A party without a leader");

	private TextObject _partyLeaderChangePopupText = new TextObject("{=OMqHwpXF}One of your parties has lost its leader. It will disband after a day has passed. You can assign a new clan member to lead it, if you wish to keep the party.{newline}{newline}Do you want to assign a new leader?");

	public PartyLeaderChangeNotificationVM(PartyLeaderChangeNotification data)
		: base(data)
	{
		_party = data.Party;
		base.NotificationIdentifier = "death";
		_onInspect = delegate
		{
			InformationManager.ShowInquiry(new InquiryData(_decisionPopupTitleText.ToString(), _partyLeaderChangePopupText.ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, GameTexts.FindText("str_yes").ToString(), GameTexts.FindText("str_no").ToString(), delegate
			{
				base.NavigationHandler?.OpenClan(_party.Party);
			}, null));
			Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
			_playerInspectedNotification = true;
			ExecuteRemove();
		};
		CampaignEvents.OnPartyLeaderChangeOfferCanceledEvent.AddNonSerializedListener(this, OnPartyLeaderChangeOfferCanceled);
		CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, OnMobilePartyDestroyed);
	}

	private void OnPartyLeaderChangeOfferCanceled(MobileParty party)
	{
		CheckAndExecuteRemove(party);
	}

	private void OnMobilePartyDestroyed(MobileParty party, PartyBase destroyerParty)
	{
		CheckAndExecuteRemove(party);
	}

	private void CheckAndExecuteRemove(MobileParty party)
	{
		if (Campaign.Current.CampaignInformationManager.InformationDataExists((PartyLeaderChangeNotification x) => x.Party == party))
		{
			ExecuteRemove();
		}
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		CampaignEventDispatcher.Instance.RemoveListeners(this);
		if (!_playerInspectedNotification)
		{
			CampaignEventDispatcher.Instance.OnPartyLeaderChangeOfferCanceled(_party);
		}
	}
}
