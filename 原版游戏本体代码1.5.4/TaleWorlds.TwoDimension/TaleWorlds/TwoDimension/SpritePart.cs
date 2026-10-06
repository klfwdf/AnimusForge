using TaleWorlds.Library;

namespace TaleWorlds.TwoDimension;

public class SpritePart
{
	private SpriteCategory _category;

	public string Name { get; private set; }

	public int Width { get; private set; }

	public int Height { get; private set; }

	public int SheetID { get; set; }

	public int SheetX { get; set; }

	public int SheetY { get; set; }

	public float MinU { get; private set; }

	public float MinV { get; private set; }

	public float MaxU { get; private set; }

	public float MaxV { get; private set; }

	public int SheetWidth { get; private set; }

	public int SheetHeight { get; private set; }

	public Texture Texture
	{
		get
		{
			SpriteCategory category = _category;
			if (category != null && category.IsLoaded && _category.SpriteSheets?.Count >= SheetID)
			{
				return _category.SpriteSheets[SheetID - 1];
			}
			return null;
		}
	}

	public SpriteCategory Category
	{
		get
		{
			return _category;
		}
		internal set
		{
			_category = value;
		}
	}

	public SpritePart(string name, SpriteCategory category, int width, int height)
	{
		Name = name;
		Width = width;
		Height = height;
		_category = category;
		_category.SpriteParts.Add(this);
	}

	public void UpdateInitValues()
	{
		Vec2i vec2i = _category.SheetSizes[SheetID - 1];
		SheetWidth = vec2i.X;
		SheetHeight = vec2i.Y;
		double num = 1.0 / (double)SheetWidth;
		double num2 = 1.0 / (double)SheetHeight;
		double num3 = (double)SheetX * num;
		double num4 = (double)(SheetX + Width) * num;
		double num5 = (double)SheetY * num2;
		double num6 = (double)(SheetY + Height) * num2;
		MinU = (float)num3;
		MaxU = (float)num4;
		MinV = (float)num5;
		MaxV = (float)num6;
	}
}
