using System;
using TaleWorlds.DotNet;

namespace TaleWorlds.Engine
{
	// Token: 0x02000070 RID: 112
	[EngineClass("rglNative_script_component")]
	public sealed class NativeScriptComponent : ScriptComponent
	{
		// Token: 0x06000A49 RID: 2633 RVA: 0x0000A775 File Offset: 0x00008975
		internal NativeScriptComponent(UIntPtr pointer)
			: base(pointer)
		{
		}
	}
}
