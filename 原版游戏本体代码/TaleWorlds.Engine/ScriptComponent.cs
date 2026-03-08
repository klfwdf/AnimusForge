using System;
using TaleWorlds.DotNet;

namespace TaleWorlds.Engine
{
	// Token: 0x02000086 RID: 134
	[EngineClass("rglScript_component")]
	public abstract class ScriptComponent : NativeObject
	{
		// Token: 0x06000C11 RID: 3089 RVA: 0x0000D3CA File Offset: 0x0000B5CA
		protected ScriptComponent()
		{
		}

		// Token: 0x06000C12 RID: 3090 RVA: 0x0000D3D2 File Offset: 0x0000B5D2
		internal ScriptComponent(UIntPtr pointer)
		{
			base.Construct(pointer);
		}

		// Token: 0x06000C13 RID: 3091 RVA: 0x0000D3E1 File Offset: 0x0000B5E1
		public string GetName()
		{
			return EngineApplicationInterface.IScriptComponent.GetName(base.Pointer);
		}
	}
}
