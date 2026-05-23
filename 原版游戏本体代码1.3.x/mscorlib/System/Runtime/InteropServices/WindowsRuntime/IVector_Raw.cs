using System;
using System.Collections;
using System.Collections.Generic;

namespace System.Runtime.InteropServices.WindowsRuntime
{
	// Token: 0x02000A1A RID: 2586
	[Guid("913337e9-11a1-4345-a3a2-4e7f956e222d")]
	[ComImport]
	internal interface IVector_Raw<T> : IIterable<T>, IEnumerable<!0>, IEnumerable
	{
		// Token: 0x060065D6 RID: 26070
		T GetAt(uint index);

		// Token: 0x17001180 RID: 4480
		// (get) Token: 0x060065D7 RID: 26071
		uint Size { get; }

		// Token: 0x060065D8 RID: 26072
		IVectorView<T> GetView();

		// Token: 0x060065D9 RID: 26073
		bool IndexOf(T value, out uint index);

		// Token: 0x060065DA RID: 26074
		void SetAt(uint index, T value);

		// Token: 0x060065DB RID: 26075
		void InsertAt(uint index, T value);

		// Token: 0x060065DC RID: 26076
		void RemoveAt(uint index);

		// Token: 0x060065DD RID: 26077
		void Append(T value);

		// Token: 0x060065DE RID: 26078
		void RemoveAtEnd();

		// Token: 0x060065DF RID: 26079
		void Clear();

		// Token: 0x060065E0 RID: 26080
		uint GetMany(uint startIndex, [Out] T[] items);

		// Token: 0x060065E1 RID: 26081
		void ReplaceAll(T[] items);
	}
}
