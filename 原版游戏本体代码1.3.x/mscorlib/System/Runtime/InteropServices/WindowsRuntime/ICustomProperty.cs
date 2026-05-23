using System;

namespace System.Runtime.InteropServices.WindowsRuntime
{
	// Token: 0x02000A10 RID: 2576
	[Guid("30DA92C0-23E8-42A0-AE7C-734A0E5D2782")]
	[ComImport]
	internal interface ICustomProperty
	{
		// Token: 0x17001173 RID: 4467
		// (get) Token: 0x060065A6 RID: 26022
		Type Type { get; }

		// Token: 0x17001174 RID: 4468
		// (get) Token: 0x060065A7 RID: 26023
		string Name { get; }

		// Token: 0x060065A8 RID: 26024
		object GetValue(object target);

		// Token: 0x060065A9 RID: 26025
		void SetValue(object target, object value);

		// Token: 0x060065AA RID: 26026
		object GetValue(object target, object indexValue);

		// Token: 0x060065AB RID: 26027
		void SetValue(object target, object value, object indexValue);

		// Token: 0x17001175 RID: 4469
		// (get) Token: 0x060065AC RID: 26028
		bool CanWrite { get; }

		// Token: 0x17001176 RID: 4470
		// (get) Token: 0x060065AD RID: 26029
		bool CanRead { get; }
	}
}
