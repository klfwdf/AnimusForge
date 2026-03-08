using System;
using SandBox.BoardGames.Objects;
using SandBox.BoardGames.Pawns;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace SandBox.BoardGames.Tiles
{
	// Token: 0x020000F0 RID: 240
	public abstract class TileBase
	{
		// Token: 0x170000DE RID: 222
		// (get) Token: 0x06000C34 RID: 3124 RVA: 0x0005C3ED File Offset: 0x0005A5ED
		public GameEntity Entity { get; }

		// Token: 0x170000DF RID: 223
		// (get) Token: 0x06000C35 RID: 3125 RVA: 0x0005C3F5 File Offset: 0x0005A5F5
		public BoardGameDecal ValidMoveDecal { get; }

		// Token: 0x06000C36 RID: 3126 RVA: 0x0005C3FD File Offset: 0x0005A5FD
		protected TileBase(GameEntity entity, BoardGameDecal decal)
		{
			this.Entity = entity;
			this.ValidMoveDecal = decal;
		}

		// Token: 0x06000C37 RID: 3127 RVA: 0x0005C413 File Offset: 0x0005A613
		public virtual void Reset()
		{
			this.PawnOnTile = null;
		}

		// Token: 0x06000C38 RID: 3128 RVA: 0x0005C41C File Offset: 0x0005A61C
		public void Tick(float dt)
		{
			int num = (this._showTile ? 1 : (-1));
			this._tileFadeTimer += (float)num * dt * 5f;
			this._tileFadeTimer = MBMath.ClampFloat(this._tileFadeTimer, 0f, 1f);
			this.ValidMoveDecal.SetAlpha(this._tileFadeTimer);
		}

		// Token: 0x06000C39 RID: 3129 RVA: 0x0005C479 File Offset: 0x0005A679
		public void SetVisibility(bool isVisible)
		{
			this._showTile = isVisible;
		}

		// Token: 0x0400053E RID: 1342
		public PawnBase PawnOnTile;

		// Token: 0x0400053F RID: 1343
		private bool _showTile;

		// Token: 0x04000540 RID: 1344
		private float _tileFadeTimer;

		// Token: 0x04000541 RID: 1345
		private const float TileFadeDuration = 0.2f;
	}
}
