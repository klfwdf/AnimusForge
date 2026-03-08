using System;
using System.Collections;

namespace System.IO.IsolatedStorage
{
	// Token: 0x020001B9 RID: 441
	internal sealed class TwoLevelFileEnumerator : IEnumerator
	{
		// Token: 0x06001BC6 RID: 7110 RVA: 0x0005F702 File Offset: 0x0005D902
		public TwoLevelFileEnumerator(string root)
		{
			this.m_Root = root;
			this.Reset();
		}

		// Token: 0x06001BC7 RID: 7111 RVA: 0x0005F718 File Offset: 0x0005D918
		public bool MoveNext()
		{
			lock (this)
			{
				if (this.m_fReset)
				{
					this.m_fReset = false;
					return this.AdvanceRootDir();
				}
				if (this.m_RootDir.Length == 0)
				{
					return false;
				}
				this.m_nSubDir++;
				if (this.m_nSubDir >= this.m_SubDir.Length)
				{
					this.m_nSubDir = this.m_SubDir.Length;
					return this.AdvanceRootDir();
				}
				this.UpdateCurrent();
			}
			return true;
		}

		// Token: 0x06001BC8 RID: 7112 RVA: 0x0005F7B0 File Offset: 0x0005D9B0
		private bool AdvanceRootDir()
		{
			this.m_nRootDir++;
			if (this.m_nRootDir >= this.m_RootDir.Length)
			{
				this.m_nRootDir = this.m_RootDir.Length;
				return false;
			}
			this.m_SubDir = Directory.GetDirectories(this.m_RootDir[this.m_nRootDir]);
			if (this.m_SubDir.Length == 0)
			{
				return this.AdvanceRootDir();
			}
			this.m_nSubDir = 0;
			this.UpdateCurrent();
			return true;
		}

		// Token: 0x06001BC9 RID: 7113 RVA: 0x0005F821 File Offset: 0x0005DA21
		private void UpdateCurrent()
		{
			this.m_Current.Path1 = Path.GetFileName(this.m_RootDir[this.m_nRootDir]);
			this.m_Current.Path2 = Path.GetFileName(this.m_SubDir[this.m_nSubDir]);
		}

		// Token: 0x17000324 RID: 804
		// (get) Token: 0x06001BCA RID: 7114 RVA: 0x0005F85D File Offset: 0x0005DA5D
		public object Current
		{
			get
			{
				if (this.m_fReset)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumNotStarted"));
				}
				if (this.m_nRootDir >= this.m_RootDir.Length)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumEnded"));
				}
				return this.m_Current;
			}
		}

		// Token: 0x06001BCB RID: 7115 RVA: 0x0005F8A0 File Offset: 0x0005DAA0
		public void Reset()
		{
			this.m_RootDir = null;
			this.m_nRootDir = -1;
			this.m_SubDir = null;
			this.m_nSubDir = -1;
			this.m_Current = new TwoPaths();
			this.m_fReset = true;
			this.m_RootDir = Directory.GetDirectories(this.m_Root);
		}

		// Token: 0x0400099E RID: 2462
		private string m_Root;

		// Token: 0x0400099F RID: 2463
		private TwoPaths m_Current;

		// Token: 0x040009A0 RID: 2464
		private bool m_fReset;

		// Token: 0x040009A1 RID: 2465
		private string[] m_RootDir;

		// Token: 0x040009A2 RID: 2466
		private int m_nRootDir;

		// Token: 0x040009A3 RID: 2467
		private string[] m_SubDir;

		// Token: 0x040009A4 RID: 2468
		private int m_nSubDir;
	}
}
