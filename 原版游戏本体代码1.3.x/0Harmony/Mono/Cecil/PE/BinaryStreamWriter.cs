using System;
using System.IO;

namespace Mono.Cecil.PE
{
	// Token: 0x020002BE RID: 702
	internal class BinaryStreamWriter : BinaryWriter
	{
		// Token: 0x170004CE RID: 1230
		// (get) Token: 0x060011F1 RID: 4593 RVA: 0x0003761A File Offset: 0x0003581A
		// (set) Token: 0x060011F2 RID: 4594 RVA: 0x00037628 File Offset: 0x00035828
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

		// Token: 0x060011F3 RID: 4595 RVA: 0x00037637 File Offset: 0x00035837
		public BinaryStreamWriter(Stream stream)
			: base(stream)
		{
		}

		// Token: 0x060011F4 RID: 4596 RVA: 0x00037640 File Offset: 0x00035840
		public void WriteByte(byte value)
		{
			this.Write(value);
		}

		// Token: 0x060011F5 RID: 4597 RVA: 0x00037649 File Offset: 0x00035849
		public void WriteUInt16(ushort value)
		{
			this.Write(value);
		}

		// Token: 0x060011F6 RID: 4598 RVA: 0x00037652 File Offset: 0x00035852
		public void WriteInt16(short value)
		{
			this.Write(value);
		}

		// Token: 0x060011F7 RID: 4599 RVA: 0x0003765B File Offset: 0x0003585B
		public void WriteUInt32(uint value)
		{
			this.Write(value);
		}

		// Token: 0x060011F8 RID: 4600 RVA: 0x00037664 File Offset: 0x00035864
		public void WriteInt32(int value)
		{
			this.Write(value);
		}

		// Token: 0x060011F9 RID: 4601 RVA: 0x0003766D File Offset: 0x0003586D
		public void WriteUInt64(ulong value)
		{
			this.Write(value);
		}

		// Token: 0x060011FA RID: 4602 RVA: 0x00037676 File Offset: 0x00035876
		public void WriteBytes(byte[] bytes)
		{
			this.Write(bytes);
		}

		// Token: 0x060011FB RID: 4603 RVA: 0x0003767F File Offset: 0x0003587F
		public void WriteDataDirectory(DataDirectory directory)
		{
			this.Write(directory.VirtualAddress);
			this.Write(directory.Size);
		}

		// Token: 0x060011FC RID: 4604 RVA: 0x00037699 File Offset: 0x00035899
		public void WriteBuffer(ByteBuffer buffer)
		{
			this.Write(buffer.buffer, 0, buffer.length);
		}

		// Token: 0x060011FD RID: 4605 RVA: 0x000376AE File Offset: 0x000358AE
		protected void Advance(int bytes)
		{
			this.BaseStream.Seek((long)bytes, SeekOrigin.Current);
		}

		// Token: 0x060011FE RID: 4606 RVA: 0x000376C0 File Offset: 0x000358C0
		public void Align(int align)
		{
			align--;
			int position = this.Position;
			int bytes = ((position + align) & ~align) - position;
			for (int i = 0; i < bytes; i++)
			{
				this.WriteByte(0);
			}
		}
	}
}
