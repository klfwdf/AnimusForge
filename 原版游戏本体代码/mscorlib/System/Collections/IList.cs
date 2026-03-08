using System;
using System.Runtime.InteropServices;

namespace System.Collections
{
	// Token: 0x020004A2 RID: 1186
	[ComVisible(true)]
	[__DynamicallyInvokable]
	public interface IList : ICollection, IEnumerable
	{
		// Token: 0x17000879 RID: 2169
		[__DynamicallyInvokable]
		object this[int index]
		{
			[__DynamicallyInvokable]
			get;
			[__DynamicallyInvokable]
			set;
		}

		// Token: 0x060038C4 RID: 14532
		[__DynamicallyInvokable]
		int Add(object value);

		// Token: 0x060038C5 RID: 14533
		[__DynamicallyInvokable]
		bool Contains(object value);

		// Token: 0x060038C6 RID: 14534
		[__DynamicallyInvokable]
		void Clear();

		// Token: 0x1700087A RID: 2170
		// (get) Token: 0x060038C7 RID: 14535
		[__DynamicallyInvokable]
		bool IsReadOnly
		{
			[__DynamicallyInvokable]
			get;
		}

		// Token: 0x1700087B RID: 2171
		// (get) Token: 0x060038C8 RID: 14536
		[__DynamicallyInvokable]
		bool IsFixedSize
		{
			[__DynamicallyInvokable]
			get;
		}

		// Token: 0x060038C9 RID: 14537
		[__DynamicallyInvokable]
		int IndexOf(object value);

		// Token: 0x060038CA RID: 14538
		[__DynamicallyInvokable]
		void Insert(int index, object value);

		// Token: 0x060038CB RID: 14539
		[__DynamicallyInvokable]
		void Remove(object value);

		// Token: 0x060038CC RID: 14540
		[__DynamicallyInvokable]
		void RemoveAt(int index);
	}
}
