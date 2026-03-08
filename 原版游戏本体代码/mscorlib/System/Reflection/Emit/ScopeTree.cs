using System;
using System.Diagnostics.SymbolStore;

namespace System.Reflection.Emit
{
	// Token: 0x02000641 RID: 1601
	internal sealed class ScopeTree
	{
		// Token: 0x06004AF6 RID: 19190 RVA: 0x0010F84A File Offset: 0x0010DA4A
		internal ScopeTree()
		{
			this.m_iOpenScopeCount = 0;
			this.m_iCount = 0;
		}

		// Token: 0x06004AF7 RID: 19191 RVA: 0x0010F860 File Offset: 0x0010DA60
		internal int GetCurrentActiveScopeIndex()
		{
			int num = 0;
			int num2 = this.m_iCount - 1;
			if (this.m_iCount == 0)
			{
				return -1;
			}
			while (num > 0 || this.m_ScopeActions[num2] == ScopeAction.Close)
			{
				if (this.m_ScopeActions[num2] == ScopeAction.Open)
				{
					num--;
				}
				else
				{
					num++;
				}
				num2--;
			}
			return num2;
		}

		// Token: 0x06004AF8 RID: 19192 RVA: 0x0010F8AC File Offset: 0x0010DAAC
		internal void AddLocalSymInfoToCurrentScope(string strName, byte[] signature, int slot, int startOffset, int endOffset)
		{
			int currentActiveScopeIndex = this.GetCurrentActiveScopeIndex();
			if (this.m_localSymInfos[currentActiveScopeIndex] == null)
			{
				this.m_localSymInfos[currentActiveScopeIndex] = new LocalSymInfo();
			}
			this.m_localSymInfos[currentActiveScopeIndex].AddLocalSymInfo(strName, signature, slot, startOffset, endOffset);
		}

		// Token: 0x06004AF9 RID: 19193 RVA: 0x0010F8EC File Offset: 0x0010DAEC
		internal void AddUsingNamespaceToCurrentScope(string strNamespace)
		{
			int currentActiveScopeIndex = this.GetCurrentActiveScopeIndex();
			if (this.m_localSymInfos[currentActiveScopeIndex] == null)
			{
				this.m_localSymInfos[currentActiveScopeIndex] = new LocalSymInfo();
			}
			this.m_localSymInfos[currentActiveScopeIndex].AddUsingNamespace(strNamespace);
		}

		// Token: 0x06004AFA RID: 19194 RVA: 0x0010F928 File Offset: 0x0010DB28
		internal void AddScopeInfo(ScopeAction sa, int iOffset)
		{
			if (sa == ScopeAction.Close && this.m_iOpenScopeCount <= 0)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_UnmatchingSymScope"));
			}
			this.EnsureCapacity();
			this.m_ScopeActions[this.m_iCount] = sa;
			this.m_iOffsets[this.m_iCount] = iOffset;
			this.m_localSymInfos[this.m_iCount] = null;
			checked
			{
				this.m_iCount++;
			}
			if (sa == ScopeAction.Open)
			{
				this.m_iOpenScopeCount++;
				return;
			}
			this.m_iOpenScopeCount--;
		}

		// Token: 0x06004AFB RID: 19195 RVA: 0x0010F9B0 File Offset: 0x0010DBB0
		internal void EnsureCapacity()
		{
			if (this.m_iCount == 0)
			{
				this.m_iOffsets = new int[16];
				this.m_ScopeActions = new ScopeAction[16];
				this.m_localSymInfos = new LocalSymInfo[16];
				return;
			}
			if (this.m_iCount == this.m_iOffsets.Length)
			{
				int num = checked(this.m_iCount * 2);
				int[] array = new int[num];
				Array.Copy(this.m_iOffsets, array, this.m_iCount);
				this.m_iOffsets = array;
				ScopeAction[] array2 = new ScopeAction[num];
				Array.Copy(this.m_ScopeActions, array2, this.m_iCount);
				this.m_ScopeActions = array2;
				LocalSymInfo[] array3 = new LocalSymInfo[num];
				Array.Copy(this.m_localSymInfos, array3, this.m_iCount);
				this.m_localSymInfos = array3;
			}
		}

		// Token: 0x06004AFC RID: 19196 RVA: 0x0010FA68 File Offset: 0x0010DC68
		internal void EmitScopeTree(ISymbolWriter symWriter)
		{
			for (int i = 0; i < this.m_iCount; i++)
			{
				if (this.m_ScopeActions[i] == ScopeAction.Open)
				{
					symWriter.OpenScope(this.m_iOffsets[i]);
				}
				else
				{
					symWriter.CloseScope(this.m_iOffsets[i]);
				}
				if (this.m_localSymInfos[i] != null)
				{
					this.m_localSymInfos[i].EmitLocalSymInfo(symWriter);
				}
			}
		}

		// Token: 0x04001EF4 RID: 7924
		internal int[] m_iOffsets;

		// Token: 0x04001EF5 RID: 7925
		internal ScopeAction[] m_ScopeActions;

		// Token: 0x04001EF6 RID: 7926
		internal int m_iCount;

		// Token: 0x04001EF7 RID: 7927
		internal int m_iOpenScopeCount;

		// Token: 0x04001EF8 RID: 7928
		internal const int InitialSize = 16;

		// Token: 0x04001EF9 RID: 7929
		internal LocalSymInfo[] m_localSymInfos;
	}
}
