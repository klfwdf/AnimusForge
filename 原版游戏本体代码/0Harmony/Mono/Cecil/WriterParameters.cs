using System;
using System.IO;
using System.Reflection;
using Mono.Cecil.Cil;

namespace Mono.Cecil
{
	// Token: 0x0200027A RID: 634
	internal sealed class WriterParameters
	{
		// Token: 0x170003C7 RID: 967
		// (get) Token: 0x06000F2F RID: 3887 RVA: 0x0002FF59 File Offset: 0x0002E159
		// (set) Token: 0x06000F30 RID: 3888 RVA: 0x0002FF61 File Offset: 0x0002E161
		public uint? Timestamp
		{
			get
			{
				return this.timestamp;
			}
			set
			{
				this.timestamp = value;
			}
		}

		// Token: 0x170003C8 RID: 968
		// (get) Token: 0x06000F31 RID: 3889 RVA: 0x0002FF6A File Offset: 0x0002E16A
		// (set) Token: 0x06000F32 RID: 3890 RVA: 0x0002FF72 File Offset: 0x0002E172
		public Stream SymbolStream
		{
			get
			{
				return this.symbol_stream;
			}
			set
			{
				this.symbol_stream = value;
			}
		}

		// Token: 0x170003C9 RID: 969
		// (get) Token: 0x06000F33 RID: 3891 RVA: 0x0002FF7B File Offset: 0x0002E17B
		// (set) Token: 0x06000F34 RID: 3892 RVA: 0x0002FF83 File Offset: 0x0002E183
		public ISymbolWriterProvider SymbolWriterProvider
		{
			get
			{
				return this.symbol_writer_provider;
			}
			set
			{
				this.symbol_writer_provider = value;
			}
		}

		// Token: 0x170003CA RID: 970
		// (get) Token: 0x06000F35 RID: 3893 RVA: 0x0002FF8C File Offset: 0x0002E18C
		// (set) Token: 0x06000F36 RID: 3894 RVA: 0x0002FF94 File Offset: 0x0002E194
		public bool WriteSymbols
		{
			get
			{
				return this.write_symbols;
			}
			set
			{
				this.write_symbols = value;
			}
		}

		// Token: 0x170003CB RID: 971
		// (get) Token: 0x06000F37 RID: 3895 RVA: 0x0002FF9D File Offset: 0x0002E19D
		public bool HasStrongNameKey
		{
			get
			{
				return this.key_pair != null || this.key_blob != null || this.key_container != null;
			}
		}

		// Token: 0x170003CC RID: 972
		// (get) Token: 0x06000F38 RID: 3896 RVA: 0x0002FFBA File Offset: 0x0002E1BA
		// (set) Token: 0x06000F39 RID: 3897 RVA: 0x0002FFC2 File Offset: 0x0002E1C2
		public byte[] StrongNameKeyBlob
		{
			get
			{
				return this.key_blob;
			}
			set
			{
				this.key_blob = value;
			}
		}

		// Token: 0x170003CD RID: 973
		// (get) Token: 0x06000F3A RID: 3898 RVA: 0x0002FFCB File Offset: 0x0002E1CB
		// (set) Token: 0x06000F3B RID: 3899 RVA: 0x0002FFD3 File Offset: 0x0002E1D3
		public string StrongNameKeyContainer
		{
			get
			{
				return this.key_container;
			}
			set
			{
				this.key_container = value;
			}
		}

		// Token: 0x170003CE RID: 974
		// (get) Token: 0x06000F3C RID: 3900 RVA: 0x0002FFDC File Offset: 0x0002E1DC
		// (set) Token: 0x06000F3D RID: 3901 RVA: 0x0002FFE4 File Offset: 0x0002E1E4
		public StrongNameKeyPair StrongNameKeyPair
		{
			get
			{
				return this.key_pair;
			}
			set
			{
				this.key_pair = value;
			}
		}

		// Token: 0x170003CF RID: 975
		// (get) Token: 0x06000F3E RID: 3902 RVA: 0x0002FFED File Offset: 0x0002E1ED
		// (set) Token: 0x06000F3F RID: 3903 RVA: 0x0002FFF5 File Offset: 0x0002E1F5
		public bool DeterministicMvid { get; set; }

		// Token: 0x040004AE RID: 1198
		private uint? timestamp;

		// Token: 0x040004AF RID: 1199
		private Stream symbol_stream;

		// Token: 0x040004B0 RID: 1200
		private ISymbolWriterProvider symbol_writer_provider;

		// Token: 0x040004B1 RID: 1201
		private bool write_symbols;

		// Token: 0x040004B2 RID: 1202
		private byte[] key_blob;

		// Token: 0x040004B3 RID: 1203
		private string key_container;

		// Token: 0x040004B4 RID: 1204
		private StrongNameKeyPair key_pair;
	}
}
