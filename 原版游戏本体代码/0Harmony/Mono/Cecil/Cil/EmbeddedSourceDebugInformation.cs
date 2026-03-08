using System;
using Mono.Cecil.Metadata;

namespace Mono.Cecil.Cil
{
	// Token: 0x0200031B RID: 795
	internal sealed class EmbeddedSourceDebugInformation : CustomDebugInformation
	{
		// Token: 0x17000548 RID: 1352
		// (get) Token: 0x06001499 RID: 5273 RVA: 0x000412C7 File Offset: 0x0003F4C7
		// (set) Token: 0x0600149A RID: 5274 RVA: 0x000412DD File Offset: 0x0003F4DD
		public byte[] Content
		{
			get
			{
				if (!this.resolved)
				{
					this.Resolve();
				}
				return this.content;
			}
			set
			{
				this.content = value;
				this.resolved = true;
			}
		}

		// Token: 0x17000549 RID: 1353
		// (get) Token: 0x0600149B RID: 5275 RVA: 0x000412ED File Offset: 0x0003F4ED
		// (set) Token: 0x0600149C RID: 5276 RVA: 0x00041303 File Offset: 0x0003F503
		public bool Compress
		{
			get
			{
				if (!this.resolved)
				{
					this.Resolve();
				}
				return this.compress;
			}
			set
			{
				this.compress = value;
				this.resolved = true;
			}
		}

		// Token: 0x1700054A RID: 1354
		// (get) Token: 0x0600149D RID: 5277 RVA: 0x00041313 File Offset: 0x0003F513
		public override CustomDebugInformationKind Kind
		{
			get
			{
				return CustomDebugInformationKind.EmbeddedSource;
			}
		}

		// Token: 0x0600149E RID: 5278 RVA: 0x00041316 File Offset: 0x0003F516
		internal EmbeddedSourceDebugInformation(uint index, MetadataReader debug_reader)
			: base(EmbeddedSourceDebugInformation.KindIdentifier)
		{
			this.index = index;
			this.debug_reader = debug_reader;
		}

		// Token: 0x0600149F RID: 5279 RVA: 0x00041331 File Offset: 0x0003F531
		public EmbeddedSourceDebugInformation(byte[] content, bool compress)
			: base(EmbeddedSourceDebugInformation.KindIdentifier)
		{
			this.resolved = true;
			this.content = content;
			this.compress = compress;
		}

		// Token: 0x060014A0 RID: 5280 RVA: 0x00041353 File Offset: 0x0003F553
		internal byte[] ReadRawEmbeddedSourceDebugInformation()
		{
			if (this.debug_reader == null)
			{
				throw new InvalidOperationException();
			}
			return this.debug_reader.ReadRawEmbeddedSourceDebugInformation(this.index);
		}

		// Token: 0x060014A1 RID: 5281 RVA: 0x00041374 File Offset: 0x0003F574
		private void Resolve()
		{
			if (this.resolved)
			{
				return;
			}
			if (this.debug_reader == null)
			{
				throw new InvalidOperationException();
			}
			Row<byte[], bool> row = this.debug_reader.ReadEmbeddedSourceDebugInformation(this.index);
			this.content = row.Col1;
			this.compress = row.Col2;
			this.resolved = true;
		}

		// Token: 0x04000A51 RID: 2641
		internal uint index;

		// Token: 0x04000A52 RID: 2642
		internal MetadataReader debug_reader;

		// Token: 0x04000A53 RID: 2643
		internal bool resolved;

		// Token: 0x04000A54 RID: 2644
		internal byte[] content;

		// Token: 0x04000A55 RID: 2645
		internal bool compress;

		// Token: 0x04000A56 RID: 2646
		public static Guid KindIdentifier = new Guid("{0E8A571B-6926-466E-B4AD-8AB04611F5FE}");
	}
}
