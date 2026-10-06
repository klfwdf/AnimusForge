using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.MapNotificationTypes;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes;

public class EducationNotificationItemVM : MapNotificationItemBaseVM
{
	private readonly Hero _child;

	private readonly int _age;

	public EducationNotificationItemVM(EducationMapNotification data)
		: base(data)
	{
		base.NotificationIdentifier = "education";
		base.ForceInspection = true;
		_child = data.Child;
		_age = data.Age;
		_onInspect = OnInspect;
		CampaignEvents.ChildEducationCompletedEvent.AddNonSerializedListener(this, OnEducationCompletedForChild);
	}

	private void OnInspect()
	{
		EducationMapNotification educationMapNotification = (EducationMapNotification)base.Data;
		if (educationMapNotification != null && !educationMapNotification.IsValid())
		{
			InformationManager.ShowInquiry(new InquiryData("", new TextObject("{=wGWYNYYX}This education stage is no longer relevant.").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: false, GameTexts.FindText("str_ok").ToString(), "", null, null));
			ExecuteRemove();
		}
		else
		{
			Game.Current.GameStateManager.PushState(Game.Current.GameStateManager.CreateState<EducationState>(new object[1] { _child }));
		}
	}

	private void OnEducationCompletedForChild(Hero child, int age)
	{
		if (child == _child && age >= _age)
		{
			ExecuteRemove();
		}
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		CampaignEvents.ChildEducationCompletedEvent.ClearListeners(this);
	}
}
