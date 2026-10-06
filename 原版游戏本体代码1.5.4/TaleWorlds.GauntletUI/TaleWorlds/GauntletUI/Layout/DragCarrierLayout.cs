using System.Numerics;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI.Layout;

public class DragCarrierLayout : ILayout
{
	Vector2 ILayout.MeasureChildren(Widget widget, Vector2 measureSpec, SpriteData spriteData, float renderScale)
	{
		Widget child = widget.GetChild(0);
		child.Measure(measureSpec);
		return child.MeasuredSize;
	}

	void ILayout.OnLayout(Widget widget, float left, float bottom, float right, float top)
	{
		float left2 = 0f;
		float top2 = 0f;
		float right2 = right - left;
		float bottom2 = bottom - top;
		widget.GetChild(0).Layout(left2, bottom2, right2, top2);
	}
}
