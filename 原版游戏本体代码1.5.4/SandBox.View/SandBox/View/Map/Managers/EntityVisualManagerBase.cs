using SandBox.View.Map.Visuals;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Engine;

namespace SandBox.View.Map.Managers;

public abstract class EntityVisualManagerBase : CampaignEntityVisualComponent
{
	private Scene _mapScene;

	public Scene MapScene
	{
		get
		{
			if (_mapScene == null && Campaign.Current != null && Campaign.Current.MapSceneWrapper != null)
			{
				_mapScene = ((MapScene)Campaign.Current.MapSceneWrapper).Scene;
			}
			return _mapScene;
		}
	}
}
public abstract class EntityVisualManagerBase<TEntity> : EntityVisualManagerBase
{
	public abstract MapEntityVisual<TEntity> GetVisualOfEntity(TEntity entity);

	public static EntityVisualManagerBase<TEntity> GetEntityVisualManagerBase()
	{
		return SandBoxViewSubModule.SandBoxViewVisualManager.GetEntityComponent<EntityVisualManagerBase<TEntity>>();
	}
}
