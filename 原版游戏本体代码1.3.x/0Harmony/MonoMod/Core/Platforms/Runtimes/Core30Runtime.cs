using System;
using System.Runtime.CompilerServices;
using MonoMod.Core.Interop;
using MonoMod.Utils;

namespace MonoMod.Core.Platforms.Runtimes
{
	// Token: 0x02000532 RID: 1330
	[NullableContext(1)]
	[Nullable(0)]
	internal class Core30Runtime : Core21Runtime
	{
		// Token: 0x06001DDA RID: 7642 RVA: 0x00060D34 File Offset: 0x0005EF34
		public Core30Runtime(ISystem system)
			: base(system)
		{
		}

		// Token: 0x17000677 RID: 1655
		// (get) Token: 0x06001DDB RID: 7643 RVA: 0x00060D3D File Offset: 0x0005EF3D
		protected override Guid ExpectedJitVersion
		{
			get
			{
				return Core30Runtime.JitVersionGuid;
			}
		}

		// Token: 0x17000678 RID: 1656
		// (get) Token: 0x06001DDC RID: 7644 RVA: 0x00060292 File Offset: 0x0005E492
		protected override CoreCLR.InvokeCompileMethodPtr InvokeCompileMethodPtr
		{
			get
			{
				return CoreCLR.V21.InvokeCompileMethodPtr;
			}
		}

		// Token: 0x06001DDD RID: 7645 RVA: 0x00060299 File Offset: 0x0005E499
		protected override Delegate CastCompileHookToRealType(Delegate del)
		{
			return del.CastDelegate<CoreCLR.V21.CompileMethodDelegate>();
		}

		// Token: 0x04001247 RID: 4679
		private static readonly Guid JitVersionGuid = new Guid(3590962897U, 30769, 18940, 189, 73, 182, 240, 84, 221, 77, 70);
	}
}
