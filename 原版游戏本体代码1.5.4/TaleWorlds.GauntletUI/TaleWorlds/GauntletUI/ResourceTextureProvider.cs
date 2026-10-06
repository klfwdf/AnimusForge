using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI;

public class ResourceTextureProvider : TextureProvider
{
	protected override Texture OnGetTextureForRender(TwoDimensionContext twoDimensionContext, string name)
	{
		return twoDimensionContext.LoadTexture(name);
	}
}
