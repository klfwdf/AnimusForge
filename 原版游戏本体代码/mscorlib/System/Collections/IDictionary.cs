using System;
using System.Runtime.InteropServices;

namespace System.Collections
{
	// Token: 0x0200049C RID: 1180
	[ComVisible(true)]
	[__DynamicallyInvokable]
	public interface IDictionary : ICollection, IEnumerable
	{
		// Token: 0x17000870 RID: 2160
		[__DynamicallyInvokable]
		object this[object key]
		{
			[__DynamicallyInvokable]
			get;
			[__DynamicallyInvokable]
			set;
		}

		// Token: 0x17000871 RID: 2161
		// (get) Token: 0x060038AF RID: 14511
		[__DynamicallyInvokable]
		ICollection Keys
		{
			[__DynamicallyInvokable]
			get;
		}

		// Token: 0x17000872 RID: 2162
		// (get) Token: 0x060038B0 RID: 14512
		[__DynamicallyInvokable]
		ICollection Values
		{
			[__DynamicallyInvokable]
			get;
		}

		// Token: 0x060038B1 RID: 14513
		[__DynamicallyInvokable]
		bool Contains(object key);

		// Token: 0x060038B2 RID: 14514
		[__DynamicallyInvokable]
		void Add(object key, object value);

		// Token: 0x060038B3 RID: 14515
		[__DynamicallyInvokable]
		void Clear();

		// Token: 0x17000873 RID: 2163
		// (get) Token: 0x060038B4 RID: 14516
		[__DynamicallyInvokable]
		bool IsReadOnly
		{
			[__DynamicallyInvokable]
			get;
		}

		// Token: 0x17000874 RID: 2164
		// (get) Token: 0x060038B5 RID: 14517
		[__DynamicallyInvokable]
		bool IsFixedSize
		{
			[__DynamicallyInvokable]
			get;
		}

		// Token: 0x060038B6 RID: 14518
		[__DynamicallyInvokable]
		IDictionaryEnumerator GetEnumerator();

		// Token: 0x060038B7 RID: 14519
		[__DynamicallyInvokable]
		void Remove(object key);
	}
}
