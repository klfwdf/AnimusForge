using System.Numerics;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI.Layout;

public class TextLayout : ILayout
{
	private ILayout _defaultLayout;

	private IText _text;

	public TextLayout(IText text)
	{
		_defaultLayout = new DefaultLayout();
		_text = text;
	}

	Vector2 ILayout.MeasureChildren(Widget widget, Vector2 measureSpec, SpriteData spriteData, float renderScale)
	{
		Vector2 result = _defaultLayout.MeasureChildren(widget, measureSpec, spriteData, renderScale);
		bool fixedWidth = widget.WidthSizePolicy != SizePolicy.CoverChildren || widget.MaxWidth != 0f;
		bool fixedHeight = widget.HeightSizePolicy != SizePolicy.CoverChildren || widget.MaxHeight != 0f;
		float x = measureSpec.X;
		float y = measureSpec.Y;
		Vector2 preferredSize = _text.GetPreferredSize(fixedWidth, x, fixedHeight, y, spriteData, renderScale);
		if (result.X < preferredSize.X)
		{
			result.X = preferredSize.X;
		}
		if (result.Y < preferredSize.Y)
		{
			result.Y = preferredSize.Y;
		}
		return result;
	}

	void ILayout.OnLayout(Widget widget, float left, float bottom, float right, float top)
	{
		_defaultLayout.OnLayout(widget, left, bottom, right, top);
	}
}
