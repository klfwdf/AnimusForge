using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Map.Siege;

public class MapSiegeConstructionControllerWidget : Widget
{
	private MapSiegePOIBrushWidget _currentWidget;

	public MapSiegeConstructionControllerWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		float num;
		if (_currentWidget != null)
		{
			base.PositionXOffset = MathF.Clamp(_currentWidget.PositionXOffset + _currentWidget.Size.X * base._inverseScaleToUse, 0f, base.EventManager.PageSize.X - base.Size.X);
			base.PositionYOffset = MathF.Clamp(_currentWidget.PositionYOffset, 175f, base.EventManager.PageSize.Y - base.Size.Y - 70f);
			num = _currentWidget.ReadOnlyBrush.GlobalAlphaFactor;
		}
		else
		{
			base.PositionXOffset = -1000f;
			base.PositionYOffset = -1000f;
			num = 0f;
		}
		base.IsEnabled = num >= 0.95f;
		this.SetGlobalAlphaRecursively(num);
	}

	public void SetCurrentPOIWidget(MapSiegePOIBrushWidget widget)
	{
		if (widget == null || widget == _currentWidget)
		{
			_currentWidget = null;
		}
		else
		{
			_currentWidget = (widget.IsPlayerSidePOI ? widget : null);
		}
	}
}
