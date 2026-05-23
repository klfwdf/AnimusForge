using System;
using TaleWorlds.Library;

namespace TaleWorlds.DotNet
{
	// Token: 0x02000017 RID: 23
	[LibraryInterfaceBase]
	internal interface IManaged
	{
		// Token: 0x0600006E RID: 110
		[EngineMethod("increase_reference_count", false, null, true)]
		void IncreaseReferenceCount(UIntPtr ptr);

		// Token: 0x0600006F RID: 111
		[EngineMethod("decrease_reference_count", false, null, true)]
		void DecreaseReferenceCount(UIntPtr ptr);

		// Token: 0x06000070 RID: 112
		[EngineMethod("get_class_type_definition_count", false, null, false)]
		int GetClassTypeDefinitionCount();

		// Token: 0x06000071 RID: 113
		[EngineMethod("get_class_type_definition", false, null, false)]
		void GetClassTypeDefinition(int index, ref EngineClassTypeDefinition engineClassTypeDefinition);

		// Token: 0x06000072 RID: 114
		[EngineMethod("release_managed_object", false, null, false)]
		void ReleaseManagedObject(UIntPtr ptr);
	}
}
