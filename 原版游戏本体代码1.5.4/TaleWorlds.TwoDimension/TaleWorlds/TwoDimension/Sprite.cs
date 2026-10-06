using TaleWorlds.Library;

namespace TaleWorlds.TwoDimension;

public abstract class Sprite
{
	public abstract Texture Texture { get; }

	public string Name { get; private set; }

	public int Width { get; private set; }

	public int Height { get; private set; }

	public SpriteNinePatchParameters NinePatchParameters { get; private set; }

	public abstract Vec2 GetMinUvs();

	public abstract Vec2 GetMaxUvs();

	protected Sprite(string name, int width, int height, SpriteNinePatchParameters ninePatchParameters)
	{
		Name = name;
		Width = width;
		Height = height;
		NinePatchParameters = ninePatchParameters;
	}

	public override string ToString()
	{
		if (string.IsNullOrEmpty(Name))
		{
			return base.ToString();
		}
		return Name;
	}
}
