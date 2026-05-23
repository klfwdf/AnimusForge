using System;
using System.Runtime.CompilerServices;
using MonoMod.Core.Interop;
using MonoMod.Utils;

namespace MonoMod.Core.Platforms.Runtimes
{
	// Token: 0x02000534 RID: 1332
	[NullableContext(1)]
	[Nullable(0)]
	internal class Core50Runtime : Core31Runtime
	{
		// Token: 0x06001DE2 RID: 7650 RVA: 0x00060D8F File Offset: 0x0005EF8F
		public Core50Runtime(ISystem system)
			: base(system)
		{
		}

		// Token: 0x1700067A RID: 1658
		// (get) Token: 0x06001DE3 RID: 7651 RVA: 0x00060D98 File Offset: 0x0005EF98
		protected override Guid ExpectedJitVersion
		{
			get
			{
				return Core50Runtime.JitVersionGuid;
			}
		}

		// Token: 0x1700067B RID: 1659
		// (get) Token: 0x06001DE4 RID: 7652 RVA: 0x0001EBDB File Offset: 0x0001CDDB
		protected override int VtableIndexICorJitCompilerGetVersionGuid
		{
			get
			{
				return 2;
			}
		}

		// Token: 0x1700067C RID: 1660
		// (get) Token: 0x06001DE5 RID: 7653 RVA: 0x00060292 File Offset: 0x0005E492
		protected override CoreCLR.InvokeCompileMethodPtr InvokeCompileMethodPtr
		{
			get
			{
				return CoreCLR.V21.InvokeCompileMethodPtr;
			}
		}

		// Token: 0x06001DE6 RID: 7654 RVA: 0x00060299 File Offset: 0x0005E499
		protected override Delegate CastCompileHookToRealType(Delegate del)
		{
			return del.CastDelegate<CoreCLR.V21.CompileMethodDelegate>();
		}

		// Token: 0x04001248 RID: 4680
		private static readonly Guid JitVersionGuid = new Guid(2783888292U, 16758, 17319, 140, 43, 160, 91, 85, 29, 79, 73);
	}
}
