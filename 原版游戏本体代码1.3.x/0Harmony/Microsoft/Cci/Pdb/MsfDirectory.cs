using System;

namespace Microsoft.Cci.Pdb
{
	// Token: 0x02000432 RID: 1074
	internal class MsfDirectory
	{
		// Token: 0x06001785 RID: 6021 RVA: 0x00048C64 File Offset: 0x00046E64
		internal MsfDirectory(PdbReader reader, PdbFileHeader head, BitAccess bits)
		{
			int pages = reader.PagesFromSize(head.directorySize);
			bits.MinCapacity(head.directorySize);
			int directoryRootPages = head.directoryRoot.Length;
			int pagesPerPage = head.pageSize / 4;
			int pagesToGo = pages;
			for (int i = 0; i < directoryRootPages; i++)
			{
				int pagesInThisPage = ((pagesToGo <= pagesPerPage) ? pagesToGo : pagesPerPage);
				reader.Seek(head.directoryRoot[i], 0);
				bits.Append(reader.reader, pagesInThisPage * 4);
				pagesToGo -= pagesInThisPage;
			}
			bits.Position = 0;
			DataStream dataStream = new DataStream(head.directorySize, bits, pages);
			bits.MinCapacity(head.directorySize);
			dataStream.Read(reader, bits);
			int count;
			bits.ReadInt32(out count);
			int[] sizes = new int[count];
			bits.ReadInt32(sizes);
			this.streams = new DataStream[count];
			for (int j = 0; j < count; j++)
			{
				if (sizes[j] <= 0)
				{
					this.streams[j] = new DataStream();
				}
				else
				{
					this.streams[j] = new DataStream(sizes[j], bits, reader.PagesFromSize(sizes[j]));
				}
			}
		}

		// Token: 0x04000FFE RID: 4094
		internal DataStream[] streams;
	}
}
