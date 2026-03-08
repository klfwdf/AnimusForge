using System;
using System.Runtime.CompilerServices;
using MonoMod.Core.Interop;
using MonoMod.Utils;

namespace MonoMod.Core.Platforms.Runtimes
{
	// Token: 0x02000533 RID: 1331
	[NullableContext(1)]
	[Nullable(0)]
	internal class Core31Runtime : Core30Runtime
	{
		// Token: 0x06001DDF RID: 7647 RVA: 0x00060D86 File Offset: 0x0005EF86
		public Core31Runtime(ISystem system)
			: base(system)
		{
		}

		// Token: 0x17000679 RID: 1657
		// (get) Token: 0x06001DE0 RID: 7648 RVA: 0x00060292 File Offset: 0x0005E492
		protected override CoreCLR.InvokeCompileMethodPtr InvokeCompileMethodPtr
		{
			get
			{
				return CoreCLR.V21.InvokeCompileMethodPtr;
			}
		}

		// Token: 0x06001DE1 RID: 7649 RVA: 0x00060299 File Offset: 0x0005E499
		protected override Delegate CastCompileHookToRealType(Delegate del)
		{
			return del.CastDelegate<CoreCLR.V21.CompileMethodDelegate>();
		}
	}
}
