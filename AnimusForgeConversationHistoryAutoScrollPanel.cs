using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace AnimusForge;

public class AnimusForgeConversationHistoryAutoScrollPanel : ScrollablePanel
{
	private const int InitialAutoScrollFrames = 24;

	private int _remainingAutoScrollFrames = InitialAutoScrollFrames;

	public AnimusForgeConversationHistoryAutoScrollPanel(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);

		if (_remainingAutoScrollFrames <= 0 || VerticalScrollbar == null)
		{
			return;
		}

		_remainingAutoScrollFrames--;
		if (VerticalScrollbar.MaxValue > 0f)
		{
			VerticalScrollbar.ValueFloat = VerticalScrollbar.MaxValue;
		}
	}

	protected override void OnMouseScroll()
	{
		_remainingAutoScrollFrames = 0;
		base.OnMouseScroll();
	}

	protected override void OnRightStickMovement()
	{
		_remainingAutoScrollFrames = 0;
		base.OnRightStickMovement();
	}
}
