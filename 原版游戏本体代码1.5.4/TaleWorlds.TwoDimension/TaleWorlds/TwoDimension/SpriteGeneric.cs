using TaleWorlds.Library;

namespace TaleWorlds.TwoDimension;

public class SpriteGeneric : Sprite
{
	public override Texture Texture => SpritePart.Texture;

	public SpritePart SpritePart { get; private set; }

	public override Vec2 GetMinUvs()
	{
		if (SpritePart != null)
		{
			return new Vec2(SpritePart.MinU, SpritePart.MinV);
		}
		return Vec2.Zero;
	}

	public override Vec2 GetMaxUvs()
	{
		if (SpritePart != null)
		{
			return new Vec2(SpritePart.MaxU, SpritePart.MaxV);
		}
		return Vec2.Zero;
	}

	public SpriteGeneric(string name, SpritePart spritePart, in SpriteNinePatchParameters ninePatchParameters)
		: base(name, spritePart.Width, spritePart.Height, ninePatchParameters)
	{
		SpritePart = spritePart;
	}
}
