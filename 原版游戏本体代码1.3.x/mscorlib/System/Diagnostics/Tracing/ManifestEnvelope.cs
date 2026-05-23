using System;

namespace System.Diagnostics.Tracing
{
	// Token: 0x02000432 RID: 1074
	internal struct ManifestEnvelope
	{
		// Token: 0x040017E0 RID: 6112
		public const int MaxChunkSize = 65280;

		// Token: 0x040017E1 RID: 6113
		public ManifestEnvelope.ManifestFormats Format;

		// Token: 0x040017E2 RID: 6114
		public byte MajorVersion;

		// Token: 0x040017E3 RID: 6115
		public byte MinorVersion;

		// Token: 0x040017E4 RID: 6116
		public byte Magic;

		// Token: 0x040017E5 RID: 6117
		public ushort TotalChunks;

		// Token: 0x040017E6 RID: 6118
		public ushort ChunkNumber;

		// Token: 0x02000B96 RID: 2966
		public enum ManifestFormats : byte
		{
			// Token: 0x04003524 RID: 13604
			SimpleXmlFormat = 1
		}
	}
}
