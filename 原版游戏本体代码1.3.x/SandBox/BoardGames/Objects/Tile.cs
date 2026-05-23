using System;
using TaleWorlds.Engine;

namespace SandBox.BoardGames.Objects
{
	// Token: 0x020000FB RID: 251
	public class Tile : ScriptComponentBehavior
	{
		// Token: 0x06000C9F RID: 3231 RVA: 0x0005D48C File Offset: 0x0005B68C
		protected override void OnInit()
		{
			base.OnInit();
			base.GameEntity.RemoveMultiMesh(base.GameEntity.GetMetaMesh(0));
		}

		// Token: 0x06000CA0 RID: 3232 RVA: 0x0005D4C0 File Offset: 0x0005B6C0
		public void SetVisibility(bool visible)
		{
			base.GameEntity.SetVisibilityExcludeParents(visible);
		}

		// Token: 0x06000CA1 RID: 3233 RVA: 0x0005D4DC File Offset: 0x0005B6DC
		protected override bool MovesEntity()
		{
			return false;
		}

		// Token: 0x0400057A RID: 1402
		public MetaMesh TileMesh;
	}
}
