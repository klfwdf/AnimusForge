using System;
using TaleWorlds.Library;

namespace TaleWorlds.Engine;

[ApplicationInterfaceBase]
internal interface IGameEntityComponent
{
	[EngineMethod("get_entity", false, null, false)]
	GameEntity GetEntity(GameEntityComponent entityComponent);

	[EngineMethod("get_entity_pointer", false, null, false)]
	UIntPtr GetEntityPointer(UIntPtr componentPointer);

	[EngineMethod("get_first_meta_mesh", false, null, false)]
	MetaMesh GetFirstMetaMesh(GameEntityComponent entityComponent);
}
