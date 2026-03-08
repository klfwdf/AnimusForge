using System;
using TaleWorlds.Engine;

namespace SandBox.BoardGames.Objects
{
	// Token: 0x020000FA RID: 250
	public class BoardGameDecal : ScriptComponentBehavior
	{
		// Token: 0x06000C9B RID: 3227 RVA: 0x0005D44E File Offset: 0x0005B64E
		protected override void OnInit()
		{
			base.OnInit();
			this.SetAlpha(0f);
		}

		// Token: 0x06000C9C RID: 3228 RVA: 0x0005D464 File Offset: 0x0005B664
		public void SetAlpha(float alpha)
		{
			base.GameEntity.SetAlpha(alpha);
		}

		// Token: 0x06000C9D RID: 3229 RVA: 0x0005D480 File Offset: 0x0005B680
		protected override bool MovesEntity()
		{
			return false;
		}
	}
}
