using System;
using System.Collections;
using System.Collections.Generic;

namespace System.Runtime.InteropServices.WindowsRuntime
{
	// Token: 0x02000A19 RID: 2585
	[Guid("913337e9-11a1-4345-a3a2-4e7f956e222d")]
	[ComImport]
	internal interface IVector<T> : IIterable<T>, IEnumerable<!0>, IEnumerable
	{
		// Token: 0x060065CA RID: 26058
		T GetAt(uint index);

		// Token: 0x1700117F RID: 4479
		// (get) Token: 0x060065CB RID: 26059
		uint Size { get; }

		// Token: 0x060065CC RID: 26060
		IReadOnlyList<T> GetView();

		// Token: 0x060065CD RID: 26061
		bool IndexOf(T value, out uint index);

		// Token: 0x060065CE RID: 26062
		void SetAt(uint index, T value);

		// Token: 0x060065CF RID: 26063
		void InsertAt(uint index, T value);

		// Token: 0x060065D0 RID: 26064
		void RemoveAt(uint index);

		// Token: 0x060065D1 RID: 26065
		void Append(T value);

		// Token: 0x060065D2 RID: 26066
		void RemoveAtEnd();

		// Token: 0x060065D3 RID: 26067
		void Clear();

		// Token: 0x060065D4 RID: 26068
		uint GetMany(uint startIndex, [Out] T[] items);

		// Token: 0x060065D5 RID: 26069
		void ReplaceAll(T[] items);
	}
}
