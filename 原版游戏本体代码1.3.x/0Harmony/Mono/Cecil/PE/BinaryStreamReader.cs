using System;
using System.IO;

namespace Mono.Cecil.PE
{
	// Token: 0x020002BD RID: 701
	internal class BinaryStreamReader : BinaryReader
	{
		// Token: 0x170004CC RID: 1228
		// (get) Token: 0x060011E9 RID: 4585 RVA: 0x00037587 File Offset: 0x00035787
		// (set) Token: 0x060011EA RID: 4586 RVA: 0x00037595 File Offset: 0x00035795
		public int Position
		{
			get
			{
				return (int)this.BaseStream.Position;
			}
			set
			{
				this.BaseStream.Position = (long)value;
			}
		}

		// Token: 0x170004CD RID: 1229
		// (get) Token: 0x060011EB RID: 4587 RVA: 0x000375A4 File Offset: 0x000357A4
		public int Length
		{
			get
			{
				return (int)this.BaseStream.Length;
			}
		}

		// Token: 0x060011EC RID: 4588 RVA: 0x000375B2 File Offset: 0x000357B2
		public BinaryStreamReader(Stream stream)
			: base(stream)
		{
		}

		// Token: 0x060011ED RID: 4589 RVA: 0x000375BB File Offset: 0x000357BB
		public void Advance(int bytes)
		{
			this.BaseStream.Seek((long)bytes, SeekOrigin.Current);
		}

		// Token: 0x060011EE RID: 4590 RVA: 0x000375CC File Offset: 0x000357CC
		public void MoveTo(uint position)
		{
			this.BaseStream.Seek((long)((ulong)position), SeekOrigin.Begin);
		}

		// Token: 0x060011EF RID: 4591 RVA: 0x000375E0 File Offset: 0x000357E0
		public void Align(int align)
		{
			align--;
			int position = this.Position;
			this.Advance(((position + align) & ~align) - position);
		}

		// Token: 0x060011F0 RID: 4592 RVA: 0x00037607 File Offset: 0x00035807
		public DataDirectory ReadDataDirectory()
		{
			return new DataDirectory(this.ReadUInt32(), this.ReadUInt32());
		}
	}
}
