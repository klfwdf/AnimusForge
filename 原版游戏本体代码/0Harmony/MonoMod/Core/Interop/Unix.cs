using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MonoMod.Core.Interop
{
	// Token: 0x020005F6 RID: 1526
	internal static class Unix
	{
		// Token: 0x06002066 RID: 8294
		[DllImport("libc", CallingConvention = CallingConvention.Cdecl, EntryPoint = "read")]
		[return: NativeInteger]
		public static extern IntPtr Read(int fd, IntPtr buf, [NativeInteger] IntPtr count);

		// Token: 0x06002067 RID: 8295
		[DllImport("libc", CallingConvention = CallingConvention.Cdecl, EntryPoint = "write")]
		[return: NativeInteger]
		public static extern IntPtr Write(int fd, IntPtr buf, [NativeInteger] IntPtr count);

		// Token: 0x06002068 RID: 8296
		[DllImport("libc", CallingConvention = CallingConvention.Cdecl, EntryPoint = "pipe2")]
		public unsafe static extern int Pipe2(int* pipefd, Unix.PipeFlags flags);

		// Token: 0x06002069 RID: 8297
		[DllImport("libc", CallingConvention = CallingConvention.Cdecl, EntryPoint = "close")]
		public static extern int Close(int fd);

		// Token: 0x0600206A RID: 8298
		[DllImport("libc", CallingConvention = CallingConvention.Cdecl, EntryPoint = "mmap")]
		[return: NativeInteger]
		public static extern IntPtr Mmap(IntPtr addr, [NativeInteger] UIntPtr length, Unix.Protection prot, Unix.MmapFlags flags, int fd, int offset);

		// Token: 0x0600206B RID: 8299
		[DllImport("libc", CallingConvention = CallingConvention.Cdecl, EntryPoint = "munmap")]
		public static extern int Munmap(IntPtr addr, [NativeInteger] UIntPtr length);

		// Token: 0x0600206C RID: 8300
		[DllImport("libc", CallingConvention = CallingConvention.Cdecl, EntryPoint = "mprotect")]
		public static extern int Mprotect(IntPtr addr, [NativeInteger] UIntPtr len, Unix.Protection prot);

		// Token: 0x0600206D RID: 8301
		[DllImport("libc", CallingConvention = CallingConvention.Cdecl, EntryPoint = "sysconf")]
		public static extern long Sysconf(Unix.SysconfName name);

		// Token: 0x0600206E RID: 8302
		[DllImport("libc", CallingConvention = CallingConvention.Cdecl, EntryPoint = "mincore")]
		public unsafe static extern int Mincore(IntPtr addr, [NativeInteger] UIntPtr len, byte* vec);

		// Token: 0x0600206F RID: 8303
		[DllImport("libc", CallingConvention = CallingConvention.Cdecl, EntryPoint = "mkstemp")]
		public unsafe static extern int MkSTemp(byte* template);

		// Token: 0x06002070 RID: 8304
		[DllImport("libc", CallingConvention = CallingConvention.Cdecl)]
		public unsafe static extern int* __errno_location();

		// Token: 0x1700074C RID: 1868
		// (get) Token: 0x06002071 RID: 8305 RVA: 0x00067EB2 File Offset: 0x000660B2
		public unsafe static int Errno
		{
			get
			{
				return *Unix.__errno_location();
			}
		}

		// Token: 0x06002072 RID: 8306 RVA: 0x00067EBA File Offset: 0x000660BA
		static Unix()
		{
			int errno = Unix.Errno;
		}

		// Token: 0x04001594 RID: 5524
		[Nullable(1)]
		public const string LibC = "libc";

		// Token: 0x020005F7 RID: 1527
		[Flags]
		public enum PipeFlags
		{
			// Token: 0x04001596 RID: 5526
			CloseOnExec = 524288
		}

		// Token: 0x020005F8 RID: 1528
		[Flags]
		public enum Protection
		{
			// Token: 0x04001598 RID: 5528
			None = 0,
			// Token: 0x04001599 RID: 5529
			Read = 1,
			// Token: 0x0400159A RID: 5530
			Write = 2,
			// Token: 0x0400159B RID: 5531
			Execute = 4
		}

		// Token: 0x020005F9 RID: 1529
		[Flags]
		public enum MmapFlags
		{
			// Token: 0x0400159D RID: 5533
			Shared = 1,
			// Token: 0x0400159E RID: 5534
			Private = 2,
			// Token: 0x0400159F RID: 5535
			SharedValidate = 3,
			// Token: 0x040015A0 RID: 5536
			Fixed = 16,
			// Token: 0x040015A1 RID: 5537
			Anonymous = 32,
			// Token: 0x040015A2 RID: 5538
			GrowsDown = 256,
			// Token: 0x040015A3 RID: 5539
			DenyWrite = 2048,
			// Token: 0x040015A4 RID: 5540
			[Obsolete("Use Protection.Execute instead", true)]
			Executable = 4096,
			// Token: 0x040015A5 RID: 5541
			Locked = 8192,
			// Token: 0x040015A6 RID: 5542
			NoReserve = 16384,
			// Token: 0x040015A7 RID: 5543
			Populate = 32768,
			// Token: 0x040015A8 RID: 5544
			NonBlock = 65536,
			// Token: 0x040015A9 RID: 5545
			Stack = 131072,
			// Token: 0x040015AA RID: 5546
			HugeTLB = 262144,
			// Token: 0x040015AB RID: 5547
			Sync = 524288,
			// Token: 0x040015AC RID: 5548
			FixedNoReplace = 1048576
		}

		// Token: 0x020005FA RID: 1530
		public enum SysconfName
		{
			// Token: 0x040015AE RID: 5550
			ArgMax,
			// Token: 0x040015AF RID: 5551
			ChildMax,
			// Token: 0x040015B0 RID: 5552
			ClockTick,
			// Token: 0x040015B1 RID: 5553
			NGroupsMax,
			// Token: 0x040015B2 RID: 5554
			OpenMax,
			// Token: 0x040015B3 RID: 5555
			StreamMax,
			// Token: 0x040015B4 RID: 5556
			TZNameMax,
			// Token: 0x040015B5 RID: 5557
			JobControl,
			// Token: 0x040015B6 RID: 5558
			SavedIds,
			// Token: 0x040015B7 RID: 5559
			RealtimeSignals,
			// Token: 0x040015B8 RID: 5560
			PriorityScheduling,
			// Token: 0x040015B9 RID: 5561
			Timers,
			// Token: 0x040015BA RID: 5562
			AsyncIO,
			// Token: 0x040015BB RID: 5563
			PrioritizedIO,
			// Token: 0x040015BC RID: 5564
			SynchronizedIO,
			// Token: 0x040015BD RID: 5565
			FSync,
			// Token: 0x040015BE RID: 5566
			MappedFiles,
			// Token: 0x040015BF RID: 5567
			MemLock,
			// Token: 0x040015C0 RID: 5568
			MemLockRange,
			// Token: 0x040015C1 RID: 5569
			MemoryProtection,
			// Token: 0x040015C2 RID: 5570
			MessagePassing,
			// Token: 0x040015C3 RID: 5571
			Semaphores,
			// Token: 0x040015C4 RID: 5572
			SharedMemoryObjects,
			// Token: 0x040015C5 RID: 5573
			AIOListIOMax,
			// Token: 0x040015C6 RID: 5574
			AIOMax,
			// Token: 0x040015C7 RID: 5575
			AIOPrioDeltaMax,
			// Token: 0x040015C8 RID: 5576
			DelayTimerMax,
			// Token: 0x040015C9 RID: 5577
			MQOpenMax,
			// Token: 0x040015CA RID: 5578
			MQPrioMax,
			// Token: 0x040015CB RID: 5579
			Version,
			// Token: 0x040015CC RID: 5580
			PageSize,
			// Token: 0x040015CD RID: 5581
			RTSigMax,
			// Token: 0x040015CE RID: 5582
			SemNSemsMax,
			// Token: 0x040015CF RID: 5583
			SemValueMax,
			// Token: 0x040015D0 RID: 5584
			SigQueueMax,
			// Token: 0x040015D1 RID: 5585
			TimerMax
		}
	}
}
