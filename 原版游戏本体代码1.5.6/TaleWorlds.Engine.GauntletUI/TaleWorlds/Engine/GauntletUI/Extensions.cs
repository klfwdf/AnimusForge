using TaleWorlds.TwoDimension;

namespace TaleWorlds.Engine.GauntletUI;

public static class Extensions
{
	public static void Load(this SpriteCategory category)
	{
		category.Load(UIResourceManager.ResourceContext, UIResourceManager.ResourceDepot);
	}
}
