using System;

namespace Mono.Cecil
{
	// Token: 0x02000264 RID: 612
	internal interface IAssemblyResolver : IDisposable
	{
		// Token: 0x06000DDF RID: 3551
		AssemblyDefinition Resolve(AssemblyNameReference name);

		// Token: 0x06000DE0 RID: 3552
		AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters);
	}
}
