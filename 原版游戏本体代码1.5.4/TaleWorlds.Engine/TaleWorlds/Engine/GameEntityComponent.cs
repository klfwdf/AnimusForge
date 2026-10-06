using System;
using TaleWorlds.DotNet;

namespace TaleWorlds.Engine;

[EngineClass("rglEntity_component")]
public abstract class GameEntityComponent : NativeObject
{
	internal GameEntityComponent(UIntPtr pointer)
	{
		Construct(pointer);
	}

	public WeakGameEntity GetEntity()
	{
		return new WeakGameEntity(EngineApplicationInterface.IGameEntityComponent.GetEntityPointer(base.Pointer));
	}

	public virtual MetaMesh GetFirstMetaMesh()
	{
		return EngineApplicationInterface.IGameEntityComponent.GetFirstMetaMesh(this);
	}
}
