using System.Collections.Generic;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Mission.DamageFeed;

public class MissionAgentDamageFeedWidget : Widget
{
	private int _speedUpWidgetLimit = 1;

	private readonly Queue<MissionAgentDamageFeedItemWidget> _feedItemQueue;

	private MissionAgentDamageFeedItemWidget _activeFeedItem;

	public MissionAgentDamageFeedWidget(UIContext context)
		: base(context)
	{
		_feedItemQueue = new Queue<MissionAgentDamageFeedItemWidget>();
	}

	protected override void OnChildAdded(Widget child)
	{
		base.OnChildAdded(child);
		MissionAgentDamageFeedItemWidget item = (MissionAgentDamageFeedItemWidget)child;
		_feedItemQueue.Enqueue(item);
		UpdateSpeedModifiers();
	}

	protected override void OnBeforeChildRemoved(Widget child)
	{
		_activeFeedItem = null;
		base.OnBeforeChildRemoved(child);
	}

	protected override void OnUpdate(float dt)
	{
		if (_activeFeedItem == null && _feedItemQueue.Count > 0)
		{
			MissionAgentDamageFeedItemWidget activeFeedItem = _feedItemQueue.Dequeue();
			_activeFeedItem = activeFeedItem;
			_activeFeedItem.ShowFeed();
		}
	}

	private void UpdateSpeedModifiers()
	{
		if (base.ChildCount > _speedUpWidgetLimit)
		{
			float speedModifier = (float)(base.ChildCount - _speedUpWidgetLimit) / 3f + 1f;
			for (int i = 0; i < base.ChildCount - _speedUpWidgetLimit; i++)
			{
				((MissionAgentDamageFeedItemWidget)GetChild(i)).SetSpeedModifier(speedModifier);
			}
		}
	}
}
