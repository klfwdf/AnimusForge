using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using MonoMod.Utils;

namespace MonoMod.Core.Platforms.Architectures
{
	// Token: 0x0200055A RID: 1370
	internal static class Shared
	{
		// Token: 0x06001EBB RID: 7867 RVA: 0x00064974 File Offset: 0x00062B74
		[NullableContext(1)]
		public unsafe static IAllocatedMemory CreateSingleExecutableStub(ISystem system, [Nullable(0)] ReadOnlySpan<byte> stubBytes)
		{
			IControlFlowGuard cfg = system as IControlFlowGuard;
			if (cfg != null && !cfg.IsSupported)
			{
				cfg = null;
			}
			IAllocatedMemory alloc;
			Helpers.Assert(system.MemoryAllocator.TryAllocate(new AllocationRequest(stubBytes.Length)
			{
				Executable = true,
				Alignment = ((cfg != null) ? cfg.TargetAlignmentRequirement : 1)
			}, out alloc), null, "system.MemoryAllocator.TryAllocate(new(stubBytes.Length)\n            {\n                Executable = true,\n                Alignment = cfg is not null ? cfg.TargetAlignmentRequirement : 1, // if CFG is supported, use that alignment\n            }, out var alloc)");
			system.PatchData(PatchTargetKind.Executable, alloc.BaseAddress, stubBytes, default(Span<byte>));
			if (cfg != null)
			{
				IControlFlowGuard controlFlowGuard = cfg;
				void* memoryRegionStart = (void*)alloc.BaseAddress;
				IntPtr memoryRegionLength = (IntPtr)alloc.Size;
				IntPtr[] array;
				if ((array = <24b3ba8a-00b7-40fc-a603-2711fa115297><PrivateImplementationDetails>.DF3F619804A92FDB4057192DC43DD748EA778ADC52BC498CE80524C014B81119_B8) == null)
				{
					array = (<24b3ba8a-00b7-40fc-a603-2711fa115297><PrivateImplementationDetails>.DF3F619804A92FDB4057192DC43DD748EA778ADC52BC498CE80524C014B81119_B8 = new IntPtr[1]);
				}
				controlFlowGuard.RegisterValidIndirectCallTargets(memoryRegionStart, memoryRegionLength, new ReadOnlySpan<IntPtr>(array));
			}
			return alloc;
		}

		// Token: 0x06001EBC RID: 7868 RVA: 0x00064A24 File Offset: 0x00062C24
		[return: Nullable(new byte[] { 0, 1 })]
		public unsafe static ReadOnlyMemory<IAllocatedMemory> CreateVtableStubs([Nullable(1)] ISystem system, IntPtr vtableBase, int vtableSize, ReadOnlySpan<byte> stubData, int indexOffs, bool premulOffset)
		{
			IControlFlowGuard cfg = system as IControlFlowGuard;
			if (cfg != null && !cfg.IsSupported)
			{
				cfg = null;
			}
			int stubSize = stubData.Length;
			if (cfg != null)
			{
				int requiredAlign = cfg.TargetAlignmentRequirement;
				stubSize = ((stubSize - 1) / requiredAlign + 1) * requiredAlign;
			}
			int maxAllocSize = system.MemoryAllocator.MaxSize;
			int num = stubSize * vtableSize;
			int numMainAllocs = num / maxAllocSize;
			int numPerAlloc = maxAllocSize / stubSize;
			int mainAllocSize = numPerAlloc * stubSize;
			int lastAllocSize = num % mainAllocSize;
			IAllocatedMemory[] allocs = new IAllocatedMemory[numMainAllocs + ((lastAllocSize != 0) ? 1 : 0)];
			byte[] mainAllocArr = ArrayPool<byte>.Shared.Rent(mainAllocSize);
			IntPtr[] offsetArr = ArrayPool<IntPtr>.Shared.Rent(numPerAlloc);
			Span<byte> mainAllocBuf = mainAllocArr.AsSpan<byte>().Slice(0, mainAllocSize);
			for (int i = 0; i < numPerAlloc; i++)
			{
				stubData.CopyTo(mainAllocBuf.Slice(i * stubSize));
			}
			ref IntPtr vtblBase = ref Unsafe.AsRef<IntPtr>((void*)vtableBase);
			AllocationRequest allocationRequest = new AllocationRequest(mainAllocSize)
			{
				Alignment = ((cfg != null) ? cfg.TargetAlignmentRequirement : IntPtr.Size),
				Executable = true
			};
			AllocationRequest allocReq = allocationRequest;
			for (int j = 0; j < numMainAllocs; j++)
			{
				IAllocatedMemory alloc;
				Helpers.Assert(system.MemoryAllocator.TryAllocate(allocReq, out alloc), null, "system.MemoryAllocator.TryAllocate(allocReq, out var alloc)");
				allocs[j] = alloc;
				Shared.<CreateVtableStubs>g__FillBufferIndicies|1_0(stubSize, indexOffs, numPerAlloc, j, mainAllocBuf, premulOffset);
				Shared.<CreateVtableStubs>g__FillVtbl|1_1(stubSize, numPerAlloc * j, ref vtblBase, numPerAlloc, alloc.BaseAddress, offsetArr);
				system.PatchData(PatchTargetKind.Executable, alloc.BaseAddress, mainAllocBuf, default(Span<byte>));
				if (cfg != null)
				{
					cfg.RegisterValidIndirectCallTargets((void*)alloc.BaseAddress, (IntPtr)alloc.Size, offsetArr.AsSpan(0, numPerAlloc));
				}
			}
			if (lastAllocSize > 0)
			{
				allocationRequest = allocReq;
				allocationRequest.Size = lastAllocSize;
				allocReq = allocationRequest;
				IAllocatedMemory alloc2;
				Helpers.Assert(system.MemoryAllocator.TryAllocate(allocReq, out alloc2), null, "system.MemoryAllocator.TryAllocate(allocReq, out var alloc)");
				allocs[allocs.Length - 1] = alloc2;
				int numEntries = lastAllocSize / stubSize;
				Shared.<CreateVtableStubs>g__FillBufferIndicies|1_0(stubSize, indexOffs, numPerAlloc, numMainAllocs, mainAllocBuf, premulOffset);
				Shared.<CreateVtableStubs>g__FillVtbl|1_1(stubSize, numPerAlloc * numMainAllocs, ref vtblBase, numEntries, alloc2.BaseAddress, offsetArr);
				system.PatchData(PatchTargetKind.Executable, alloc2.BaseAddress, mainAllocBuf.Slice(0, lastAllocSize), default(Span<byte>));
				if (cfg != null)
				{
					cfg.RegisterValidIndirectCallTargets((void*)alloc2.BaseAddress, (IntPtr)alloc2.Size, offsetArr.AsSpan(0, numEntries));
				}
			}
			ArrayPool<IntPtr>.Shared.Return(offsetArr, false);
			ArrayPool<byte>.Shared.Return(mainAllocArr, false);
			return allocs;
		}

		// Token: 0x06001EBD RID: 7869 RVA: 0x00064CAC File Offset: 0x00062EAC
		[CompilerGenerated]
		internal static void <CreateVtableStubs>g__FillBufferIndicies|1_0(int stubSize, int indexOffs, int numPerAlloc, int i, Span<byte> mainAllocBuf, bool premul)
		{
			for (int j = 0; j < numPerAlloc; j++)
			{
				ref byte destination = ref mainAllocBuf[j * stubSize + indexOffs];
				uint index = (uint)(numPerAlloc * i + j);
				if (premul)
				{
					index *= (uint)IntPtr.Size;
				}
				Unsafe.WriteUnaligned<uint>(ref destination, index);
			}
		}

		// Token: 0x06001EBE RID: 7870 RVA: 0x00064CEC File Offset: 0x00062EEC
		[NullableContext(1)]
		[CompilerGenerated]
		internal unsafe static void <CreateVtableStubs>g__FillVtbl|1_1(int stubSize, int baseIndex, ref IntPtr vtblBase, int numEntries, [NativeInteger] IntPtr baseAddr, [NativeInteger] IntPtr[] offsets)
		{
			for (int i = 0; i < numEntries; i++)
			{
				IntPtr offs = (offsets[i] = (IntPtr)(stubSize * i));
				*Unsafe.Add<IntPtr>(ref vtblBase, baseIndex + i) = baseAddr + offs;
			}
		}
	}
}
