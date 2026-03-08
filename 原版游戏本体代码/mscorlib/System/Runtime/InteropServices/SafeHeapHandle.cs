using System;
using System.Security;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000958 RID: 2392
	[SecurityCritical]
	internal sealed class SafeHeapHandle : SafeBuffer
	{
		// Token: 0x060061E5 RID: 25061 RVA: 0x0014EDA6 File Offset: 0x0014CFA6
		public SafeHeapHandle(ulong byteLength)
			: base(true)
		{
			this.Resize(byteLength);
		}

		// Token: 0x17001107 RID: 4359
		// (get) Token: 0x060061E6 RID: 25062 RVA: 0x0014EDB6 File Offset: 0x0014CFB6
		public override bool IsInvalid
		{
			[SecurityCritical]
			get
			{
				return this.handle == IntPtr.Zero;
			}
		}

		// Token: 0x060061E7 RID: 25063 RVA: 0x0014EDC8 File Offset: 0x0014CFC8
		public void Resize(ulong byteLength)
		{
			if (base.IsClosed)
			{
				throw new ObjectDisposedException("SafeHeapHandle");
			}
			ulong num = 0UL;
			if (this.handle == IntPtr.Zero)
			{
				this.handle = Marshal.AllocHGlobal((IntPtr)((long)byteLength));
			}
			else
			{
				num = base.ByteLength;
				this.handle = Marshal.ReAllocHGlobal(this.handle, (IntPtr)((long)byteLength));
			}
			if (this.handle == IntPtr.Zero)
			{
				throw new OutOfMemoryException();
			}
			if (byteLength > num)
			{
				ulong num2 = byteLength - num;
				if (num2 > 9223372036854775807UL)
				{
					GC.AddMemoryPressure(long.MaxValue);
					GC.AddMemoryPressure((long)(num2 - 9223372036854775807UL));
				}
				else
				{
					GC.AddMemoryPressure((long)num2);
				}
			}
			else
			{
				this.RemoveMemoryPressure(num - byteLength);
			}
			base.Initialize(byteLength);
		}

		// Token: 0x060061E8 RID: 25064 RVA: 0x0014EE92 File Offset: 0x0014D092
		private void RemoveMemoryPressure(ulong removedBytes)
		{
			if (removedBytes == 0UL)
			{
				return;
			}
			if (removedBytes > 9223372036854775807UL)
			{
				GC.RemoveMemoryPressure(long.MaxValue);
				GC.RemoveMemoryPressure((long)(removedBytes - 9223372036854775807UL));
				return;
			}
			GC.RemoveMemoryPressure((long)removedBytes);
		}

		// Token: 0x060061E9 RID: 25065 RVA: 0x0014EECC File Offset: 0x0014D0CC
		[SecurityCritical]
		protected override bool ReleaseHandle()
		{
			IntPtr handle = this.handle;
			this.handle = IntPtr.Zero;
			if (handle != IntPtr.Zero)
			{
				this.RemoveMemoryPressure(base.ByteLength);
				Marshal.FreeHGlobal(handle);
			}
			return true;
		}
	}
}
