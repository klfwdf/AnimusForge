using System;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x02000028 RID: 40
	[ApplicationInterfaceBase]
	internal interface IGameEntityComponent
	{
		// Token: 0x060002A1 RID: 673
		[EngineMethod("get_entity", false, null, false)]
		GameEntity GetEntity(GameEntityComponent entityComponent);

		// Token: 0x060002A2 RID: 674
		[EngineMethod("get_entity_pointer", false, null, false)]
		UIntPtr GetEntityPointer(UIntPtr componentPointer);

		// Token: 0x060002A3 RID: 675
		[EngineMethod("get_first_meta_mesh", false, null, false)]
		MetaMesh GetFirstMetaMesh(GameEntityComponent entityComponent);
	}
}
