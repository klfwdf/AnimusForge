using System;

namespace Mono.Cecil.Cil
{
	// Token: 0x02000308 RID: 776
	internal sealed class ImageDebugHeader
	{
		// Token: 0x17000513 RID: 1299
		// (get) Token: 0x06001434 RID: 5172 RVA: 0x00040B77 File Offset: 0x0003ED77
		public bool HasEntries
		{
			get
			{
				return !this.entries.IsNullOrEmpty<ImageDebugHeaderEntry>();
			}
		}

		// Token: 0x17000514 RID: 1300
		// (get) Token: 0x06001435 RID: 5173 RVA: 0x00040B87 File Offset: 0x0003ED87
		public ImageDebugHeaderEntry[] Entries
		{
			get
			{
				return this.entries;
			}
		}

		// Token: 0x06001436 RID: 5174 RVA: 0x00040B8F File Offset: 0x0003ED8F
		public ImageDebugHeader(ImageDebugHeaderEntry[] entries)
		{
			this.entries = entries ?? Empty<ImageDebugHeaderEntry>.Array;
		}

		// Token: 0x06001437 RID: 5175 RVA: 0x00040BA7 File Offset: 0x0003EDA7
		public ImageDebugHeader()
			: this(Empty<ImageDebugHeaderEntry>.Array)
		{
		}

		// Token: 0x06001438 RID: 5176 RVA: 0x00040BB4 File Offset: 0x0003EDB4
		public ImageDebugHeader(ImageDebugHeaderEntry entry)
			: this(new ImageDebugHeaderEntry[] { entry })
		{
		}

		// Token: 0x04000A15 RID: 2581
		private readonly ImageDebugHeaderEntry[] entries;
	}
}
