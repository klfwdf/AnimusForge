using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using MonoMod.Core.Interop;
using MonoMod.Core.Platforms.Memory;
using MonoMod.Utils;

namespace MonoMod.Core.Platforms.Systems
{
	// Token: 0x02000525 RID: 1317
	internal sealed class WindowsSystem : ISystem, IControlFlowGuard
	{
		// Token: 0x17000666 RID: 1638
		// (get) Token: 0x06001D8B RID: 7563 RVA: 0x0001EBDB File Offset: 0x0001CDDB
		public OSKind Target
		{
			get
			{
				return OSKind.Windows;
			}
		}

		// Token: 0x17000667 RID: 1639
		// (get) Token: 0x06001D8C RID: 7564 RVA: 0x0001B6C7 File Offset: 0x000198C7
		public SystemFeature Features
		{
			get
			{
				return SystemFeature.RWXPages;
			}
		}

		// Token: 0x17000668 RID: 1640
		// (get) Token: 0x06001D8D RID: 7565 RVA: 0x0002B871 File Offset: 0x00029A71
		[Nullable(2)]
		public INativeExceptionHelper NativeExceptionHelper
		{
			[NullableContext(2)]
			get
			{
				return null;
			}
		}

		// Token: 0x17000669 RID: 1641
		// (get) Token: 0x06001D8E RID: 7566 RVA: 0x0005F762 File Offset: 0x0005D962
		public Abi? DefaultAbi { get; }

		// Token: 0x06001D8F RID: 7567 RVA: 0x0005F76C File Offset: 0x0005D96C
		[NullableContext(1)]
		private static TypeClassification ClassifyX64(Type type, bool isReturn)
		{
			int size = type.GetManagedSize();
			bool flag = size - 1 <= 1 || size == 4 || size == 8;
			if (flag)
			{
				return TypeClassification.InRegister;
			}
			return TypeClassification.ByReference;
		}

		// Token: 0x06001D90 RID: 7568 RVA: 0x0005F79C File Offset: 0x0005D99C
		[NullableContext(1)]
		private static TypeClassification ClassifyX86(Type type, bool isReturn)
		{
			if (!isReturn)
			{
				return TypeClassification.OnStack;
			}
			int managedSize = type.GetManagedSize();
			bool flag = managedSize - 1 <= 1 || managedSize == 4;
			if (flag)
			{
				return TypeClassification.InRegister;
			}
			return TypeClassification.ByReference;
		}

		// Token: 0x06001D91 RID: 7569 RVA: 0x0005F7CC File Offset: 0x0005D9CC
		public WindowsSystem()
		{
			if (PlatformDetection.Architecture == ArchitectureKind.x86_64)
			{
				ReadOnlyMemory<SpecialArgumentKind> argumentOrder = new SpecialArgumentKind[]
				{
					SpecialArgumentKind.ReturnBuffer,
					SpecialArgumentKind.ThisPointer,
					SpecialArgumentKind.UserArguments
				};
				Classifier classifier;
				if ((classifier = WindowsSystem.<>O.<0>__ClassifyX64) == null)
				{
					classifier = (WindowsSystem.<>O.<0>__ClassifyX64 = new Classifier(WindowsSystem.ClassifyX64));
				}
				this.DefaultAbi = new Abi?(new Abi(argumentOrder, classifier, true));
				return;
			}
			if (PlatformDetection.Architecture == ArchitectureKind.x86)
			{
				ReadOnlyMemory<SpecialArgumentKind> argumentOrder2 = new SpecialArgumentKind[]
				{
					SpecialArgumentKind.ThisPointer,
					SpecialArgumentKind.ReturnBuffer,
					SpecialArgumentKind.UserArguments
				};
				Classifier classifier2;
				if ((classifier2 = WindowsSystem.<>O.<1>__ClassifyX86) == null)
				{
					classifier2 = (WindowsSystem.<>O.<1>__ClassifyX86 = new Classifier(WindowsSystem.ClassifyX86));
				}
				this.DefaultAbi = new Abi?(new Abi(argumentOrder2, classifier2, true));
			}
		}

		// Token: 0x06001D92 RID: 7570 RVA: 0x0005F880 File Offset: 0x0005DA80
		public unsafe void PatchData(PatchTargetKind patchKind, IntPtr patchTarget, ReadOnlySpan<byte> data, Span<byte> backup)
		{
			if (patchKind == PatchTargetKind.Executable)
			{
				WindowsSystem.ProtectRWX(patchTarget, (UIntPtr)((IntPtr)data.Length));
			}
			else
			{
				WindowsSystem.ProtectRW(patchTarget, (UIntPtr)((IntPtr)data.Length));
			}
			Span<byte> target = new Span<byte>((void*)patchTarget, data.Length);
			target.TryCopyTo(backup);
			data.CopyTo(target);
			if (patchKind == PatchTargetKind.Executable)
			{
				WindowsSystem.FlushInstructionCache(patchTarget, (UIntPtr)((IntPtr)data.Length));
			}
		}

		// Token: 0x06001D93 RID: 7571 RVA: 0x0005F8E8 File Offset: 0x0005DAE8
		private unsafe static void ProtectRW(IntPtr addr, [NativeInteger] UIntPtr size)
		{
			uint oldProtect;
			if (!Windows.VirtualProtect((void*)addr, size, 4U, &oldProtect))
			{
				throw WindowsSystem.LogAllSections(Windows.GetLastError(), addr, size, "ProtectRW");
			}
		}

		// Token: 0x06001D94 RID: 7572 RVA: 0x0005F920 File Offset: 0x0005DB20
		private unsafe static void ProtectRWX(IntPtr addr, [NativeInteger] UIntPtr size)
		{
			uint oldProtect;
			if (!Windows.VirtualProtect((void*)addr, size, 64U, &oldProtect))
			{
				throw WindowsSystem.LogAllSections(Windows.GetLastError(), addr, size, "ProtectRWX");
			}
		}

		// Token: 0x06001D95 RID: 7573 RVA: 0x0005F957 File Offset: 0x0005DB57
		private unsafe static void FlushInstructionCache(IntPtr addr, [NativeInteger] UIntPtr size)
		{
			if (!Windows.FlushInstructionCache(Windows.GetCurrentProcess(), (void*)addr, size))
			{
				throw WindowsSystem.LogAllSections(Windows.GetLastError(), addr, size, "FlushInstructionCache");
			}
		}

		// Token: 0x06001D96 RID: 7574 RVA: 0x0005F983 File Offset: 0x0005DB83
		[return: Nullable(new byte[] { 1, 2 })]
		public IEnumerable<string> EnumerateLoadedModuleFiles()
		{
			return from ProcessModule m in Process.GetCurrentProcess().Modules
				select m.FileName;
		}

		// Token: 0x06001D97 RID: 7575 RVA: 0x0005F9B8 File Offset: 0x0005DBB8
		[return: NativeInteger]
		public unsafe IntPtr GetSizeOfReadableMemory([NativeInteger] IntPtr start, [NativeInteger] IntPtr guess)
		{
			IntPtr knownSize = (IntPtr)0;
			Windows.MEMORY_BASIC_INFORMATION buf;
			bool flag;
			while (Windows.VirtualQuery(start, &buf, (UIntPtr)((IntPtr)sizeof(Windows.MEMORY_BASIC_INFORMATION))) != 0)
			{
				<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogSpamStringHandler debugLogSpamStringHandler = new <24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogSpamStringHandler(56, 4, ref flag);
				if (flag)
				{
					debugLogSpamStringHandler.AppendLiteral("VirtualQuery(0x");
					debugLogSpamStringHandler.AppendFormatted<IntPtr>(start, "x16");
					debugLogSpamStringHandler.AppendLiteral(") == { Protect = ");
					debugLogSpamStringHandler.AppendFormatted<uint>(buf.Protect, "x");
					debugLogSpamStringHandler.AppendLiteral(", BaseAddr = ");
					debugLogSpamStringHandler.AppendFormatted<UIntPtr>(buf.BaseAddress, "x16");
					debugLogSpamStringHandler.AppendLiteral(", Size = ");
					debugLogSpamStringHandler.AppendFormatted<UIntPtr>(buf.RegionSize, "x4");
					debugLogSpamStringHandler.AppendLiteral(" }");
				}
				<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.Spam(ref debugLogSpamStringHandler);
				if ((buf.Protect & 102U) <= 0U)
				{
					return knownSize;
				}
				IntPtr nextPage = (byte*)buf.BaseAddress + buf.RegionSize;
				knownSize += nextPage - start;
				start = nextPage;
				if (knownSize >= guess)
				{
					return knownSize;
				}
			}
			uint lastError = Windows.GetLastError();
			<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogWarningStringHandler debugLogWarningStringHandler = new <24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogWarningStringHandler(22, 2, ref flag);
			if (flag)
			{
				debugLogWarningStringHandler.AppendLiteral("VirtualQuery failed: ");
				debugLogWarningStringHandler.AppendFormatted<uint>(lastError);
				debugLogWarningStringHandler.AppendLiteral(" ");
				debugLogWarningStringHandler.AppendFormatted(new Win32Exception((int)lastError).Message);
			}
			<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.Warning(ref debugLogWarningStringHandler);
			return (IntPtr)0;
		}

		// Token: 0x06001D98 RID: 7576 RVA: 0x0005FAF4 File Offset: 0x0005DCF4
		[NullableContext(1)]
		private unsafe static Exception LogAllSections(uint error, IntPtr src, [NativeInteger] UIntPtr size, [CallerMemberName] string from = "")
		{
			Exception ex = new Win32Exception((int)error);
			if (!<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.IsWritingLog)
			{
				return ex;
			}
			bool flag;
			<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogErrorStringHandler debugLogErrorStringHandler = new <24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogErrorStringHandler(47, 3, ref flag);
			if (flag)
			{
				debugLogErrorStringHandler.AppendFormatted(from);
				debugLogErrorStringHandler.AppendLiteral(" failed for 0x");
				debugLogErrorStringHandler.AppendFormatted<IntPtr>(src, "X16");
				debugLogErrorStringHandler.AppendLiteral(" + ");
				debugLogErrorStringHandler.AppendFormatted<UIntPtr>(size);
				debugLogErrorStringHandler.AppendLiteral(" - logging all memory sections");
			}
			<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.Error(ref debugLogErrorStringHandler);
			<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogErrorStringHandler debugLogErrorStringHandler2 = new <24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogErrorStringHandler(8, 1, ref flag);
			if (flag)
			{
				debugLogErrorStringHandler2.AppendLiteral("reason: ");
				debugLogErrorStringHandler2.AppendFormatted(ex.Message);
			}
			<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.Error(ref debugLogErrorStringHandler2);
			try
			{
				IntPtr addr = (IntPtr)65536;
				int i = 0;
				Windows.MEMORY_BASIC_INFORMATION infoBasic;
				while (Windows.VirtualQuery((void*)addr, &infoBasic, (UIntPtr)((IntPtr)sizeof(Windows.MEMORY_BASIC_INFORMATION))) != 0)
				{
					UIntPtr srcR = (UIntPtr)(src + (IntPtr)size);
					void* baseAddress = infoBasic.BaseAddress;
					UIntPtr infoR = (byte*)baseAddress + infoBasic.RegionSize;
					bool overlap = baseAddress == srcR && src <= (IntPtr)infoR;
					<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogTraceStringHandler debugLogTraceStringHandler = new <24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogTraceStringHandler(2, 2, ref flag);
					if (flag)
					{
						debugLogTraceStringHandler.AppendFormatted(overlap ? "*" : "-");
						debugLogTraceStringHandler.AppendLiteral(" #");
						debugLogTraceStringHandler.AppendFormatted<int>(i++);
					}
					<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.Trace(ref debugLogTraceStringHandler);
					<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogTraceStringHandler debugLogTraceStringHandler2 = new <24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogTraceStringHandler(8, 1, ref flag);
					if (flag)
					{
						debugLogTraceStringHandler2.AppendLiteral("addr: 0x");
						debugLogTraceStringHandler2.AppendFormatted<UIntPtr>(infoBasic.BaseAddress, "X16");
					}
					<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.Trace(ref debugLogTraceStringHandler2);
					<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogTraceStringHandler debugLogTraceStringHandler3 = new <24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogTraceStringHandler(8, 1, ref flag);
					if (flag)
					{
						debugLogTraceStringHandler3.AppendLiteral("size: 0x");
						debugLogTraceStringHandler3.AppendFormatted<UIntPtr>(infoBasic.RegionSize, "X16");
					}
					<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.Trace(ref debugLogTraceStringHandler3);
					<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogTraceStringHandler debugLogTraceStringHandler4 = new <24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogTraceStringHandler(9, 1, ref flag);
					if (flag)
					{
						debugLogTraceStringHandler4.AppendLiteral("aaddr: 0x");
						debugLogTraceStringHandler4.AppendFormatted<UIntPtr>(infoBasic.AllocationBase, "X16");
					}
					<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.Trace(ref debugLogTraceStringHandler4);
					<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogTraceStringHandler debugLogTraceStringHandler5 = new <24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogTraceStringHandler(7, 1, ref flag);
					if (flag)
					{
						debugLogTraceStringHandler5.AppendLiteral("state: ");
						debugLogTraceStringHandler5.AppendFormatted<uint>(infoBasic.State);
					}
					<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.Trace(ref debugLogTraceStringHandler5);
					<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogTraceStringHandler debugLogTraceStringHandler6 = new <24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogTraceStringHandler(6, 1, ref flag);
					if (flag)
					{
						debugLogTraceStringHandler6.AppendLiteral("type: ");
						debugLogTraceStringHandler6.AppendFormatted<uint>(infoBasic.Type);
					}
					<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.Trace(ref debugLogTraceStringHandler6);
					<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogTraceStringHandler debugLogTraceStringHandler7 = new <24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogTraceStringHandler(9, 1, ref flag);
					if (flag)
					{
						debugLogTraceStringHandler7.AppendLiteral("protect: ");
						debugLogTraceStringHandler7.AppendFormatted<uint>(infoBasic.Protect);
					}
					<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.Trace(ref debugLogTraceStringHandler7);
					<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogTraceStringHandler debugLogTraceStringHandler8 = new <24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogTraceStringHandler(10, 1, ref flag);
					if (flag)
					{
						debugLogTraceStringHandler8.AppendLiteral("aprotect: ");
						debugLogTraceStringHandler8.AppendFormatted<uint>(infoBasic.AllocationProtect);
					}
					<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.Trace(ref debugLogTraceStringHandler8);
					try
					{
						IntPtr addrPrev = addr;
						addr = (IntPtr)((byte*)infoBasic.BaseAddress + (ulong)infoBasic.RegionSize);
						if ((long)addr > (long)addrPrev)
						{
							continue;
						}
					}
					catch (OverflowException oe)
					{
						<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogErrorStringHandler debugLogErrorStringHandler3 = new <24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogErrorStringHandler(9, 1, ref flag);
						if (flag)
						{
							debugLogErrorStringHandler3.AppendLiteral("overflow ");
							debugLogErrorStringHandler3.AppendFormatted<OverflowException>(oe);
						}
						<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.Error(ref debugLogErrorStringHandler3);
					}
					break;
				}
			}
			catch
			{
				throw ex;
			}
			return ex;
		}

		// Token: 0x1700066A RID: 1642
		// (get) Token: 0x06001D99 RID: 7577 RVA: 0x0005FE28 File Offset: 0x0005E028
		[Nullable(1)]
		public IMemoryAllocator MemoryAllocator
		{
			[NullableContext(1)]
			get;
		} = new QueryingPagedMemoryAllocator(new WindowsSystem.PageAllocator());

		// Token: 0x1700066B RID: 1643
		// (get) Token: 0x06001D9A RID: 7578 RVA: 0x0005FE30 File Offset: 0x0005E030
		bool IControlFlowGuard.IsSupported
		{
			get
			{
				return Windows.HasSetProcessValidCallTargets;
			}
		}

		// Token: 0x1700066C RID: 1644
		// (get) Token: 0x06001D9B RID: 7579 RVA: 0x0005FE37 File Offset: 0x0005E037
		int IControlFlowGuard.TargetAlignmentRequirement
		{
			get
			{
				return 16;
			}
		}

		// Token: 0x06001D9C RID: 7580 RVA: 0x0005FE3C File Offset: 0x0005E03C
		unsafe void IControlFlowGuard.RegisterValidIndirectCallTargets(void* memoryRegionStart, [NativeInteger] IntPtr memoryRegionLength, [NativeInteger] ReadOnlySpan<IntPtr> validTargetsInMemoryRegion)
		{
			Windows.CFG_CALL_TARGET_INFO[] callTargetInfos = ArrayPool<Windows.CFG_CALL_TARGET_INFO>.Shared.Rent(validTargetsInMemoryRegion.Length);
			for (int i = 0; i < validTargetsInMemoryRegion.Length; i++)
			{
				IntPtr offset = *validTargetsInMemoryRegion[i];
				callTargetInfos[i] = new Windows.CFG_CALL_TARGET_INFO
				{
					Offset = (UIntPtr)offset,
					Flags = (UIntPtr)((IntPtr)9)
				};
			}
			Windows.CFG_CALL_TARGET_INFO[] array;
			Windows.CFG_CALL_TARGET_INFO* pCallTargets;
			if ((array = callTargetInfos) == null || array.Length == 0)
			{
				pCallTargets = null;
			}
			else
			{
				pCallTargets = &array[0];
			}
			Windows.TrySetProcessValidCallTargets(memoryRegionStart, (UIntPtr)memoryRegionLength, (uint)validTargetsInMemoryRegion.Length, pCallTargets);
			array = null;
			ArrayPool<Windows.CFG_CALL_TARGET_INFO>.Shared.Return(callTargetInfos, false);
		}

		// Token: 0x06001D9D RID: 7581 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public IntPtr GetNativeJitHookConfig(int runtimeMajMin)
		{
			throw new NotImplementedException();
		}

		// Token: 0x02000526 RID: 1318
		private sealed class PageAllocator : QueryingMemoryPageAllocatorBase
		{
			// Token: 0x1700066D RID: 1645
			// (get) Token: 0x06001D9E RID: 7582 RVA: 0x0005FED6 File Offset: 0x0005E0D6
			public override uint PageSize { get; }

			// Token: 0x06001D9F RID: 7583 RVA: 0x0005FEE0 File Offset: 0x0005E0E0
			public unsafe PageAllocator()
			{
				Windows.SYSTEM_INFO sysInfo;
				Windows.GetSystemInfo(&sysInfo);
				this.PageSize = sysInfo.dwAllocationGranularity;
			}

			// Token: 0x06001DA0 RID: 7584 RVA: 0x0005FF08 File Offset: 0x0005E108
			public override bool TryAllocatePage([NativeInteger] IntPtr size, bool executable, out IntPtr allocated)
			{
				int pageProt = (executable ? 64 : 4);
				allocated = (IntPtr)Windows.VirtualAlloc(null, (UIntPtr)size, 12288U, (uint)pageProt);
				return allocated != IntPtr.Zero;
			}

			// Token: 0x06001DA1 RID: 7585 RVA: 0x0005FF40 File Offset: 0x0005E140
			public unsafe override bool TryAllocatePage(IntPtr pageAddr, [NativeInteger] IntPtr size, bool executable, out IntPtr allocated)
			{
				int pageProt = (executable ? 64 : 4);
				allocated = (IntPtr)Windows.VirtualAlloc((void*)pageAddr, (UIntPtr)size, 12288U, (uint)pageProt);
				return allocated != IntPtr.Zero;
			}

			// Token: 0x06001DA2 RID: 7586 RVA: 0x0005FF7D File Offset: 0x0005E17D
			[NullableContext(2)]
			public unsafe override bool TryFreePage(IntPtr pageAddr, [<24b3ba8a-00b7-40fc-a603-2711fa115297>NotNullWhen(false)] out string errorMsg)
			{
				if (!Windows.VirtualFree((void*)pageAddr, (UIntPtr)((IntPtr)0), 32768U))
				{
					errorMsg = new Win32Exception((int)Windows.GetLastError()).Message;
					return false;
				}
				errorMsg = null;
				return true;
			}

			// Token: 0x06001DA3 RID: 7587 RVA: 0x0005FFB0 File Offset: 0x0005E1B0
			public unsafe override bool TryQueryPage(IntPtr pageAddr, out bool isFree, out IntPtr allocBase, [NativeInteger] out IntPtr allocSize)
			{
				Windows.MEMORY_BASIC_INFORMATION buffer;
				if (Windows.VirtualQuery((void*)pageAddr, &buffer, (UIntPtr)((IntPtr)sizeof(Windows.MEMORY_BASIC_INFORMATION))) != 0)
				{
					isFree = buffer.State == 65536U;
					allocBase = (isFree ? buffer.BaseAddress : buffer.AllocationBase);
					allocSize = pageAddr + (IntPtr)buffer.RegionSize - allocBase;
					return true;
				}
				isFree = false;
				allocBase = IntPtr.Zero;
				allocSize = (IntPtr)0;
				return false;
			}
		}

		// Token: 0x02000527 RID: 1319
		[CompilerGenerated]
		private static class <>O
		{
			// Token: 0x0400122E RID: 4654
			public static Classifier <0>__ClassifyX64;

			// Token: 0x0400122F RID: 4655
			public static Classifier <1>__ClassifyX86;
		}
	}
}
