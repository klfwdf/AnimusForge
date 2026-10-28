using TaleWorlds.Engine;

namespace TaleWorlds.MountAndBlade;

public class UsableGameObjectGroup : ScriptComponentBehavior, IVisible
{
	public bool IsVisible
	{
		get
		{
			return base.GameEntity.IsVisibleIncludeParents();
		}
		set
		{
			base.GameEntity.SetVisibilityExcludeParents(value);
		}
	}
}
