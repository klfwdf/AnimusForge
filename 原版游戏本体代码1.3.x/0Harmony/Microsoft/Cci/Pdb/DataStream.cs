using System;

namespace Microsoft.Cci.Pdb
{
	// Token: 0x02000427 RID: 1063
	internal class DataStream
	{
		// Token: 0x06001768 RID: 5992 RVA: 0x00002B15 File Offset: 0x00000D15
		internal DataStream()
		{
		}

		// Token: 0x06001769 RID: 5993 RVA: 0x000482DD File Offset: 0x000464DD
		internal DataStream(int contentSize, BitAccess bits, int count)
		{
			this.contentSize = contentSize;
			if (count > 0)
			{
				this.pages = new int[count];
				bits.ReadInt32(this.pages);
			}
		}

		// Token: 0x0600176A RID: 5994 RVA: 0x00048308 File Offset: 0x00046508
		internal void Read(PdbReader reader, BitAccess bits)
		{
			bits.MinCapacity(this.contentSize);
			this.Read(reader, 0, bits.Buffer, 0, this.contentSize);
		}

		// Token: 0x0600176B RID: 5995 RVA: 0x0004832C File Offset: 0x0004652C
		internal void Read(PdbReader reader, int position, byte[] bytes, int offset, int data)
		{
			if (position + data > this.contentSize)
			{
				throw new PdbException("DataStream can't read off end of stream. (pos={0},siz={1})", new object[] { position, data });
			}
			if (position == this.contentSize)
			{
				return;
			}
			int left = data;
			int page = position / reader.pageSize;
			int rema = position % reader.pageSize;
			if (rema != 0)
			{
				int todo = reader.pageSize - rema;
				if (todo > left)
				{
					todo = left;
				}
				reader.Seek(this.pages[page], rema);
				reader.Read(bytes, offset, todo);
				offset += todo;
				left -= todo;
				page++;
			}
			while (left > 0)
			{
				int todo2 = reader.pageSize;
				if (todo2 > left)
				{
					todo2 = left;
				}
				reader.Seek(this.pages[page], 0);
				reader.Read(bytes, offset, todo2);
				offset += todo2;
				left -= todo2;
				page++;
			}
		}

		// Token: 0x17000588 RID: 1416
		// (get) Token: 0x0600176C RID: 5996 RVA: 0x00048401 File Offset: 0x00046601
		internal int Length
		{
			get
			{
				return this.contentSize;
			}
		}

		// Token: 0x04000FBD RID: 4029
		internal int contentSize;

		// Token: 0x04000FBE RID: 4030
		internal int[] pages;
	}
}
