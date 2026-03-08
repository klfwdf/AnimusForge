using System;

namespace System.Runtime
{
	// Token: 0x02000719 RID: 1817
	[AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
	public sealed class TargetedPatchingOptOutAttribute : Attribute
	{
		// Token: 0x06005132 RID: 20786 RVA: 0x0011E6E6 File Offset: 0x0011C8E6
		public TargetedPatchingOptOutAttribute(string reason)
		{
			this.m_reason = reason;
		}

		// Token: 0x17000D5A RID: 3418
		// (get) Token: 0x06005133 RID: 20787 RVA: 0x0011E6F5 File Offset: 0x0011C8F5
		public string Reason
		{
			get
			{
				return this.m_reason;
			}
		}

		// Token: 0x06005134 RID: 20788 RVA: 0x0011E6FD File Offset: 0x0011C8FD
		private TargetedPatchingOptOutAttribute()
		{
		}

		// Token: 0x040023FD RID: 9213
		private string m_reason;
	}
}
