using System.Numerics;

namespace TaleWorlds.TwoDimension;

public class TextPart
{
	internal TextMeshGenerator TextMeshGenerator { get; set; }

	public TextDrawObject DrawObject2D { get; set; }

	public Font DefaultFont { get; set; }

	public float WordWidth { get; set; }

	public Vector2 PartPosition { get; set; }

	internal TextPart()
	{
	}
}
