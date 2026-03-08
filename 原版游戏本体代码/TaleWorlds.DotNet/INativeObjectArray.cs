using System;
using TaleWorlds.Library;

namespace TaleWorlds.DotNet
{
	// Token: 0x0200001B RID: 27
	[LibraryInterfaceBase]
	internal interface INativeObjectArray
	{
		// Token: 0x06000083 RID: 131
		[EngineMethod("create", false, null, false)]
		NativeObjectArray Create();

		// Token: 0x06000084 RID: 132
		[EngineMethod("get_count", false, null, false)]
		int GetCount(UIntPtr pointer);

		// Token: 0x06000085 RID: 133
		[EngineMethod("add_element", false, null, false)]
		void AddElement(UIntPtr pointer, UIntPtr nativeObject);

		// Token: 0x06000086 RID: 134
		[EngineMethod("get_element_at_index", false, null, false)]
		NativeObject GetElementAtIndex(UIntPtr pointer, int index);

		// Token: 0x06000087 RID: 135
		[EngineMethod("clear", false, null, false)]
		void Clear(UIntPtr pointer);
	}
}
