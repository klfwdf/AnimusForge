using System.Numerics;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.TwoDimension;

namespace NavalDLC.GauntletUI.Widgets.Widgets;

public class PortPieceTooltipGradientWidget : BrushWidget
{
	private bool _isBottomHalf;

	private Widget _containerWidget;

	[Editor(false)]
	public bool IsBottomHalf
	{
		get
		{
			return _isBottomHalf;
		}
		set
		{
			if (value != _isBottomHalf)
			{
				_isBottomHalf = value;
				OnPropertyChanged(value, "IsBottomHalf");
			}
		}
	}

	[Editor(false)]
	public Widget ContainerWidget
	{
		get
		{
			return _containerWidget;
		}
		set
		{
			if (value != _containerWidget)
			{
				_containerWidget = value;
				OnPropertyChanged(value, "ContainerWidget");
			}
		}
	}

	public PortPieceTooltipGradientWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnRender(TwoDimensionContext twoDimensionContext, TwoDimensionDrawContext drawContext)
	{
		if (ContainerWidget != null)
		{
			Vector2 vector = ContainerWidget.Size * base._inverseScaleToUse;
			base.BrushRenderer.Render(drawContext, in AreaRect, base._scaleToUse, base.Context.ContextAlpha, new Vector2(0f, IsBottomHalf ? (0f - vector.Y) : 0f), new Vector2(vector.X + base.Brush.DefaultLayer.ExtendLeft, IsBottomHalf ? (0f - vector.Y) : vector.Y));
		}
	}
}
