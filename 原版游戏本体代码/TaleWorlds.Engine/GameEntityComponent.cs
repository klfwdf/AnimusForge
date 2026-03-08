using System;
using TaleWorlds.DotNet;

namespace TaleWorlds.Engine
{
	// Token: 0x0200004B RID: 75
	[EngineClass("rglEntity_component")]
	public abstract class GameEntityComponent : NativeObject
	{
		// Token: 0x060007DA RID: 2010 RVA: 0x00005BFE File Offset: 0x00003DFE
		internal GameEntityComponent(UIntPtr pointer)
		{
			base.Construct(pointer);
		}

		// Token: 0x060007DB RID: 2011 RVA: 0x00005C0D File Offset: 0x00003E0D
		public WeakGameEntity GetEntity()
		{
			return new WeakGameEntity(EngineApplicationInterface.IGameEntityComponent.GetEntityPointer(base.Pointer));
		}

		// Token: 0x060007DC RID: 2012 RVA: 0x00005C24 File Offset: 0x00003E24
		public virtual MetaMesh GetFirstMetaMesh()
		{
			return EngineApplicationInterface.IGameEntityComponent.GetFirstMetaMesh(this);
		}
	}
}
