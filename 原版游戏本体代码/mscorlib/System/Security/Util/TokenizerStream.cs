using System;

namespace System.Security.Util
{
	// Token: 0x02000388 RID: 904
	internal sealed class TokenizerStream
	{
		// Token: 0x06002CD0 RID: 11472 RVA: 0x000A8F91 File Offset: 0x000A7191
		internal TokenizerStream()
		{
			this.m_countTokens = 0;
			this.m_headTokens = new TokenizerShortBlock();
			this.m_headStrings = new TokenizerStringBlock();
			this.Reset();
		}

		// Token: 0x06002CD1 RID: 11473 RVA: 0x000A8FBC File Offset: 0x000A71BC
		internal void AddToken(short token)
		{
			if (this.m_currentTokens.m_block.Length <= this.m_indexTokens)
			{
				this.m_currentTokens.m_next = new TokenizerShortBlock();
				this.m_currentTokens = this.m_currentTokens.m_next;
				this.m_indexTokens = 0;
			}
			this.m_countTokens++;
			short[] block = this.m_currentTokens.m_block;
			int indexTokens = this.m_indexTokens;
			this.m_indexTokens = indexTokens + 1;
			block[indexTokens] = token;
		}

		// Token: 0x06002CD2 RID: 11474 RVA: 0x000A9034 File Offset: 0x000A7234
		internal void AddString(string str)
		{
			if (this.m_currentStrings.m_block.Length <= this.m_indexStrings)
			{
				this.m_currentStrings.m_next = new TokenizerStringBlock();
				this.m_currentStrings = this.m_currentStrings.m_next;
				this.m_indexStrings = 0;
			}
			string[] block = this.m_currentStrings.m_block;
			int indexStrings = this.m_indexStrings;
			this.m_indexStrings = indexStrings + 1;
			block[indexStrings] = str;
		}

		// Token: 0x06002CD3 RID: 11475 RVA: 0x000A909C File Offset: 0x000A729C
		internal void Reset()
		{
			this.m_lastTokens = null;
			this.m_currentTokens = this.m_headTokens;
			this.m_currentStrings = this.m_headStrings;
			this.m_indexTokens = 0;
			this.m_indexStrings = 0;
		}

		// Token: 0x06002CD4 RID: 11476 RVA: 0x000A90CC File Offset: 0x000A72CC
		internal short GetNextFullToken()
		{
			if (this.m_currentTokens.m_block.Length <= this.m_indexTokens)
			{
				this.m_lastTokens = this.m_currentTokens;
				this.m_currentTokens = this.m_currentTokens.m_next;
				this.m_indexTokens = 0;
			}
			short[] block = this.m_currentTokens.m_block;
			int indexTokens = this.m_indexTokens;
			this.m_indexTokens = indexTokens + 1;
			return block[indexTokens];
		}

		// Token: 0x06002CD5 RID: 11477 RVA: 0x000A9130 File Offset: 0x000A7330
		internal short GetNextToken()
		{
			return this.GetNextFullToken() & 255;
		}

		// Token: 0x06002CD6 RID: 11478 RVA: 0x000A914C File Offset: 0x000A734C
		internal string GetNextString()
		{
			if (this.m_currentStrings.m_block.Length <= this.m_indexStrings)
			{
				this.m_currentStrings = this.m_currentStrings.m_next;
				this.m_indexStrings = 0;
			}
			string[] block = this.m_currentStrings.m_block;
			int indexStrings = this.m_indexStrings;
			this.m_indexStrings = indexStrings + 1;
			return block[indexStrings];
		}

		// Token: 0x06002CD7 RID: 11479 RVA: 0x000A91A3 File Offset: 0x000A73A3
		internal void ThrowAwayNextString()
		{
			this.GetNextString();
		}

		// Token: 0x06002CD8 RID: 11480 RVA: 0x000A91AC File Offset: 0x000A73AC
		internal void TagLastToken(short tag)
		{
			if (this.m_indexTokens == 0)
			{
				this.m_lastTokens.m_block[this.m_lastTokens.m_block.Length - 1] = (short)((ushort)this.m_lastTokens.m_block[this.m_lastTokens.m_block.Length - 1] | (ushort)tag);
				return;
			}
			this.m_currentTokens.m_block[this.m_indexTokens - 1] = (short)((ushort)this.m_currentTokens.m_block[this.m_indexTokens - 1] | (ushort)tag);
		}

		// Token: 0x06002CD9 RID: 11481 RVA: 0x000A922A File Offset: 0x000A742A
		internal int GetTokenCount()
		{
			return this.m_countTokens;
		}

		// Token: 0x06002CDA RID: 11482 RVA: 0x000A9234 File Offset: 0x000A7434
		internal void GoToPosition(int position)
		{
			this.Reset();
			for (int i = 0; i < position; i++)
			{
				if (this.GetNextToken() == 3)
				{
					this.ThrowAwayNextString();
				}
			}
		}

		// Token: 0x04001222 RID: 4642
		private int m_countTokens;

		// Token: 0x04001223 RID: 4643
		private TokenizerShortBlock m_headTokens;

		// Token: 0x04001224 RID: 4644
		private TokenizerShortBlock m_lastTokens;

		// Token: 0x04001225 RID: 4645
		private TokenizerShortBlock m_currentTokens;

		// Token: 0x04001226 RID: 4646
		private int m_indexTokens;

		// Token: 0x04001227 RID: 4647
		private TokenizerStringBlock m_headStrings;

		// Token: 0x04001228 RID: 4648
		private TokenizerStringBlock m_currentStrings;

		// Token: 0x04001229 RID: 4649
		private int m_indexStrings;
	}
}
