using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Mission.KillFeed.Personal;

public class SingleplayerPersonalKillFeedWidget : Widget
{
	private float _normalWidgetHeight = -1f;

	public SingleplayerPersonalKillFeedWidget(UIContext context)
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
			child.PositionYOffset = Mathf.Lerp(child.PositionYOffset, GetVerticalPositionOfChildByIndex(i), 0.2f);
		}
	}

	protected override void OnChildAdded(Widget child)
	{
		base.OnChildAdded(child);
		child.PositionYOffset = GetVerticalPositionOfChildByIndex(child.GetSiblingIndex());
		UpdateSpeedModifiers();
	}

	private float GetVerticalPositionOfChildByIndex(int indexOfChild)
	{
		return -1f * _normalWidgetHeight * (float)(base.ChildCount - indexOfChild - 1);
	}

	private void UpdateSpeedModifiers()
	{
		for (int i = 0; i < base.ChildCount; i++)
		{
			SingleplayerPersonalKillFeedItemWidget obj = GetChild(i) as SingleplayerPersonalKillFeedItemWidget;
			float speedModifier = MathF.Pow(base.ChildCount - i, 0.33f);
			obj.SetSpeedModifier(speedModifier);
		}
	}
}
