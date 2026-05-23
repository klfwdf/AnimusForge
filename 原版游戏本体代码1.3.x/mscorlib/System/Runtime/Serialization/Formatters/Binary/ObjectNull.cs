using System;
using System.Diagnostics;
using System.Security;

namespace System.Runtime.Serialization.Formatters.Binary
{
	// Token: 0x02000791 RID: 1937
	internal sealed class ObjectNull : IStreamable
	{
		// Token: 0x06005421 RID: 21537 RVA: 0x00128731 File Offset: 0x00126931
		internal ObjectNull()
		{
		}

		// Token: 0x06005422 RID: 21538 RVA: 0x00128739 File Offset: 0x00126939
		internal void SetNullCount(int nullCount)
		{
			this.nullCount = nullCount;
		}

		// Token: 0x06005423 RID: 21539 RVA: 0x00128744 File Offset: 0x00126944
		public void Write(__BinaryWriter sout)
		{
			if (this.nullCount == 1)
			{
				sout.WriteByte(10);
				return;
			}
			if (this.nullCount < 256)
			{
				sout.WriteByte(13);
				sout.WriteByte((byte)this.nullCount);
				return;
			}
			sout.WriteByte(14);
			sout.WriteInt32(this.nullCount);
		}

		// Token: 0x06005424 RID: 21540 RVA: 0x0012879A File Offset: 0x0012699A
		[SecurityCritical]
		public void Read(__BinaryParser input)
		{
			this.Read(input, BinaryHeaderEnum.ObjectNull);
		}

		// Token: 0x06005425 RID: 21541 RVA: 0x001287A8 File Offset: 0x001269A8
		public void Read(__BinaryParser input, BinaryHeaderEnum binaryHeaderEnum)
		{
			switch (binaryHeaderEnum)
			{
			case BinaryHeaderEnum.ObjectNull:
				this.nullCount = 1;
				return;
			case BinaryHeaderEnum.MessageEnd:
			case BinaryHeaderEnum.Assembly:
				break;
			case BinaryHeaderEnum.ObjectNullMultiple256:
				this.nullCount = (int)input.ReadByte();
				return;
			case BinaryHeaderEnum.ObjectNullMultiple:
				this.nullCount = input.ReadInt32();
				break;
			default:
				return;
			}
		}

		// Token: 0x06005426 RID: 21542 RVA: 0x001287F4 File Offset: 0x001269F4
		public void Dump()
		{
		}

		// Token: 0x06005427 RID: 21543 RVA: 0x001287F6 File Offset: 0x001269F6
		[Conditional("_LOGGING")]
		private void DumpInternal()
		{
			if (BCLDebug.CheckEnabled("BINARY") && this.nullCount != 1)
			{
				int num = this.nullCount;
			}
		}

		// Token: 0x04002603 RID: 9731
		internal int nullCount;
	}
}
