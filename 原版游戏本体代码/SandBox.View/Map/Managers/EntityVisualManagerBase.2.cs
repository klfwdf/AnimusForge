using System;
using SandBox.View.Map.Visuals;

namespace SandBox.View.Map.Managers
{
	// Token: 0x02000072 RID: 114
	public abstract class EntityVisualManagerBase<TEntity> : EntityVisualManagerBase
	{
		// Token: 0x060004E6 RID: 1254
		public abstract MapEntityVisual<TEntity> GetVisualOfEntity(TEntity entity);

		// Token: 0x060004E7 RID: 1255 RVA: 0x00025DC6 File Offset: 0x00023FC6
		public static EntityVisualManagerBase<TEntity> GetEntityVisualManagerBase()
		{
			return SandBoxViewSubModule.SandBoxViewVisualManager.GetEntityComponent<EntityVisualManagerBase<TEntity>>();
		}
	}
}
