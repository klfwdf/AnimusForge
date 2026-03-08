using System;

namespace Iced.Intel
{
	// Token: 0x0200065A RID: 1626
	internal readonly struct MemoryOperand
	{
		// Token: 0x0600235C RID: 9052 RVA: 0x00072640 File Offset: 0x00070840
		public MemoryOperand(Register @base, Register index, int scale, long displacement, int displSize, bool isBroadcast, Register segmentPrefix)
		{
			this.SegmentPrefix = segmentPrefix;
			this.Base = @base;
			this.Index = index;
			this.Scale = scale;
			this.Displacement = displacement;
			this.DisplSize = displSize;
			this.IsBroadcast = isBroadcast;
		}

		// Token: 0x0600235D RID: 9053 RVA: 0x00072677 File Offset: 0x00070877
		public MemoryOperand(Register @base, Register index, int scale, bool isBroadcast, Register segmentPrefix)
		{
			this.SegmentPrefix = segmentPrefix;
			this.Base = @base;
			this.Index = index;
			this.Scale = scale;
			this.Displacement = 0L;
			this.DisplSize = 0;
			this.IsBroadcast = isBroadcast;
		}

		// Token: 0x0600235E RID: 9054 RVA: 0x000726AD File Offset: 0x000708AD
		public MemoryOperand(Register @base, long displacement, int displSize, bool isBroadcast, Register segmentPrefix)
		{
			this.SegmentPrefix = segmentPrefix;
			this.Base = @base;
			this.Index = Register.None;
			this.Scale = 1;
			this.Displacement = displacement;
			this.DisplSize = displSize;
			this.IsBroadcast = isBroadcast;
		}

		// Token: 0x0600235F RID: 9055 RVA: 0x000726E2 File Offset: 0x000708E2
		public MemoryOperand(Register index, int scale, long displacement, int displSize, bool isBroadcast, Register segmentPrefix)
		{
			this.SegmentPrefix = segmentPrefix;
			this.Base = Register.None;
			this.Index = index;
			this.Scale = scale;
			this.Displacement = displacement;
			this.DisplSize = displSize;
			this.IsBroadcast = isBroadcast;
		}

		// Token: 0x06002360 RID: 9056 RVA: 0x00072718 File Offset: 0x00070918
		public MemoryOperand(Register @base, long displacement, bool isBroadcast, Register segmentPrefix)
		{
			this.SegmentPrefix = segmentPrefix;
			this.Base = @base;
			this.Index = Register.None;
			this.Scale = 1;
			this.Displacement = displacement;
			this.DisplSize = 1;
			this.IsBroadcast = isBroadcast;
		}

		// Token: 0x06002361 RID: 9057 RVA: 0x0007274C File Offset: 0x0007094C
		public MemoryOperand(Register @base, Register index, int scale, long displacement, int displSize)
		{
			this.SegmentPrefix = Register.None;
			this.Base = @base;
			this.Index = index;
			this.Scale = scale;
			this.Displacement = displacement;
			this.DisplSize = displSize;
			this.IsBroadcast = false;
		}

		// Token: 0x06002362 RID: 9058 RVA: 0x00072781 File Offset: 0x00070981
		public MemoryOperand(Register @base, Register index, int scale)
		{
			this.SegmentPrefix = Register.None;
			this.Base = @base;
			this.Index = index;
			this.Scale = scale;
			this.Displacement = 0L;
			this.DisplSize = 0;
			this.IsBroadcast = false;
		}

		// Token: 0x06002363 RID: 9059 RVA: 0x000727B5 File Offset: 0x000709B5
		public MemoryOperand(Register @base, Register index)
		{
			this.SegmentPrefix = Register.None;
			this.Base = @base;
			this.Index = index;
			this.Scale = 1;
			this.Displacement = 0L;
			this.DisplSize = 0;
			this.IsBroadcast = false;
		}

		// Token: 0x06002364 RID: 9060 RVA: 0x000727E9 File Offset: 0x000709E9
		public MemoryOperand(Register @base, long displacement, int displSize)
		{
			this.SegmentPrefix = Register.None;
			this.Base = @base;
			this.Index = Register.None;
			this.Scale = 1;
			this.Displacement = displacement;
			this.DisplSize = displSize;
			this.IsBroadcast = false;
		}

		// Token: 0x06002365 RID: 9061 RVA: 0x0007281C File Offset: 0x00070A1C
		public MemoryOperand(Register index, int scale, long displacement, int displSize)
		{
			this.SegmentPrefix = Register.None;
			this.Base = Register.None;
			this.Index = index;
			this.Scale = scale;
			this.Displacement = displacement;
			this.DisplSize = displSize;
			this.IsBroadcast = false;
		}

		// Token: 0x06002366 RID: 9062 RVA: 0x00072850 File Offset: 0x00070A50
		public MemoryOperand(Register @base, long displacement)
		{
			this.SegmentPrefix = Register.None;
			this.Base = @base;
			this.Index = Register.None;
			this.Scale = 1;
			this.Displacement = displacement;
			this.DisplSize = 1;
			this.IsBroadcast = false;
		}

		// Token: 0x06002367 RID: 9063 RVA: 0x00072883 File Offset: 0x00070A83
		public MemoryOperand(Register @base)
		{
			this.SegmentPrefix = Register.None;
			this.Base = @base;
			this.Index = Register.None;
			this.Scale = 1;
			this.Displacement = 0L;
			this.DisplSize = 0;
			this.IsBroadcast = false;
		}

		// Token: 0x06002368 RID: 9064 RVA: 0x000728B7 File Offset: 0x00070AB7
		public MemoryOperand(ulong displacement, int displSize)
		{
			this.SegmentPrefix = Register.None;
			this.Base = Register.None;
			this.Index = Register.None;
			this.Scale = 1;
			this.Displacement = (long)displacement;
			this.DisplSize = displSize;
			this.IsBroadcast = false;
		}

		// Token: 0x04002AFA RID: 11002
		public readonly Register SegmentPrefix;

		// Token: 0x04002AFB RID: 11003
		public readonly Register Base;

		// Token: 0x04002AFC RID: 11004
		public readonly Register Index;

		// Token: 0x04002AFD RID: 11005
		public readonly int Scale;

		// Token: 0x04002AFE RID: 11006
		public readonly long Displacement;

		// Token: 0x04002AFF RID: 11007
		public readonly int DisplSize;

		// Token: 0x04002B00 RID: 11008
		public readonly bool IsBroadcast;
	}
}
