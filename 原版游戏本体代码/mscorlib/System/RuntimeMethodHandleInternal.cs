using System;
using System.Security;

namespace System
{
	// Token: 0x02000131 RID: 305
	internal struct RuntimeMethodHandleInternal
	{
		// Token: 0x170001EF RID: 495
		// (get) Token: 0x06001231 RID: 4657 RVA: 0x00037390 File Offset: 0x00035590
		internal static RuntimeMethodHandleInternal EmptyHandle
		{
			get
			{
				return default(RuntimeMethodHandleInternal);
			}
		}

		// Token: 0x06001232 RID: 4658 RVA: 0x000373A6 File Offset: 0x000355A6
		internal bool IsNullHandle()
		{
			return this.m_handle.IsNull();
		}

		// Token: 0x170001F0 RID: 496
		// (get) Token: 0x06001233 RID: 4659 RVA: 0x000373B3 File Offset: 0x000355B3
		internal IntPtr Value
		{
			[SecurityCritical]
			get
			{
				return this.m_handle;
			}
		}

		// Token: 0x06001234 RID: 4660 RVA: 0x000373BB File Offset: 0x000355BB
		[SecurityCritical]
		internal RuntimeMethodHandleInternal(IntPtr value)
		{
			this.m_handle = value;
		}

		// Token: 0x04000660 RID: 1632
		internal IntPtr m_handle;
	}
}
