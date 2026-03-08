using System;
using System.Runtime.CompilerServices;

namespace System.Buffers
{
	// Token: 0x0200049C RID: 1180
	internal abstract class ReadOnlySequenceSegment<[Nullable(2)] T>
	{
		// Token: 0x170005CE RID: 1486
		// (get) Token: 0x06001A60 RID: 6752 RVA: 0x00056394 File Offset: 0x00054594
		// (set) Token: 0x06001A61 RID: 6753 RVA: 0x0005639C File Offset: 0x0005459C
		[Nullable(new byte[] { 0, 1 })]
		public ReadOnlyMemory<T> Memory
		{
			[return: Nullable(new byte[] { 0, 1 })]
			get;
			[param: Nullable(new byte[] { 0, 1 })]
			protected set;
		}

		// Token: 0x170005CF RID: 1487
		// (get) Token: 0x06001A62 RID: 6754 RVA: 0x000563A5 File Offset: 0x000545A5
		// (set) Token: 0x06001A63 RID: 6755 RVA: 0x000563AD File Offset: 0x000545AD
		[Nullable(new byte[] { 2, 1 })]
		public ReadOnlySequenceSegment<T> Next
		{
			[return: Nullable(new byte[] { 2, 1 })]
			get;
			[param: Nullable(new byte[] { 2, 1 })]
			protected set;
		}

		// Token: 0x170005D0 RID: 1488
		// (get) Token: 0x06001A64 RID: 6756 RVA: 0x000563B6 File Offset: 0x000545B6
		// (set) Token: 0x06001A65 RID: 6757 RVA: 0x000563BE File Offset: 0x000545BE
		public long RunningIndex { get; protected set; }
	}
}
