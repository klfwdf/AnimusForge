using System.Numerics;

namespace TaleWorlds.TwoDimension;

public class RichTextPart
{
	public string Style { get; set; }

	internal TextMeshGenerator TextMeshGenerator { get; set; }

	public ImageDrawObject ImageDrawObject { get; set; }

	public TextDrawObject TextDrawObject { get; set; }

	public Font DefaultFont { get; set; }

	public float WordWidth { get; set; }

	public Vector2 PartPosition { get; set; }

	public Sprite Sprite { get; set; }

	public Vector2 SpritePosition { get; set; }

	public RichTextPartType Type { get; set; }

	public float Extend { get; set; }

	internal RichTextPart()
	{
		Style = "Default";
	}
}
