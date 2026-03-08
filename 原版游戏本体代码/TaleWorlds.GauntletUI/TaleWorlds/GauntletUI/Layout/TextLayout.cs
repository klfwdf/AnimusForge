using System;
using System.Numerics;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI.Layout
{
	// Token: 0x02000048 RID: 72
	public class TextLayout : ILayout
	{
		// Token: 0x0600044C RID: 1100 RVA: 0x00011AE9 File Offset: 0x0000FCE9
		public TextLayout(IText text)
		{
			this._defaultLayout = new DefaultLayout();
			this._text = text;
		}

		// Token: 0x0600044D RID: 1101 RVA: 0x00011B04 File Offset: 0x0000FD04
		Vector2 ILayout.MeasureChildren(Widget widget, Vector2 measureSpec, SpriteData spriteData, float renderScale)
		{
			Vector2 vector = this._defaultLayout.MeasureChildren(widget, measureSpec, spriteData, renderScale);
			bool fixedWidth = widget.WidthSizePolicy != SizePolicy.CoverChildren || widget.MaxWidth != 0f;
			bool fixedHeight = widget.HeightSizePolicy != SizePolicy.CoverChildren || widget.MaxHeight != 0f;
			float x = measureSpec.X;
			float y = measureSpec.Y;
			Vector2 preferredSize = this._text.GetPreferredSize(fixedWidth, x, fixedHeight, y, spriteData, renderScale);
			if (vector.X < preferredSize.X)
			{
				vector.X = preferredSize.X;
			}
			if (vector.Y < preferredSize.Y)
			{
				vector.Y = preferredSize.Y;
			}
			return vector;
		}

		// Token: 0x0600044E RID: 1102 RVA: 0x00011BBB File Offset: 0x0000FDBB
		void ILayout.OnLayout(Widget widget, float left, float bottom, float right, float top)
		{
			this._defaultLayout.OnLayout(widget, left, bottom, right, top);
		}

		// Token: 0x04000223 RID: 547
		private ILayout _defaultLayout;

		// Token: 0x04000224 RID: 548
		private IText _text;
	}
}
