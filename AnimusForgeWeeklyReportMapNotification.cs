using System;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace AnimusForge;

internal sealed class AnimusForgeWeeklyReportMapNotification : InformationData
{
	private readonly TextObject _titleText;

	public string EventId { get; }

	public override TextObject TitleText => _titleText;

	public override string SoundEventPath => "event:/ui/notification/kingdom_decision";

	public AnimusForgeWeeklyReportMapNotification(string eventId, string titleText, string descriptionText)
		: base(new TextObject(string.IsNullOrWhiteSpace(descriptionText) ? "点击查看当前周报。" : descriptionText))
	{
		EventId = (eventId ?? "").Trim();
		_titleText = new TextObject(string.IsNullOrWhiteSpace(titleText) ? "周报已生成" : titleText);
	}

	public override bool IsValid()
	{
		return !string.IsNullOrWhiteSpace(EventId);
	}
}

internal sealed class AnimusForgeWeeklyReportMapNotificationItemVM : MapNotificationItemBaseVM
{
	public AnimusForgeWeeklyReportMapNotificationItemVM(AnimusForgeWeeklyReportMapNotification data)
		: base(data)
	{
		NotificationIdentifier = "ransom";
		_onInspect = delegate
		{
			if (MyBehavior.Instance?.OpenWeeklyReportNoticeFromMap(data.EventId) == true)
			{
				ExecuteRemove();
			}
		};
	}
}
