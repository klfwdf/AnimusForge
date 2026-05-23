using System;
using System.Runtime.CompilerServices;

namespace System.Buffers
{
	// Token: 0x02000033 RID: 51
	public abstract class ReadOnlySequenceSegment<[Nullable(2)] T>
	{
		// Token: 0x17000028 RID: 40
		// (get) Token: 0x060001FE RID: 510 RVA: 0x0000B177 File Offset: 0x00009377
		// (set) Token: 0x060001FF RID: 511 RVA: 0x0000B17F File Offset: 0x0000937F
		[Nullable(new byte[] { 0, 1 })]
		public ReadOnlyMemory<T> Memory
		{
			[return: Nullable(new byte[] { 0, 1 })]
			get;
			[param: Nullable(new byte[] { 0, 1 })]
			protected set;
		}

		// Token: 0x17000029 RID: 41
		// (get) Token: 0x06000200 RID: 512 RVA: 0x0000B188 File Offset: 0x00009388
		// (set) Token: 0x06000201 RID: 513 RVA: 0x0000B190 File Offset: 0x00009390
		[Nullable(new byte[] { 2, 1 })]
		public ReadOnlySequenceSegment<T> Next
		{
			[return: Nullable(new byte[] { 2, 1 })]
			get;
			[param: Nullable(new byte[] { 2, 1 })]
			protected set;
		}

		// Token: 0x1700002A RID: 42
		// (get) Token: 0x06000202 RID: 514 RVA: 0x0000B199 File Offset: 0x00009399
		// (set) Token: 0x06000203 RID: 515 RVA: 0x0000B1A1 File Offset: 0x000093A1
		public long RunningIndex { get; protected set; }
	}
}
