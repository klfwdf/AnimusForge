using System;
using System.IO;

namespace Mono.CompilerServices.SymbolWriter
{
	// Token: 0x02000336 RID: 822
	internal class OffsetTable
	{
		// Token: 0x06001517 RID: 5399 RVA: 0x00042BA8 File Offset: 0x00040DA8
		internal OffsetTable()
		{
			int platform = (int)Environment.OSVersion.Platform;
			if (platform != 4 && platform != 128)
			{
				this.FileFlags |= OffsetTable.Flags.WindowsFileNames;
			}
		}

		// Token: 0x06001518 RID: 5400 RVA: 0x00042BF8 File Offset: 0x00040DF8
		internal OffsetTable(BinaryReader reader, int major_version, int minor_version)
		{
			this.TotalFileSize = reader.ReadInt32();
			this.DataSectionOffset = reader.ReadInt32();
			this.DataSectionSize = reader.ReadInt32();
			this.CompileUnitCount = reader.ReadInt32();
			this.CompileUnitTableOffset = reader.ReadInt32();
			this.CompileUnitTableSize = reader.ReadInt32();
			this.SourceCount = reader.ReadInt32();
			this.SourceTableOffset = reader.ReadInt32();
			this.SourceTableSize = reader.ReadInt32();
			this.MethodCount = reader.ReadInt32();
			this.MethodTableOffset = reader.ReadInt32();
			this.MethodTableSize = reader.ReadInt32();
			this.TypeCount = reader.ReadInt32();
			this.AnonymousScopeCount = reader.ReadInt32();
			this.AnonymousScopeTableOffset = reader.ReadInt32();
			this.AnonymousScopeTableSize = reader.ReadInt32();
			this.LineNumberTable_LineBase = reader.ReadInt32();
			this.LineNumberTable_LineRange = reader.ReadInt32();
			this.LineNumberTable_OpcodeBase = reader.ReadInt32();
			this.FileFlags = (OffsetTable.Flags)reader.ReadInt32();
		}

		// Token: 0x06001519 RID: 5401 RVA: 0x00042D14 File Offset: 0x00040F14
		internal void Write(BinaryWriter bw, int major_version, int minor_version)
		{
			bw.Write(this.TotalFileSize);
			bw.Write(this.DataSectionOffset);
			bw.Write(this.DataSectionSize);
			bw.Write(this.CompileUnitCount);
			bw.Write(this.CompileUnitTableOffset);
			bw.Write(this.CompileUnitTableSize);
			bw.Write(this.SourceCount);
			bw.Write(this.SourceTableOffset);
			bw.Write(this.SourceTableSize);
			bw.Write(this.MethodCount);
			bw.Write(this.MethodTableOffset);
			bw.Write(this.MethodTableSize);
			bw.Write(this.TypeCount);
			bw.Write(this.AnonymousScopeCount);
			bw.Write(this.AnonymousScopeTableOffset);
			bw.Write(this.AnonymousScopeTableSize);
			bw.Write(this.LineNumberTable_LineBase);
			bw.Write(this.LineNumberTable_LineRange);
			bw.Write(this.LineNumberTable_OpcodeBase);
			bw.Write((int)this.FileFlags);
		}

		// Token: 0x0600151A RID: 5402 RVA: 0x00042E14 File Offset: 0x00041014
		public override string ToString()
		{
			return string.Format("OffsetTable [{0} - {1}:{2} - {3}:{4}:{5} - {6}:{7}:{8} - {9}]", new object[] { this.TotalFileSize, this.DataSectionOffset, this.DataSectionSize, this.SourceCount, this.SourceTableOffset, this.SourceTableSize, this.MethodCount, this.MethodTableOffset, this.MethodTableSize, this.TypeCount });
		}

		// Token: 0x04000A8B RID: 2699
		public const int MajorVersion = 50;

		// Token: 0x04000A8C RID: 2700
		public const int MinorVersion = 0;

		// Token: 0x04000A8D RID: 2701
		public const long Magic = 5037318119232611860L;

		// Token: 0x04000A8E RID: 2702
		public int TotalFileSize;

		// Token: 0x04000A8F RID: 2703
		public int DataSectionOffset;

		// Token: 0x04000A90 RID: 2704
		public int DataSectionSize;

		// Token: 0x04000A91 RID: 2705
		public int CompileUnitCount;

		// Token: 0x04000A92 RID: 2706
		public int CompileUnitTableOffset;

		// Token: 0x04000A93 RID: 2707
		public int CompileUnitTableSize;

		// Token: 0x04000A94 RID: 2708
		public int SourceCount;

		// Token: 0x04000A95 RID: 2709
		public int SourceTableOffset;

		// Token: 0x04000A96 RID: 2710
		public int SourceTableSize;

		// Token: 0x04000A97 RID: 2711
		public int MethodCount;

		// Token: 0x04000A98 RID: 2712
		public int MethodTableOffset;

		// Token: 0x04000A99 RID: 2713
		public int MethodTableSize;

		// Token: 0x04000A9A RID: 2714
		public int TypeCount;

		// Token: 0x04000A9B RID: 2715
		public int AnonymousScopeCount;

		// Token: 0x04000A9C RID: 2716
		public int AnonymousScopeTableOffset;

		// Token: 0x04000A9D RID: 2717
		public int AnonymousScopeTableSize;

		// Token: 0x04000A9E RID: 2718
		public OffsetTable.Flags FileFlags;

		// Token: 0x04000A9F RID: 2719
		public int LineNumberTable_LineBase = -1;

		// Token: 0x04000AA0 RID: 2720
		public int LineNumberTable_LineRange = 8;

		// Token: 0x04000AA1 RID: 2721
		public int LineNumberTable_OpcodeBase = 9;

		// Token: 0x02000337 RID: 823
		[Flags]
		public enum Flags
		{
			// Token: 0x04000AA3 RID: 2723
			IsAspxSource = 1,
			// Token: 0x04000AA4 RID: 2724
			WindowsFileNames = 2
		}
	}
}
