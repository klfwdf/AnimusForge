using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.Engine.GauntletUI;

public class TwoDimensionEngineResourceContext : ITwoDimensionResourceContext
{
	TaleWorlds.TwoDimension.Texture ITwoDimensionResourceContext.LoadTexture(ResourceDepot resourceDepot, string name)
	{
		Texture fromResource = Texture.GetFromResource(name.Split(new char[1] { '\\' })[^1]);
		if (fromResource == null)
		{
			return null;
		}
		fromResource.SetTextureAsAlwaysValid();
		bool flag = true;
		flag = true;
		fromResource.PreloadTexture(flag);
		return new TaleWorlds.TwoDimension.Texture(new EngineTexture(fromResource));
	}
}
