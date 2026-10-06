using System.Numerics;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI.Layout;

public class DefaultLayout : ILayout
{
	private void ParallelMeasureChildren(Widget widget, Vector2 measureSpec)
	{
		TWParallel.ForWithoutRenderThread(0, widget.ChildCount, UpdateChildWidgetMT);
		void UpdateChildWidgetMT(int startInclusive, int endExclusive)
		{
			for (int i = startInclusive; i < endExclusive; i++)
			{
				Widget child = widget.GetChild(i);
				if (child != null && child.IsVisible)
				{
					child.Measure(measureSpec);
				}
			}
		}
	}

	Vector2 ILayout.MeasureChildren(Widget widget, Vector2 measureSpec, SpriteData spriteData, float renderScale)
	{
		Vector2 result = default(Vector2);
		if (widget.ChildCount > 0)
		{
			if (widget.ChildCount >= 64)
			{
				ParallelMeasureChildren(widget, measureSpec);
			}
			for (int i = 0; i < widget.ChildCount; i++)
			{
				Widget child = widget.GetChild(i);
				if (child != null && child.IsVisible)
				{
					if (widget.ChildCount < 64)
					{
						child.Measure(measureSpec);
					}
					Vector2 measuredSize = child.MeasuredSize;
					measuredSize.X += child.ScaledMarginLeft + child.ScaledMarginRight;
					measuredSize.Y += child.ScaledMarginTop + child.ScaledMarginBottom;
					if (measuredSize.X > result.X)
					{
						result.X = measuredSize.X;
					}
					if (measuredSize.Y > result.Y)
					{
						result.Y = measuredSize.Y;
					}
				}
			}
		}
		return result;
	}

	void ILayout.OnLayout(Widget widget, float left, float bottom, float right, float top)
	{
		float left2 = 0f;
		float top2 = 0f;
		float right2 = right - left;
		float bottom2 = bottom - top;
		for (int i = 0; i < widget.ChildCount; i++)
		{
			Widget child = widget.GetChild(i);
			if (child != null && child.IsVisible)
			{
				child.Layout(left2, bottom2, right2, top2);
			}
		}
	}
}
