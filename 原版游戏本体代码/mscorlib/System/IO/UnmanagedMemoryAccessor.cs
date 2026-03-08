using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;

namespace System.IO
{
	// Token: 0x020001A8 RID: 424
	public class UnmanagedMemoryAccessor : IDisposable
	{
		// Token: 0x06001A77 RID: 6775 RVA: 0x00058296 File Offset: 0x00056496
		protected UnmanagedMemoryAccessor()
		{
			this._isOpen = false;
		}

		// Token: 0x06001A78 RID: 6776 RVA: 0x000582A5 File Offset: 0x000564A5
		[SecuritySafeCritical]
		public UnmanagedMemoryAccessor(SafeBuffer buffer, long offset, long capacity)
		{
			this.Initialize(buffer, offset, capacity, FileAccess.Read);
		}

		// Token: 0x06001A79 RID: 6777 RVA: 0x000582B7 File Offset: 0x000564B7
		[SecuritySafeCritical]
		public UnmanagedMemoryAccessor(SafeBuffer buffer, long offset, long capacity, FileAccess access)
		{
			this.Initialize(buffer, offset, capacity, access);
		}

		// Token: 0x06001A7A RID: 6778 RVA: 0x000582CC File Offset: 0x000564CC
		[SecuritySafeCritical]
		[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
		protected unsafe void Initialize(SafeBuffer buffer, long offset, long capacity, FileAccess access)
		{
			if (buffer == null)
			{
				throw new ArgumentNullException("buffer");
			}
			if (offset < 0L)
			{
				throw new ArgumentOutOfRangeException("offset", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			if (capacity < 0L)
			{
				throw new ArgumentOutOfRangeException("capacity", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			if (buffer.ByteLength < (ulong)(offset + capacity))
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_OffsetAndCapacityOutOfBounds"));
			}
			if (access < FileAccess.Read || access > FileAccess.ReadWrite)
			{
				throw new ArgumentOutOfRangeException("access");
			}
			if (this._isOpen)
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CalledTwice"));
			}
			byte* ptr = null;
			RuntimeHelpers.PrepareConstrainedRegions();
			try
			{
				buffer.AcquirePointer(ref ptr);
				if (ptr + offset + capacity < ptr)
				{
					throw new ArgumentException(Environment.GetResourceString("Argument_UnmanagedMemAccessorWrapAround"));
				}
			}
			finally
			{
				if (ptr != null)
				{
					buffer.ReleasePointer();
				}
			}
			this._offset = offset;
			this._buffer = buffer;
			this._capacity = capacity;
			this._access = access;
			this._isOpen = true;
			this._canRead = (this._access & FileAccess.Read) > (FileAccess)0;
			this._canWrite = (this._access & FileAccess.Write) > (FileAccess)0;
		}

		// Token: 0x170002EF RID: 751
		// (get) Token: 0x06001A7B RID: 6779 RVA: 0x000583F4 File Offset: 0x000565F4
		public long Capacity
		{
			get
			{
				return this._capacity;
			}
		}

		// Token: 0x170002F0 RID: 752
		// (get) Token: 0x06001A7C RID: 6780 RVA: 0x000583FC File Offset: 0x000565FC
		public bool CanRead
		{
			get
			{
				return this._isOpen && this._canRead;
			}
		}

		// Token: 0x170002F1 RID: 753
		// (get) Token: 0x06001A7D RID: 6781 RVA: 0x0005840E File Offset: 0x0005660E
		public bool CanWrite
		{
			get
			{
				return this._isOpen && this._canWrite;
			}
		}

		// Token: 0x06001A7E RID: 6782 RVA: 0x00058420 File Offset: 0x00056620
		protected virtual void Dispose(bool disposing)
		{
			this._isOpen = false;
		}

		// Token: 0x06001A7F RID: 6783 RVA: 0x00058429 File Offset: 0x00056629
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		// Token: 0x170002F2 RID: 754
		// (get) Token: 0x06001A80 RID: 6784 RVA: 0x00058438 File Offset: 0x00056638
		protected bool IsOpen
		{
			get
			{
				return this._isOpen;
			}
		}

		// Token: 0x06001A81 RID: 6785 RVA: 0x00058440 File Offset: 0x00056640
		public bool ReadBoolean(long position)
		{
			int sizeOfType = 1;
			this.EnsureSafeToRead(position, sizeOfType);
			byte b = this.InternalReadByte(position);
			return b > 0;
		}

		// Token: 0x06001A82 RID: 6786 RVA: 0x00058464 File Offset: 0x00056664
		public byte ReadByte(long position)
		{
			int sizeOfType = 1;
			this.EnsureSafeToRead(position, sizeOfType);
			return this.InternalReadByte(position);
		}

		// Token: 0x06001A83 RID: 6787 RVA: 0x00058484 File Offset: 0x00056684
		[SecuritySafeCritical]
		public unsafe char ReadChar(long position)
		{
			int num = 2;
			this.EnsureSafeToRead(position, num);
			byte* ptr = null;
			RuntimeHelpers.PrepareConstrainedRegions();
			char result;
			try
			{
				this._buffer.AcquirePointer(ref ptr);
				ptr += this._offset + position;
				if ((ptr & (num - 1)) == 0)
				{
					result = (char)(*(ushort*)ptr);
				}
				else
				{
					result = (char)((int)(*ptr) | ((int)ptr[1] << 8));
				}
			}
			finally
			{
				if (ptr != null)
				{
					this._buffer.ReleasePointer();
				}
			}
			return result;
		}

		// Token: 0x06001A84 RID: 6788 RVA: 0x000584F8 File Offset: 0x000566F8
		[SecuritySafeCritical]
		public unsafe short ReadInt16(long position)
		{
			int num = 2;
			this.EnsureSafeToRead(position, num);
			byte* ptr = null;
			RuntimeHelpers.PrepareConstrainedRegions();
			short result;
			try
			{
				this._buffer.AcquirePointer(ref ptr);
				ptr += this._offset + position;
				if ((ptr & (num - 1)) == 0)
				{
					result = *(short*)ptr;
				}
				else
				{
					result = (short)((int)(*ptr) | ((int)ptr[1] << 8));
				}
			}
			finally
			{
				if (ptr != null)
				{
					this._buffer.ReleasePointer();
				}
			}
			return result;
		}

		// Token: 0x06001A85 RID: 6789 RVA: 0x0005856C File Offset: 0x0005676C
		[SecuritySafeCritical]
		public unsafe int ReadInt32(long position)
		{
			int num = 4;
			this.EnsureSafeToRead(position, num);
			byte* ptr = null;
			RuntimeHelpers.PrepareConstrainedRegions();
			int result;
			try
			{
				this._buffer.AcquirePointer(ref ptr);
				ptr += this._offset + position;
				if ((ptr & (num - 1)) == 0)
				{
					result = *(int*)ptr;
				}
				else
				{
					result = (int)(*ptr) | ((int)ptr[1] << 8) | ((int)ptr[2] << 16) | ((int)ptr[3] << 24);
				}
			}
			finally
			{
				if (ptr != null)
				{
					this._buffer.ReleasePointer();
				}
			}
			return result;
		}

		// Token: 0x06001A86 RID: 6790 RVA: 0x000585F0 File Offset: 0x000567F0
		[SecuritySafeCritical]
		public unsafe long ReadInt64(long position)
		{
			int num = 8;
			this.EnsureSafeToRead(position, num);
			byte* ptr = null;
			RuntimeHelpers.PrepareConstrainedRegions();
			long result;
			try
			{
				this._buffer.AcquirePointer(ref ptr);
				ptr += this._offset + position;
				if ((ptr & (num - 1)) == 0)
				{
					result = *(long*)ptr;
				}
				else
				{
					int num2 = (int)(*ptr) | ((int)ptr[1] << 8) | ((int)ptr[2] << 16) | ((int)ptr[3] << 24);
					int num3 = (int)ptr[4] | ((int)ptr[5] << 8) | ((int)ptr[6] << 16) | ((int)ptr[7] << 24);
					result = ((long)num3 << 32) | (long)((ulong)num2);
				}
			}
			finally
			{
				if (ptr != null)
				{
					this._buffer.ReleasePointer();
				}
			}
			return result;
		}

		// Token: 0x06001A87 RID: 6791 RVA: 0x0005869C File Offset: 0x0005689C
		[SecuritySafeCritical]
		public decimal ReadDecimal(long position)
		{
			int sizeOfType = 16;
			this.EnsureSafeToRead(position, sizeOfType);
			int[] array = new int[4];
			this.ReadArray<int>(position, array, 0, array.Length);
			return new decimal(array);
		}

		// Token: 0x06001A88 RID: 6792 RVA: 0x000586D0 File Offset: 0x000568D0
		[SecuritySafeCritical]
		public unsafe float ReadSingle(long position)
		{
			int num = 4;
			this.EnsureSafeToRead(position, num);
			byte* ptr = null;
			RuntimeHelpers.PrepareConstrainedRegions();
			float result;
			try
			{
				this._buffer.AcquirePointer(ref ptr);
				ptr += this._offset + position;
				if ((ptr & (num - 1)) == 0)
				{
					result = *(float*)ptr;
				}
				else
				{
					uint num2 = (uint)((int)(*ptr) | ((int)ptr[1] << 8) | ((int)ptr[2] << 16) | ((int)ptr[3] << 24));
					result = *(float*)(&num2);
				}
			}
			finally
			{
				if (ptr != null)
				{
					this._buffer.ReleasePointer();
				}
			}
			return result;
		}

		// Token: 0x06001A89 RID: 6793 RVA: 0x00058758 File Offset: 0x00056958
		[SecuritySafeCritical]
		public unsafe double ReadDouble(long position)
		{
			int num = 8;
			this.EnsureSafeToRead(position, num);
			byte* ptr = null;
			RuntimeHelpers.PrepareConstrainedRegions();
			double result;
			try
			{
				this._buffer.AcquirePointer(ref ptr);
				ptr += this._offset + position;
				if ((ptr & (num - 1)) == 0)
				{
					result = *(double*)ptr;
				}
				else
				{
					uint num2 = (uint)((int)(*ptr) | ((int)ptr[1] << 8) | ((int)ptr[2] << 16) | ((int)ptr[3] << 24));
					uint num3 = (uint)((int)ptr[4] | ((int)ptr[5] << 8) | ((int)ptr[6] << 16) | ((int)ptr[7] << 24));
					ulong num4 = ((ulong)num3 << 32) | (ulong)num2;
					result = *(double*)(&num4);
				}
			}
			finally
			{
				if (ptr != null)
				{
					this._buffer.ReleasePointer();
				}
			}
			return result;
		}

		// Token: 0x06001A8A RID: 6794 RVA: 0x00058808 File Offset: 0x00056A08
		[SecuritySafeCritical]
		[CLSCompliant(false)]
		public unsafe sbyte ReadSByte(long position)
		{
			int sizeOfType = 1;
			this.EnsureSafeToRead(position, sizeOfType);
			byte* ptr = null;
			RuntimeHelpers.PrepareConstrainedRegions();
			sbyte result;
			try
			{
				this._buffer.AcquirePointer(ref ptr);
				ptr += this._offset + position;
				result = *(sbyte*)ptr;
			}
			finally
			{
				if (ptr != null)
				{
					this._buffer.ReleasePointer();
				}
			}
			return result;
		}

		// Token: 0x06001A8B RID: 6795 RVA: 0x00058868 File Offset: 0x00056A68
		[SecuritySafeCritical]
		[CLSCompliant(false)]
		public unsafe ushort ReadUInt16(long position)
		{
			int num = 2;
			this.EnsureSafeToRead(position, num);
			byte* ptr = null;
			RuntimeHelpers.PrepareConstrainedRegions();
			ushort result;
			try
			{
				this._buffer.AcquirePointer(ref ptr);
				ptr += this._offset + position;
				if ((ptr & (num - 1)) == 0)
				{
					result = *(ushort*)ptr;
				}
				else
				{
					result = (ushort)((int)(*ptr) | ((int)ptr[1] << 8));
				}
			}
			finally
			{
				if (ptr != null)
				{
					this._buffer.ReleasePointer();
				}
			}
			return result;
		}

		// Token: 0x06001A8C RID: 6796 RVA: 0x000588DC File Offset: 0x00056ADC
		[SecuritySafeCritical]
		[CLSCompliant(false)]
		public unsafe uint ReadUInt32(long position)
		{
			int num = 4;
			this.EnsureSafeToRead(position, num);
			byte* ptr = null;
			RuntimeHelpers.PrepareConstrainedRegions();
			uint result;
			try
			{
				this._buffer.AcquirePointer(ref ptr);
				ptr += this._offset + position;
				if ((ptr & (num - 1)) == 0)
				{
					result = *(uint*)ptr;
				}
				else
				{
					result = (uint)((int)(*ptr) | ((int)ptr[1] << 8) | ((int)ptr[2] << 16) | ((int)ptr[3] << 24));
				}
			}
			finally
			{
				if (ptr != null)
				{
					this._buffer.ReleasePointer();
				}
			}
			return result;
		}

		// Token: 0x06001A8D RID: 6797 RVA: 0x00058960 File Offset: 0x00056B60
		[SecuritySafeCritical]
		[CLSCompliant(false)]
		public unsafe ulong ReadUInt64(long position)
		{
			int num = 8;
			this.EnsureSafeToRead(position, num);
			byte* ptr = null;
			RuntimeHelpers.PrepareConstrainedRegions();
			ulong result;
			try
			{
				this._buffer.AcquirePointer(ref ptr);
				ptr += this._offset + position;
				if ((ptr & (num - 1)) == 0)
				{
					result = (ulong)(*(long*)ptr);
				}
				else
				{
					uint num2 = (uint)((int)(*ptr) | ((int)ptr[1] << 8) | ((int)ptr[2] << 16) | ((int)ptr[3] << 24));
					uint num3 = (uint)((int)ptr[4] | ((int)ptr[5] << 8) | ((int)ptr[6] << 16) | ((int)ptr[7] << 24));
					result = ((ulong)num3 << 32) | (ulong)num2;
				}
			}
			finally
			{
				if (ptr != null)
				{
					this._buffer.ReleasePointer();
				}
			}
			return result;
		}

		// Token: 0x06001A8E RID: 6798 RVA: 0x00058A0C File Offset: 0x00056C0C
		[SecurityCritical]
		public void Read<T>(long position, out T structure) where T : struct
		{
			if (position < 0L)
			{
				throw new ArgumentOutOfRangeException("position", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			if (!this._isOpen)
			{
				throw new ObjectDisposedException("UnmanagedMemoryAccessor", Environment.GetResourceString("ObjectDisposed_ViewAccessorClosed"));
			}
			if (!this.CanRead)
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_Reading"));
			}
			uint num = Marshal.SizeOfType(typeof(T));
			if (position <= this._capacity - (long)((ulong)num))
			{
				structure = this._buffer.Read<T>((ulong)(this._offset + position));
				return;
			}
			if (position >= this._capacity)
			{
				throw new ArgumentOutOfRangeException("position", Environment.GetResourceString("ArgumentOutOfRange_PositionLessThanCapacityRequired"));
			}
			throw new ArgumentException(Environment.GetResourceString("Argument_NotEnoughBytesToRead", new object[] { typeof(T).FullName }), "position");
		}

		// Token: 0x06001A8F RID: 6799 RVA: 0x00058AE8 File Offset: 0x00056CE8
		[SecurityCritical]
		public int ReadArray<T>(long position, T[] array, int offset, int count) where T : struct
		{
			if (array == null)
			{
				throw new ArgumentNullException("array", "Buffer cannot be null.");
			}
			if (offset < 0)
			{
				throw new ArgumentOutOfRangeException("offset", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			if (array.Length - offset < count)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_OffsetAndLengthOutOfBounds"));
			}
			if (!this.CanRead)
			{
				if (!this._isOpen)
				{
					throw new ObjectDisposedException("UnmanagedMemoryAccessor", Environment.GetResourceString("ObjectDisposed_ViewAccessorClosed"));
				}
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_Reading"));
			}
			else
			{
				if (position < 0L)
				{
					throw new ArgumentOutOfRangeException("position", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
				}
				uint num = Marshal.AlignedSizeOf<T>();
				if (position >= this._capacity)
				{
					throw new ArgumentOutOfRangeException("position", Environment.GetResourceString("ArgumentOutOfRange_PositionLessThanCapacityRequired"));
				}
				int num2 = count;
				long num3 = this._capacity - position;
				if (num3 < 0L)
				{
					num2 = 0;
				}
				else
				{
					ulong num4 = (ulong)num * (ulong)((long)count);
					if (num3 < (long)num4)
					{
						num2 = (int)(num3 / (long)((ulong)num));
					}
				}
				this._buffer.ReadArray<T>((ulong)(this._offset + position), array, offset, num2);
				return num2;
			}
		}

		// Token: 0x06001A90 RID: 6800 RVA: 0x00058C04 File Offset: 0x00056E04
		public void Write(long position, bool value)
		{
			int sizeOfType = 1;
			this.EnsureSafeToWrite(position, sizeOfType);
			byte value2 = (value ? 1 : 0);
			this.InternalWrite(position, value2);
		}

		// Token: 0x06001A91 RID: 6801 RVA: 0x00058C2C File Offset: 0x00056E2C
		public void Write(long position, byte value)
		{
			int sizeOfType = 1;
			this.EnsureSafeToWrite(position, sizeOfType);
			this.InternalWrite(position, value);
		}

		// Token: 0x06001A92 RID: 6802 RVA: 0x00058C4C File Offset: 0x00056E4C
		[SecuritySafeCritical]
		public unsafe void Write(long position, char value)
		{
			int num = 2;
			this.EnsureSafeToWrite(position, num);
			byte* ptr = null;
			RuntimeHelpers.PrepareConstrainedRegions();
			try
			{
				this._buffer.AcquirePointer(ref ptr);
				ptr += this._offset + position;
				if ((ptr & (num - 1)) == 0)
				{
					*(short*)ptr = (short)value;
				}
				else
				{
					*ptr = (byte)value;
					ptr[1] = (byte)(value >> 8);
				}
			}
			finally
			{
				if (ptr != null)
				{
					this._buffer.ReleasePointer();
				}
			}
		}

		// Token: 0x06001A93 RID: 6803 RVA: 0x00058CC0 File Offset: 0x00056EC0
		[SecuritySafeCritical]
		public unsafe void Write(long position, short value)
		{
			int num = 2;
			this.EnsureSafeToWrite(position, num);
			byte* ptr = null;
			RuntimeHelpers.PrepareConstrainedRegions();
			try
			{
				this._buffer.AcquirePointer(ref ptr);
				ptr += this._offset + position;
				if ((ptr & (num - 1)) == 0)
				{
					*(short*)ptr = value;
				}
				else
				{
					*ptr = (byte)value;
					ptr[1] = (byte)(value >> 8);
				}
			}
			finally
			{
				if (ptr != null)
				{
					this._buffer.ReleasePointer();
				}
			}
		}

		// Token: 0x06001A94 RID: 6804 RVA: 0x00058D34 File Offset: 0x00056F34
		[SecuritySafeCritical]
		public unsafe void Write(long position, int value)
		{
			int num = 4;
			this.EnsureSafeToWrite(position, num);
			byte* ptr = null;
			RuntimeHelpers.PrepareConstrainedRegions();
			try
			{
				this._buffer.AcquirePointer(ref ptr);
				ptr += this._offset + position;
				if ((ptr & (num - 1)) == 0)
				{
					*(int*)ptr = value;
				}
				else
				{
					*ptr = (byte)value;
					ptr[1] = (byte)(value >> 8);
					ptr[2] = (byte)(value >> 16);
					ptr[3] = (byte)(value >> 24);
				}
			}
			finally
			{
				if (ptr != null)
				{
					this._buffer.ReleasePointer();
				}
			}
		}

		// Token: 0x06001A95 RID: 6805 RVA: 0x00058DBC File Offset: 0x00056FBC
		[SecuritySafeCritical]
		public unsafe void Write(long position, long value)
		{
			int num = 8;
			this.EnsureSafeToWrite(position, num);
			byte* ptr = null;
			RuntimeHelpers.PrepareConstrainedRegions();
			try
			{
				this._buffer.AcquirePointer(ref ptr);
				ptr += this._offset + position;
				if ((ptr & (num - 1)) == 0)
				{
					*(long*)ptr = value;
				}
				else
				{
					*ptr = (byte)value;
					ptr[1] = (byte)(value >> 8);
					ptr[2] = (byte)(value >> 16);
					ptr[3] = (byte)(value >> 24);
					ptr[4] = (byte)(value >> 32);
					ptr[5] = (byte)(value >> 40);
					ptr[6] = (byte)(value >> 48);
					ptr[7] = (byte)(value >> 56);
				}
			}
			finally
			{
				if (ptr != null)
				{
					this._buffer.ReleasePointer();
				}
			}
		}

		// Token: 0x06001A96 RID: 6806 RVA: 0x00058E68 File Offset: 0x00057068
		[SecuritySafeCritical]
		public void Write(long position, decimal value)
		{
			int sizeOfType = 16;
			this.EnsureSafeToWrite(position, sizeOfType);
			byte[] array = new byte[16];
			decimal.GetBytes(value, array);
			int[] array2 = new int[4];
			int num = (int)array[12] | ((int)array[13] << 8) | ((int)array[14] << 16) | ((int)array[15] << 24);
			int num2 = (int)array[0] | ((int)array[1] << 8) | ((int)array[2] << 16) | ((int)array[3] << 24);
			int num3 = (int)array[4] | ((int)array[5] << 8) | ((int)array[6] << 16) | ((int)array[7] << 24);
			int num4 = (int)array[8] | ((int)array[9] << 8) | ((int)array[10] << 16) | ((int)array[11] << 24);
			array2[0] = num2;
			array2[1] = num3;
			array2[2] = num4;
			array2[3] = num;
			this.WriteArray<int>(position, array2, 0, array2.Length);
		}

		// Token: 0x06001A97 RID: 6807 RVA: 0x00058F20 File Offset: 0x00057120
		[SecuritySafeCritical]
		public unsafe void Write(long position, float value)
		{
			int num = 4;
			this.EnsureSafeToWrite(position, num);
			byte* ptr = null;
			RuntimeHelpers.PrepareConstrainedRegions();
			try
			{
				this._buffer.AcquirePointer(ref ptr);
				ptr += this._offset + position;
				if ((ptr & (num - 1)) == 0)
				{
					*(float*)ptr = value;
				}
				else
				{
					uint num2 = *(uint*)(&value);
					*ptr = (byte)num2;
					ptr[1] = (byte)(num2 >> 8);
					ptr[2] = (byte)(num2 >> 16);
					ptr[3] = (byte)(num2 >> 24);
				}
			}
			finally
			{
				if (ptr != null)
				{
					this._buffer.ReleasePointer();
				}
			}
		}

		// Token: 0x06001A98 RID: 6808 RVA: 0x00058FAC File Offset: 0x000571AC
		[SecuritySafeCritical]
		public unsafe void Write(long position, double value)
		{
			int num = 8;
			this.EnsureSafeToWrite(position, num);
			byte* ptr = null;
			RuntimeHelpers.PrepareConstrainedRegions();
			try
			{
				this._buffer.AcquirePointer(ref ptr);
				ptr += this._offset + position;
				if ((ptr & (num - 1)) == 0)
				{
					*(double*)ptr = value;
				}
				else
				{
					ulong num2 = (ulong)(*(long*)(&value));
					*ptr = (byte)num2;
					ptr[1] = (byte)(num2 >> 8);
					ptr[2] = (byte)(num2 >> 16);
					ptr[3] = (byte)(num2 >> 24);
					ptr[4] = (byte)(num2 >> 32);
					ptr[5] = (byte)(num2 >> 40);
					ptr[6] = (byte)(num2 >> 48);
					ptr[7] = (byte)(num2 >> 56);
				}
			}
			finally
			{
				if (ptr != null)
				{
					this._buffer.ReleasePointer();
				}
			}
		}

		// Token: 0x06001A99 RID: 6809 RVA: 0x0005905C File Offset: 0x0005725C
		[SecuritySafeCritical]
		[CLSCompliant(false)]
		public unsafe void Write(long position, sbyte value)
		{
			int sizeOfType = 1;
			this.EnsureSafeToWrite(position, sizeOfType);
			byte* ptr = null;
			RuntimeHelpers.PrepareConstrainedRegions();
			try
			{
				this._buffer.AcquirePointer(ref ptr);
				ptr += this._offset + position;
				*ptr = (byte)value;
			}
			finally
			{
				if (ptr != null)
				{
					this._buffer.ReleasePointer();
				}
			}
		}

		// Token: 0x06001A9A RID: 6810 RVA: 0x000590BC File Offset: 0x000572BC
		[SecuritySafeCritical]
		[CLSCompliant(false)]
		public unsafe void Write(long position, ushort value)
		{
			int num = 2;
			this.EnsureSafeToWrite(position, num);
			byte* ptr = null;
			RuntimeHelpers.PrepareConstrainedRegions();
			try
			{
				this._buffer.AcquirePointer(ref ptr);
				ptr += this._offset + position;
				if ((ptr & (num - 1)) == 0)
				{
					*(short*)ptr = (short)value;
				}
				else
				{
					*ptr = (byte)value;
					ptr[1] = (byte)(value >> 8);
				}
			}
			finally
			{
				if (ptr != null)
				{
					this._buffer.ReleasePointer();
				}
			}
		}

		// Token: 0x06001A9B RID: 6811 RVA: 0x00059130 File Offset: 0x00057330
		[SecuritySafeCritical]
		[CLSCompliant(false)]
		public unsafe void Write(long position, uint value)
		{
			int num = 4;
			this.EnsureSafeToWrite(position, num);
			byte* ptr = null;
			RuntimeHelpers.PrepareConstrainedRegions();
			try
			{
				this._buffer.AcquirePointer(ref ptr);
				ptr += this._offset + position;
				if ((ptr & (num - 1)) == 0)
				{
					*(int*)ptr = (int)value;
				}
				else
				{
					*ptr = (byte)value;
					ptr[1] = (byte)(value >> 8);
					ptr[2] = (byte)(value >> 16);
					ptr[3] = (byte)(value >> 24);
				}
			}
			finally
			{
				if (ptr != null)
				{
					this._buffer.ReleasePointer();
				}
			}
		}

		// Token: 0x06001A9C RID: 6812 RVA: 0x000591B8 File Offset: 0x000573B8
		[SecuritySafeCritical]
		[CLSCompliant(false)]
		public unsafe void Write(long position, ulong value)
		{
			int num = 8;
			this.EnsureSafeToWrite(position, num);
			byte* ptr = null;
			RuntimeHelpers.PrepareConstrainedRegions();
			try
			{
				this._buffer.AcquirePointer(ref ptr);
				ptr += this._offset + position;
				if ((ptr & (num - 1)) == 0)
				{
					*(long*)ptr = (long)value;
				}
				else
				{
					*ptr = (byte)value;
					ptr[1] = (byte)(value >> 8);
					ptr[2] = (byte)(value >> 16);
					ptr[3] = (byte)(value >> 24);
					ptr[4] = (byte)(value >> 32);
					ptr[5] = (byte)(value >> 40);
					ptr[6] = (byte)(value >> 48);
					ptr[7] = (byte)(value >> 56);
				}
			}
			finally
			{
				if (ptr != null)
				{
					this._buffer.ReleasePointer();
				}
			}
		}

		// Token: 0x06001A9D RID: 6813 RVA: 0x00059264 File Offset: 0x00057464
		[SecurityCritical]
		public void Write<T>(long position, ref T structure) where T : struct
		{
			if (position < 0L)
			{
				throw new ArgumentOutOfRangeException("position", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			if (!this._isOpen)
			{
				throw new ObjectDisposedException("UnmanagedMemoryAccessor", Environment.GetResourceString("ObjectDisposed_ViewAccessorClosed"));
			}
			if (!this.CanWrite)
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_Writing"));
			}
			uint num = Marshal.SizeOfType(typeof(T));
			if (position <= this._capacity - (long)((ulong)num))
			{
				this._buffer.Write<T>((ulong)(this._offset + position), structure);
				return;
			}
			if (position >= this._capacity)
			{
				throw new ArgumentOutOfRangeException("position", Environment.GetResourceString("ArgumentOutOfRange_PositionLessThanCapacityRequired"));
			}
			throw new ArgumentException(Environment.GetResourceString("Argument_NotEnoughBytesToWrite", new object[] { typeof(T).FullName }), "position");
		}

		// Token: 0x06001A9E RID: 6814 RVA: 0x00059340 File Offset: 0x00057540
		[SecurityCritical]
		public void WriteArray<T>(long position, T[] array, int offset, int count) where T : struct
		{
			if (array == null)
			{
				throw new ArgumentNullException("array", "Buffer cannot be null.");
			}
			if (offset < 0)
			{
				throw new ArgumentOutOfRangeException("offset", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			if (array.Length - offset < count)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_OffsetAndLengthOutOfBounds"));
			}
			if (position < 0L)
			{
				throw new ArgumentOutOfRangeException("position", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			if (position >= this.Capacity)
			{
				throw new ArgumentOutOfRangeException("position", Environment.GetResourceString("ArgumentOutOfRange_PositionLessThanCapacityRequired"));
			}
			if (!this._isOpen)
			{
				throw new ObjectDisposedException("UnmanagedMemoryAccessor", Environment.GetResourceString("ObjectDisposed_ViewAccessorClosed"));
			}
			if (!this.CanWrite)
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_Writing"));
			}
			this._buffer.WriteArray<T>((ulong)(this._offset + position), array, offset, count);
		}

		// Token: 0x06001A9F RID: 6815 RVA: 0x00059430 File Offset: 0x00057630
		[SecuritySafeCritical]
		private unsafe byte InternalReadByte(long position)
		{
			byte* ptr = null;
			RuntimeHelpers.PrepareConstrainedRegions();
			byte result;
			try
			{
				this._buffer.AcquirePointer(ref ptr);
				result = (ptr + this._offset)[position];
			}
			finally
			{
				if (ptr != null)
				{
					this._buffer.ReleasePointer();
				}
			}
			return result;
		}

		// Token: 0x06001AA0 RID: 6816 RVA: 0x00059484 File Offset: 0x00057684
		[SecuritySafeCritical]
		private unsafe void InternalWrite(long position, byte value)
		{
			byte* ptr = null;
			RuntimeHelpers.PrepareConstrainedRegions();
			try
			{
				this._buffer.AcquirePointer(ref ptr);
				(ptr + this._offset)[position] = value;
			}
			finally
			{
				if (ptr != null)
				{
					this._buffer.ReleasePointer();
				}
			}
		}

		// Token: 0x06001AA1 RID: 6817 RVA: 0x000594D8 File Offset: 0x000576D8
		private void EnsureSafeToRead(long position, int sizeOfType)
		{
			if (!this._isOpen)
			{
				throw new ObjectDisposedException("UnmanagedMemoryAccessor", Environment.GetResourceString("ObjectDisposed_ViewAccessorClosed"));
			}
			if (!this.CanRead)
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_Reading"));
			}
			if (position < 0L)
			{
				throw new ArgumentOutOfRangeException("position", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			if (position <= this._capacity - (long)sizeOfType)
			{
				return;
			}
			if (position >= this._capacity)
			{
				throw new ArgumentOutOfRangeException("position", Environment.GetResourceString("ArgumentOutOfRange_PositionLessThanCapacityRequired"));
			}
			throw new ArgumentException(Environment.GetResourceString("Argument_NotEnoughBytesToRead"), "position");
		}

		// Token: 0x06001AA2 RID: 6818 RVA: 0x00059574 File Offset: 0x00057774
		private void EnsureSafeToWrite(long position, int sizeOfType)
		{
			if (!this._isOpen)
			{
				throw new ObjectDisposedException("UnmanagedMemoryAccessor", Environment.GetResourceString("ObjectDisposed_ViewAccessorClosed"));
			}
			if (!this.CanWrite)
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_Writing"));
			}
			if (position < 0L)
			{
				throw new ArgumentOutOfRangeException("position", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			if (position <= this._capacity - (long)sizeOfType)
			{
				return;
			}
			if (position >= this._capacity)
			{
				throw new ArgumentOutOfRangeException("position", Environment.GetResourceString("ArgumentOutOfRange_PositionLessThanCapacityRequired"));
			}
			throw new ArgumentException(Environment.GetResourceString("Argument_NotEnoughBytesToWrite", new object[] { "Byte" }), "position");
		}

		// Token: 0x04000931 RID: 2353
		[SecurityCritical]
		private SafeBuffer _buffer;

		// Token: 0x04000932 RID: 2354
		private long _offset;

		// Token: 0x04000933 RID: 2355
		private long _capacity;

		// Token: 0x04000934 RID: 2356
		private FileAccess _access;

		// Token: 0x04000935 RID: 2357
		private bool _isOpen;

		// Token: 0x04000936 RID: 2358
		private bool _canRead;

		// Token: 0x04000937 RID: 2359
		private bool _canWrite;
	}
}
