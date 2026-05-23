using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using MonoMod.Core.Utils;
using MonoMod.Utils;

namespace MonoMod.Core.Platforms.Runtimes
{
	// Token: 0x0200053F RID: 1343
	[NullableContext(1)]
	[Nullable(0)]
	internal abstract class CoreBaseRuntime : FxCoreBaseRuntime, IInitialize
	{
		// Token: 0x06001E14 RID: 7700 RVA: 0x000618F4 File Offset: 0x0005FAF4
		public static CoreBaseRuntime CreateForVersion(Version version, ISystem system, IArchitecture arch)
		{
			switch (version.Major)
			{
			case 2:
			case 4:
				return new Core21Runtime(system);
			case 3:
			{
				int minor = version.Minor;
				Core30Runtime result;
				if (minor != 0)
				{
					if (minor != 1)
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(36, 1);
						defaultInterpolatedStringHandler.AppendLiteral("Unknown .NET Core 3.x minor version ");
						defaultInterpolatedStringHandler.AppendFormatted<int>(version.Minor);
						throw new PlatformNotSupportedException(defaultInterpolatedStringHandler.ToStringAndClear());
					}
					result = new Core31Runtime(system);
				}
				else
				{
					result = new Core30Runtime(system);
				}
				return result;
			}
			case 5:
				return new Core50Runtime(system);
			case 6:
				return new Core60Runtime(system, arch);
			case 7:
				return new Core70Runtime(system, arch);
			case 8:
				return new Core80Runtime(system, arch);
			case 9:
				return new Core90Runtime(system, arch);
			case 10:
				return new Core100Runtime(system, arch);
			default:
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler2 = new DefaultInterpolatedStringHandler(33, 1);
				defaultInterpolatedStringHandler2.AppendLiteral("CoreCLR version ");
				defaultInterpolatedStringHandler2.AppendFormatted<Version>(version);
				defaultInterpolatedStringHandler2.AppendLiteral(" is not supported");
				throw new PlatformNotSupportedException(defaultInterpolatedStringHandler2.ToStringAndClear());
			}
			}
		}

		// Token: 0x1700068C RID: 1676
		// (get) Token: 0x06001E15 RID: 7701 RVA: 0x0001EBDB File Offset: 0x0001CDDB
		public override RuntimeKind Target
		{
			get
			{
				return RuntimeKind.CoreCLR;
			}
		}

		// Token: 0x1700068D RID: 1677
		// (get) Token: 0x06001E16 RID: 7702 RVA: 0x000619F9 File Offset: 0x0005FBF9
		protected ISystem System { get; }

		// Token: 0x06001E17 RID: 7703 RVA: 0x00061A04 File Offset: 0x0005FC04
		protected CoreBaseRuntime(ISystem system)
		{
			this.System = system;
			Abi? defaultAbi = system.DefaultAbi;
			if (defaultAbi != null)
			{
				Abi abi = defaultAbi.GetValueOrDefault();
				if (PlatformDetection.Architecture == ArchitectureKind.x86_64)
				{
					this.AbiCore = new Abi?(FxCoreBaseRuntime.AbiForCoreFx45X64(abi));
					return;
				}
				if (PlatformDetection.Architecture == ArchitectureKind.Arm64)
				{
					this.AbiCore = new Abi?(FxCoreBaseRuntime.AbiForCoreFx45ARM64(abi));
				}
			}
		}

		// Token: 0x06001E18 RID: 7704 RVA: 0x00061A69 File Offset: 0x0005FC69
		void IInitialize.Initialize()
		{
			this.InstallJitHook(this.JitObject);
		}

		// Token: 0x06001E19 RID: 7705 RVA: 0x00061A77 File Offset: 0x0005FC77
		private static bool IsMaybeClrJitPath(string path)
		{
			return Path.GetFileNameWithoutExtension(path).EndsWith("clrjit", StringComparison.Ordinal);
		}

		// Token: 0x06001E1A RID: 7706 RVA: 0x00061A8C File Offset: 0x0005FC8C
		protected virtual string GetClrJitPath()
		{
			string clrjitFile = null;
			object swValue;
			bool flag;
			if (Switches.TryGetSwitchValue("JitPath", out swValue))
			{
				string jitPath = swValue as string;
				if (jitPath != null)
				{
					if (!CoreBaseRuntime.IsMaybeClrJitPath(jitPath))
					{
						<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogWarningStringHandler debugLogWarningStringHandler = new <24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogWarningStringHandler(77, 1, ref flag);
						if (flag)
						{
							debugLogWarningStringHandler.AppendLiteral("Provided value for MonoMod.JitPath switch '");
							debugLogWarningStringHandler.AppendFormatted(jitPath);
							debugLogWarningStringHandler.AppendLiteral("' does not look like a ClrJIT path");
						}
						<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.Warning(ref debugLogWarningStringHandler);
					}
					else
					{
						clrjitFile = this.System.EnumerateLoadedModuleFiles().FirstOrDefault((string f) => f != null && f == jitPath);
						if (clrjitFile == null)
						{
							<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogWarningStringHandler debugLogWarningStringHandler2 = new <24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogWarningStringHandler(82, 1, ref flag);
							if (flag)
							{
								debugLogWarningStringHandler2.AppendLiteral("Provided path for MonoMod.JitPath switch was not loaded in this process. jitPath: ");
								debugLogWarningStringHandler2.AppendFormatted(jitPath);
							}
							<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.Warning(ref debugLogWarningStringHandler2);
						}
					}
				}
			}
			if (clrjitFile == null)
			{
				clrjitFile = this.System.EnumerateLoadedModuleFiles().FirstOrDefault((string f) => f != null && CoreBaseRuntime.IsMaybeClrJitPath(f));
			}
			if (clrjitFile == null)
			{
				throw new PlatformNotSupportedException("Could not locate clrjit library");
			}
			<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogTraceStringHandler debugLogTraceStringHandler = new <24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogTraceStringHandler(14, 1, ref flag);
			if (flag)
			{
				debugLogTraceStringHandler.AppendLiteral("Got jit path: ");
				debugLogTraceStringHandler.AppendFormatted(clrjitFile);
			}
			<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.Trace(ref debugLogTraceStringHandler);
			return clrjitFile;
		}

		// Token: 0x1700068E RID: 1678
		// (get) Token: 0x06001E1B RID: 7707 RVA: 0x00061BD0 File Offset: 0x0005FDD0
		protected IntPtr JitObject
		{
			get
			{
				IntPtr intPtr = this.lazyJitObject.GetValueOrDefault();
				if (this.lazyJitObject == null)
				{
					intPtr = this.GetJitObject();
					this.lazyJitObject = new IntPtr?(intPtr);
					return intPtr;
				}
				return intPtr;
			}
		}

		// Token: 0x06001E1C RID: 7708 RVA: 0x00061C0C File Offset: 0x0005FE0C
		private unsafe IntPtr GetJitObject()
		{
			IntPtr clrjit;
			if (!DynDll.TryOpenLibrary(this.GetClrJitPath(), out clrjit))
			{
				throw new PlatformNotSupportedException("Could not open clrjit library");
			}
			IntPtr result;
			try
			{
				result = calli(System.IntPtr(), (void*)clrjit.GetExport("getJit"));
			}
			catch
			{
				DynDll.CloseLibrary(clrjit);
				throw;
			}
			return result;
		}

		// Token: 0x06001E1D RID: 7709 RVA: 0x00061C68 File Offset: 0x0005FE68
		protected virtual void InstallJitHook(IntPtr jit)
		{
			this.InstallManagedJitHook(jit);
		}

		// Token: 0x06001E1E RID: 7710
		protected abstract void InstallManagedJitHook(IntPtr jit);

		// Token: 0x1700068F RID: 1679
		// (get) Token: 0x06001E1F RID: 7711 RVA: 0x00061C74 File Offset: 0x0005FE74
		[Nullable(2)]
		protected INativeExceptionHelper NativeExceptionHelper
		{
			[NullableContext(2)]
			get
			{
				INativeExceptionHelper result;
				if ((result = this.lazyNativeExceptionHelper) == null)
				{
					result = (this.lazyNativeExceptionHelper = this.System.NativeExceptionHelper);
				}
				return result;
			}
		}

		// Token: 0x06001E20 RID: 7712 RVA: 0x00061C9F File Offset: 0x0005FE9F
		[NullableContext(2)]
		protected IntPtr EHNativeToManaged(IntPtr target, out IDisposable handle)
		{
			if (this.NativeExceptionHelper != null)
			{
				return this.NativeExceptionHelper.CreateNativeToManagedHelper(target, out handle);
			}
			handle = null;
			return target;
		}

		// Token: 0x06001E21 RID: 7713 RVA: 0x00061CBB File Offset: 0x0005FEBB
		[NullableContext(2)]
		protected IntPtr EHManagedToNative(IntPtr target, out IDisposable handle)
		{
			if (this.NativeExceptionHelper != null)
			{
				return this.NativeExceptionHelper.CreateManagedToNativeHelper(target, out handle);
			}
			handle = null;
			return target;
		}

		// Token: 0x04001272 RID: 4722
		private IntPtr? lazyJitObject;

		// Token: 0x04001273 RID: 4723
		[Nullable(2)]
		private INativeExceptionHelper lazyNativeExceptionHelper;
	}
}
