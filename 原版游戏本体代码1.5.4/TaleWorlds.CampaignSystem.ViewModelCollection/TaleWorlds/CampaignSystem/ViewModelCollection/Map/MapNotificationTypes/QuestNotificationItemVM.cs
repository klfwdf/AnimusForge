using System;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes;

public class QuestNotificationItemVM : MapNotificationItemBaseVM
{
	private QuestBase _quest;

	private IssueBase _issue;

	private Action<QuestBase> _onQuestNotificationInspect;

	private Action<IssueBase> _onIssueNotificationInspect;

	protected Action _onInspectAction;

	public QuestNotificationItemVM(QuestBase quest, InformationData data, Action<QuestBase> onQuestNotificationInspect, Action<MapNotificationItemBaseVM> onRemove)
		: base(data)
	{
		_quest = quest;
		_onQuestNotificationInspect = onQuestNotificationInspect;
		_onInspect = (_onInspectAction = delegate
		{
			_onQuestNotificationInspect(_quest);
		});
		base.NotificationIdentifier = "quest";
	}

	public QuestNotificationItemVM(IssueBase issue, InformationData data, Action<IssueBase> onIssueNotificationInspect, Action<MapNotificationItemBaseVM> onRemove)
		: base(data)
	{
		_issue = issue;
		_onIssueNotificationInspect = onIssueNotificationInspect;
		_onInspect = (_onInspectAction = delegate
		{
			_onIssueNotificationInspect(_issue);
		});
		base.NotificationIdentifier = "quest";
	}

	public override void ManualRefreshRelevantStatus()
	{
		base.ManualRefreshRelevantStatus();
	}
}
