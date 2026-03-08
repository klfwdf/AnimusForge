using System;
using System.Numerics;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI.Layout
{
	// Token: 0x02000044 RID: 68
	public interface ILayout
	{
		// Token: 0x06000439 RID: 1081
		Vector2 MeasureChildren(Widget widget, Vector2 measureSpec, SpriteData spriteData, float renderScale);

		// Token: 0x0600043A RID: 1082
		void OnLayout(Widget widget, float left, float bottom, float right, float top);
	}
}
