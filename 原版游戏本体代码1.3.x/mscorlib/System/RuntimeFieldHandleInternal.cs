using System;
using System.Security;

namespace System
{
	// Token: 0x02000135 RID: 309
	internal struct RuntimeFieldHandleInternal
	{
		// Token: 0x170001F5 RID: 501
		// (get) Token: 0x06001281 RID: 4737 RVA: 0x00037804 File Offset: 0x00035A04
		internal static RuntimeFieldHandleInternal EmptyHandle
		{
			get
			{
				return default(RuntimeFieldHandleInternal);
			}
		}

		// Token: 0x06001282 RID: 4738 RVA: 0x0003781A File Offset: 0x00035A1A
		internal bool IsNullHandle()
		{
			return this.m_handle.IsNull();
		}

		// Token: 0x170001F6 RID: 502
		// (get) Token: 0x06001283 RID: 4739 RVA: 0x00037827 File Offset: 0x00035A27
		internal IntPtr Value
		{
			[SecurityCritical]
			get
			{
				return this.m_handle;
			}
		}

		// Token: 0x06001284 RID: 4740 RVA: 0x0003782F File Offset: 0x00035A2F
		[SecurityCritical]
		internal RuntimeFieldHandleInternal(IntPtr value)
		{
			this.m_handle = value;
		}

		// Token: 0x0400066C RID: 1644
		internal IntPtr m_handle;
	}
}
