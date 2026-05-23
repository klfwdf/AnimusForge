using System;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.Engine.GauntletUI
{
	// Token: 0x02000005 RID: 5
	public static class Extensions
	{
		// Token: 0x0600000E RID: 14 RVA: 0x0000213E File Offset: 0x0000033E
		public static void Load(this SpriteCategory category)
		{
			category.Load(UIResourceManager.ResourceContext, UIResourceManager.ResourceDepot);
		}
	}
}
