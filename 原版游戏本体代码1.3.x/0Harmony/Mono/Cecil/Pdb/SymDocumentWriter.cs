using System;

namespace Mono.Cecil.Pdb
{
	// Token: 0x02000369 RID: 873
	internal class SymDocumentWriter
	{
		// Token: 0x17000584 RID: 1412
		// (get) Token: 0x06001730 RID: 5936 RVA: 0x000478B7 File Offset: 0x00045AB7
		public ISymUnmanagedDocumentWriter Writer
		{
			get
			{
				return this.writer;
			}
		}

		// Token: 0x06001731 RID: 5937 RVA: 0x000478BF File Offset: 0x00045ABF
		public SymDocumentWriter(ISymUnmanagedDocumentWriter writer)
		{
			this.writer = writer;
		}

		// Token: 0x06001732 RID: 5938 RVA: 0x000478CE File Offset: 0x00045ACE
		public void SetSource(byte[] source)
		{
			this.writer.SetSource((uint)source.Length, source);
		}

		// Token: 0x06001733 RID: 5939 RVA: 0x000478DF File Offset: 0x00045ADF
		public void SetCheckSum(Guid hashAlgo, byte[] checkSum)
		{
			this.writer.SetCheckSum(hashAlgo, (uint)checkSum.Length, checkSum);
		}

		// Token: 0x04000B55 RID: 2901
		private readonly ISymUnmanagedDocumentWriter writer;
	}
}
