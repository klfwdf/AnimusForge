using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MonoMod.Utils;

namespace MonoMod.Core.Interop
{
	// Token: 0x020005FB RID: 1531
	internal static class Windows
	{
		// Token: 0x06002073 RID: 8307
		[DllImport("kernel32", ExactSpelling = true)]
		public unsafe static extern void* VirtualAlloc(void* lpAddress, [NativeInteger] UIntPtr dwSize, uint flAllocationType, uint flProtect);

		// Token: 0x06002074 RID: 8308
		[DllImport("kernel32", ExactSpelling = true)]
		public unsafe static extern Windows.BOOL VirtualProtect(void* lpAddress, [NativeInteger] UIntPtr dwSize, uint flNewProtect, uint* lpflOldProtect);

		// Token: 0x06002075 RID: 8309
		[DllImport("kernel32", ExactSpelling = true)]
		public unsafe static extern Windows.BOOL VirtualFree(void* lpAddress, [NativeInteger] UIntPtr dwSize, uint dwFreeType);

		// Token: 0x06002076 RID: 8310
		[DllImport("kernel32", ExactSpelling = true)]
		[return: NativeInteger]
		public unsafe static extern UIntPtr VirtualQuery(void* lpAddress, Windows.MEMORY_BASIC_INFORMATION* lpBuffer, [NativeInteger] UIntPtr dwLength);

		// Token: 0x06002077 RID: 8311
		[DllImport("kernel32", ExactSpelling = true)]
		public unsafe static extern void GetSystemInfo(Windows.SYSTEM_INFO* lpSystemInfo);

		// Token: 0x06002078 RID: 8312
		[DllImport("kernel32", ExactSpelling = true)]
		public static extern Windows.HANDLE GetCurrentProcess();

		// Token: 0x06002079 RID: 8313
		[DllImport("kernel32", ExactSpelling = true)]
		public unsafe static extern Windows.BOOL FlushInstructionCache(Windows.HANDLE hProcess, void* lpBaseAddress, [NativeInteger] UIntPtr dwSize);

		// Token: 0x0600207A RID: 8314
		[DllImport("kernel32", ExactSpelling = true)]
		public static extern uint GetLastError();

		// Token: 0x0600207B RID: 8315
		[DllImport("kernelbase", ExactSpelling = true)]
		private unsafe static extern Windows.BOOL SetProcessValidCallTargets(Windows.HANDLE hProcess, void* VirtualAddress, [NativeInteger] UIntPtr RegionSize, uint NumberOfOffsets, Windows.CFG_CALL_TARGET_INFO* OffsetInformation);

		// Token: 0x1700074D RID: 1869
		// (get) Token: 0x0600207C RID: 8316 RVA: 0x00067EC4 File Offset: 0x000660C4
		public static bool HasSetProcessValidCallTargets
		{
			get
			{
				bool flag = Windows.hasSetProcessValidCallTargets.GetValueOrDefault();
				if (Windows.hasSetProcessValidCallTargets == null)
				{
					flag = Windows.<get_HasSetProcessValidCallTargets>g__DoGet|101_0();
					Windows.hasSetProcessValidCallTargets = new bool?(flag);
					return flag;
				}
				return flag;
			}
		}

		// Token: 0x0600207D RID: 8317 RVA: 0x00067EFC File Offset: 0x000660FC
		public unsafe static Windows.BOOL TrySetProcessValidCallTargets(void* VirtualAddress, [NativeInteger] UIntPtr RegionSize, uint NumberOfOffsets, Windows.CFG_CALL_TARGET_INFO* OffsetInformation)
		{
			if (Windows.HasSetProcessValidCallTargets)
			{
				return Windows.SetProcessValidCallTargets(Windows.GetCurrentProcess(), VirtualAddress, RegionSize, NumberOfOffsets, OffsetInformation);
			}
			return false;
		}

		// Token: 0x0600207E RID: 8318 RVA: 0x00067F1C File Offset: 0x0006611C
		[CompilerGenerated]
		[MethodImpl(MethodImplOptions.NoInlining)]
		internal static bool <get_HasSetProcessValidCallTargets>g__DoGet|101_0()
		{
			IntPtr kbase = DynDll.OpenLibrary("kernelbase");
			bool result;
			try
			{
				IntPtr intPtr;
				result = kbase.TryGetExport("SetProcessValidCallTargets", out intPtr);
			}
			finally
			{
				DynDll.CloseLibrary(kbase);
			}
			return result;
		}

		// Token: 0x040015D2 RID: 5586
		public const int MEM_COMMIT = 4096;

		// Token: 0x040015D3 RID: 5587
		public const int MEM_RESERVE = 8192;

		// Token: 0x040015D4 RID: 5588
		public const int MEM_REPLACE_PLACEHOLDER = 16384;

		// Token: 0x040015D5 RID: 5589
		public const int MEM_RESERVE_PLACEHOLDER = 262144;

		// Token: 0x040015D6 RID: 5590
		public const int MEM_RESET = 524288;

		// Token: 0x040015D7 RID: 5591
		public const int MEM_TOP_DOWN = 1048576;

		// Token: 0x040015D8 RID: 5592
		public const int MEM_WRITE_WATCH = 2097152;

		// Token: 0x040015D9 RID: 5593
		public const int MEM_PHYSICAL = 4194304;

		// Token: 0x040015DA RID: 5594
		public const int MEM_ROTATE = 8388608;

		// Token: 0x040015DB RID: 5595
		public const int MEM_DIFFERENT_IMAGE_BASE_OK = 8388608;

		// Token: 0x040015DC RID: 5596
		public const int MEM_RESET_UNDO = 16777216;

		// Token: 0x040015DD RID: 5597
		public const int MEM_LARGE_PAGES = 536870912;

		// Token: 0x040015DE RID: 5598
		public const uint MEM_4MB_PAGES = 2147483648U;

		// Token: 0x040015DF RID: 5599
		public const int MEM_64K_PAGES = 541065216;

		// Token: 0x040015E0 RID: 5600
		public const int MEM_UNMAP_WITH_TRANSIENT_BOOST = 1;

		// Token: 0x040015E1 RID: 5601
		public const int MEM_COALESCE_PLACEHOLDERS = 1;

		// Token: 0x040015E2 RID: 5602
		public const int MEM_PRESERVE_PLACEHOLDER = 2;

		// Token: 0x040015E3 RID: 5603
		public const int MEM_DECOMMIT = 16384;

		// Token: 0x040015E4 RID: 5604
		public const int MEM_RELEASE = 32768;

		// Token: 0x040015E5 RID: 5605
		public const int MEM_FREE = 65536;

		// Token: 0x040015E6 RID: 5606
		public const int MEM_EXTENDED_PARAMETER_GRAPHICS = 1;

		// Token: 0x040015E7 RID: 5607
		public const int MEM_EXTENDED_PARAMETER_NONPAGED = 2;

		// Token: 0x040015E8 RID: 5608
		public const int MEM_EXTENDED_PARAMETER_ZERO_PAGES_OPTIONAL = 4;

		// Token: 0x040015E9 RID: 5609
		public const int MEM_EXTENDED_PARAMETER_NONPAGED_LARGE = 8;

		// Token: 0x040015EA RID: 5610
		public const int MEM_EXTENDED_PARAMETER_NONPAGED_HUGE = 16;

		// Token: 0x040015EB RID: 5611
		public const int MEM_EXTENDED_PARAMETER_SOFT_FAULT_PAGES = 32;

		// Token: 0x040015EC RID: 5612
		public const int MEM_EXTENDED_PARAMETER_EC_CODE = 64;

		// Token: 0x040015ED RID: 5613
		public const int MEM_EXTENDED_PARAMETER_IMAGE_NO_HPAT = 128;

		// Token: 0x040015EE RID: 5614
		public const long MEM_EXTENDED_PARAMETER_NUMA_NODE_MANDATORY = -9223372036854775808L;

		// Token: 0x040015EF RID: 5615
		public const int MEM_EXTENDED_PARAMETER_TYPE_BITS = 8;

		// Token: 0x040015F0 RID: 5616
		public const ulong MEM_DEDICATED_ATTRIBUTE_NOT_SPECIFIED = 18446744073709551615UL;

		// Token: 0x040015F1 RID: 5617
		public const int MEM_PRIVATE = 131072;

		// Token: 0x040015F2 RID: 5618
		public const int MEM_MAPPED = 262144;

		// Token: 0x040015F3 RID: 5619
		public const int MEM_IMAGE = 16777216;

		// Token: 0x040015F4 RID: 5620
		public const int PAGE_NOACCESS = 1;

		// Token: 0x040015F5 RID: 5621
		public const int PAGE_READONLY = 2;

		// Token: 0x040015F6 RID: 5622
		public const int PAGE_READWRITE = 4;

		// Token: 0x040015F7 RID: 5623
		public const int PAGE_WRITECOPY = 8;

		// Token: 0x040015F8 RID: 5624
		public const int PAGE_EXECUTE = 16;

		// Token: 0x040015F9 RID: 5625
		public const int PAGE_EXECUTE_READ = 32;

		// Token: 0x040015FA RID: 5626
		public const int PAGE_EXECUTE_READWRITE = 64;

		// Token: 0x040015FB RID: 5627
		public const int PAGE_EXECUTE_WRITECOPY = 128;

		// Token: 0x040015FC RID: 5628
		public const int PAGE_GUARD = 256;

		// Token: 0x040015FD RID: 5629
		public const int PAGE_NOCACHE = 512;

		// Token: 0x040015FE RID: 5630
		public const int PAGE_WRITECOMBINE = 1024;

		// Token: 0x040015FF RID: 5631
		public const int PAGE_GRAPHICS_NOACCESS = 2048;

		// Token: 0x04001600 RID: 5632
		public const int PAGE_GRAPHICS_READONLY = 4096;

		// Token: 0x04001601 RID: 5633
		public const int PAGE_GRAPHICS_READWRITE = 8192;

		// Token: 0x04001602 RID: 5634
		public const int PAGE_GRAPHICS_EXECUTE = 16384;

		// Token: 0x04001603 RID: 5635
		public const int PAGE_GRAPHICS_EXECUTE_READ = 32768;

		// Token: 0x04001604 RID: 5636
		public const int PAGE_GRAPHICS_EXECUTE_READWRITE = 65536;

		// Token: 0x04001605 RID: 5637
		public const int PAGE_GRAPHICS_COHERENT = 131072;

		// Token: 0x04001606 RID: 5638
		public const int PAGE_GRAPHICS_NOCACHE = 262144;

		// Token: 0x04001607 RID: 5639
		public const uint PAGE_ENCLAVE_THREAD_CONTROL = 2147483648U;

		// Token: 0x04001608 RID: 5640
		public const uint PAGE_REVERT_TO_FILE_MAP = 2147483648U;

		// Token: 0x04001609 RID: 5641
		public const int PAGE_TARGETS_NO_UPDATE = 1073741824;

		// Token: 0x0400160A RID: 5642
		public const int PAGE_TARGETS_INVALID = 1073741824;

		// Token: 0x0400160B RID: 5643
		public const int PAGE_ENCLAVE_UNVALIDATED = 536870912;

		// Token: 0x0400160C RID: 5644
		public const int PAGE_ENCLAVE_MASK = 268435456;

		// Token: 0x0400160D RID: 5645
		public const int PAGE_ENCLAVE_DECOMMIT = 268435456;

		// Token: 0x0400160E RID: 5646
		public const int PAGE_ENCLAVE_SS_FIRST = 268435457;

		// Token: 0x0400160F RID: 5647
		public const int PAGE_ENCLAVE_SS_REST = 268435458;

		// Token: 0x04001610 RID: 5648
		public const int PROCESSOR_ARCHITECTURE_INTEL = 0;

		// Token: 0x04001611 RID: 5649
		public const int PROCESSOR_ARCHITECTURE_MIPS = 1;

		// Token: 0x04001612 RID: 5650
		public const int PROCESSOR_ARCHITECTURE_ALPHA = 2;

		// Token: 0x04001613 RID: 5651
		public const int PROCESSOR_ARCHITECTURE_PPC = 3;

		// Token: 0x04001614 RID: 5652
		public const int PROCESSOR_ARCHITECTURE_SHX = 4;

		// Token: 0x04001615 RID: 5653
		public const int PROCESSOR_ARCHITECTURE_ARM = 5;

		// Token: 0x04001616 RID: 5654
		public const int PROCESSOR_ARCHITECTURE_IA64 = 6;

		// Token: 0x04001617 RID: 5655
		public const int PROCESSOR_ARCHITECTURE_ALPHA64 = 7;

		// Token: 0x04001618 RID: 5656
		public const int PROCESSOR_ARCHITECTURE_MSIL = 8;

		// Token: 0x04001619 RID: 5657
		public const int PROCESSOR_ARCHITECTURE_AMD64 = 9;

		// Token: 0x0400161A RID: 5658
		public const int PROCESSOR_ARCHITECTURE_IA32_ON_WIN64 = 10;

		// Token: 0x0400161B RID: 5659
		public const int PROCESSOR_ARCHITECTURE_NEUTRAL = 11;

		// Token: 0x0400161C RID: 5660
		public const int PROCESSOR_ARCHITECTURE_ARM64 = 12;

		// Token: 0x0400161D RID: 5661
		public const int PROCESSOR_ARCHITECTURE_ARM32_ON_WIN64 = 13;

		// Token: 0x0400161E RID: 5662
		public const int PROCESSOR_ARCHITECTURE_IA32_ON_ARM64 = 14;

		// Token: 0x0400161F RID: 5663
		public const int PROCESSOR_ARCHITECTURE_UNKNOWN = 65535;

		// Token: 0x04001620 RID: 5664
		public const int CFG_CALL_TARGET_VALID = 1;

		// Token: 0x04001621 RID: 5665
		public const int CFG_CALL_TARGET_PROCESSED = 2;

		// Token: 0x04001622 RID: 5666
		public const int CFG_CALL_TARGET_CONVERT_EXPORT_SUPPRESSED_TO_VALID = 4;

		// Token: 0x04001623 RID: 5667
		public const int CFG_CALL_TARGET_VALID_XFG = 8;

		// Token: 0x04001624 RID: 5668
		public const int CFG_CALL_TARGET_CONVERT_XFG_TO_CFG = 16;

		// Token: 0x04001625 RID: 5669
		private static bool? hasSetProcessValidCallTargets;

		// Token: 0x020005FC RID: 1532
		[Conditional("NEVER")]
		[AttributeUsage(AttributeTargets.All)]
		private sealed class SetsLastSystemErrorAttribute : Attribute
		{
		}

		// Token: 0x020005FD RID: 1533
		[Conditional("NEVER")]
		[AttributeUsage(AttributeTargets.All)]
		private sealed class NativeTypeNameAttribute : Attribute
		{
			// Token: 0x06002080 RID: 8320 RVA: 0x00002057 File Offset: 0x00000257
			[NullableContext(1)]
			public NativeTypeNameAttribute(string x)
			{
			}
		}

		// Token: 0x020005FE RID: 1534
		public struct MEMORY_BASIC_INFORMATION
		{
			// Token: 0x04001626 RID: 5670
			public unsafe void* BaseAddress;

			// Token: 0x04001627 RID: 5671
			public unsafe void* AllocationBase;

			// Token: 0x04001628 RID: 5672
			public uint AllocationProtect;

			// Token: 0x04001629 RID: 5673
			[NativeInteger]
			public UIntPtr RegionSize;

			// Token: 0x0400162A RID: 5674
			public uint State;

			// Token: 0x0400162B RID: 5675
			public uint Protect;

			// Token: 0x0400162C RID: 5676
			public uint Type;
		}

		// Token: 0x020005FF RID: 1535
		public struct SYSTEM_INFO
		{
			// Token: 0x1700074E RID: 1870
			// (get) Token: 0x06002081 RID: 8321 RVA: 0x00067F5C File Offset: 0x0006615C
			[UnscopedRef]
			public ref uint dwOemId
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get
				{
					return ref this.Anonymous.dwOemId;
				}
			}

			// Token: 0x1700074F RID: 1871
			// (get) Token: 0x06002082 RID: 8322 RVA: 0x00067F69 File Offset: 0x00066169
			[UnscopedRef]
			public ref ushort wProcessorArchitecture
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get
				{
					return ref this.Anonymous.Anonymous.wProcessorArchitecture;
				}
			}

			// Token: 0x17000750 RID: 1872
			// (get) Token: 0x06002083 RID: 8323 RVA: 0x00067F7B File Offset: 0x0006617B
			[UnscopedRef]
			public ref ushort wReserved
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get
				{
					return ref this.Anonymous.Anonymous.wReserved;
				}
			}

			// Token: 0x0400162D RID: 5677
			public Windows.SYSTEM_INFO._Anonymous_e__Union Anonymous;

			// Token: 0x0400162E RID: 5678
			public uint dwPageSize;

			// Token: 0x0400162F RID: 5679
			public unsafe void* lpMinimumApplicationAddress;

			// Token: 0x04001630 RID: 5680
			public unsafe void* lpMaximumApplicationAddress;

			// Token: 0x04001631 RID: 5681
			[NativeInteger]
			public UIntPtr dwActiveProcessorMask;

			// Token: 0x04001632 RID: 5682
			public uint dwNumberOfProcessors;

			// Token: 0x04001633 RID: 5683
			public uint dwProcessorType;

			// Token: 0x04001634 RID: 5684
			public uint dwAllocationGranularity;

			// Token: 0x04001635 RID: 5685
			public ushort wProcessorLevel;

			// Token: 0x04001636 RID: 5686
			public ushort wProcessorRevision;

			// Token: 0x02000600 RID: 1536
			[StructLayout(LayoutKind.Explicit)]
			public struct _Anonymous_e__Union
			{
				// Token: 0x04001637 RID: 5687
				[FieldOffset(0)]
				public uint dwOemId;

				// Token: 0x04001638 RID: 5688
				[FieldOffset(0)]
				public Windows.SYSTEM_INFO._Anonymous_e__Union._Anonymous_e__Struct Anonymous;

				// Token: 0x02000601 RID: 1537
				public struct _Anonymous_e__Struct
				{
					// Token: 0x04001639 RID: 5689
					public ushort wProcessorArchitecture;

					// Token: 0x0400163A RID: 5690
					public ushort wReserved;
				}
			}
		}

		// Token: 0x02000602 RID: 1538
		public readonly struct BOOL : IComparable, IComparable<Windows.BOOL>, IEquatable<Windows.BOOL>, IFormattable
		{
			// Token: 0x06002084 RID: 8324 RVA: 0x00067F8D File Offset: 0x0006618D
			public BOOL(int value)
			{
				this.Value = value;
			}

			// Token: 0x17000751 RID: 1873
			// (get) Token: 0x06002085 RID: 8325 RVA: 0x00067F96 File Offset: 0x00066196
			public static Windows.BOOL FALSE
			{
				get
				{
					return new Windows.BOOL(0);
				}
			}

			// Token: 0x17000752 RID: 1874
			// (get) Token: 0x06002086 RID: 8326 RVA: 0x00067F9E File Offset: 0x0006619E
			public static Windows.BOOL TRUE
			{
				get
				{
					return new Windows.BOOL(1);
				}
			}

			// Token: 0x06002087 RID: 8327 RVA: 0x00067FA6 File Offset: 0x000661A6
			public static bool operator ==(Windows.BOOL left, Windows.BOOL right)
			{
				return left.Value == right.Value;
			}

			// Token: 0x06002088 RID: 8328 RVA: 0x00067FB6 File Offset: 0x000661B6
			public static bool operator !=(Windows.BOOL left, Windows.BOOL right)
			{
				return left.Value != right.Value;
			}

			// Token: 0x06002089 RID: 8329 RVA: 0x00067FC9 File Offset: 0x000661C9
			public static bool operator <(Windows.BOOL left, Windows.BOOL right)
			{
				return left.Value < right.Value;
			}

			// Token: 0x0600208A RID: 8330 RVA: 0x00067FD9 File Offset: 0x000661D9
			public static bool operator <=(Windows.BOOL left, Windows.BOOL right)
			{
				return left.Value <= right.Value;
			}

			// Token: 0x0600208B RID: 8331 RVA: 0x00067FEC File Offset: 0x000661EC
			public static bool operator >(Windows.BOOL left, Windows.BOOL right)
			{
				return left.Value > right.Value;
			}

			// Token: 0x0600208C RID: 8332 RVA: 0x00067FFC File Offset: 0x000661FC
			public static bool operator >=(Windows.BOOL left, Windows.BOOL right)
			{
				return left.Value >= right.Value;
			}

			// Token: 0x0600208D RID: 8333 RVA: 0x0006800F File Offset: 0x0006620F
			public static implicit operator bool(Windows.BOOL value)
			{
				return value.Value != 0;
			}

			// Token: 0x0600208E RID: 8334 RVA: 0x0006801A File Offset: 0x0006621A
			public static implicit operator Windows.BOOL(bool value)
			{
				return new Windows.BOOL((value > false) ? 1 : 0);
			}

			// Token: 0x0600208F RID: 8335 RVA: 0x00068025 File Offset: 0x00066225
			public static bool operator false(Windows.BOOL value)
			{
				return value.Value == 0;
			}

			// Token: 0x06002090 RID: 8336 RVA: 0x0006800F File Offset: 0x0006620F
			public static bool operator true(Windows.BOOL value)
			{
				return value.Value != 0;
			}

			// Token: 0x06002091 RID: 8337 RVA: 0x00068030 File Offset: 0x00066230
			public static implicit operator Windows.BOOL(byte value)
			{
				return new Windows.BOOL((int)value);
			}

			// Token: 0x06002092 RID: 8338 RVA: 0x00068038 File Offset: 0x00066238
			public static explicit operator byte(Windows.BOOL value)
			{
				return (byte)value.Value;
			}

			// Token: 0x06002093 RID: 8339 RVA: 0x00068030 File Offset: 0x00066230
			public static implicit operator Windows.BOOL(short value)
			{
				return new Windows.BOOL((int)value);
			}

			// Token: 0x06002094 RID: 8340 RVA: 0x00068041 File Offset: 0x00066241
			public static explicit operator short(Windows.BOOL value)
			{
				return (short)value.Value;
			}

			// Token: 0x06002095 RID: 8341 RVA: 0x00068030 File Offset: 0x00066230
			public static implicit operator Windows.BOOL(int value)
			{
				return new Windows.BOOL(value);
			}

			// Token: 0x06002096 RID: 8342 RVA: 0x0006804A File Offset: 0x0006624A
			public static implicit operator int(Windows.BOOL value)
			{
				return value.Value;
			}

			// Token: 0x06002097 RID: 8343 RVA: 0x00068052 File Offset: 0x00066252
			public static explicit operator Windows.BOOL(long value)
			{
				return new Windows.BOOL((int)value);
			}

			// Token: 0x06002098 RID: 8344 RVA: 0x0006805B File Offset: 0x0006625B
			public static implicit operator long(Windows.BOOL value)
			{
				return (long)value.Value;
			}

			// Token: 0x06002099 RID: 8345 RVA: 0x00068052 File Offset: 0x00066252
			public static explicit operator Windows.BOOL([NativeInteger] IntPtr value)
			{
				return new Windows.BOOL((int)value);
			}

			// Token: 0x0600209A RID: 8346 RVA: 0x00068064 File Offset: 0x00066264
			[return: NativeInteger]
			public static implicit operator IntPtr(Windows.BOOL value)
			{
				return (IntPtr)value.Value;
			}

			// Token: 0x0600209B RID: 8347 RVA: 0x00068030 File Offset: 0x00066230
			public static implicit operator Windows.BOOL(sbyte value)
			{
				return new Windows.BOOL((int)value);
			}

			// Token: 0x0600209C RID: 8348 RVA: 0x0006806D File Offset: 0x0006626D
			public static explicit operator sbyte(Windows.BOOL value)
			{
				return (sbyte)value.Value;
			}

			// Token: 0x0600209D RID: 8349 RVA: 0x00068030 File Offset: 0x00066230
			public static implicit operator Windows.BOOL(ushort value)
			{
				return new Windows.BOOL((int)value);
			}

			// Token: 0x0600209E RID: 8350 RVA: 0x00068076 File Offset: 0x00066276
			public static explicit operator ushort(Windows.BOOL value)
			{
				return (ushort)value.Value;
			}

			// Token: 0x0600209F RID: 8351 RVA: 0x00068030 File Offset: 0x00066230
			public static explicit operator Windows.BOOL(uint value)
			{
				return new Windows.BOOL((int)value);
			}

			// Token: 0x060020A0 RID: 8352 RVA: 0x0006804A File Offset: 0x0006624A
			public static explicit operator uint(Windows.BOOL value)
			{
				return (uint)value.Value;
			}

			// Token: 0x060020A1 RID: 8353 RVA: 0x00068052 File Offset: 0x00066252
			public static explicit operator Windows.BOOL(ulong value)
			{
				return new Windows.BOOL((int)value);
			}

			// Token: 0x060020A2 RID: 8354 RVA: 0x0006805B File Offset: 0x0006625B
			public static explicit operator ulong(Windows.BOOL value)
			{
				return (ulong)((long)value.Value);
			}

			// Token: 0x060020A3 RID: 8355 RVA: 0x00068052 File Offset: 0x00066252
			public static explicit operator Windows.BOOL([NativeInteger] UIntPtr value)
			{
				return new Windows.BOOL((int)value);
			}

			// Token: 0x060020A4 RID: 8356 RVA: 0x00068064 File Offset: 0x00066264
			[return: NativeInteger]
			public static explicit operator UIntPtr(Windows.BOOL value)
			{
				return (UIntPtr)((IntPtr)value.Value);
			}

			// Token: 0x060020A5 RID: 8357 RVA: 0x00068080 File Offset: 0x00066280
			[NullableContext(2)]
			public int CompareTo(object obj)
			{
				if (obj is Windows.BOOL)
				{
					Windows.BOOL other = (Windows.BOOL)obj;
					return this.CompareTo(other);
				}
				if (obj != null)
				{
					throw new ArgumentException("obj is not an instance of BOOL.");
				}
				return 1;
			}

			// Token: 0x060020A6 RID: 8358 RVA: 0x000680B4 File Offset: 0x000662B4
			public int CompareTo(Windows.BOOL other)
			{
				return this.Value.CompareTo(other.Value);
			}

			// Token: 0x060020A7 RID: 8359 RVA: 0x000680D8 File Offset: 0x000662D8
			[NullableContext(2)]
			public override bool Equals(object obj)
			{
				if (obj is Windows.BOOL)
				{
					Windows.BOOL other = (Windows.BOOL)obj;
					return this.Equals(other);
				}
				return false;
			}

			// Token: 0x060020A8 RID: 8360 RVA: 0x00068100 File Offset: 0x00066300
			public bool Equals(Windows.BOOL other)
			{
				return this.Value.Equals(other.Value);
			}

			// Token: 0x060020A9 RID: 8361 RVA: 0x00068124 File Offset: 0x00066324
			public override int GetHashCode()
			{
				return this.Value.GetHashCode();
			}

			// Token: 0x060020AA RID: 8362 RVA: 0x00068140 File Offset: 0x00066340
			[NullableContext(1)]
			public override string ToString()
			{
				return this.Value.ToString(null);
			}

			// Token: 0x060020AB RID: 8363 RVA: 0x0006815C File Offset: 0x0006635C
			[NullableContext(2)]
			[return: Nullable(1)]
			public string ToString(string format, IFormatProvider formatProvider)
			{
				return this.Value.ToString(format, formatProvider);
			}

			// Token: 0x0400163B RID: 5691
			public readonly int Value;
		}

		// Token: 0x02000603 RID: 1539
		public readonly struct HANDLE : IComparable, IComparable<Windows.HANDLE>, IEquatable<Windows.HANDLE>, IFormattable
		{
			// Token: 0x060020AC RID: 8364 RVA: 0x00068179 File Offset: 0x00066379
			public unsafe HANDLE(void* value)
			{
				this.Value = value;
			}

			// Token: 0x17000753 RID: 1875
			// (get) Token: 0x060020AD RID: 8365 RVA: 0x00068182 File Offset: 0x00066382
			public static Windows.HANDLE INVALID_VALUE
			{
				get
				{
					return new Windows.HANDLE(-1);
				}
			}

			// Token: 0x17000754 RID: 1876
			// (get) Token: 0x060020AE RID: 8366 RVA: 0x0006818B File Offset: 0x0006638B
			public static Windows.HANDLE NULL
			{
				get
				{
					return new Windows.HANDLE(null);
				}
			}

			// Token: 0x060020AF RID: 8367 RVA: 0x00068194 File Offset: 0x00066394
			public static bool operator ==(Windows.HANDLE left, Windows.HANDLE right)
			{
				return left.Value == right.Value;
			}

			// Token: 0x060020B0 RID: 8368 RVA: 0x000681A4 File Offset: 0x000663A4
			public static bool operator !=(Windows.HANDLE left, Windows.HANDLE right)
			{
				return left.Value != right.Value;
			}

			// Token: 0x060020B1 RID: 8369 RVA: 0x000681B7 File Offset: 0x000663B7
			public static bool operator <(Windows.HANDLE left, Windows.HANDLE right)
			{
				return left.Value < right.Value;
			}

			// Token: 0x060020B2 RID: 8370 RVA: 0x000681C7 File Offset: 0x000663C7
			public static bool operator <=(Windows.HANDLE left, Windows.HANDLE right)
			{
				return left.Value == right.Value;
			}

			// Token: 0x060020B3 RID: 8371 RVA: 0x000681DA File Offset: 0x000663DA
			public static bool operator >(Windows.HANDLE left, Windows.HANDLE right)
			{
				return left.Value != right.Value;
			}

			// Token: 0x060020B4 RID: 8372 RVA: 0x000681EA File Offset: 0x000663EA
			public static bool operator >=(Windows.HANDLE left, Windows.HANDLE right)
			{
				return left.Value >= right.Value;
			}

			// Token: 0x060020B5 RID: 8373 RVA: 0x000681FD File Offset: 0x000663FD
			public unsafe static explicit operator Windows.HANDLE(void* value)
			{
				return new Windows.HANDLE(value);
			}

			// Token: 0x060020B6 RID: 8374 RVA: 0x00068205 File Offset: 0x00066405
			public unsafe static implicit operator void*(Windows.HANDLE value)
			{
				return value.Value;
			}

			// Token: 0x060020B7 RID: 8375 RVA: 0x0006820D File Offset: 0x0006640D
			public static explicit operator Windows.HANDLE(byte value)
			{
				return new Windows.HANDLE(value);
			}

			// Token: 0x060020B8 RID: 8376 RVA: 0x00068216 File Offset: 0x00066416
			public static explicit operator byte(Windows.HANDLE value)
			{
				return value.Value;
			}

			// Token: 0x060020B9 RID: 8377 RVA: 0x0006821F File Offset: 0x0006641F
			public static explicit operator Windows.HANDLE(short value)
			{
				return new Windows.HANDLE(value);
			}

			// Token: 0x060020BA RID: 8378 RVA: 0x00068228 File Offset: 0x00066428
			public static explicit operator short(Windows.HANDLE value)
			{
				return value.Value;
			}

			// Token: 0x060020BB RID: 8379 RVA: 0x0006821F File Offset: 0x0006641F
			public static explicit operator Windows.HANDLE(int value)
			{
				return new Windows.HANDLE(value);
			}

			// Token: 0x060020BC RID: 8380 RVA: 0x00068231 File Offset: 0x00066431
			public static explicit operator int(Windows.HANDLE value)
			{
				return value.Value;
			}

			// Token: 0x060020BD RID: 8381 RVA: 0x0006820D File Offset: 0x0006640D
			public static explicit operator Windows.HANDLE(long value)
			{
				return new Windows.HANDLE(value);
			}

			// Token: 0x060020BE RID: 8382 RVA: 0x0006823A File Offset: 0x0006643A
			public static explicit operator long(Windows.HANDLE value)
			{
				return value.Value;
			}

			// Token: 0x060020BF RID: 8383 RVA: 0x000681FD File Offset: 0x000663FD
			public static explicit operator Windows.HANDLE([NativeInteger] IntPtr value)
			{
				return new Windows.HANDLE(value);
			}

			// Token: 0x060020C0 RID: 8384 RVA: 0x00068205 File Offset: 0x00066405
			[return: NativeInteger]
			public static implicit operator IntPtr(Windows.HANDLE value)
			{
				return value.Value;
			}

			// Token: 0x060020C1 RID: 8385 RVA: 0x0006821F File Offset: 0x0006641F
			public static explicit operator Windows.HANDLE(sbyte value)
			{
				return new Windows.HANDLE(value);
			}

			// Token: 0x060020C2 RID: 8386 RVA: 0x00068243 File Offset: 0x00066443
			public static explicit operator sbyte(Windows.HANDLE value)
			{
				return value.Value;
			}

			// Token: 0x060020C3 RID: 8387 RVA: 0x0006820D File Offset: 0x0006640D
			public static explicit operator Windows.HANDLE(ushort value)
			{
				return new Windows.HANDLE(value);
			}

			// Token: 0x060020C4 RID: 8388 RVA: 0x0006824C File Offset: 0x0006644C
			public static explicit operator ushort(Windows.HANDLE value)
			{
				return value.Value;
			}

			// Token: 0x060020C5 RID: 8389 RVA: 0x0006820D File Offset: 0x0006640D
			public static explicit operator Windows.HANDLE(uint value)
			{
				return new Windows.HANDLE(value);
			}

			// Token: 0x060020C6 RID: 8390 RVA: 0x00068255 File Offset: 0x00066455
			public static explicit operator uint(Windows.HANDLE value)
			{
				return value.Value;
			}

			// Token: 0x060020C7 RID: 8391 RVA: 0x0006820D File Offset: 0x0006640D
			public static explicit operator Windows.HANDLE(ulong value)
			{
				return new Windows.HANDLE(value);
			}

			// Token: 0x060020C8 RID: 8392 RVA: 0x0006823A File Offset: 0x0006643A
			public static explicit operator ulong(Windows.HANDLE value)
			{
				return value.Value;
			}

			// Token: 0x060020C9 RID: 8393 RVA: 0x000681FD File Offset: 0x000663FD
			public static explicit operator Windows.HANDLE([NativeInteger] UIntPtr value)
			{
				return new Windows.HANDLE(value);
			}

			// Token: 0x060020CA RID: 8394 RVA: 0x00068205 File Offset: 0x00066405
			[return: NativeInteger]
			public static implicit operator UIntPtr(Windows.HANDLE value)
			{
				return value.Value;
			}

			// Token: 0x060020CB RID: 8395 RVA: 0x00068260 File Offset: 0x00066460
			[NullableContext(2)]
			public int CompareTo(object obj)
			{
				if (obj is Windows.HANDLE)
				{
					Windows.HANDLE other = (Windows.HANDLE)obj;
					return this.CompareTo(other);
				}
				if (obj != null)
				{
					throw new ArgumentException("obj is not an instance of HANDLE.");
				}
				return 1;
			}

			// Token: 0x060020CC RID: 8396 RVA: 0x00068294 File Offset: 0x00066494
			public int CompareTo(Windows.HANDLE other)
			{
				if (sizeof(IntPtr) != 4)
				{
					return this.Value.CompareTo(other.Value);
				}
				return this.Value.CompareTo(other.Value);
			}

			// Token: 0x060020CD RID: 8397 RVA: 0x000682D8 File Offset: 0x000664D8
			[NullableContext(2)]
			public override bool Equals(object obj)
			{
				if (obj is Windows.HANDLE)
				{
					Windows.HANDLE other = (Windows.HANDLE)obj;
					return this.Equals(other);
				}
				return false;
			}

			// Token: 0x060020CE RID: 8398 RVA: 0x00068300 File Offset: 0x00066500
			public bool Equals(Windows.HANDLE other)
			{
				return this.Value.Equals(other.Value);
			}

			// Token: 0x060020CF RID: 8399 RVA: 0x00068328 File Offset: 0x00066528
			public override int GetHashCode()
			{
				return this.Value.GetHashCode();
			}

			// Token: 0x060020D0 RID: 8400 RVA: 0x00068344 File Offset: 0x00066544
			[NullableContext(1)]
			public override string ToString()
			{
				if (sizeof(UIntPtr) != 4)
				{
					return this.Value.ToString("X16", null);
				}
				return this.Value.ToString("X8", null);
			}

			// Token: 0x060020D1 RID: 8401 RVA: 0x00068388 File Offset: 0x00066588
			[NullableContext(2)]
			[return: Nullable(1)]
			public string ToString(string format, IFormatProvider formatProvider)
			{
				if (sizeof(IntPtr) != 4)
				{
					return this.Value.ToString(format, formatProvider);
				}
				return this.Value.ToString(format, formatProvider);
			}

			// Token: 0x0400163C RID: 5692
			public unsafe readonly void* Value;
		}

		// Token: 0x02000604 RID: 1540
		public struct CFG_CALL_TARGET_INFO
		{
			// Token: 0x0400163D RID: 5693
			[NativeInteger]
			public UIntPtr Offset;

			// Token: 0x0400163E RID: 5694
			[NativeInteger]
			public UIntPtr Flags;
		}
	}
}
