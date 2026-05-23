using System;
using TaleWorlds.Engine;

namespace SandBox.BoardGames.Pawns
{
	// Token: 0x020000F6 RID: 246
	public class PawnMuTorere : PawnBase
	{
		// Token: 0x170000F9 RID: 249
		// (get) Token: 0x06000C78 RID: 3192 RVA: 0x0005CEC9 File Offset: 0x0005B0C9
		// (set) Token: 0x06000C79 RID: 3193 RVA: 0x0005CED1 File Offset: 0x0005B0D1
		public int X { get; set; }

		// Token: 0x170000FA RID: 250
		// (get) Token: 0x06000C7A RID: 3194 RVA: 0x0005CEDA File Offset: 0x0005B0DA
		public override bool IsPlaced
		{
			get
			{
				return true;
			}
		}

		// Token: 0x06000C7B RID: 3195 RVA: 0x0005CEDD File Offset: 0x0005B0DD
		public PawnMuTorere(GameEntity entity, bool playerOne)
			: base(entity, playerOne)
		{
			this.X = -1;
		}

		// Token: 0x06000C7C RID: 3196 RVA: 0x0005CEEE File Offset: 0x0005B0EE
		public override void Reset()
		{
			base.Reset();
			this.X = -1;
		}
	}
}
