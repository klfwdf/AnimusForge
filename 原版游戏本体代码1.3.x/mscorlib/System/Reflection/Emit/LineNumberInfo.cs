using System;
using System.Diagnostics.SymbolStore;

namespace System.Reflection.Emit
{
	// Token: 0x02000642 RID: 1602
	internal sealed class LineNumberInfo
	{
		// Token: 0x06004AFD RID: 19197 RVA: 0x0010FAC7 File Offset: 0x0010DCC7
		internal LineNumberInfo()
		{
			this.m_DocumentCount = 0;
			this.m_iLastFound = 0;
		}

		// Token: 0x06004AFE RID: 19198 RVA: 0x0010FAE0 File Offset: 0x0010DCE0
		internal void AddLineNumberInfo(ISymbolDocumentWriter document, int iOffset, int iStartLine, int iStartColumn, int iEndLine, int iEndColumn)
		{
			int num = this.FindDocument(document);
			this.m_Documents[num].AddLineNumberInfo(document, iOffset, iStartLine, iStartColumn, iEndLine, iEndColumn);
		}

		// Token: 0x06004AFF RID: 19199 RVA: 0x0010FB0C File Offset: 0x0010DD0C
		private int FindDocument(ISymbolDocumentWriter document)
		{
			if (this.m_iLastFound < this.m_DocumentCount && this.m_Documents[this.m_iLastFound].m_document == document)
			{
				return this.m_iLastFound;
			}
			for (int i = 0; i < this.m_DocumentCount; i++)
			{
				if (this.m_Documents[i].m_document == document)
				{
					this.m_iLastFound = i;
					return this.m_iLastFound;
				}
			}
			this.EnsureCapacity();
			this.m_iLastFound = this.m_DocumentCount;
			this.m_Documents[this.m_iLastFound] = new REDocument(document);
			checked
			{
				this.m_DocumentCount++;
				return this.m_iLastFound;
			}
		}

		// Token: 0x06004B00 RID: 19200 RVA: 0x0010FBAC File Offset: 0x0010DDAC
		private void EnsureCapacity()
		{
			if (this.m_DocumentCount == 0)
			{
				this.m_Documents = new REDocument[16];
				return;
			}
			if (this.m_DocumentCount == this.m_Documents.Length)
			{
				REDocument[] array = new REDocument[this.m_DocumentCount * 2];
				Array.Copy(this.m_Documents, array, this.m_DocumentCount);
				this.m_Documents = array;
			}
		}

		// Token: 0x06004B01 RID: 19201 RVA: 0x0010FC08 File Offset: 0x0010DE08
		internal void EmitLineNumberInfo(ISymbolWriter symWriter)
		{
			for (int i = 0; i < this.m_DocumentCount; i++)
			{
				this.m_Documents[i].EmitLineNumberInfo(symWriter);
			}
		}

		// Token: 0x04001EFA RID: 7930
		private int m_DocumentCount;

		// Token: 0x04001EFB RID: 7931
		private REDocument[] m_Documents;

		// Token: 0x04001EFC RID: 7932
		private const int InitialSize = 16;

		// Token: 0x04001EFD RID: 7933
		private int m_iLastFound;
	}
}
