using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer.KillFeed;

public class MultiplayerPersonalKillFeedWidget : Widget
{
	private int _speedUpWidgetLimit => 2;

	public MultiplayerPersonalKillFeedWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		for (int i = 0; i < base.ChildCount; i++)
		{
			Widget child = GetChild(i);
			child.PositionYOffset = Mathf.Lerp(child.PositionYOffset, GetVerticalPositionOfChildByIndex(i), 0.35f);
		}
	}

	protected override void OnChildAdded(Widget child)
	{
		base.OnChildAdded(child);
		UpdateSpeedModifiers();
		UpdateMaxTargetAlphas();
	}

	private void UpdateMaxTargetAlphas()
	{
		for (int num = base.ChildCount - 1; num >= 0; num--)
		{
			MultiplayerPersonalKillFeedItemWidget multiplayerPersonalKillFeedItemWidget = GetChild(num) as MultiplayerPersonalKillFeedItemWidget;
			if (num <= base.ChildCount - 1 && num >= base.ChildCount - 4)
			{
				multiplayerPersonalKillFeedItemWidget.SetMaxAlphaValue(1f);
			}
			else if (num == base.ChildCount - 5)
			{
				multiplayerPersonalKillFeedItemWidget.SetMaxAlphaValue(0.7f);
			}
			else if (num == base.ChildCount - 6)
			{
				multiplayerPersonalKillFeedItemWidget.SetMaxAlphaValue(0.4f);
			}
			else if (num == base.ChildCount - 7)
			{
				multiplayerPersonalKillFeedItemWidget.SetMaxAlphaValue(0.15f);
			}
			else
			{
				multiplayerPersonalKillFeedItemWidget.SetMaxAlphaValue(0f);
			}
		}
	}

	private float GetVerticalPositionOfChildByIndex(int indexOfChild)
	{
		float num = 0f;
		for (int num2 = base.ChildCount - 1; num2 > indexOfChild; num2--)
		{
			num += GetChild(num2).Size.Y * base._inverseScaleToUse;
		}
		return num;
	}

	private void UpdateSpeedModifiers()
	{
		if (base.ChildCount > _speedUpWidgetLimit)
		{
			float speedModifier = (float)(base.ChildCount - _speedUpWidgetLimit) / 2f + 1f;
			for (int i = 0; i < base.ChildCount - _speedUpWidgetLimit; i++)
			{
				(GetChild(i) as MultiplayerPersonalKillFeedItemWidget)?.SetSpeedModifier(speedModifier);
			}
		}
	}
}
