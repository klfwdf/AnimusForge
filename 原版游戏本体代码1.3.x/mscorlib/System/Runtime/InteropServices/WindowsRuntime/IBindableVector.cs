using System;

namespace System.Runtime.InteropServices.WindowsRuntime
{
	// Token: 0x02000A1C RID: 2588
	[Guid("393de7de-6fd0-4c0d-bb71-47244a113e93")]
	[ComImport]
	internal interface IBindableVector : IBindableIterable
	{
		// Token: 0x060065E6 RID: 26086
		object GetAt(uint index);

		// Token: 0x17001182 RID: 4482
		// (get) Token: 0x060065E7 RID: 26087
		uint Size { get; }

		// Token: 0x060065E8 RID: 26088
		IBindableVectorView GetView();

		// Token: 0x060065E9 RID: 26089
		bool IndexOf(object value, out uint index);

		// Token: 0x060065EA RID: 26090
		void SetAt(uint index, object value);

		// Token: 0x060065EB RID: 26091
		void InsertAt(uint index, object value);

		// Token: 0x060065EC RID: 26092
		void RemoveAt(uint index);

		// Token: 0x060065ED RID: 26093
		void Append(object value);

		// Token: 0x060065EE RID: 26094
		void RemoveAtEnd();

		// Token: 0x060065EF RID: 26095
		void Clear();
	}
}
