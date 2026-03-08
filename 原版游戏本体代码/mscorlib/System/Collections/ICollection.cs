using System;
using System.Runtime.InteropServices;

namespace System.Collections
{
	// Token: 0x0200049A RID: 1178
	[ComVisible(true)]
	[__DynamicallyInvokable]
	public interface ICollection : IEnumerable
	{
		// Token: 0x060038A8 RID: 14504
		[__DynamicallyInvokable]
		void CopyTo(Array array, int index);

		// Token: 0x1700086D RID: 2157
		// (get) Token: 0x060038A9 RID: 14505
		[__DynamicallyInvokable]
		int Count
		{
			[__DynamicallyInvokable]
			get;
		}

		// Token: 0x1700086E RID: 2158
		// (get) Token: 0x060038AA RID: 14506
		[__DynamicallyInvokable]
		object SyncRoot
		{
			[__DynamicallyInvokable]
			get;
		}

		// Token: 0x1700086F RID: 2159
		// (get) Token: 0x060038AB RID: 14507
		[__DynamicallyInvokable]
		bool IsSynchronized
		{
			[__DynamicallyInvokable]
			get;
		}
	}
}
