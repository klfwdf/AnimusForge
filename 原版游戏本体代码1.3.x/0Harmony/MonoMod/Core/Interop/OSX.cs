using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MonoMod.Logs;
using MonoMod.Utils;

namespace MonoMod.Core.Interop
{
	// Token: 0x020005E3 RID: 1507
	internal static class OSX
	{
		// Token: 0x06002035 RID: 8245
		[DllImport("libSystem", EntryPoint = "getpagesize")]
		public static extern int GetPageSize();

		// Token: 0x06002036 RID: 8246
		[DllImport("libSystem")]
		public unsafe static extern void sys_icache_invalidate(void* start, [NativeInteger] UIntPtr size);

		// Token: 0x06002037 RID: 8247
		[DllImport("libSystem", CallingConvention = CallingConvention.Cdecl, EntryPoint = "mkstemp")]
		public unsafe static extern int MkSTemp(byte* template);

		// Token: 0x06002038 RID: 8248
		[DllImport("libSystem", CallingConvention = CallingConvention.Cdecl, EntryPoint = "close")]
		public static extern int Close(int fd);

		// Token: 0x06002039 RID: 8249
		[DllImport("libSystem", CallingConvention = CallingConvention.Cdecl)]
		public unsafe static extern int* __error();

		// Token: 0x17000744 RID: 1860
		// (get) Token: 0x0600203A RID: 8250 RVA: 0x00067A7E File Offset: 0x00065C7E
		public unsafe static int Errno
		{
			get
			{
				return *OSX.__error();
			}
		}

		// Token: 0x0600203B RID: 8251 RVA: 0x00067A86 File Offset: 0x00065C86
		static OSX()
		{
			int errno = OSX.Errno;
		}

		// Token: 0x0600203C RID: 8252
		[DllImport("libSystem")]
		public unsafe static extern OSX.kern_return_t mach_vm_region_recurse(int targetTask, [In] [Out] ulong* address, [Out] ulong* size, [In] [Out] int* nestingDepth, [Out] OSX.vm_region_submap_short_info_64* info, [In] [Out] int* infoSize);

		// Token: 0x0600203D RID: 8253
		[DllImport("libSystem")]
		public static extern OSX.kern_return_t mach_vm_protect(int targetTask, ulong address, ulong size, OSX.boolean_t setMax, OSX.vm_prot_t protection);

		// Token: 0x0600203E RID: 8254
		[DllImport("libSystem")]
		public unsafe static extern OSX.kern_return_t mach_vm_allocate(int targetTask, [In] [Out] ulong* address, ulong size, OSX.vm_flags flags);

		// Token: 0x0600203F RID: 8255
		[DllImport("libSystem")]
		public unsafe static extern OSX.kern_return_t mach_vm_map(int targetTask, [In] [Out] ulong* address, ulong size, ulong mask, OSX.vm_flags flags, int @object, ulong offset, OSX.boolean_t copy, OSX.vm_prot_t curProt, OSX.vm_prot_t maxProt, OSX.vm_inherit_t inheritance);

		// Token: 0x06002040 RID: 8256
		[DllImport("libSystem")]
		public unsafe static extern OSX.kern_return_t mach_vm_remap(int targetTask, [In] [Out] ulong* targetAddress, ulong size, ulong offset, OSX.vm_flags flags, int srcTask, ulong srcAddress, OSX.boolean_t copy, [Out] OSX.vm_prot_t* curProt, [Out] OSX.vm_prot_t* maxProt, OSX.vm_inherit_t inherit);

		// Token: 0x06002041 RID: 8257
		[DllImport("libSystem")]
		public unsafe static extern OSX.kern_return_t mach_make_memory_entry_64(int targetTask, [In] [Out] ulong* size, ulong offset, OSX.vm_prot_t permission, int* objectHandle, int parentHandle);

		// Token: 0x06002042 RID: 8258
		[DllImport("libSystem")]
		public static extern OSX.kern_return_t mach_vm_deallocate(int targetTask, ulong address, ulong size);

		// Token: 0x06002043 RID: 8259 RVA: 0x00067A90 File Offset: 0x00065C90
		public unsafe static int mach_task_self()
		{
			int* ptr = OSX.mach_task_self_;
			if (ptr == null)
			{
				IntPtr lib = DynDll.OpenLibrary("libSystem");
				try
				{
					ptr = (OSX.mach_task_self_ = (int*)(void*)lib.GetExport("mach_task_self_"));
				}
				finally
				{
					DynDll.CloseLibrary(lib);
				}
			}
			return *ptr;
		}

		// Token: 0x06002044 RID: 8260
		[DllImport("libSystem")]
		public unsafe static extern OSX.kern_return_t task_info(int targetTask, OSX.task_flavor_t flavor, [Out] OSX.task_dyld_info* taskInfoOut, int* taskInfoCnt);

		// Token: 0x06002045 RID: 8261 RVA: 0x00067AE8 File Offset: 0x00065CE8
		public static OSX.VmProtFmtProxy P(OSX.vm_prot_t prot)
		{
			return new OSX.VmProtFmtProxy(prot);
		}

		// Token: 0x06002046 RID: 8262
		[DllImport("libSystem")]
		public static extern IntPtr mmap(IntPtr address, ulong length, OSX.map_prot prot, OSX.map_flags flags, int fd, long offset);

		// Token: 0x0400152A RID: 5418
		[Nullable(1)]
		public const string LibSystem = "libSystem";

		// Token: 0x0400152B RID: 5419
		private unsafe static int* mach_task_self_;

		// Token: 0x020005E4 RID: 1508
		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		public struct vm_region_submap_short_info_64
		{
			// Token: 0x17000745 RID: 1861
			// (get) Token: 0x06002047 RID: 8263 RVA: 0x00067AF0 File Offset: 0x00065CF0
			public static int Count
			{
				get
				{
					return sizeof(OSX.vm_region_submap_short_info_64) / 4;
				}
			}

			// Token: 0x0400152C RID: 5420
			public OSX.vm_prot_t protection;

			// Token: 0x0400152D RID: 5421
			public OSX.vm_prot_t max_protection;

			// Token: 0x0400152E RID: 5422
			public OSX.vm_inherit_t inheritance;

			// Token: 0x0400152F RID: 5423
			public ulong offset;

			// Token: 0x04001530 RID: 5424
			public uint user_tag;

			// Token: 0x04001531 RID: 5425
			public uint ref_count;

			// Token: 0x04001532 RID: 5426
			public ushort shadow_depth;

			// Token: 0x04001533 RID: 5427
			public byte external_pager;

			// Token: 0x04001534 RID: 5428
			public OSX.ShareMode share_mode;

			// Token: 0x04001535 RID: 5429
			public OSX.boolean_t is_submap;

			// Token: 0x04001536 RID: 5430
			public OSX.vm_behavior_t behavior;

			// Token: 0x04001537 RID: 5431
			public uint object_id;

			// Token: 0x04001538 RID: 5432
			public ushort user_wired_count;
		}

		// Token: 0x020005E5 RID: 1509
		public enum ShareMode : byte
		{
			// Token: 0x0400153A RID: 5434
			COW = 1,
			// Token: 0x0400153B RID: 5435
			Private,
			// Token: 0x0400153C RID: 5436
			Empty,
			// Token: 0x0400153D RID: 5437
			Shared,
			// Token: 0x0400153E RID: 5438
			TrueShared,
			// Token: 0x0400153F RID: 5439
			PrivateAliased,
			// Token: 0x04001540 RID: 5440
			SharedAliased,
			// Token: 0x04001541 RID: 5441
			LargePage
		}

		// Token: 0x020005E6 RID: 1510
		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		public struct task_dyld_info
		{
			// Token: 0x17000746 RID: 1862
			// (get) Token: 0x06002048 RID: 8264 RVA: 0x00067AFA File Offset: 0x00065CFA
			public unsafe OSX.dyld_all_image_infos* all_image_infos
			{
				get
				{
					return this.all_image_info_addr;
				}
			}

			// Token: 0x17000747 RID: 1863
			// (get) Token: 0x06002049 RID: 8265 RVA: 0x00067B03 File Offset: 0x00065D03
			public static int Count
			{
				get
				{
					return sizeof(OSX.task_dyld_info) / 4;
				}
			}

			// Token: 0x04001542 RID: 5442
			public ulong all_image_info_addr;

			// Token: 0x04001543 RID: 5443
			public ulong all_image_info_size;

			// Token: 0x04001544 RID: 5444
			public OSX.task_dyld_all_image_info_format all_image_info_format;
		}

		// Token: 0x020005E7 RID: 1511
		public struct dyld_all_image_infos
		{
			// Token: 0x17000748 RID: 1864
			// (get) Token: 0x0600204A RID: 8266 RVA: 0x00067B0D File Offset: 0x00065D0D
			public unsafe ReadOnlySpan<OSX.dyld_image_info> InfoArray
			{
				get
				{
					return new ReadOnlySpan<OSX.dyld_image_info>((void*)this.infoArray, (int)this.infoArrayCount);
				}
			}

			// Token: 0x04001545 RID: 5445
			public uint version;

			// Token: 0x04001546 RID: 5446
			public uint infoArrayCount;

			// Token: 0x04001547 RID: 5447
			public unsafe OSX.dyld_image_info* infoArray;
		}

		// Token: 0x020005E8 RID: 1512
		public struct dyld_image_info
		{
			// Token: 0x04001548 RID: 5448
			public unsafe void* imageLoadAddress;

			// Token: 0x04001549 RID: 5449
			public PCSTR imageFilePath;

			// Token: 0x0400154A RID: 5450
			[NativeInteger]
			public UIntPtr imageFileModDate;
		}

		// Token: 0x020005E9 RID: 1513
		public enum task_dyld_all_image_info_format
		{
			// Token: 0x0400154C RID: 5452
			Bits32,
			// Token: 0x0400154D RID: 5453
			Bits64
		}

		// Token: 0x020005EA RID: 1514
		public enum task_flavor_t : uint
		{
			// Token: 0x0400154F RID: 5455
			DyldInfo = 17U
		}

		// Token: 0x020005EB RID: 1515
		public enum vm_region_flavor_t
		{
			// Token: 0x04001551 RID: 5457
			BasicInfo64 = 9
		}

		// Token: 0x020005EC RID: 1516
		[Flags]
		public enum vm_prot_t
		{
			// Token: 0x04001553 RID: 5459
			None = 0,
			// Token: 0x04001554 RID: 5460
			Read = 1,
			// Token: 0x04001555 RID: 5461
			Write = 2,
			// Token: 0x04001556 RID: 5462
			Execute = 4,
			// Token: 0x04001557 RID: 5463
			Default = 3,
			// Token: 0x04001558 RID: 5464
			All = 7,
			// Token: 0x04001559 RID: 5465
			[Obsolete("Only used for memory_object_lock_request. Invalid otherwise.")]
			NoChange = 8,
			// Token: 0x0400155A RID: 5466
			Copy = 16,
			// Token: 0x0400155B RID: 5467
			WantsCopy = 16,
			// Token: 0x0400155C RID: 5468
			[Obsolete("Invalid value. Indicates that other bits are to be applied as mask to actual bits.")]
			IsMask = 64,
			// Token: 0x0400155D RID: 5469
			[Obsolete("Invalid value. Tells mprotect to not set Read. Used for execute-only.")]
			StripRead = 128,
			// Token: 0x0400155E RID: 5470
			[Obsolete("Invalid value. Use only for mprotect.")]
			ExecuteOnly = 132
		}

		// Token: 0x020005ED RID: 1517
		public struct VmProtFmtProxy : IDebugFormattable
		{
			// Token: 0x0600204B RID: 8267 RVA: 0x00067B20 File Offset: 0x00065D20
			public VmProtFmtProxy(OSX.vm_prot_t value)
			{
				this.value = value;
			}

			// Token: 0x0600204C RID: 8268 RVA: 0x00067B2C File Offset: 0x00065D2C
			public unsafe bool TryFormatInto(Span<char> span, out int wrote)
			{
				int w = 0;
				if (this.value.Has(OSX.vm_prot_t.NoChange))
				{
					if (span.Slice(w).Length < 1)
					{
						wrote = w;
						return false;
					}
					*span[w++] = '~';
				}
				if (span.Slice(w).Length < 3)
				{
					wrote = 0;
					return false;
				}
				*span[w++] = (this.value.Has(OSX.vm_prot_t.Read) ? 'r' : '-');
				*span[w++] = (this.value.Has(OSX.vm_prot_t.Write) ? 'w' : '-');
				*span[w++] = (this.value.Has(OSX.vm_prot_t.Execute) ? 'x' : '-');
				if (this.value.Has(OSX.vm_prot_t.StripRead))
				{
					if (span.Slice(w).Length < 1)
					{
						wrote = w;
						return false;
					}
					*span[w++] = '!';
				}
				if (this.value.Has(OSX.vm_prot_t.Copy))
				{
					if (span.Slice(w).Length < 1)
					{
						wrote = w;
						return false;
					}
					*span[w++] = 'c';
				}
				if (this.value.Has(OSX.vm_prot_t.IsMask))
				{
					if (span.Slice(w).Length < " (mask)".Length)
					{
						wrote = w;
						return false;
					}
					" (mask)".AsSpan().CopyTo(span.Slice(w));
					w += " (mask)".Length;
				}
				wrote = w;
				return true;
			}

			// Token: 0x0400155F RID: 5471
			private readonly OSX.vm_prot_t value;
		}

		// Token: 0x020005EE RID: 1518
		[Flags]
		public enum vm_flags
		{
			// Token: 0x04001561 RID: 5473
			Fixed = 0,
			// Token: 0x04001562 RID: 5474
			Anywhere = 1,
			// Token: 0x04001563 RID: 5475
			Purgable = 2,
			// Token: 0x04001564 RID: 5476
			Chunk4GB = 4,
			// Token: 0x04001565 RID: 5477
			RandomAddr = 8,
			// Token: 0x04001566 RID: 5478
			NoCache = 16,
			// Token: 0x04001567 RID: 5479
			Overwrite = 16384,
			// Token: 0x04001568 RID: 5480
			SuperpageMask = 458752,
			// Token: 0x04001569 RID: 5481
			SuperpageSizeAny = 65536,
			// Token: 0x0400156A RID: 5482
			SuperpageWSize2MB = 131072,
			// Token: 0x0400156B RID: 5483
			AliasMask = -16777216
		}

		// Token: 0x020005EF RID: 1519
		public enum vm_inherit_t : uint
		{
			// Token: 0x0400156D RID: 5485
			Share,
			// Token: 0x0400156E RID: 5486
			Copy,
			// Token: 0x0400156F RID: 5487
			None,
			// Token: 0x04001570 RID: 5488
			DonateCopy,
			// Token: 0x04001571 RID: 5489
			Default = 1U,
			// Token: 0x04001572 RID: 5490
			LastValid
		}

		// Token: 0x020005F0 RID: 1520
		public enum vm_behavior_t
		{
			// Token: 0x04001574 RID: 5492
			Default,
			// Token: 0x04001575 RID: 5493
			Random,
			// Token: 0x04001576 RID: 5494
			Sequential,
			// Token: 0x04001577 RID: 5495
			ReverseSequential,
			// Token: 0x04001578 RID: 5496
			WillNeed,
			// Token: 0x04001579 RID: 5497
			DontNeed,
			// Token: 0x0400157A RID: 5498
			Free,
			// Token: 0x0400157B RID: 5499
			ZeroWiredPages,
			// Token: 0x0400157C RID: 5500
			Reusable,
			// Token: 0x0400157D RID: 5501
			Reuse,
			// Token: 0x0400157E RID: 5502
			CanReuse,
			// Token: 0x0400157F RID: 5503
			PageOut
		}

		// Token: 0x020005F1 RID: 1521
		[DebuggerDisplay("{ToString(),nq}")]
		public struct boolean_t
		{
			// Token: 0x0600204D RID: 8269 RVA: 0x00067CB5 File Offset: 0x00065EB5
			public boolean_t(bool value)
			{
				this.value = ((value > false) ? 1 : 0);
			}

			// Token: 0x0600204E RID: 8270 RVA: 0x00067CC1 File Offset: 0x00065EC1
			public static implicit operator bool(OSX.boolean_t v)
			{
				return v.value != 0;
			}

			// Token: 0x0600204F RID: 8271 RVA: 0x00067CCC File Offset: 0x00065ECC
			public static implicit operator OSX.boolean_t(bool v)
			{
				return new OSX.boolean_t(v);
			}

			// Token: 0x06002050 RID: 8272 RVA: 0x00067CD4 File Offset: 0x00065ED4
			public static bool operator true(OSX.boolean_t v)
			{
				return v;
			}

			// Token: 0x06002051 RID: 8273 RVA: 0x00067CDC File Offset: 0x00065EDC
			public static bool operator false(OSX.boolean_t v)
			{
				return !v;
			}

			// Token: 0x06002052 RID: 8274 RVA: 0x00067CE7 File Offset: 0x00065EE7
			[NullableContext(1)]
			public override string ToString()
			{
				if (!this)
				{
					return "false";
				}
				return "true";
			}

			// Token: 0x04001580 RID: 5504
			private int value;
		}

		// Token: 0x020005F2 RID: 1522
		[DebuggerDisplay("{Value}")]
		public struct kern_return_t : IEquatable<OSX.kern_return_t>
		{
			// Token: 0x17000749 RID: 1865
			// (get) Token: 0x06002053 RID: 8275 RVA: 0x00067D01 File Offset: 0x00065F01
			public int Value
			{
				get
				{
					return this.value;
				}
			}

			// Token: 0x06002054 RID: 8276 RVA: 0x00067D09 File Offset: 0x00065F09
			public kern_return_t(int value)
			{
				this.value = value;
			}

			// Token: 0x06002055 RID: 8277 RVA: 0x00067D12 File Offset: 0x00065F12
			public static implicit operator bool(OSX.kern_return_t v)
			{
				return v.value == 0;
			}

			// Token: 0x06002056 RID: 8278 RVA: 0x00067D1D File Offset: 0x00065F1D
			public static bool operator ==(OSX.kern_return_t x, OSX.kern_return_t y)
			{
				return x.value == y.value;
			}

			// Token: 0x06002057 RID: 8279 RVA: 0x00067D2D File Offset: 0x00065F2D
			public static bool operator !=(OSX.kern_return_t x, OSX.kern_return_t y)
			{
				return x.value != y.value;
			}

			// Token: 0x06002058 RID: 8280 RVA: 0x00067D40 File Offset: 0x00065F40
			[NullableContext(2)]
			public override bool Equals(object obj)
			{
				if (obj is OSX.kern_return_t)
				{
					OSX.kern_return_t t = (OSX.kern_return_t)obj;
					return this.Equals(t);
				}
				return false;
			}

			// Token: 0x06002059 RID: 8281 RVA: 0x00067D1D File Offset: 0x00065F1D
			public bool Equals(OSX.kern_return_t other)
			{
				return this.value == other.value;
			}

			// Token: 0x0600205A RID: 8282 RVA: 0x00067D65 File Offset: 0x00065F65
			public override int GetHashCode()
			{
				return HashCode.Combine<int>(this.value);
			}

			// Token: 0x04001581 RID: 5505
			private int value;

			// Token: 0x04001582 RID: 5506
			public static OSX.kern_return_t Success = new OSX.kern_return_t(0);

			// Token: 0x04001583 RID: 5507
			public static OSX.kern_return_t InvalidAddress = new OSX.kern_return_t(1);

			// Token: 0x04001584 RID: 5508
			public static OSX.kern_return_t ProtectionFailure = new OSX.kern_return_t(2);

			// Token: 0x04001585 RID: 5509
			public static OSX.kern_return_t NoSpace = new OSX.kern_return_t(3);

			// Token: 0x04001586 RID: 5510
			public static OSX.kern_return_t InvalidArgument = new OSX.kern_return_t(4);

			// Token: 0x04001587 RID: 5511
			public static OSX.kern_return_t Failure = new OSX.kern_return_t(5);
		}

		// Token: 0x020005F3 RID: 1523
		[Flags]
		public enum map_prot
		{
			// Token: 0x04001589 RID: 5513
			None = 0,
			// Token: 0x0400158A RID: 5514
			Read = 1,
			// Token: 0x0400158B RID: 5515
			Write = 2,
			// Token: 0x0400158C RID: 5516
			Execute = 4
		}

		// Token: 0x020005F4 RID: 1524
		[Flags]
		public enum map_flags
		{
			// Token: 0x0400158E RID: 5518
			Private = 2,
			// Token: 0x0400158F RID: 5519
			Fixed = 16,
			// Token: 0x04001590 RID: 5520
			JIT = 2048,
			// Token: 0x04001591 RID: 5521
			Anonymous = 4096,
			// Token: 0x04001592 RID: 5522
			Failed = 4097
		}
	}
}
