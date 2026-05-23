using System;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.Engine.GauntletUI
{
	// Token: 0x02000004 RID: 4
	public class EngineTexture : ITexture
	{
		// Token: 0x17000001 RID: 1
		// (get) Token: 0x06000003 RID: 3 RVA: 0x00002058 File Offset: 0x00000258
		// (set) Token: 0x06000004 RID: 4 RVA: 0x00002060 File Offset: 0x00000260
		public Texture Texture { get; private set; }

		// Token: 0x17000002 RID: 2
		// (get) Token: 0x06000005 RID: 5 RVA: 0x00002069 File Offset: 0x00000269
		bool ITexture.IsValid
		{
			get
			{
				return this.Texture.IsValid && !this.Texture.IsReleased;
			}
		}

		// Token: 0x17000003 RID: 3
		// (get) Token: 0x06000006 RID: 6 RVA: 0x00002088 File Offset: 0x00000288
		int ITexture.Width
		{
			get
			{
				return this.Texture.Width;
			}
		}

		// Token: 0x17000004 RID: 4
		// (get) Token: 0x06000007 RID: 7 RVA: 0x00002095 File Offset: 0x00000295
		int ITexture.Height
		{
			get
			{
				return this.Texture.Height;
			}
		}

		// Token: 0x17000005 RID: 5
		// (get) Token: 0x06000008 RID: 8 RVA: 0x000020A2 File Offset: 0x000002A2
		// (set) Token: 0x06000009 RID: 9 RVA: 0x000020AF File Offset: 0x000002AF
		string ITexture.Name
		{
			get
			{
				return this.Texture.Name;
			}
			set
			{
				this.Texture.Name = value;
			}
		}

		// Token: 0x0600000A RID: 10 RVA: 0x000020BD File Offset: 0x000002BD
		public EngineTexture(Texture engineTexture)
		{
			this.Texture = engineTexture;
		}

		// Token: 0x0600000B RID: 11 RVA: 0x000020CC File Offset: 0x000002CC
		bool ITexture.IsLoaded()
		{
			return this.Texture.IsLoaded();
		}

		// Token: 0x0600000C RID: 12 RVA: 0x000020D9 File Offset: 0x000002D9
		void ITexture.Release()
		{
			this.Texture.ReleaseImmediately();
			this.Texture = null;
		}

		// Token: 0x0600000D RID: 13 RVA: 0x000020F0 File Offset: 0x000002F0
		public override int GetHashCode()
		{
			return (((((691 * 8713) ^ this.Texture.GetHashCode()) * 8713) ^ ((ITexture)this).Width.GetHashCode()) * 8713) ^ ((ITexture)this).Height.GetHashCode();
		}
	}
}
