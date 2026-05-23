using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer.KillFeed;

public class MultiplayerGeneralKillFeedWidget : Widget
{
	private float _normalWidgetHeight;

	private int _speedUpWidgetLimit = 10;

	public float VerticalPaddingAmount { get; set; } = 3f;

	public MultiplayerGeneralKillFeedWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		if (_normalWidgetHeight <= 0f && base.ChildCount > 1)
		{
			_normalWidgetHeight = GetChild(0).ScaledSuggestedHeight * base._inverseScaleToUse;
		}
		for (int i = 0; i < base.ChildCount; i++)
		{
			Widget child = GetChild(i);
			child.PositionYOffset = Mathf.Lerp(child.PositionYOffset, GetVerticalPositionOfChildByIndex(i, base.ChildCount), 0.35f);
		}
	}

	protected override void OnChildAdded(Widget child)
	{
		base.OnChildAdded(child);
		child.PositionYOffset = GetVerticalPositionOfChildByIndex(child.GetSiblingIndex(), base.ChildCount);
		UpdateSpeedModifiers();
	}

	private float GetVerticalPositionOfChildByIndex(int indexOfChild, int numOfTotalChild)
	{
		int num = numOfTotalChild - 1 - indexOfChild;
		return (_normalWidgetHeight + VerticalPaddingAmount) * (float)num;
	}

	private void UpdateSpeedModifiers()
	{
		if (base.ChildCount > _speedUpWidgetLimit)
		{
			float speedModifier = (float)(base.ChildCount - _speedUpWidgetLimit) / 20f + 1f;
			for (int i = 0; i < base.ChildCount - _speedUpWidgetLimit; i++)
			{
				(GetChild(i) as MultiplayerGeneralKillFeedItemWidget).SetSpeedModifier(speedModifier);
			}
		}
	}
}
