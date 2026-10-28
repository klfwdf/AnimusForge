using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI.BaseTypes;

internal class SpriteFromTexture : Sprite
{
	private Texture _texture;

	public override Texture Texture => _texture;

	public override Vec2 GetMinUvs()
	{
		return Vec2.Zero;
	}

	public override Vec2 GetMaxUvs()
	{
		return Vec2.One;
	}

	public SpriteFromTexture(Texture texture, int width, int height)
		: base("Sprite", width, height, SpriteNinePatchParameters.Empty)
	{
		_texture = texture;
	}
}
