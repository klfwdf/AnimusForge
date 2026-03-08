using System;
using System.Runtime.CompilerServices;
using Mono.Cecil;

namespace MonoMod.Utils
{
	// Token: 0x020008CC RID: 2252
	[NullableContext(1)]
	internal interface ICallSiteGenerator
	{
		// Token: 0x06002EC8 RID: 11976
		Mono.Cecil.CallSite ToCallSite(ModuleDefinition module);
	}
}
