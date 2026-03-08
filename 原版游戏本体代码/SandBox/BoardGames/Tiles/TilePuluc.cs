using System;
using SandBox.BoardGames.Objects;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace SandBox.BoardGames.Tiles
{
	// Token: 0x020000F2 RID: 242
	public class TilePuluc : Tile1D
	{
		// Token: 0x170000E2 RID: 226
		// (get) Token: 0x06000C3D RID: 3133 RVA: 0x0005C4AD File Offset: 0x0005A6AD
		// (set) Token: 0x06000C3E RID: 3134 RVA: 0x0005C4B5 File Offset: 0x0005A6B5
		public Vec3 PosLeft { get; private set; }

		// Token: 0x170000E3 RID: 227
		// (get) Token: 0x06000C3F RID: 3135 RVA: 0x0005C4BE File Offset: 0x0005A6BE
		// (set) Token: 0x06000C40 RID: 3136 RVA: 0x0005C4C6 File Offset: 0x0005A6C6
		public Vec3 PosLeftMid { get; private set; }

		// Token: 0x170000E4 RID: 228
		// (get) Token: 0x06000C41 RID: 3137 RVA: 0x0005C4CF File Offset: 0x0005A6CF
		// (set) Token: 0x06000C42 RID: 3138 RVA: 0x0005C4D7 File Offset: 0x0005A6D7
		public Vec3 PosRight { get; private set; }

		// Token: 0x170000E5 RID: 229
		// (get) Token: 0x06000C43 RID: 3139 RVA: 0x0005C4E0 File Offset: 0x0005A6E0
		// (set) Token: 0x06000C44 RID: 3140 RVA: 0x0005C4E8 File Offset: 0x0005A6E8
		public Vec3 PosRightMid { get; private set; }

		// Token: 0x06000C45 RID: 3141 RVA: 0x0005C4F1 File Offset: 0x0005A6F1
		public TilePuluc(GameEntity entity, BoardGameDecal decal, int x)
			: base(entity, decal, x)
		{
			this.UpdateTilePosition();
		}

		// Token: 0x06000C46 RID: 3142 RVA: 0x0005C504 File Offset: 0x0005A704
		public void UpdateTilePosition()
		{
			MatrixFrame globalFrame = base.Entity.GetGlobalFrame();
			MetaMesh tileMesh = base.Entity.GetFirstScriptOfType<Tile>().TileMesh;
			Vec3 vec = tileMesh.GetBoundingBox().max - tileMesh.GetBoundingBox().min;
			Mat3 mat = globalFrame.rotation.TransformToParent(tileMesh.Frame.rotation);
			Vec3 vec2 = new Vec3(0f, vec.y / 6f, 0f, -1f);
			Vec3 v = mat.TransformToParent(vec2);
			vec2 = new Vec3(0f, vec.y / 3f, 0f, -1f);
			Vec3 v2 = mat.TransformToParent(vec2);
			Vec3 globalPosition = base.Entity.GlobalPosition;
			this.PosLeft = globalPosition + v2;
			this.PosLeftMid = globalPosition + v;
			this.PosRight = globalPosition - v2;
			this.PosRightMid = globalPosition - v;
		}
	}
}
