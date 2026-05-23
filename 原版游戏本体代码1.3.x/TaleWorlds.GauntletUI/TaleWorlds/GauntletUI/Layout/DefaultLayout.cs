using System;
using System.Numerics;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI.Layout
{
	// Token: 0x0200003E RID: 62
	public class DefaultLayout : ILayout
	{
		// Token: 0x06000421 RID: 1057 RVA: 0x000103CC File Offset: 0x0000E5CC
		private void ParallelMeasureChildren(Widget widget, Vector2 measureSpec)
		{
			DefaultLayout.<>c__DisplayClass0_0 CS$<>8__locals1 = new DefaultLayout.<>c__DisplayClass0_0();
			CS$<>8__locals1.widget = widget;
			CS$<>8__locals1.measureSpec = measureSpec;
			TWParallel.For(0, CS$<>8__locals1.widget.ChildCount, new TWParallel.ParallelForAuxPredicate(CS$<>8__locals1.<ParallelMeasureChildren>g__UpdateChildWidgetMT|0), 16);
		}

		// Token: 0x06000422 RID: 1058 RVA: 0x0001040C File Offset: 0x0000E60C
		Vector2 ILayout.MeasureChildren(Widget widget, Vector2 measureSpec, SpriteData spriteData, float renderScale)
		{
			Vector2 vector = default(Vector2);
			if (widget.ChildCount > 0)
			{
				if (widget.ChildCount >= 64)
				{
					this.ParallelMeasureChildren(widget, measureSpec);
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
						if (measuredSize.X > vector.X)
						{
							vector.X = measuredSize.X;
						}
						if (measuredSize.Y > vector.Y)
						{
							vector.Y = measuredSize.Y;
						}
					}
				}
			}
			return vector;
		}

		// Token: 0x06000423 RID: 1059 RVA: 0x000104EC File Offset: 0x0000E6EC
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
}
