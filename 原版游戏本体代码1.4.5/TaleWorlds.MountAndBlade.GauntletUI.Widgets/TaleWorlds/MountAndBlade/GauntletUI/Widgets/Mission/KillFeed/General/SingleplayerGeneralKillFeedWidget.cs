using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Mission.KillFeed.General;

public class SingleplayerGeneralKillFeedWidget : Widget
{
	private float _normalWidgetHeight = -1f;

	public float VerticalPaddingAmount { get; set; } = 3f;

	public SingleplayerGeneralKillFeedWidget(UIContext context)
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
			child.PositionYOffset = Mathf.Lerp(child.PositionYOffset, GetVerticalPositionOfChildByIndex(i), 0.35f);
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
		return (_normalWidgetHeight + VerticalPaddingAmount) * (float)(base.ChildCount - indexOfChild - 1);
	}

	private void UpdateSpeedModifiers()
	{
		for (int i = 0; i < base.ChildCount; i++)
		{
			SingleplayerGeneralKillFeedItemWidget obj = GetChild(i) as SingleplayerGeneralKillFeedItemWidget;
			float speedModifier = MathF.Pow(base.ChildCount - i, 0.33f);
			obj.SetSpeedModifier(speedModifier);
		}
	}
}
