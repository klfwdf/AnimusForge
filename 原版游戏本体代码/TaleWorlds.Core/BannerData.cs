using System;
using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.SaveSystem;

namespace TaleWorlds.Core
{
	// Token: 0x02000013 RID: 19
	public class BannerData
	{
		// Token: 0x17000030 RID: 48
		// (get) Token: 0x060000C2 RID: 194 RVA: 0x00004222 File Offset: 0x00002422
		public int LocalVersion
		{
			get
			{
				return this._localVersion;
			}
		}

		// Token: 0x17000031 RID: 49
		// (get) Token: 0x060000C3 RID: 195 RVA: 0x0000422A File Offset: 0x0000242A
		// (set) Token: 0x060000C4 RID: 196 RVA: 0x00004232 File Offset: 0x00002432
		public int MeshId
		{
			get
			{
				return this._meshId;
			}
			set
			{
				if (value != this._meshId)
				{
					this._meshId = value;
					this._localVersion++;
				}
			}
		}

		// Token: 0x17000032 RID: 50
		// (get) Token: 0x060000C5 RID: 197 RVA: 0x00004252 File Offset: 0x00002452
		// (set) Token: 0x060000C6 RID: 198 RVA: 0x0000425A File Offset: 0x0000245A
		public int ColorId
		{
			get
			{
				return this._colorId;
			}
			set
			{
				if (value != this._colorId)
				{
					this._colorId = value;
					this._localVersion++;
				}
			}
		}

		// Token: 0x17000033 RID: 51
		// (get) Token: 0x060000C7 RID: 199 RVA: 0x0000427A File Offset: 0x0000247A
		// (set) Token: 0x060000C8 RID: 200 RVA: 0x00004282 File Offset: 0x00002482
		public int ColorId2
		{
			get
			{
				return this._colorId2;
			}
			set
			{
				if (value != this._colorId2)
				{
					this._colorId2 = value;
					this._localVersion++;
				}
			}
		}

		// Token: 0x17000034 RID: 52
		// (get) Token: 0x060000C9 RID: 201 RVA: 0x000042A2 File Offset: 0x000024A2
		// (set) Token: 0x060000CA RID: 202 RVA: 0x000042AA File Offset: 0x000024AA
		public Vec2 Size
		{
			get
			{
				return this._size;
			}
			set
			{
				if (value != this._size)
				{
					this._size = value;
					this._localVersion++;
				}
			}
		}

		// Token: 0x17000035 RID: 53
		// (get) Token: 0x060000CB RID: 203 RVA: 0x000042CF File Offset: 0x000024CF
		// (set) Token: 0x060000CC RID: 204 RVA: 0x000042D7 File Offset: 0x000024D7
		public Vec2 Position
		{
			get
			{
				return this._position;
			}
			set
			{
				if (value != this._position)
				{
					this._position = value;
					this._localVersion++;
				}
			}
		}

		// Token: 0x17000036 RID: 54
		// (get) Token: 0x060000CD RID: 205 RVA: 0x000042FC File Offset: 0x000024FC
		// (set) Token: 0x060000CE RID: 206 RVA: 0x00004304 File Offset: 0x00002504
		public bool DrawStroke
		{
			get
			{
				return this._drawStroke;
			}
			set
			{
				if (value != this._drawStroke)
				{
					this._drawStroke = value;
					this._localVersion++;
				}
			}
		}

		// Token: 0x17000037 RID: 55
		// (get) Token: 0x060000CF RID: 207 RVA: 0x00004324 File Offset: 0x00002524
		// (set) Token: 0x060000D0 RID: 208 RVA: 0x0000432C File Offset: 0x0000252C
		public bool Mirror
		{
			get
			{
				return this._mirror;
			}
			set
			{
				if (value != this._mirror)
				{
					this._mirror = value;
					this._localVersion++;
				}
			}
		}

		// Token: 0x17000038 RID: 56
		// (get) Token: 0x060000D1 RID: 209 RVA: 0x0000434C File Offset: 0x0000254C
		// (set) Token: 0x060000D2 RID: 210 RVA: 0x00004354 File Offset: 0x00002554
		public float RotationValue
		{
			get
			{
				return this._rotationValue;
			}
			set
			{
				if (value != this._rotationValue)
				{
					this._rotationValue = value;
					this._localVersion++;
				}
			}
		}

		// Token: 0x17000039 RID: 57
		// (get) Token: 0x060000D3 RID: 211 RVA: 0x00004374 File Offset: 0x00002574
		public float Rotation
		{
			get
			{
				return 6.2831855f * this.RotationValue;
			}
		}

		// Token: 0x060000D4 RID: 212 RVA: 0x00004384 File Offset: 0x00002584
		public BannerData(int meshId, int colorId, int colorId2, Vec2 size, Vec2 position, bool drawStroke, bool mirror, float rotationValue)
		{
			this.MeshId = meshId;
			this.ColorId = colorId;
			this.ColorId2 = colorId2;
			this.Size = size;
			this.Position = position;
			this.DrawStroke = drawStroke;
			this.Mirror = mirror;
			this.RotationValue = rotationValue;
		}

		// Token: 0x060000D5 RID: 213 RVA: 0x000043D4 File Offset: 0x000025D4
		public BannerData(BannerData bannerData)
			: this(bannerData.MeshId, bannerData.ColorId, bannerData.ColorId2, bannerData.Size, bannerData.Position, bannerData.DrawStroke, bannerData.Mirror, bannerData.RotationValue)
		{
		}

		// Token: 0x060000D6 RID: 214 RVA: 0x00004418 File Offset: 0x00002618
		public override bool Equals(object obj)
		{
			BannerData bannerData;
			return (bannerData = obj as BannerData) != null && bannerData.MeshId == this.MeshId && bannerData.ColorId == this.ColorId && bannerData.ColorId2 == this.ColorId2 && bannerData.Size.X == this.Size.X && bannerData.Size.Y == this.Size.Y && bannerData.Position.X == this.Position.X && bannerData.Position.Y == this.Position.Y && bannerData.DrawStroke == this.DrawStroke && bannerData.Mirror == this.Mirror && bannerData.RotationValue == this.RotationValue;
		}

		// Token: 0x060000D7 RID: 215 RVA: 0x0000450D File Offset: 0x0000270D
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		// Token: 0x060000D8 RID: 216 RVA: 0x00004515 File Offset: 0x00002715
		internal static void AutoGeneratedStaticCollectObjectsBannerData(object o, List<object> collectedObjects)
		{
			((BannerData)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		// Token: 0x060000D9 RID: 217 RVA: 0x00004523 File Offset: 0x00002723
		protected virtual void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
		}

		// Token: 0x060000DA RID: 218 RVA: 0x00004525 File Offset: 0x00002725
		internal static object AutoGeneratedGetMemberValue_colorId2(object o)
		{
			return ((BannerData)o)._colorId2;
		}

		// Token: 0x060000DB RID: 219 RVA: 0x00004537 File Offset: 0x00002737
		internal static object AutoGeneratedGetMemberValue_size(object o)
		{
			return ((BannerData)o)._size;
		}

		// Token: 0x060000DC RID: 220 RVA: 0x00004549 File Offset: 0x00002749
		internal static object AutoGeneratedGetMemberValue_position(object o)
		{
			return ((BannerData)o)._position;
		}

		// Token: 0x060000DD RID: 221 RVA: 0x0000455B File Offset: 0x0000275B
		internal static object AutoGeneratedGetMemberValue_drawStroke(object o)
		{
			return ((BannerData)o)._drawStroke;
		}

		// Token: 0x060000DE RID: 222 RVA: 0x0000456D File Offset: 0x0000276D
		internal static object AutoGeneratedGetMemberValue_mirror(object o)
		{
			return ((BannerData)o)._mirror;
		}

		// Token: 0x060000DF RID: 223 RVA: 0x0000457F File Offset: 0x0000277F
		internal static object AutoGeneratedGetMemberValue_rotationValue(object o)
		{
			return ((BannerData)o)._rotationValue;
		}

		// Token: 0x060000E0 RID: 224 RVA: 0x00004591 File Offset: 0x00002791
		internal static object AutoGeneratedGetMemberValue_meshId(object o)
		{
			return ((BannerData)o)._meshId;
		}

		// Token: 0x060000E1 RID: 225 RVA: 0x000045A3 File Offset: 0x000027A3
		internal static object AutoGeneratedGetMemberValue_colorId(object o)
		{
			return ((BannerData)o)._colorId;
		}

		// Token: 0x04000105 RID: 261
		public const float RotationPrecision = 0.0027777778f;

		// Token: 0x04000106 RID: 262
		[CachedData]
		private int _localVersion;

		// Token: 0x04000107 RID: 263
		[SaveableField(1)]
		private int _meshId;

		// Token: 0x04000108 RID: 264
		[SaveableField(2)]
		private int _colorId;

		// Token: 0x04000109 RID: 265
		[SaveableField(3)]
		public int _colorId2;

		// Token: 0x0400010A RID: 266
		[SaveableField(4)]
		public Vec2 _size;

		// Token: 0x0400010B RID: 267
		[SaveableField(5)]
		public Vec2 _position;

		// Token: 0x0400010C RID: 268
		[SaveableField(6)]
		public bool _drawStroke;

		// Token: 0x0400010D RID: 269
		[SaveableField(7)]
		public bool _mirror;

		// Token: 0x0400010E RID: 270
		[SaveableField(8)]
		public float _rotationValue;
	}
}
