using TaleWorlds.CampaignSystem.MapNotificationTypes;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes;

public class AlleyLeaderDiedMapNotificationItemVM : MapNotificationItemBaseVM
{
	private Alley _alley;

	public AlleyLeaderDiedMapNotificationItemVM(AlleyLeaderDiedMapNotification data)
		: base(data)
	{
		_alley = data.Alley;
		base.NotificationIdentifier = "alley_leader_died";
		_onInspect = CreateAlleyLeaderDiedPopUp;
	}

	private void CreateAlleyLeaderDiedPopUp()
	{
		TextObject textObject = new TextObject("{=6QoSHiWC}An alley without a leader");
		TextObject textObject2 = new TextObject("{=FzbeSkBb}One of your alleys has lost its leader or is lacking troops. It will be abandoned after {DAYS} days have passed. You can assign a new clan member from the clan screen or travel to the alley to add more troops, if you wish to keep it. Any troops left in the alley will be lost when it is abandoned.");
		textObject2.SetTextVariable("DAYS", (int)Campaign.Current.Models.AlleyModel.DestroyAlleyAfterDaysWhenLeaderIsDeath.ToDays);
		InformationManager.ShowInquiry(new InquiryData(affirmativeText: new TextObject("{=jVLJTuwl}Learn more").ToString(), titleText: textObject.ToString(), text: textObject2.ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, negativeText: GameTexts.FindText("str_dismiss").ToString(), affirmativeAction: OpenClanScreenAfterAlleyLeaderDeath, negativeAction: base.ExecuteRemove));
	}

	private void OpenClanScreenAfterAlleyLeaderDeath()
	{
		if (base.NavigationHandler != null && _alley != null)
		{
			base.NavigationHandler.OpenClan(_alley);
			ExecuteRemove();
		}
	}
}
