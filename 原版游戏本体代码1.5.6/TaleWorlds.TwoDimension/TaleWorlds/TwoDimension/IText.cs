using System.Numerics;

namespace TaleWorlds.TwoDimension;

public interface IText
{
	string Value { get; set; }

	TextHorizontalAlignment HorizontalAlignment { get; set; }

	TextVerticalAlignment VerticalAlignment { get; set; }

	Vector2 GetPreferredSize(bool fixedWidth, float widthSize, bool fixedHeight, float heightSize, SpriteData spriteData, float renderScale);
}
