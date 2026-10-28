using TaleWorlds.Library;

namespace TaleWorlds.TwoDimension;

public interface ITwoDimensionResourceContext
{
	Texture LoadTexture(ResourceDepot resourceDepot, string name);
}
