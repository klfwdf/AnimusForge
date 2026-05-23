using System;
using TaleWorlds.Library;

namespace TaleWorlds.DotNet
{
	// Token: 0x0200001A RID: 26
	[LibraryInterfaceBase]
	internal interface INativeArray
	{
		// Token: 0x0600007B RID: 123
		[EngineMethod("get_data_pointer_offset", false, null, false)]
		int GetDataPointerOffset();

		// Token: 0x0600007C RID: 124
		[EngineMethod("create", false, null, false)]
		NativeArray Create();

		// Token: 0x0600007D RID: 125
		[EngineMethod("get_data_size", false, null, false)]
		int GetDataSize(UIntPtr pointer);

		// Token: 0x0600007E RID: 126
		[EngineMethod("get_data_pointer", false, null, false)]
		UIntPtr GetDataPointer(UIntPtr pointer);

		// Token: 0x0600007F RID: 127
		[EngineMethod("add_integer_element", false, null, false)]
		void AddIntegerElement(UIntPtr pointer, int value);

		// Token: 0x06000080 RID: 128
		[EngineMethod("add_float_element", false, null, false)]
		void AddFloatElement(UIntPtr pointer, float value);

		// Token: 0x06000081 RID: 129
		[EngineMethod("add_element", false, null, false)]
		void AddElement(UIntPtr pointer, IntPtr element, int elementSize);

		// Token: 0x06000082 RID: 130
		[EngineMethod("clear", false, null, false)]
		void Clear(UIntPtr pointer);
	}
}
