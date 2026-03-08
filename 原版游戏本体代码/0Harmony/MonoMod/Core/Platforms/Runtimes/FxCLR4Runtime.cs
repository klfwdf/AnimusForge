using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using MonoMod.Core.Interop;
using MonoMod.Utils;

namespace MonoMod.Core.Platforms.Runtimes
{
	// Token: 0x02000544 RID: 1348
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class FxCLR4Runtime : FxBaseRuntime
	{
		// Token: 0x06001E2B RID: 7723 RVA: 0x00061DA8 File Offset: 0x0005FFA8
		public FxCLR4Runtime(ISystem system)
		{
			this.system = system;
			if (PlatformDetection.Architecture == ArchitectureKind.x86_64 && (PlatformDetection.RuntimeVersion.Revision >= 17379 || PlatformDetection.RuntimeVersion.Minor >= 5))
			{
				Abi? defaultAbi = system.DefaultAbi;
				if (defaultAbi != null)
				{
					Abi abi = defaultAbi.GetValueOrDefault();
					this.AbiCore = new Abi?(FxCoreBaseRuntime.AbiForCoreFx45X64(abi));
				}
			}
		}

		// Token: 0x17000691 RID: 1681
		// (get) Token: 0x06001E2C RID: 7724 RVA: 0x00061E11 File Offset: 0x00060011
		public override RuntimeFeature Features
		{
			get
			{
				return base.Features & ~RuntimeFeature.RequiresBodyThunkWalking;
			}
		}

		// Token: 0x06001E2D RID: 7725 RVA: 0x00061E20 File Offset: 0x00060020
		private unsafe IntPtr GetMethodBodyPtr(MethodBase method, RuntimeMethodHandle handle)
		{
			Fx.V48.MethodDesc* md = (Fx.V48.MethodDesc*)(void*)handle.Value;
			md = Fx.V48.MethodDesc.FindTightlyBoundWrappedMethodDesc(md);
			return (IntPtr)md->GetNativeCode();
		}

		// Token: 0x06001E2E RID: 7726 RVA: 0x00061E4C File Offset: 0x0006004C
		public override IntPtr GetMethodEntryPoint(MethodBase method)
		{
			method = this.GetIdentifiable(method);
			RuntimeMethodHandle handle = this.GetMethodHandle(method);
			bool didPrepare = false;
			IntPtr ptr;
			for (;;)
			{
				Helpers.Assert(base.TryInvokeBclCompileMethod(handle), null, "TryInvokeBclCompileMethod(handle)");
				handle.GetFunctionPointer();
				ptr = this.GetMethodBodyPtr(method, handle);
				if (!(ptr == IntPtr.Zero))
				{
					return ptr;
				}
				if (didPrepare)
				{
					break;
				}
				Helpers.Assert(base.TryInvokeBclCompileMethod(handle), null, "TryInvokeBclCompileMethod(handle)");
				didPrepare = true;
			}
			ptr = handle.GetFunctionPointer();
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(59, 1);
			defaultInterpolatedStringHandler.AppendLiteral("Could not get entry point normally, GetFunctionPointer() = ");
			defaultInterpolatedStringHandler.AppendFormatted<IntPtr>(ptr, "x16");
			throw new InvalidOperationException(defaultInterpolatedStringHandler.ToStringAndClear());
		}

		// Token: 0x04001278 RID: 4728
		private ISystem system;
	}
}
