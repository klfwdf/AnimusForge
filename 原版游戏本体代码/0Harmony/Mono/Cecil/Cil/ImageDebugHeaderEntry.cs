using System;

namespace Mono.Cecil.Cil
{
	// Token: 0x02000309 RID: 777
	internal sealed class ImageDebugHeaderEntry
	{
		// Token: 0x17000515 RID: 1301
		// (get) Token: 0x06001439 RID: 5177 RVA: 0x00040BC6 File Offset: 0x0003EDC6
		// (set) Token: 0x0600143A RID: 5178 RVA: 0x00040BCE File Offset: 0x0003EDCE
		public ImageDebugDirectory Directory
		{
			get
			{
				return this.directory;
			}
			internal set
			{
				this.directory = value;
			}
		}

		// Token: 0x17000516 RID: 1302
		// (get) Token: 0x0600143B RID: 5179 RVA: 0x00040BD7 File Offset: 0x0003EDD7
		public byte[] Data
		{
			get
			{
				return this.data;
			}
		}

		// Token: 0x0600143C RID: 5180 RVA: 0x00040BDF File Offset: 0x0003EDDF
		public ImageDebugHeaderEntry(ImageDebugDirectory directory, byte[] data)
		{
			this.directory = directory;
			this.data = data ?? Empty<byte>.Array;
		}

		// Token: 0x04000A16 RID: 2582
		private ImageDebugDirectory directory;

		// Token: 0x04000A17 RID: 2583
		private readonly byte[] data;
	}
}
