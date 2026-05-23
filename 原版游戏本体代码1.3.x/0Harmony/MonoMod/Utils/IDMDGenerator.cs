using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace MonoMod.Utils
{
	// Token: 0x0200087C RID: 2172
	[NullableContext(1)]
	internal interface IDMDGenerator
	{
		// Token: 0x06002CA5 RID: 11429
		MethodInfo Generate(DynamicMethodDefinition dmd, [Nullable(2)] object context);
	}
}
