using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace NavalDLC.GauntletUI.Widgets.Widgets;

public class PortUpgradesPanelArrowWidget : Widget
{
	private Widget _targetSlot;

	private float _currentLerpSpeed = -1f;

	public PortUpgradesPanelArrowWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (_targetSlot != null)
		{
			UpdateAnimation(dt);
		}
	}

	private void UpdateAnimation(float dt)
	{
		base.VerticalAlignment = VerticalAlignment.Top;
		float y = _targetSlot.AreaRect.GetCenter().Y;
		float y2 = AreaRect.GetCenter().Y;
		float num = y * base._inverseScaleToUse - y2 * base._inverseScaleToUse;
		if (_currentLerpSpeed > 0f)
		{
			float num2 = MathF.Lerp(0f, num, _currentLerpSpeed * dt);
			if (MathF.Abs(num - num2) < 1f)
			{
				_currentLerpSpeed = -1f;
			}
			else
			{
				_currentLerpSpeed += 10f * dt;
			}
			num = num2;
		}
		base.PositionYOffset += num;
	}

	public void SetTargetSlot(Widget targetSlot)
	{
		if (_targetSlot != targetSlot)
		{
			_targetSlot = targetSlot;
			_currentLerpSpeed = 10f;
		}
	}
}
