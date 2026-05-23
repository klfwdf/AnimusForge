using System;
using System.Runtime.InteropServices;

namespace System.Reflection
{
	// Token: 0x02000614 RID: 1556
	[ComVisible(true)]
	[__DynamicallyInvokable]
	public class LocalVariableInfo
	{
		// Token: 0x0600480C RID: 18444 RVA: 0x00106136 File Offset: 0x00104336
		[__DynamicallyInvokable]
		protected LocalVariableInfo()
		{
		}

		// Token: 0x0600480D RID: 18445 RVA: 0x00106140 File Offset: 0x00104340
		[__DynamicallyInvokable]
		public override string ToString()
		{
			string text = this.LocalType.ToString() + " (" + this.LocalIndex.ToString() + ")";
			if (this.IsPinned)
			{
				text += " (pinned)";
			}
			return text;
		}

		// Token: 0x17000B22 RID: 2850
		// (get) Token: 0x0600480E RID: 18446 RVA: 0x0010618B File Offset: 0x0010438B
		[__DynamicallyInvokable]
		public virtual Type LocalType
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_type;
			}
		}

		// Token: 0x17000B23 RID: 2851
		// (get) Token: 0x0600480F RID: 18447 RVA: 0x00106193 File Offset: 0x00104393
		[__DynamicallyInvokable]
		public virtual bool IsPinned
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_isPinned != 0;
			}
		}

		// Token: 0x17000B24 RID: 2852
		// (get) Token: 0x06004810 RID: 18448 RVA: 0x0010619E File Offset: 0x0010439E
		[__DynamicallyInvokable]
		public virtual int LocalIndex
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_localIndex;
			}
		}

		// Token: 0x04001DDE RID: 7646
		private RuntimeType m_type;

		// Token: 0x04001DDF RID: 7647
		private int m_isPinned;

		// Token: 0x04001DE0 RID: 7648
		private int m_localIndex;
	}
}
