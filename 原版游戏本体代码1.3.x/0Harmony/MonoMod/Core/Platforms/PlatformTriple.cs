using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Core.Platforms.Architectures;
using MonoMod.Core.Platforms.Runtimes;
using MonoMod.Core.Platforms.Systems;
using MonoMod.Core.Utils;
using MonoMod.Logs;
using MonoMod.Utils;

namespace MonoMod.Core.Platforms
{
	// Token: 0x02000506 RID: 1286
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class PlatformTriple
	{
		// Token: 0x06001CC5 RID: 7365 RVA: 0x0005B528 File Offset: 0x00059728
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		public static IRuntime CreateCurrentRuntime(ISystem system, IArchitecture arch)
		{
			Helpers.ThrowIfArgumentNull<ISystem>(system, "system");
			Helpers.ThrowIfArgumentNull<IArchitecture>(arch, "arch");
			RuntimeKind kind = PlatformDetection.Runtime;
			IRuntime result;
			switch (kind)
			{
			case RuntimeKind.Framework:
				result = FxBaseRuntime.CreateForVersion(PlatformDetection.RuntimeVersion, system);
				break;
			case RuntimeKind.CoreCLR:
				result = CoreBaseRuntime.CreateForVersion(PlatformDetection.RuntimeVersion, system, arch);
				break;
			case RuntimeKind.Mono:
				result = new MonoRuntime(system);
				break;
			default:
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(27, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Runtime kind ");
				defaultInterpolatedStringHandler.AppendFormatted<RuntimeKind>(kind);
				defaultInterpolatedStringHandler.AppendLiteral(" not supported");
				throw new PlatformNotSupportedException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			}
			return result;
		}

		// Token: 0x06001CC6 RID: 7366 RVA: 0x0005B5C8 File Offset: 0x000597C8
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		public static IArchitecture CreateCurrentArchitecture(ISystem system)
		{
			Helpers.ThrowIfArgumentNull<ISystem>(system, "system");
			ArchitectureKind kind = PlatformDetection.Architecture;
			IArchitecture result;
			switch (kind)
			{
			case ArchitectureKind.x86:
				result = new x86Arch(system);
				break;
			case ArchitectureKind.x86_64:
				result = new x86_64Arch(system);
				break;
			case ArchitectureKind.Arm:
				throw new NotImplementedException();
			case ArchitectureKind.Arm64:
				result = new Arm64Arch(system);
				break;
			default:
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(32, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Architecture kind ");
				defaultInterpolatedStringHandler.AppendFormatted<ArchitectureKind>(kind);
				defaultInterpolatedStringHandler.AppendLiteral(" not supported");
				throw new PlatformNotSupportedException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			}
			return result;
		}

		// Token: 0x06001CC7 RID: 7367 RVA: 0x0005B65C File Offset: 0x0005985C
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		public static ISystem CreateCurrentSystem()
		{
			OSKind kind = PlatformDetection.OS;
			if (kind <= OSKind.BSD)
			{
				switch (kind)
				{
				case OSKind.Posix:
					throw new NotImplementedException();
				case OSKind.Windows:
					break;
				case (OSKind)3:
				case (OSKind)4:
					goto IL_74;
				case OSKind.OSX:
					return new MacOSSystem();
				default:
					if (kind == OSKind.Linux)
					{
						return new LinuxSystem();
					}
					if (kind != OSKind.BSD)
					{
						goto IL_74;
					}
					throw new NotImplementedException();
				}
			}
			else if (kind != OSKind.Wine)
			{
				if (kind == OSKind.IOS)
				{
					throw new NotImplementedException();
				}
				if (kind != OSKind.Android)
				{
					goto IL_74;
				}
				throw new NotImplementedException();
			}
			return new WindowsSystem();
			IL_74:
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(22, 1);
			defaultInterpolatedStringHandler.AppendLiteral("OS kind ");
			defaultInterpolatedStringHandler.AppendFormatted<OSKind>(kind);
			defaultInterpolatedStringHandler.AppendLiteral(" not supported");
			throw new PlatformNotSupportedException(defaultInterpolatedStringHandler.ToStringAndClear());
		}

		// Token: 0x1700063D RID: 1597
		// (get) Token: 0x06001CC8 RID: 7368 RVA: 0x0005B715 File Offset: 0x00059915
		public IArchitecture Architecture { get; }

		// Token: 0x1700063E RID: 1598
		// (get) Token: 0x06001CC9 RID: 7369 RVA: 0x0005B71D File Offset: 0x0005991D
		public ISystem System { get; }

		// Token: 0x1700063F RID: 1599
		// (get) Token: 0x06001CCA RID: 7370 RVA: 0x0005B725 File Offset: 0x00059925
		public IRuntime Runtime { get; }

		// Token: 0x17000640 RID: 1600
		// (get) Token: 0x06001CCB RID: 7371 RVA: 0x0005B72D File Offset: 0x0005992D
		public static PlatformTriple Current
		{
			get
			{
				return Helpers.GetOrInitWithLock<PlatformTriple>(ref PlatformTriple.lazyCurrent, PlatformTriple.lazyCurrentLock, PlatformTriple.createCurrentFunc);
			}
		}

		// Token: 0x06001CCC RID: 7372 RVA: 0x0005B744 File Offset: 0x00059944
		private static PlatformTriple CreateCurrent()
		{
			ISystem sys = PlatformTriple.CreateCurrentSystem();
			IArchitecture arch = PlatformTriple.CreateCurrentArchitecture(sys);
			IRuntime runtime = PlatformTriple.CreateCurrentRuntime(sys, arch);
			return new PlatformTriple(arch, sys, runtime);
		}

		// Token: 0x06001CCD RID: 7373 RVA: 0x0005B770 File Offset: 0x00059970
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		public static void SetPlatformTriple(PlatformTriple triple)
		{
			Helpers.ThrowIfArgumentNull<PlatformTriple>(triple, "triple");
			if (PlatformTriple.lazyCurrent == null)
			{
				PlatformTriple.ThrowTripleAlreadyExists();
			}
			object obj = PlatformTriple.lazyCurrentLock;
			lock (obj)
			{
				if (PlatformTriple.lazyCurrent == null)
				{
					PlatformTriple.ThrowTripleAlreadyExists();
				}
				PlatformTriple.lazyCurrent = triple;
			}
		}

		// Token: 0x06001CCE RID: 7374 RVA: 0x0005B7D4 File Offset: 0x000599D4
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static void ThrowTripleAlreadyExists()
		{
			throw new InvalidOperationException("The platform triple has already been initialized; cannot set a new one");
		}

		// Token: 0x06001CCF RID: 7375 RVA: 0x0005B7E0 File Offset: 0x000599E0
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		public PlatformTriple(IArchitecture architecture, ISystem system, IRuntime runtime)
		{
			Helpers.ThrowIfArgumentNull<IArchitecture>(architecture, "architecture");
			Helpers.ThrowIfArgumentNull<ISystem>(system, "system");
			Helpers.ThrowIfArgumentNull<IRuntime>(runtime, "runtime");
			this.Architecture = architecture;
			this.System = system;
			this.Runtime = runtime;
			this.SupportedFeatures = new FeatureFlags(this.Architecture.Features, this.System.Features, this.Runtime.Features);
			this.InitIfNeeded(this.Architecture);
			this.InitIfNeeded(this.System);
			this.InitIfNeeded(this.Runtime);
			this.Abi = this.Runtime.Abi;
		}

		// Token: 0x06001CD0 RID: 7376 RVA: 0x0005B898 File Offset: 0x00059A98
		private void InitIfNeeded(object obj)
		{
			IInitialize<ISystem> initialize = obj as IInitialize<ISystem>;
			if (initialize != null)
			{
				initialize.Initialize(this.System);
			}
			IInitialize<IArchitecture> initialize2 = obj as IInitialize<IArchitecture>;
			if (initialize2 != null)
			{
				initialize2.Initialize(this.Architecture);
			}
			IInitialize<IRuntime> initialize3 = obj as IInitialize<IRuntime>;
			if (initialize3 != null)
			{
				initialize3.Initialize(this.Runtime);
			}
			IInitialize<PlatformTriple> initialize4 = obj as IInitialize<PlatformTriple>;
			if (initialize4 != null)
			{
				initialize4.Initialize(this);
			}
			IInitialize initialize5 = obj as IInitialize;
			if (initialize5 == null)
			{
				return;
			}
			initialize5.Initialize();
		}

		// Token: 0x17000641 RID: 1601
		// (get) Token: 0x06001CD1 RID: 7377 RVA: 0x0005B90C File Offset: 0x00059B0C
		[TupleElementNames(new string[] { "Arch", "OS", "Runtime" })]
		[Nullable(0)]
		public ValueTuple<ArchitectureKind, OSKind, RuntimeKind> HostTriple
		{
			[NullableContext(0)]
			[return: TupleElementNames(new string[] { "Arch", "OS", "Runtime" })]
			get
			{
				return new ValueTuple<ArchitectureKind, OSKind, RuntimeKind>(this.Architecture.Target, this.System.Target, this.Runtime.Target);
			}
		}

		// Token: 0x17000642 RID: 1602
		// (get) Token: 0x06001CD2 RID: 7378 RVA: 0x0005B934 File Offset: 0x00059B34
		public FeatureFlags SupportedFeatures { get; }

		// Token: 0x17000643 RID: 1603
		// (get) Token: 0x06001CD3 RID: 7379 RVA: 0x0005B93C File Offset: 0x00059B3C
		public Abi Abi { get; }

		// Token: 0x06001CD4 RID: 7380 RVA: 0x0005B944 File Offset: 0x00059B44
		public void Compile(MethodBase method)
		{
			Helpers.ThrowIfArgumentNull<MethodBase>(method, "method");
			if (method.IsGenericMethodDefinition)
			{
				throw new ArgumentException("Cannot prepare generic method definition", "method");
			}
			method = this.GetIdentifiable(method);
			if (this.SupportedFeatures.Has(RuntimeFeature.RequiresCustomMethodCompile))
			{
				this.Runtime.Compile(method);
				return;
			}
			RuntimeMethodHandle handle = this.Runtime.GetMethodHandle(method);
			if (method.IsGenericMethod)
			{
				Type[] typeArgs = method.GetGenericArguments();
				RuntimeTypeHandle[] argHandles = new RuntimeTypeHandle[typeArgs.Length];
				for (int i = 0; i < typeArgs.Length; i++)
				{
					argHandles[i] = typeArgs[i].TypeHandle;
				}
				RuntimeHelpers.PrepareMethod(handle, argHandles);
				return;
			}
			RuntimeHelpers.PrepareMethod(handle);
		}

		// Token: 0x06001CD5 RID: 7381 RVA: 0x0005B9F8 File Offset: 0x00059BF8
		public MethodBase GetIdentifiable(MethodBase method)
		{
			Helpers.ThrowIfArgumentNull<MethodBase>(method, "method");
			if (this.SupportedFeatures.Has(RuntimeFeature.RequiresMethodIdentification))
			{
				method = this.Runtime.GetIdentifiable(method);
			}
			if (method.ReflectedType != method.DeclaringType)
			{
				ParameterInfo[] parameters = method.GetParameters();
				Type[] paramTypes = new Type[parameters.Length];
				for (int i = 0; i < parameters.Length; i++)
				{
					paramTypes[i] = parameters[i].ParameterType;
				}
				if (method.DeclaringType == null)
				{
					MethodInfo method2 = method.Module.GetMethod(method.Name, (BindingFlags)(-1), null, method.CallingConvention, paramTypes, null);
					bool flag = method2 != null;
					bool value = flag;
					bool flag2;
					AssertionInterpolatedStringHandler assertionInterpolatedStringHandler = new AssertionInterpolatedStringHandler(16, 2, flag, ref flag2);
					if (flag2)
					{
						assertionInterpolatedStringHandler.AppendLiteral("orig: ");
						assertionInterpolatedStringHandler.AppendFormatted<MethodBase>(method);
						assertionInterpolatedStringHandler.AppendLiteral(", module: ");
						assertionInterpolatedStringHandler.AppendFormatted<Module>(method.Module);
					}
					Helpers.Assert(value, ref assertionInterpolatedStringHandler, "got is not null");
					method = method2;
				}
				else if (method.IsConstructor)
				{
					ConstructorInfo constructor = method.DeclaringType.GetConstructor((BindingFlags)(-1), null, method.CallingConvention, paramTypes, null);
					bool flag2 = constructor != null;
					bool value2 = flag2;
					bool flag;
					AssertionInterpolatedStringHandler assertionInterpolatedStringHandler2 = new AssertionInterpolatedStringHandler(6, 1, flag2, ref flag);
					if (flag)
					{
						assertionInterpolatedStringHandler2.AppendLiteral("orig: ");
						assertionInterpolatedStringHandler2.AppendFormatted<MethodBase>(method);
					}
					Helpers.Assert(value2, ref assertionInterpolatedStringHandler2, "got is not null");
					method = constructor;
				}
				else
				{
					MethodInfo method3 = method.DeclaringType.GetMethod(method.Name, (BindingFlags)(-1), null, method.CallingConvention, paramTypes, null);
					bool flag = method3 != null;
					bool value3 = flag;
					bool flag2;
					AssertionInterpolatedStringHandler assertionInterpolatedStringHandler3 = new AssertionInterpolatedStringHandler(6, 1, flag, ref flag2);
					if (flag2)
					{
						assertionInterpolatedStringHandler3.AppendLiteral("orig: ");
						assertionInterpolatedStringHandler3.AppendFormatted<MethodBase>(method);
					}
					Helpers.Assert(value3, ref assertionInterpolatedStringHandler3, "got is not null");
					method = method3;
				}
			}
			return method;
		}

		// Token: 0x06001CD6 RID: 7382 RVA: 0x0005BBA0 File Offset: 0x00059DA0
		[return: Nullable(2)]
		public IDisposable PinMethodIfNeeded(MethodBase method)
		{
			if (this.SupportedFeatures.Has(RuntimeFeature.RequiresMethodPinning))
			{
				return this.Runtime.PinMethodIfNeeded(method);
			}
			return null;
		}

		// Token: 0x06001CD7 RID: 7383 RVA: 0x0005BBD0 File Offset: 0x00059DD0
		public bool TryDisableInlining(MethodBase method)
		{
			if (this.SupportedFeatures.Has(RuntimeFeature.DisableInlining))
			{
				this.Runtime.DisableInlining(method);
				return true;
			}
			return false;
		}

		// Token: 0x06001CD8 RID: 7384 RVA: 0x0005BC00 File Offset: 0x00059E00
		public unsafe SimpleNativeDetour CreateSimpleDetour(IntPtr from, IntPtr to, int detourMaxSize = -1, IntPtr fromRw = default(IntPtr))
		{
			if (fromRw == (IntPtr)0)
			{
				fromRw = from;
			}
			bool flag = from != to;
			bool value = flag;
			bool flag2;
			AssertionInterpolatedStringHandler assertionInterpolatedStringHandler = new AssertionInterpolatedStringHandler(48, 2, flag, ref flag2);
			if (flag2)
			{
				assertionInterpolatedStringHandler.AppendLiteral("Cannot detour a method to itself! (from: ");
				assertionInterpolatedStringHandler.AppendFormatted<IntPtr>(from);
				assertionInterpolatedStringHandler.AppendLiteral(", to: ");
				assertionInterpolatedStringHandler.AppendFormatted<IntPtr>(to);
				assertionInterpolatedStringHandler.AppendLiteral(")");
			}
			Helpers.Assert(value, ref assertionInterpolatedStringHandler, "from != to");
			<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogTraceStringHandler debugLogTraceStringHandler = new <24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogTraceStringHandler(31, 2, ref flag2);
			if (flag2)
			{
				debugLogTraceStringHandler.AppendLiteral("Creating simple detour 0x");
				debugLogTraceStringHandler.AppendFormatted<IntPtr>(from, "x16");
				debugLogTraceStringHandler.AppendLiteral(" => 0x");
				debugLogTraceStringHandler.AppendFormatted<IntPtr>(to, "x16");
			}
			<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.Trace(ref debugLogTraceStringHandler);
			NativeDetourInfo detourInfo = this.Architecture.ComputeDetourInfo(from, to, detourMaxSize);
			int size = detourInfo.Size;
			Span<byte> detourData = new Span<byte>(stackalloc byte[(UIntPtr)size], size);
			IDisposable allocHandle;
			this.Architecture.GetDetourBytes(detourInfo, detourData, out allocHandle);
			byte[] backup = new byte[detourInfo.Size];
			this.System.PatchData(PatchTargetKind.Executable, fromRw, detourData, backup);
			return new SimpleNativeDetour(this, detourInfo, backup, allocHandle);
		}

		// Token: 0x06001CD9 RID: 7385 RVA: 0x0005BD2C File Offset: 0x00059F2C
		public unsafe PlatformTriple.NativeDetour CreateNativeDetour(IntPtr from, IntPtr to, int detourMaxSize = -1, IntPtr fromRw = default(IntPtr))
		{
			if (fromRw == (IntPtr)0)
			{
				fromRw = from;
			}
			bool flag = from != to;
			bool value = flag;
			bool flag2;
			AssertionInterpolatedStringHandler assertionInterpolatedStringHandler = new AssertionInterpolatedStringHandler(48, 2, flag, ref flag2);
			if (flag2)
			{
				assertionInterpolatedStringHandler.AppendLiteral("Cannot detour a method to itself! (from: ");
				assertionInterpolatedStringHandler.AppendFormatted<IntPtr>(from);
				assertionInterpolatedStringHandler.AppendLiteral(", to: ");
				assertionInterpolatedStringHandler.AppendFormatted<IntPtr>(to);
				assertionInterpolatedStringHandler.AppendLiteral(")");
			}
			Helpers.Assert(value, ref assertionInterpolatedStringHandler, "from != to");
			<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogTraceStringHandler debugLogTraceStringHandler = new <24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogTraceStringHandler(31, 2, ref flag2);
			if (flag2)
			{
				debugLogTraceStringHandler.AppendLiteral("Creating simple detour 0x");
				debugLogTraceStringHandler.AppendFormatted<IntPtr>(from, "x16");
				debugLogTraceStringHandler.AppendLiteral(" => 0x");
				debugLogTraceStringHandler.AppendFormatted<IntPtr>(to, "x16");
			}
			<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.Trace(ref debugLogTraceStringHandler);
			NativeDetourInfo detourInfo = this.Architecture.ComputeDetourInfo(from, to, detourMaxSize);
			int size2 = detourInfo.Size;
			Span<byte> detourData = new Span<byte>(stackalloc byte[(UIntPtr)size2], size2);
			IDisposable allocHandle;
			int size = this.Architecture.GetDetourBytes(detourInfo, detourData, out allocHandle);
			IntPtr altEntry = IntPtr.Zero;
			IDisposable altHandle = null;
			if (this.SupportedFeatures.Has(ArchitectureFeature.CreateAltEntryPoint))
			{
				altEntry = this.Architecture.AltEntryFactory.CreateAlternateEntrypoint(from, size, out altHandle);
			}
			else
			{
				<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogWarningStringHandler debugLogWarningStringHandler = new <24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogWarningStringHandler(67, 2, ref flag2);
				if (flag2)
				{
					debugLogWarningStringHandler.AppendLiteral("Cannot create alternate entry point for native detour (from: ");
					debugLogWarningStringHandler.AppendFormatted<IntPtr>(from, "x16");
					debugLogWarningStringHandler.AppendLiteral(", to: ");
					debugLogWarningStringHandler.AppendFormatted<IntPtr>(to, "x16");
				}
				<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.Warning(ref debugLogWarningStringHandler);
			}
			byte[] backup = new byte[detourInfo.Size];
			this.System.PatchData(PatchTargetKind.Executable, fromRw, detourData, backup);
			return new PlatformTriple.NativeDetour(new SimpleNativeDetour(this, detourInfo, backup, allocHandle), altEntry, altHandle);
		}

		// Token: 0x06001CDA RID: 7386 RVA: 0x0005BEE0 File Offset: 0x0005A0E0
		public IntPtr GetNativeMethodBody(MethodBase method)
		{
			if (this.SupportedFeatures.Has(RuntimeFeature.RequiresBodyThunkWalking))
			{
				return this.GetNativeMethodBodyWalk(method, true);
			}
			return this.GetNativeMethodBodyDirect(method);
		}

		// Token: 0x06001CDB RID: 7387 RVA: 0x0005BF14 File Offset: 0x0005A114
		private IntPtr GetNativeMethodBodyWalk(MethodBase method, bool reloadPtr)
		{
			bool regenerated = false;
			bool didPrepareLastIter = false;
			int iters = 0;
			BytePatternCollection archMatchCollection = this.Architecture.KnownMethodThunks;
			bool flag;
			<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogTraceStringHandler debugLogTraceStringHandler = new <24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogTraceStringHandler(32, 1, ref flag);
			if (flag)
			{
				debugLogTraceStringHandler.AppendLiteral("Performing method body walk for ");
				debugLogTraceStringHandler.AppendFormatted<MethodBase>(method);
			}
			<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.Trace(ref debugLogTraceStringHandler);
			IntPtr prevEntry = (IntPtr)(-1);
			IntPtr entry;
			for (;;)
			{
				IL_41:
				entry = this.Runtime.GetMethodEntryPoint(method);
				<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogTraceStringHandler debugLogTraceStringHandler2 = new <24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogTraceStringHandler(25, 1, ref flag);
				if (flag)
				{
					debugLogTraceStringHandler2.AppendLiteral("Starting entry point = 0x");
					debugLogTraceStringHandler2.AppendFormatted<IntPtr>(entry, "x16");
				}
				<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.Trace(ref debugLogTraceStringHandler2);
				while (iters++ <= 20)
				{
					if (!didPrepareLastIter && prevEntry == entry)
					{
						return entry;
					}
					prevEntry = entry;
					IntPtr readableLen = this.System.GetSizeOfReadableMemory(entry, (IntPtr)archMatchCollection.MaxMinLength);
					if (readableLen <= (IntPtr)0)
					{
						<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogWarningStringHandler debugLogWarningStringHandler = new <24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogWarningStringHandler(43, 2, ref flag);
						if (flag)
						{
							debugLogWarningStringHandler.AppendLiteral("Got zero or negative readable length ");
							debugLogWarningStringHandler.AppendFormatted<IntPtr>(readableLen);
							debugLogWarningStringHandler.AppendLiteral(" at 0x");
							debugLogWarningStringHandler.AppendFormatted<IntPtr>(entry, "x16");
						}
						<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.Warning(ref debugLogWarningStringHandler);
					}
					ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(entry, Math.Min((int)readableLen, archMatchCollection.MaxMinLength));
					ulong addr;
					BytePattern match;
					int offset;
					int num;
					if (!archMatchCollection.TryFindMatch(span, out addr, out match, out offset, out num))
					{
						return entry;
					}
					IntPtr lastEntry = entry;
					didPrepareLastIter = false;
					AddressMeaning meaning = match.AddressMeaning;
					<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogTraceStringHandler debugLogTraceStringHandler3 = new <24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogTraceStringHandler(46, 4, ref flag);
					if (flag)
					{
						debugLogTraceStringHandler3.AppendLiteral("Matched thunk with ");
						debugLogTraceStringHandler3.AppendFormatted<AddressMeaning>(meaning);
						debugLogTraceStringHandler3.AppendLiteral(" at 0x");
						debugLogTraceStringHandler3.AppendFormatted<IntPtr>(entry, "x16");
						debugLogTraceStringHandler3.AppendLiteral(" (addr: 0x");
						debugLogTraceStringHandler3.AppendFormatted<ulong>(addr, "x8");
						debugLogTraceStringHandler3.AppendLiteral(", offset: ");
						debugLogTraceStringHandler3.AppendFormatted<int>(offset);
						debugLogTraceStringHandler3.AppendLiteral(")");
					}
					<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.Trace(ref debugLogTraceStringHandler3);
					if (meaning.Kind.IsPrecodeFixup() && !regenerated)
					{
						IntPtr precode = meaning.ProcessAddress(entry, offset, addr);
						if (reloadPtr)
						{
							<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogTraceStringHandler debugLogTraceStringHandler4 = new <24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogTraceStringHandler(56, 1, ref flag);
							if (flag)
							{
								debugLogTraceStringHandler4.AppendLiteral("Method thunk reset; regenerating (PrecodeFixupThunk: 0x");
								debugLogTraceStringHandler4.AppendFormatted<IntPtr>(precode, "X16");
								debugLogTraceStringHandler4.AppendLiteral(")");
							}
							<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.Trace(ref debugLogTraceStringHandler4);
							this.Compile(method);
							didPrepareLastIter = true;
							goto IL_41;
						}
						entry = precode;
					}
					else
					{
						entry = meaning.ProcessAddress(entry, offset, addr);
					}
					<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogTraceStringHandler debugLogTraceStringHandler5 = new <24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogTraceStringHandler(23, 1, ref flag);
					if (flag)
					{
						debugLogTraceStringHandler5.AppendLiteral("Got next entry point 0x");
						debugLogTraceStringHandler5.AppendFormatted<IntPtr>(entry, "x16");
					}
					<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.Trace(ref debugLogTraceStringHandler5);
					bool wasPreStub;
					entry = this.NotThePreStub(lastEntry, entry, out wasPreStub);
					if (wasPreStub && reloadPtr)
					{
						<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.Trace("Matched ThePreStub");
						this.Compile(method);
						goto IL_41;
					}
				}
				break;
			}
			<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogErrorStringHandler debugLogErrorStringHandler = new <24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogErrorStringHandler(70, 4, ref flag);
			if (flag)
			{
				debugLogErrorStringHandler.AppendLiteral("Could not get entry point for ");
				debugLogErrorStringHandler.AppendFormatted<MethodBase>(method);
				debugLogErrorStringHandler.AppendLiteral("! (tried ");
				debugLogErrorStringHandler.AppendFormatted<int>(iters);
				debugLogErrorStringHandler.AppendLiteral(" times) entry: 0x");
				debugLogErrorStringHandler.AppendFormatted<IntPtr>(entry, "x16");
				debugLogErrorStringHandler.AppendLiteral(" prevEntry: 0x");
				debugLogErrorStringHandler.AppendFormatted<IntPtr>(prevEntry, "x16");
			}
			<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.Error(ref debugLogErrorStringHandler);
			FormatInterpolatedStringHandler formatInterpolatedStringHandler = new FormatInterpolatedStringHandler(47, 1);
			formatInterpolatedStringHandler.AppendLiteral("Could not get entrypoint for ");
			formatInterpolatedStringHandler.AppendFormatted<MethodBase>(method);
			formatInterpolatedStringHandler.AppendLiteral(" (stuck in a loop)");
			throw new NotSupportedException(DebugFormatter.Format(ref formatInterpolatedStringHandler));
		}

		// Token: 0x06001CDC RID: 7388 RVA: 0x0005C268 File Offset: 0x0005A468
		private IntPtr GetNativeMethodBodyDirect(MethodBase method)
		{
			return this.Runtime.GetMethodEntryPoint(method);
		}

		// Token: 0x06001CDD RID: 7389 RVA: 0x0005C278 File Offset: 0x0005A478
		private IntPtr NotThePreStub(IntPtr ptrGot, IntPtr ptrParsed, out bool wasPreStub)
		{
			if (this.ThePreStub == IntPtr.Zero)
			{
				this.ThePreStub = (IntPtr)(-2);
				Type type = typeof(HttpWebRequest).Assembly.GetType("System.Net.Connection");
				IntPtr intPtr;
				if (type == null)
				{
					intPtr = (IntPtr)(-1);
				}
				else
				{
					intPtr = (from m in type.GetMethods()
						group m by this.GetNativeMethodBodyWalk(m, false)).First((IGrouping<IntPtr, MethodInfo> g) => g.Count<MethodInfo>() > 1).Key;
				}
				IntPtr pre = intPtr;
				this.ThePreStub = pre;
				bool flag;
				<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogTraceStringHandler debugLogTraceStringHandler = new <24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogTraceStringHandler(14, 1, ref flag);
				if (flag)
				{
					debugLogTraceStringHandler.AppendLiteral("ThePreStub: 0x");
					debugLogTraceStringHandler.AppendFormatted<IntPtr>(this.ThePreStub, "X16");
				}
				<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.Trace(ref debugLogTraceStringHandler);
			}
			wasPreStub = ptrParsed == this.ThePreStub;
			if (!wasPreStub)
			{
				return ptrParsed;
			}
			return ptrGot;
		}

		// Token: 0x06001CDE RID: 7390 RVA: 0x0005C358 File Offset: 0x0005A558
		public unsafe MethodBase GetRealDetourTarget(MethodBase from, MethodBase to)
		{
			Helpers.ThrowIfArgumentNull<MethodBase>(from, "from");
			Helpers.ThrowIfArgumentNull<MethodBase>(to, "to");
			to = this.GetIdentifiable(to);
			MethodInfo fromInfo = from as MethodInfo;
			if (fromInfo != null)
			{
				MethodInfo toInfo = to as MethodInfo;
				if (toInfo != null)
				{
					bool returnBufferIsArgument = false;
					ReadOnlySpan<SpecialArgumentKind> span = this.Abi.ArgumentOrder.Span;
					for (int k = 0; k < span.Length; k++)
					{
						if (*span[k] == 1)
						{
							returnBufferIsArgument = true;
							break;
						}
					}
					Type returnType = fromInfo.ReturnType;
					bool hasReturnBuffer = this.Abi.Classify(returnType, true) == TypeClassification.ByReference;
					bool hasThis = !fromInfo.IsStatic;
					bool flag = hasThis && toInfo.IsStatic && hasReturnBuffer && returnBufferIsArgument;
					bool requiresGenericContextFixup = PlatformTriple.HasGenericContext(this.Abi) && this.Runtime.RequiresGenericContext(fromInfo);
					if (!flag && !requiresGenericContextFixup)
					{
						return to;
					}
					Type returnBufferType = (hasReturnBuffer ? returnType.MakeByRefType() : returnType);
					Type newReturnType = ((hasReturnBuffer && !this.Abi.ReturnsReturnBuffer) ? typeof(void) : returnBufferType);
					int thisPos = -1;
					int returnBufferPos = -1;
					int userArgumentsOffset = -1;
					ParameterInfo[] parameters = from.GetParameters();
					List<Type> argumentTypes = new List<Type>(parameters.Length + 3);
					ReadOnlySpan<SpecialArgumentKind> argumentKinds = this.Abi.ArgumentOrder.Span;
					for (int i = 0; i < argumentKinds.Length; i++)
					{
						switch (*argumentKinds[i])
						{
						case 0:
							if (hasThis)
							{
								thisPos = argumentTypes.Count;
								argumentTypes.Add(from.GetThisParamType());
							}
							break;
						case 1:
							if (hasReturnBuffer)
							{
								returnBufferPos = argumentTypes.Count;
								argumentTypes.Add(returnBufferType);
							}
							break;
						case 2:
							if (requiresGenericContextFixup)
							{
								argumentTypes.Add(typeof(IntPtr));
							}
							break;
						case 3:
							userArgumentsOffset = argumentTypes.Count;
							argumentTypes.AddRange(from p in parameters
								select p.ParameterType);
							break;
						}
					}
					FormatInterpolatedStringHandler formatInterpolatedStringHandler = new FormatInterpolatedStringHandler(16, 2);
					formatInterpolatedStringHandler.AppendLiteral("Glue:AbiFixup<");
					formatInterpolatedStringHandler.AppendFormatted<MethodBase>(from);
					formatInterpolatedStringHandler.AppendLiteral(",");
					formatInterpolatedStringHandler.AppendFormatted<MethodBase>(to);
					formatInterpolatedStringHandler.AppendLiteral(">");
					MethodBase result;
					using (DynamicMethodDefinition dmd = new DynamicMethodDefinition(DebugFormatter.Format(ref formatInterpolatedStringHandler), newReturnType, argumentTypes.ToArray()))
					{
						dmd.Definition.ImplAttributes |= Mono.Cecil.MethodImplAttributes.NoInlining | Mono.Cecil.MethodImplAttributes.AggressiveOptimization;
						ILProcessor il = dmd.GetILProcessor();
						if (hasReturnBuffer && returnBufferPos >= 0)
						{
							il.Emit(OpCodes.Ldarg, returnBufferPos);
						}
						if (hasThis)
						{
							il.Emit(OpCodes.Ldarg, thisPos);
						}
						for (int j = 0; j < parameters.Length; j++)
						{
							il.Emit(OpCodes.Ldarg, j + userArgumentsOffset);
						}
						il.Emit(OpCodes.Call, il.Body.Method.Module.ImportReference(to));
						if (hasReturnBuffer && returnBufferPos >= 0)
						{
							il.Emit(OpCodes.Stobj, il.Body.Method.Module.ImportReference(returnType));
						}
						if (hasReturnBuffer && this.Abi.ReturnsReturnBuffer)
						{
							il.Emit(OpCodes.Ldarg, returnBufferPos);
						}
						il.Emit(OpCodes.Ret);
						result = dmd.Generate();
					}
					return result;
				}
			}
			return to;
		}

		// Token: 0x06001CDF RID: 7391 RVA: 0x0005C6D8 File Offset: 0x0005A8D8
		private unsafe static bool HasGenericContext(Abi abi)
		{
			ReadOnlySpan<SpecialArgumentKind> arguments = abi.ArgumentOrder.Span;
			for (int i = 0; i < arguments.Length; i++)
			{
				if (*arguments[i] == 2)
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x040011C8 RID: 4552
		private static object lazyCurrentLock = new object();

		// Token: 0x040011C9 RID: 4553
		[Nullable(2)]
		private static PlatformTriple lazyCurrent;

		// Token: 0x040011CA RID: 4554
		private static readonly Func<PlatformTriple> createCurrentFunc = new Func<PlatformTriple>(PlatformTriple.CreateCurrent);

		// Token: 0x040011CD RID: 4557
		private IntPtr ThePreStub = IntPtr.Zero;

		// Token: 0x02000507 RID: 1287
		[Nullable(0)]
		public struct NativeDetour : IEquatable<PlatformTriple.NativeDetour>
		{
			// Token: 0x06001CE2 RID: 7394 RVA: 0x0005C73D File Offset: 0x0005A93D
			public NativeDetour(SimpleNativeDetour Simple, IntPtr AltEntry, [Nullable(2)] IDisposable AltHandle)
			{
				this.Simple = Simple;
				this.AltEntry = AltEntry;
				this.AltHandle = AltHandle;
			}

			// Token: 0x17000644 RID: 1604
			// (get) Token: 0x06001CE3 RID: 7395 RVA: 0x0005C754 File Offset: 0x0005A954
			// (set) Token: 0x06001CE4 RID: 7396 RVA: 0x0005C75C File Offset: 0x0005A95C
			public SimpleNativeDetour Simple { readonly get; set; }

			// Token: 0x17000645 RID: 1605
			// (get) Token: 0x06001CE5 RID: 7397 RVA: 0x0005C765 File Offset: 0x0005A965
			// (set) Token: 0x06001CE6 RID: 7398 RVA: 0x0005C76D File Offset: 0x0005A96D
			public IntPtr AltEntry { readonly get; set; }

			// Token: 0x17000646 RID: 1606
			// (get) Token: 0x06001CE7 RID: 7399 RVA: 0x0005C776 File Offset: 0x0005A976
			// (set) Token: 0x06001CE8 RID: 7400 RVA: 0x0005C77E File Offset: 0x0005A97E
			[Nullable(2)]
			public IDisposable AltHandle
			{
				[NullableContext(2)]
				readonly get;
				[NullableContext(2)]
				set;
			}

			// Token: 0x17000647 RID: 1607
			// (get) Token: 0x06001CE9 RID: 7401 RVA: 0x0005C787 File Offset: 0x0005A987
			public bool HasAltEntry
			{
				get
				{
					return this.AltEntry != IntPtr.Zero;
				}
			}

			// Token: 0x06001CEA RID: 7402 RVA: 0x0005C79C File Offset: 0x0005A99C
			[NullableContext(0)]
			[CompilerGenerated]
			public override string ToString()
			{
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.Append("NativeDetour");
				stringBuilder.Append(" { ");
				if (this.PrintMembers(stringBuilder))
				{
					stringBuilder.Append(' ');
				}
				stringBuilder.Append('}');
				return stringBuilder.ToString();
			}

			// Token: 0x06001CEB RID: 7403 RVA: 0x0005C7E8 File Offset: 0x0005A9E8
			[NullableContext(0)]
			[CompilerGenerated]
			private bool PrintMembers(StringBuilder builder)
			{
				builder.Append("Simple = ");
				builder.Append(this.Simple);
				builder.Append(", AltEntry = ");
				builder.Append(this.AltEntry.ToString());
				builder.Append(", AltHandle = ");
				builder.Append(this.AltHandle);
				builder.Append(", HasAltEntry = ");
				builder.Append(this.HasAltEntry.ToString());
				return true;
			}

			// Token: 0x06001CEC RID: 7404 RVA: 0x0005C876 File Offset: 0x0005AA76
			[CompilerGenerated]
			public static bool operator !=(PlatformTriple.NativeDetour left, PlatformTriple.NativeDetour right)
			{
				return !(left == right);
			}

			// Token: 0x06001CED RID: 7405 RVA: 0x0005C882 File Offset: 0x0005AA82
			[CompilerGenerated]
			public static bool operator ==(PlatformTriple.NativeDetour left, PlatformTriple.NativeDetour right)
			{
				return left.Equals(right);
			}

			// Token: 0x06001CEE RID: 7406 RVA: 0x0005C88C File Offset: 0x0005AA8C
			[CompilerGenerated]
			public override readonly int GetHashCode()
			{
				return (EqualityComparer<SimpleNativeDetour>.Default.GetHashCode(this.<Simple>k__BackingField) * -1521134295 + EqualityComparer<IntPtr>.Default.GetHashCode(this.<AltEntry>k__BackingField)) * -1521134295 + EqualityComparer<IDisposable>.Default.GetHashCode(this.<AltHandle>k__BackingField);
			}

			// Token: 0x06001CEF RID: 7407 RVA: 0x0005C8CC File Offset: 0x0005AACC
			[NullableContext(0)]
			[CompilerGenerated]
			public override readonly bool Equals(object obj)
			{
				return obj is PlatformTriple.NativeDetour && this.Equals((PlatformTriple.NativeDetour)obj);
			}

			// Token: 0x06001CF0 RID: 7408 RVA: 0x0005C8E4 File Offset: 0x0005AAE4
			[CompilerGenerated]
			public readonly bool Equals(PlatformTriple.NativeDetour other)
			{
				return EqualityComparer<SimpleNativeDetour>.Default.Equals(this.<Simple>k__BackingField, other.<Simple>k__BackingField) && EqualityComparer<IntPtr>.Default.Equals(this.<AltEntry>k__BackingField, other.<AltEntry>k__BackingField) && EqualityComparer<IDisposable>.Default.Equals(this.<AltHandle>k__BackingField, other.<AltHandle>k__BackingField);
			}

			// Token: 0x06001CF1 RID: 7409 RVA: 0x0005C939 File Offset: 0x0005AB39
			[CompilerGenerated]
			public readonly void Deconstruct(out SimpleNativeDetour Simple, out IntPtr AltEntry, [Nullable(2)] out IDisposable AltHandle)
			{
				Simple = this.Simple;
				AltEntry = this.AltEntry;
				AltHandle = this.AltHandle;
			}
		}
	}
}
