using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using Microsoft.Win32;

namespace System.Runtime
{
	// Token: 0x02000714 RID: 1812
	public sealed class MemoryFailPoint : CriticalFinalizerObject, IDisposable
	{
		// Token: 0x17000D54 RID: 3412
		// (get) Token: 0x0600511D RID: 20765 RVA: 0x0011E26C File Offset: 0x0011C46C
		// (set) Token: 0x0600511E RID: 20766 RVA: 0x0011E278 File Offset: 0x0011C478
		private static long LastKnownFreeAddressSpace
		{
			get
			{
				return Volatile.Read(ref MemoryFailPoint.hiddenLastKnownFreeAddressSpace);
			}
			set
			{
				Volatile.Write(ref MemoryFailPoint.hiddenLastKnownFreeAddressSpace, value);
			}
		}

		// Token: 0x0600511F RID: 20767 RVA: 0x0011E285 File Offset: 0x0011C485
		private static long AddToLastKnownFreeAddressSpace(long addend)
		{
			return Interlocked.Add(ref MemoryFailPoint.hiddenLastKnownFreeAddressSpace, addend);
		}

		// Token: 0x17000D55 RID: 3413
		// (get) Token: 0x06005120 RID: 20768 RVA: 0x0011E292 File Offset: 0x0011C492
		// (set) Token: 0x06005121 RID: 20769 RVA: 0x0011E29E File Offset: 0x0011C49E
		private static long LastTimeCheckingAddressSpace
		{
			get
			{
				return Volatile.Read(ref MemoryFailPoint.hiddenLastTimeCheckingAddressSpace);
			}
			set
			{
				Volatile.Write(ref MemoryFailPoint.hiddenLastTimeCheckingAddressSpace, value);
			}
		}

		// Token: 0x06005122 RID: 20770 RVA: 0x0011E2AB File Offset: 0x0011C4AB
		[SecuritySafeCritical]
		static MemoryFailPoint()
		{
			MemoryFailPoint.GetMemorySettings(out MemoryFailPoint.GCSegmentSize, out MemoryFailPoint.TopOfMemory);
		}

		// Token: 0x06005123 RID: 20771 RVA: 0x0011E2BC File Offset: 0x0011C4BC
		[SecurityCritical]
		public unsafe MemoryFailPoint(int sizeInMegabytes)
		{
			if (sizeInMegabytes <= 0)
			{
				throw new ArgumentOutOfRangeException("sizeInMegabytes", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			ulong num = (ulong)((ulong)((long)sizeInMegabytes) << 20);
			this._reservedMemory = num;
			ulong num2 = (ulong)(Math.Ceiling(num / MemoryFailPoint.GCSegmentSize) * MemoryFailPoint.GCSegmentSize);
			if (num2 >= MemoryFailPoint.TopOfMemory)
			{
				throw new InsufficientMemoryException(Environment.GetResourceString("InsufficientMemory_MemFailPoint_TooBig"));
			}
			ulong num3 = (ulong)(Math.Ceiling((double)sizeInMegabytes / 16.0) * 16.0);
			num3 <<= 20;
			ulong num4 = 0UL;
			ulong num5 = 0UL;
			int i = 0;
			while (i < 3)
			{
				MemoryFailPoint.CheckForAvailableMemory(out num4, out num5);
				ulong memoryFailPointReservedMemory = SharedStatics.MemoryFailPointReservedMemory;
				ulong num6 = num2 + memoryFailPointReservedMemory;
				bool flag = num6 < num2 || num6 < memoryFailPointReservedMemory;
				bool flag2 = num4 < num3 + memoryFailPointReservedMemory + 16777216UL || flag;
				bool flag3 = num5 < num6 || flag;
				long num7 = (long)Environment.TickCount;
				if (num7 > MemoryFailPoint.LastTimeCheckingAddressSpace + 10000L || num7 < MemoryFailPoint.LastTimeCheckingAddressSpace || MemoryFailPoint.LastKnownFreeAddressSpace < (long)num2)
				{
					MemoryFailPoint.CheckForFreeAddressSpace(num2, false);
				}
				bool flag4 = MemoryFailPoint.LastKnownFreeAddressSpace < (long)num2;
				if (!flag2 && !flag3 && !flag4)
				{
					break;
				}
				switch (i)
				{
				case 0:
					GC.Collect();
					break;
				case 1:
					if (flag2)
					{
						RuntimeHelpers.PrepareConstrainedRegions();
						try
						{
							break;
						}
						finally
						{
							UIntPtr numBytes = new UIntPtr(num2);
							void* ptr = Win32Native.VirtualAlloc(null, numBytes, 4096, 4);
							if (ptr != null && !Win32Native.VirtualFree(ptr, UIntPtr.Zero, 32768))
							{
								__Error.WinIOError();
							}
						}
						goto IL_183;
					}
					break;
				case 2:
					goto IL_183;
				}
				IL_1B6:
				i++;
				continue;
				IL_183:
				if (flag2 || flag3)
				{
					InsufficientMemoryException ex = new InsufficientMemoryException(Environment.GetResourceString("InsufficientMemory_MemFailPoint"));
					throw ex;
				}
				if (flag4)
				{
					InsufficientMemoryException ex2 = new InsufficientMemoryException(Environment.GetResourceString("InsufficientMemory_MemFailPoint_VAFrag"));
					throw ex2;
				}
				goto IL_1B6;
			}
			MemoryFailPoint.AddToLastKnownFreeAddressSpace((long)(-(long)num));
			if (MemoryFailPoint.LastKnownFreeAddressSpace < 0L)
			{
				MemoryFailPoint.CheckForFreeAddressSpace(num2, true);
			}
			RuntimeHelpers.PrepareConstrainedRegions();
			try
			{
			}
			finally
			{
				SharedStatics.AddMemoryFailPointReservation((long)num);
				this._mustSubtractReservation = true;
			}
		}

		// Token: 0x06005124 RID: 20772 RVA: 0x0011E4D8 File Offset: 0x0011C6D8
		[SecurityCritical]
		private static void CheckForAvailableMemory(out ulong availPageFile, out ulong totalAddressSpaceFree)
		{
			Win32Native.MEMORYSTATUSEX memorystatusex = default(Win32Native.MEMORYSTATUSEX);
			if (!Win32Native.GlobalMemoryStatusEx(ref memorystatusex))
			{
				__Error.WinIOError();
			}
			availPageFile = memorystatusex.availPageFile;
			totalAddressSpaceFree = memorystatusex.availVirtual;
		}

		// Token: 0x06005125 RID: 20773 RVA: 0x0011E510 File Offset: 0x0011C710
		[SecurityCritical]
		private static bool CheckForFreeAddressSpace(ulong size, bool shouldThrow)
		{
			ulong num = MemoryFailPoint.MemFreeAfterAddress(null, size);
			MemoryFailPoint.LastKnownFreeAddressSpace = (long)num;
			MemoryFailPoint.LastTimeCheckingAddressSpace = (long)Environment.TickCount;
			if (num < size && shouldThrow)
			{
				throw new InsufficientMemoryException(Environment.GetResourceString("InsufficientMemory_MemFailPoint_VAFrag"));
			}
			return num >= size;
		}

		// Token: 0x06005126 RID: 20774 RVA: 0x0011E558 File Offset: 0x0011C758
		[SecurityCritical]
		private unsafe static ulong MemFreeAfterAddress(void* address, ulong size)
		{
			if (size >= MemoryFailPoint.TopOfMemory)
			{
				return 0UL;
			}
			ulong num = 0UL;
			Win32Native.MEMORY_BASIC_INFORMATION memory_BASIC_INFORMATION = default(Win32Native.MEMORY_BASIC_INFORMATION);
			UIntPtr sizeOfBuffer = (UIntPtr)((ulong)((long)Marshal.SizeOf<Win32Native.MEMORY_BASIC_INFORMATION>(memory_BASIC_INFORMATION)));
			while ((byte*)address + size < MemoryFailPoint.TopOfMemory)
			{
				UIntPtr value = Win32Native.VirtualQuery(address, ref memory_BASIC_INFORMATION, sizeOfBuffer);
				if (value == UIntPtr.Zero)
				{
					__Error.WinIOError();
				}
				ulong num2 = memory_BASIC_INFORMATION.RegionSize.ToUInt64();
				if (memory_BASIC_INFORMATION.State == 65536U)
				{
					if (num2 >= size)
					{
						return num2;
					}
					num = Math.Max(num, num2);
				}
				address = (void*)((byte*)address + num2);
			}
			return num;
		}

		// Token: 0x06005127 RID: 20775
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern void GetMemorySettings(out ulong maxGCSegmentSize, out ulong topOfMemory);

		// Token: 0x06005128 RID: 20776 RVA: 0x0011E5E8 File Offset: 0x0011C7E8
		[SecuritySafeCritical]
		~MemoryFailPoint()
		{
			this.Dispose(false);
		}

		// Token: 0x06005129 RID: 20777 RVA: 0x0011E618 File Offset: 0x0011C818
		[SecuritySafeCritical]
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		// Token: 0x0600512A RID: 20778 RVA: 0x0011E628 File Offset: 0x0011C828
		[SecurityCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		private void Dispose(bool disposing)
		{
			if (this._mustSubtractReservation)
			{
				RuntimeHelpers.PrepareConstrainedRegions();
				try
				{
				}
				finally
				{
					SharedStatics.AddMemoryFailPointReservation((long)(-(long)this._reservedMemory));
					this._mustSubtractReservation = false;
				}
			}
		}

		// Token: 0x040023EA RID: 9194
		private static readonly ulong TopOfMemory;

		// Token: 0x040023EB RID: 9195
		private static long hiddenLastKnownFreeAddressSpace;

		// Token: 0x040023EC RID: 9196
		private static long hiddenLastTimeCheckingAddressSpace;

		// Token: 0x040023ED RID: 9197
		private const int CheckThreshold = 10000;

		// Token: 0x040023EE RID: 9198
		private const int LowMemoryFudgeFactor = 16777216;

		// Token: 0x040023EF RID: 9199
		private const int MemoryCheckGranularity = 16;

		// Token: 0x040023F0 RID: 9200
		private static readonly ulong GCSegmentSize;

		// Token: 0x040023F1 RID: 9201
		private ulong _reservedMemory;

		// Token: 0x040023F2 RID: 9202
		private bool _mustSubtractReservation;
	}
}
