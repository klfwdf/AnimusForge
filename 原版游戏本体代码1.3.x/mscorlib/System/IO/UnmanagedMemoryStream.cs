using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO
{
	// Token: 0x020001A9 RID: 425
	public class UnmanagedMemoryStream : Stream
	{
		// Token: 0x06001AA3 RID: 6819 RVA: 0x0005961D File Offset: 0x0005781D
		[SecuritySafeCritical]
		protected UnmanagedMemoryStream()
		{
			this._mem = null;
			this._isOpen = false;
		}

		// Token: 0x06001AA4 RID: 6820 RVA: 0x00059634 File Offset: 0x00057834
		[SecuritySafeCritical]
		public UnmanagedMemoryStream(SafeBuffer buffer, long offset, long length)
		{
			this.Initialize(buffer, offset, length, FileAccess.Read, false);
		}

		// Token: 0x06001AA5 RID: 6821 RVA: 0x00059647 File Offset: 0x00057847
		[SecuritySafeCritical]
		public UnmanagedMemoryStream(SafeBuffer buffer, long offset, long length, FileAccess access)
		{
			this.Initialize(buffer, offset, length, access, false);
		}

		// Token: 0x06001AA6 RID: 6822 RVA: 0x0005965B File Offset: 0x0005785B
		[SecurityCritical]
		internal UnmanagedMemoryStream(SafeBuffer buffer, long offset, long length, FileAccess access, bool skipSecurityCheck)
		{
			this.Initialize(buffer, offset, length, access, skipSecurityCheck);
		}

		// Token: 0x06001AA7 RID: 6823 RVA: 0x00059670 File Offset: 0x00057870
		[SecuritySafeCritical]
		protected void Initialize(SafeBuffer buffer, long offset, long length, FileAccess access)
		{
			this.Initialize(buffer, offset, length, access, false);
		}

		// Token: 0x06001AA8 RID: 6824 RVA: 0x00059680 File Offset: 0x00057880
		[SecurityCritical]
		internal unsafe void Initialize(SafeBuffer buffer, long offset, long length, FileAccess access, bool skipSecurityCheck)
		{
			if (buffer == null)
			{
				throw new ArgumentNullException("buffer");
			}
			if (offset < 0L)
			{
				throw new ArgumentOutOfRangeException("offset", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			if (length < 0L)
			{
				throw new ArgumentOutOfRangeException("length", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			if (buffer.ByteLength < (ulong)(offset + length))
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidSafeBufferOffLen"));
			}
			if (access < FileAccess.Read || access > FileAccess.ReadWrite)
			{
				throw new ArgumentOutOfRangeException("access");
			}
			if (this._isOpen)
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CalledTwice"));
			}
			if (!skipSecurityCheck)
			{
				new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Demand();
			}
			byte* ptr = null;
			RuntimeHelpers.PrepareConstrainedRegions();
			try
			{
				buffer.AcquirePointer(ref ptr);
				if (ptr + offset + length < ptr)
				{
					throw new ArgumentException(Environment.GetResourceString("ArgumentOutOfRange_UnmanagedMemStreamWrapAround"));
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
			this._length = length;
			this._capacity = length;
			this._access = access;
			this._isOpen = true;
		}

		// Token: 0x06001AA9 RID: 6825 RVA: 0x0005979C File Offset: 0x0005799C
		[SecurityCritical]
		[CLSCompliant(false)]
		public unsafe UnmanagedMemoryStream(byte* pointer, long length)
		{
			this.Initialize(pointer, length, length, FileAccess.Read, false);
		}

		// Token: 0x06001AAA RID: 6826 RVA: 0x000597AF File Offset: 0x000579AF
		[SecurityCritical]
		[CLSCompliant(false)]
		public unsafe UnmanagedMemoryStream(byte* pointer, long length, long capacity, FileAccess access)
		{
			this.Initialize(pointer, length, capacity, access, false);
		}

		// Token: 0x06001AAB RID: 6827 RVA: 0x000597C3 File Offset: 0x000579C3
		[SecurityCritical]
		internal unsafe UnmanagedMemoryStream(byte* pointer, long length, long capacity, FileAccess access, bool skipSecurityCheck)
		{
			this.Initialize(pointer, length, capacity, access, skipSecurityCheck);
		}

		// Token: 0x06001AAC RID: 6828 RVA: 0x000597D8 File Offset: 0x000579D8
		[SecurityCritical]
		[CLSCompliant(false)]
		protected unsafe void Initialize(byte* pointer, long length, long capacity, FileAccess access)
		{
			this.Initialize(pointer, length, capacity, access, false);
		}

		// Token: 0x06001AAD RID: 6829 RVA: 0x000597E8 File Offset: 0x000579E8
		[SecurityCritical]
		internal unsafe void Initialize(byte* pointer, long length, long capacity, FileAccess access, bool skipSecurityCheck)
		{
			if (pointer == null)
			{
				throw new ArgumentNullException("pointer");
			}
			if (length < 0L || capacity < 0L)
			{
				throw new ArgumentOutOfRangeException((length < 0L) ? "length" : "capacity", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			if (length > capacity)
			{
				throw new ArgumentOutOfRangeException("length", Environment.GetResourceString("ArgumentOutOfRange_LengthGreaterThanCapacity"));
			}
			if (pointer + capacity < pointer)
			{
				throw new ArgumentOutOfRangeException("capacity", Environment.GetResourceString("ArgumentOutOfRange_UnmanagedMemStreamWrapAround"));
			}
			if (access < FileAccess.Read || access > FileAccess.ReadWrite)
			{
				throw new ArgumentOutOfRangeException("access", Environment.GetResourceString("ArgumentOutOfRange_Enum"));
			}
			if (this._isOpen)
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CalledTwice"));
			}
			if (!skipSecurityCheck)
			{
				new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Demand();
			}
			this._mem = pointer;
			this._offset = 0L;
			this._length = length;
			this._capacity = capacity;
			this._access = access;
			this._isOpen = true;
		}

		// Token: 0x170002F3 RID: 755
		// (get) Token: 0x06001AAE RID: 6830 RVA: 0x000598D8 File Offset: 0x00057AD8
		public override bool CanRead
		{
			get
			{
				return this._isOpen && (this._access & FileAccess.Read) > (FileAccess)0;
			}
		}

		// Token: 0x170002F4 RID: 756
		// (get) Token: 0x06001AAF RID: 6831 RVA: 0x000598EF File Offset: 0x00057AEF
		public override bool CanSeek
		{
			get
			{
				return this._isOpen;
			}
		}

		// Token: 0x170002F5 RID: 757
		// (get) Token: 0x06001AB0 RID: 6832 RVA: 0x000598F7 File Offset: 0x00057AF7
		public override bool CanWrite
		{
			get
			{
				return this._isOpen && (this._access & FileAccess.Write) > (FileAccess)0;
			}
		}

		// Token: 0x06001AB1 RID: 6833 RVA: 0x0005990E File Offset: 0x00057B0E
		[SecuritySafeCritical]
		protected override void Dispose(bool disposing)
		{
			this._isOpen = false;
			this._mem = null;
			base.Dispose(disposing);
		}

		// Token: 0x06001AB2 RID: 6834 RVA: 0x00059926 File Offset: 0x00057B26
		public override void Flush()
		{
			if (!this._isOpen)
			{
				__Error.StreamIsClosed();
			}
		}

		// Token: 0x06001AB3 RID: 6835 RVA: 0x00059938 File Offset: 0x00057B38
		[ComVisible(false)]
		[HostProtection(SecurityAction.LinkDemand, ExternalThreading = true)]
		public override Task FlushAsync(CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return Task.FromCancellation(cancellationToken);
			}
			Task result;
			try
			{
				this.Flush();
				result = Task.CompletedTask;
			}
			catch (Exception exception)
			{
				result = Task.FromException(exception);
			}
			return result;
		}

		// Token: 0x170002F6 RID: 758
		// (get) Token: 0x06001AB4 RID: 6836 RVA: 0x00059980 File Offset: 0x00057B80
		public override long Length
		{
			get
			{
				if (!this._isOpen)
				{
					__Error.StreamIsClosed();
				}
				return Interlocked.Read(ref this._length);
			}
		}

		// Token: 0x170002F7 RID: 759
		// (get) Token: 0x06001AB5 RID: 6837 RVA: 0x0005999A File Offset: 0x00057B9A
		public long Capacity
		{
			get
			{
				if (!this._isOpen)
				{
					__Error.StreamIsClosed();
				}
				return this._capacity;
			}
		}

		// Token: 0x170002F8 RID: 760
		// (get) Token: 0x06001AB6 RID: 6838 RVA: 0x000599AF File Offset: 0x00057BAF
		// (set) Token: 0x06001AB7 RID: 6839 RVA: 0x000599C9 File Offset: 0x00057BC9
		public override long Position
		{
			get
			{
				if (!this.CanSeek)
				{
					__Error.StreamIsClosed();
				}
				return Interlocked.Read(ref this._position);
			}
			[SecuritySafeCritical]
			set
			{
				if (value < 0L)
				{
					throw new ArgumentOutOfRangeException("value", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
				}
				if (!this.CanSeek)
				{
					__Error.StreamIsClosed();
				}
				Interlocked.Exchange(ref this._position, value);
			}
		}

		// Token: 0x170002F9 RID: 761
		// (get) Token: 0x06001AB8 RID: 6840 RVA: 0x00059A00 File Offset: 0x00057C00
		// (set) Token: 0x06001AB9 RID: 6841 RVA: 0x00059A64 File Offset: 0x00057C64
		[CLSCompliant(false)]
		public unsafe byte* PositionPointer
		{
			[SecurityCritical]
			get
			{
				if (this._buffer != null)
				{
					throw new NotSupportedException(Environment.GetResourceString("NotSupported_UmsSafeBuffer"));
				}
				long num = Interlocked.Read(ref this._position);
				if (num > this._capacity)
				{
					throw new IndexOutOfRangeException(Environment.GetResourceString("IndexOutOfRange_UMSPosition"));
				}
				byte* result = this._mem + num;
				if (!this._isOpen)
				{
					__Error.StreamIsClosed();
				}
				return result;
			}
			[SecurityCritical]
			set
			{
				if (this._buffer != null)
				{
					throw new NotSupportedException(Environment.GetResourceString("NotSupported_UmsSafeBuffer"));
				}
				if (!this._isOpen)
				{
					__Error.StreamIsClosed();
				}
				if (new IntPtr((long)(value - this._mem)).ToInt64() > 9223372036854775807L)
				{
					throw new ArgumentOutOfRangeException("offset", Environment.GetResourceString("ArgumentOutOfRange_UnmanagedMemStreamLength"));
				}
				if (value < this._mem)
				{
					throw new IOException(Environment.GetResourceString("IO.IO_SeekBeforeBegin"));
				}
				Interlocked.Exchange(ref this._position, (long)(value - this._mem));
			}
		}

		// Token: 0x170002FA RID: 762
		// (get) Token: 0x06001ABA RID: 6842 RVA: 0x00059AFE File Offset: 0x00057CFE
		internal unsafe byte* Pointer
		{
			[SecurityCritical]
			get
			{
				if (this._buffer != null)
				{
					throw new NotSupportedException(Environment.GetResourceString("NotSupported_UmsSafeBuffer"));
				}
				return this._mem;
			}
		}

		// Token: 0x06001ABB RID: 6843 RVA: 0x00059B20 File Offset: 0x00057D20
		[SecuritySafeCritical]
		public unsafe override int Read([In] [Out] byte[] buffer, int offset, int count)
		{
			if (buffer == null)
			{
				throw new ArgumentNullException("buffer", Environment.GetResourceString("ArgumentNull_Buffer"));
			}
			if (offset < 0)
			{
				throw new ArgumentOutOfRangeException("offset", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			if (buffer.Length - offset < count)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
			}
			if (!this._isOpen)
			{
				__Error.StreamIsClosed();
			}
			if (!this.CanRead)
			{
				__Error.ReadNotSupported();
			}
			long num = Interlocked.Read(ref this._position);
			long num2 = Interlocked.Read(ref this._length);
			long num3 = num2 - num;
			if (num3 > (long)count)
			{
				num3 = (long)count;
			}
			if (num3 <= 0L)
			{
				return 0;
			}
			int num4 = (int)num3;
			if (num4 < 0)
			{
				num4 = 0;
			}
			if (this._buffer != null)
			{
				byte* ptr = null;
				RuntimeHelpers.PrepareConstrainedRegions();
				try
				{
					this._buffer.AcquirePointer(ref ptr);
					Buffer.Memcpy(buffer, offset, ptr + num + this._offset, 0, num4);
					goto IL_10A;
				}
				finally
				{
					if (ptr != null)
					{
						this._buffer.ReleasePointer();
					}
				}
			}
			Buffer.Memcpy(buffer, offset, this._mem + num, 0, num4);
			IL_10A:
			Interlocked.Exchange(ref this._position, num + num3);
			return num4;
		}

		// Token: 0x06001ABC RID: 6844 RVA: 0x00059C58 File Offset: 0x00057E58
		[ComVisible(false)]
		[HostProtection(SecurityAction.LinkDemand, ExternalThreading = true)]
		public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			if (buffer == null)
			{
				throw new ArgumentNullException("buffer", Environment.GetResourceString("ArgumentNull_Buffer"));
			}
			if (offset < 0)
			{
				throw new ArgumentOutOfRangeException("offset", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			if (buffer.Length - offset < count)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
			}
			if (cancellationToken.IsCancellationRequested)
			{
				return Task.FromCancellation<int>(cancellationToken);
			}
			Task<int> task;
			try
			{
				int num = this.Read(buffer, offset, count);
				Task<int> lastReadTask = this._lastReadTask;
				Task<int> task2;
				if (lastReadTask == null || lastReadTask.Result != num)
				{
					task = (this._lastReadTask = Task.FromResult<int>(num));
					task2 = task;
				}
				else
				{
					task2 = lastReadTask;
				}
				task = task2;
			}
			catch (Exception exception)
			{
				task = Task.FromException<int>(exception);
			}
			return task;
		}

		// Token: 0x06001ABD RID: 6845 RVA: 0x00059D28 File Offset: 0x00057F28
		[SecuritySafeCritical]
		public unsafe override int ReadByte()
		{
			if (!this._isOpen)
			{
				__Error.StreamIsClosed();
			}
			if (!this.CanRead)
			{
				__Error.ReadNotSupported();
			}
			long num = Interlocked.Read(ref this._position);
			long num2 = Interlocked.Read(ref this._length);
			if (num >= num2)
			{
				return -1;
			}
			Interlocked.Exchange(ref this._position, num + 1L);
			if (this._buffer != null)
			{
				byte* ptr = null;
				RuntimeHelpers.PrepareConstrainedRegions();
				try
				{
					this._buffer.AcquirePointer(ref ptr);
					return (int)(ptr + num)[this._offset];
				}
				finally
				{
					if (ptr != null)
					{
						this._buffer.ReleasePointer();
					}
				}
			}
			return (int)this._mem[num];
		}

		// Token: 0x06001ABE RID: 6846 RVA: 0x00059DD8 File Offset: 0x00057FD8
		public override long Seek(long offset, SeekOrigin loc)
		{
			if (!this._isOpen)
			{
				__Error.StreamIsClosed();
			}
			if (offset > 9223372036854775807L)
			{
				throw new ArgumentOutOfRangeException("offset", Environment.GetResourceString("ArgumentOutOfRange_UnmanagedMemStreamLength"));
			}
			switch (loc)
			{
			case SeekOrigin.Begin:
				if (offset < 0L)
				{
					throw new IOException(Environment.GetResourceString("IO.IO_SeekBeforeBegin"));
				}
				Interlocked.Exchange(ref this._position, offset);
				break;
			case SeekOrigin.Current:
			{
				long num = Interlocked.Read(ref this._position);
				if (offset + num < 0L)
				{
					throw new IOException(Environment.GetResourceString("IO.IO_SeekBeforeBegin"));
				}
				Interlocked.Exchange(ref this._position, offset + num);
				break;
			}
			case SeekOrigin.End:
			{
				long num2 = Interlocked.Read(ref this._length);
				if (num2 + offset < 0L)
				{
					throw new IOException(Environment.GetResourceString("IO.IO_SeekBeforeBegin"));
				}
				Interlocked.Exchange(ref this._position, num2 + offset);
				break;
			}
			default:
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidSeekOrigin"));
			}
			return Interlocked.Read(ref this._position);
		}

		// Token: 0x06001ABF RID: 6847 RVA: 0x00059ED4 File Offset: 0x000580D4
		[SecuritySafeCritical]
		public override void SetLength(long value)
		{
			if (value < 0L)
			{
				throw new ArgumentOutOfRangeException("length", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			if (this._buffer != null)
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_UmsSafeBuffer"));
			}
			if (!this._isOpen)
			{
				__Error.StreamIsClosed();
			}
			if (!this.CanWrite)
			{
				__Error.WriteNotSupported();
			}
			if (value > this._capacity)
			{
				throw new IOException(Environment.GetResourceString("IO.IO_FixedCapacity"));
			}
			long num = Interlocked.Read(ref this._position);
			long num2 = Interlocked.Read(ref this._length);
			if (value > num2)
			{
				Buffer.ZeroMemory(this._mem + num2, value - num2);
			}
			Interlocked.Exchange(ref this._length, value);
			if (num > value)
			{
				Interlocked.Exchange(ref this._position, value);
			}
		}

		// Token: 0x06001AC0 RID: 6848 RVA: 0x00059F94 File Offset: 0x00058194
		[SecuritySafeCritical]
		public unsafe override void Write(byte[] buffer, int offset, int count)
		{
			if (buffer == null)
			{
				throw new ArgumentNullException("buffer", Environment.GetResourceString("ArgumentNull_Buffer"));
			}
			if (offset < 0)
			{
				throw new ArgumentOutOfRangeException("offset", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			if (buffer.Length - offset < count)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
			}
			if (!this._isOpen)
			{
				__Error.StreamIsClosed();
			}
			if (!this.CanWrite)
			{
				__Error.WriteNotSupported();
			}
			long num = Interlocked.Read(ref this._position);
			long num2 = Interlocked.Read(ref this._length);
			long num3 = num + (long)count;
			if (num3 < 0L)
			{
				throw new IOException(Environment.GetResourceString("IO.IO_StreamTooLong"));
			}
			if (num3 > this._capacity)
			{
				throw new NotSupportedException(Environment.GetResourceString("IO.IO_FixedCapacity"));
			}
			if (this._buffer == null)
			{
				if (num > num2)
				{
					Buffer.ZeroMemory(this._mem + num2, num - num2);
				}
				if (num3 > num2)
				{
					Interlocked.Exchange(ref this._length, num3);
				}
			}
			if (this._buffer != null)
			{
				long num4 = this._capacity - num;
				if (num4 < (long)count)
				{
					throw new ArgumentException(Environment.GetResourceString("Arg_BufferTooSmall"));
				}
				byte* ptr = null;
				RuntimeHelpers.PrepareConstrainedRegions();
				try
				{
					this._buffer.AcquirePointer(ref ptr);
					Buffer.Memcpy(ptr + num + this._offset, 0, buffer, offset, count);
					goto IL_16D;
				}
				finally
				{
					if (ptr != null)
					{
						this._buffer.ReleasePointer();
					}
				}
			}
			Buffer.Memcpy(this._mem + num, 0, buffer, offset, count);
			IL_16D:
			Interlocked.Exchange(ref this._position, num3);
		}

		// Token: 0x06001AC1 RID: 6849 RVA: 0x0005A12C File Offset: 0x0005832C
		[ComVisible(false)]
		[HostProtection(SecurityAction.LinkDemand, ExternalThreading = true)]
		public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			if (buffer == null)
			{
				throw new ArgumentNullException("buffer", Environment.GetResourceString("ArgumentNull_Buffer"));
			}
			if (offset < 0)
			{
				throw new ArgumentOutOfRangeException("offset", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			if (buffer.Length - offset < count)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
			}
			if (cancellationToken.IsCancellationRequested)
			{
				return Task.FromCancellation(cancellationToken);
			}
			Task result;
			try
			{
				this.Write(buffer, offset, count);
				result = Task.CompletedTask;
			}
			catch (Exception exception)
			{
				result = Task.FromException<int>(exception);
			}
			return result;
		}

		// Token: 0x06001AC2 RID: 6850 RVA: 0x0005A1DC File Offset: 0x000583DC
		[SecuritySafeCritical]
		public unsafe override void WriteByte(byte value)
		{
			if (!this._isOpen)
			{
				__Error.StreamIsClosed();
			}
			if (!this.CanWrite)
			{
				__Error.WriteNotSupported();
			}
			long num = Interlocked.Read(ref this._position);
			long num2 = Interlocked.Read(ref this._length);
			long num3 = num + 1L;
			if (num >= num2)
			{
				if (num3 < 0L)
				{
					throw new IOException(Environment.GetResourceString("IO.IO_StreamTooLong"));
				}
				if (num3 > this._capacity)
				{
					throw new NotSupportedException(Environment.GetResourceString("IO.IO_FixedCapacity"));
				}
				if (this._buffer == null)
				{
					if (num > num2)
					{
						Buffer.ZeroMemory(this._mem + num2, num - num2);
					}
					Interlocked.Exchange(ref this._length, num3);
				}
			}
			if (this._buffer != null)
			{
				byte* ptr = null;
				RuntimeHelpers.PrepareConstrainedRegions();
				try
				{
					this._buffer.AcquirePointer(ref ptr);
					(ptr + num)[this._offset] = value;
					goto IL_DC;
				}
				finally
				{
					if (ptr != null)
					{
						this._buffer.ReleasePointer();
					}
				}
			}
			this._mem[num] = value;
			IL_DC:
			Interlocked.Exchange(ref this._position, num3);
		}

		// Token: 0x04000938 RID: 2360
		private const long UnmanagedMemStreamMaxLength = 9223372036854775807L;

		// Token: 0x04000939 RID: 2361
		[SecurityCritical]
		private SafeBuffer _buffer;

		// Token: 0x0400093A RID: 2362
		[SecurityCritical]
		private unsafe byte* _mem;

		// Token: 0x0400093B RID: 2363
		private long _length;

		// Token: 0x0400093C RID: 2364
		private long _capacity;

		// Token: 0x0400093D RID: 2365
		private long _position;

		// Token: 0x0400093E RID: 2366
		private long _offset;

		// Token: 0x0400093F RID: 2367
		private FileAccess _access;

		// Token: 0x04000940 RID: 2368
		internal bool _isOpen;

		// Token: 0x04000941 RID: 2369
		[NonSerialized]
		private Task<int> _lastReadTask;
	}
}
