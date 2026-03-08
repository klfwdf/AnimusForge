using System;

namespace SandBox.View.Map.Visuals
{
	// Token: 0x02000061 RID: 97
	public abstract class MapEntityVisual<T> : MapEntityVisual
	{
		// Token: 0x17000067 RID: 103
		// (get) Token: 0x060003DA RID: 986 RVA: 0x0001E276 File Offset: 0x0001C476
		// (set) Token: 0x060003DB RID: 987 RVA: 0x0001E27E File Offset: 0x0001C47E
		public T MapEntity { get; private set; }

		// Token: 0x060003DC RID: 988 RVA: 0x0001E287 File Offset: 0x0001C487
		public MapEntityVisual(T entity)
		{
			this.MapEntity = entity;
		}
	}
}
