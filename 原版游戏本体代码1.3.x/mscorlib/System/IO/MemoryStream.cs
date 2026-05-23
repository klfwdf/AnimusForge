using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO
{
	// Token: 0x02000199 RID: 409
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public class MemoryStream : Stream
	{
		// Token: 0x060018DF RID: 6367 RVA: 0x0005169C File Offset: 0x0004F89C
		[__DynamicallyInvokable]
		public MemoryStream()
			: this(0)
		{
		}

		// Token: 0x060018E0 RID: 6368 RVA: 0x000516A8 File Offset: 0x0004F8A8
		[__DynamicallyInvokable]
		public MemoryStream(int capacity)
		{
			if (capacity < 0)
			{
				throw new ArgumentOutOfRangeException("capacity", Environment.GetResourceString("ArgumentOutOfRange_NegativeCapacity"));
			}
			this._buffer = new byte[capacity];
			this._capacity = capacity;
			this._expandable = true;
			this._writable = true;
			this._exposable = true;
			this._origin = 0;
			this._isOpen = true;
		}

		// Token: 0x060018E1 RID: 6369 RVA: 0x0005170A File Offset: 0x0004F90A
		[__DynamicallyInvokable]
		public MemoryStream(byte[] buffer)
			: this(buffer, true)
		{
		}

		// Token: 0x060018E2 RID: 6370 RVA: 0x00051714 File Offset: 0x0004F914
		[__DynamicallyInvokable]
		public MemoryStream(byte[] buffer, bool writable)
		{
			if (buffer == null)
			{
				throw new ArgumentNullException("buffer", Environment.GetResourceString("ArgumentNull_Buffer"));
			}
			this._buffer = buffer;
			this._length = (this._capacity = buffer.Length);
			this._writable = writable;
			this._exposable = false;
			this._origin = 0;
			this._isOpen = true;
		}

		// Token: 0x060018E3 RID: 6371 RVA: 0x00051774 File Offset: 0x0004F974
		[__DynamicallyInvokable]
		public MemoryStream(byte[] buffer, int index, int count)
			: this(buffer, index, count, true, false)
		{
		}

		// Token: 0x060018E4 RID: 6372 RVA: 0x00051781 File Offset: 0x0004F981
		[__DynamicallyInvokable]
		public MemoryStream(byte[] buffer, int index, int count, bool writable)
			: this(buffer, index, count, writable, false)
		{
		}

		// Token: 0x060018E5 RID: 6373 RVA: 0x00051790 File Offset: 0x0004F990
		[__DynamicallyInvokable]
		public MemoryStream(byte[] buffer, int index, int count, bool writable, bool publiclyVisible)
		{
			if (buffer == null)
			{
				throw new ArgumentNullException("buffer", Environment.GetResourceString("ArgumentNull_Buffer"));
			}
			if (index < 0)
			{
				throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			if (buffer.Length - index < count)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
			}
			this._buffer = buffer;
			this._position = index;
			this._origin = index;
			this._length = (this._capacity = index + count);
			this._writable = writable;
			this._exposable = publiclyVisible;
			this._expandable = false;
			this._isOpen = true;
		}

		// Token: 0x170002C0 RID: 704
		// (get) Token: 0x060018E6 RID: 6374 RVA: 0x0005184C File Offset: 0x0004FA4C
		[__DynamicallyInvokable]
		public override bool CanRead
		{
			[__DynamicallyInvokable]
			get
			{
				return this._isOpen;
			}
		}

		// Token: 0x170002C1 RID: 705
		// (get) Token: 0x060018E7 RID: 6375 RVA: 0x00051854 File Offset: 0x0004FA54
		[__DynamicallyInvokable]
		public override bool CanSeek
		{
			[__DynamicallyInvokable]
			get
			{
				return this._isOpen;
			}
		}

		// Token: 0x170002C2 RID: 706
		// (get) Token: 0x060018E8 RID: 6376 RVA: 0x0005185C File Offset: 0x0004FA5C
		[__DynamicallyInvokable]
		public override bool CanWrite
		{
			[__DynamicallyInvokable]
			get
			{
				return this._writable;
			}
		}

		// Token: 0x060018E9 RID: 6377 RVA: 0x00051864 File Offset: 0x0004FA64
		private void EnsureWriteable()
		{
			if (!this.CanWrite)
			{
				__Error.WriteNotSupported();
			}
		}

		// Token: 0x060018EA RID: 6378 RVA: 0x00051874 File Offset: 0x0004FA74
		[__DynamicallyInvokable]
		protected override void Dispose(bool disposing)
		{
			try
			{
				if (disposing)
				{
					this._isOpen = false;
					this._writable = false;
					this._expandable = false;
					this._lastReadTask = null;
				}
			}
			finally
			{
				base.Dispose(disposing);
			}
		}

		// Token: 0x060018EB RID: 6379 RVA: 0x000518BC File Offset: 0x0004FABC
		private bool EnsureCapacity(int value)
		{
			if (value < 0)
			{
				throw new IOException(Environment.GetResourceString("IO.IO_StreamTooLong"));
			}
			if (value > this._capacity)
			{
				int num = value;
				if (num < 256)
				{
					num = 256;
				}
				if (num < this._capacity * 2)
				{
					num = this._capacity * 2;
				}
				if (this._capacity * 2 > 2147483591)
				{
					num = ((value > 2147483591) ? value : 2147483591);
				}
				this.Capacity = num;
				return true;
			}
			return false;
		}

		// Token: 0x060018EC RID: 6380 RVA: 0x00051934 File Offset: 0x0004FB34
		[__DynamicallyInvokable]
		public override void Flush()
		{
		}

		// Token: 0x060018ED RID: 6381 RVA: 0x00051938 File Offset: 0x0004FB38
		[ComVisible(false)]
		[__DynamicallyInvokable]
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

		// Token: 0x060018EE RID: 6382 RVA: 0x00051980 File Offset: 0x0004FB80
		public virtual byte[] GetBuffer()
		{
			if (!this._exposable)
			{
				throw new UnauthorizedAccessException(Environment.GetResourceString("UnauthorizedAccess_MemStreamBuffer"));
			}
			return this._buffer;
		}

		// Token: 0x060018EF RID: 6383 RVA: 0x000519A0 File Offset: 0x0004FBA0
		[__DynamicallyInvokable]
		public virtual bool TryGetBuffer(out ArraySegment<byte> buffer)
		{
			if (!this._exposable)
			{
				buffer = default(ArraySegment<byte>);
				return false;
			}
			buffer = new ArraySegment<byte>(this._buffer, this._origin, this._length - this._origin);
			return true;
		}

		// Token: 0x060018F0 RID: 6384 RVA: 0x000519D8 File Offset: 0x0004FBD8
		internal byte[] InternalGetBuffer()
		{
			return this._buffer;
		}

		// Token: 0x060018F1 RID: 6385 RVA: 0x000519E0 File Offset: 0x0004FBE0
		[FriendAccessAllowed]
		internal void InternalGetOriginAndLength(out int origin, out int length)
		{
			if (!this._isOpen)
			{
				__Error.StreamIsClosed();
			}
			origin = this._origin;
			length = this._length;
		}

		// Token: 0x060018F2 RID: 6386 RVA: 0x000519FF File Offset: 0x0004FBFF
		internal int InternalGetPosition()
		{
			if (!this._isOpen)
			{
				__Error.StreamIsClosed();
			}
			return this._position;
		}

		// Token: 0x060018F3 RID: 6387 RVA: 0x00051A14 File Offset: 0x0004FC14
		internal int InternalReadInt32()
		{
			if (!this._isOpen)
			{
				__Error.StreamIsClosed();
			}
			int num = (this._position += 4);
			if (num > this._length)
			{
				this._position = this._length;
				__Error.EndOfFile();
			}
			return (int)this._buffer[num - 4] | ((int)this._buffer[num - 3] << 8) | ((int)this._buffer[num - 2] << 16) | ((int)this._buffer[num - 1] << 24);
		}

		// Token: 0x060018F4 RID: 6388 RVA: 0x00051A90 File Offset: 0x0004FC90
		internal int InternalEmulateRead(int count)
		{
			if (!this._isOpen)
			{
				__Error.StreamIsClosed();
			}
			int num = this._length - this._position;
			if (num > count)
			{
				num = count;
			}
			if (num < 0)
			{
				num = 0;
			}
			this._position += num;
			return num;
		}

		// Token: 0x170002C3 RID: 707
		// (get) Token: 0x060018F5 RID: 6389 RVA: 0x00051AD3 File Offset: 0x0004FCD3
		// (set) Token: 0x060018F6 RID: 6390 RVA: 0x00051AF0 File Offset: 0x0004FCF0
		[__DynamicallyInvokable]
		public virtual int Capacity
		{
			[__DynamicallyInvokable]
			get
			{
				if (!this._isOpen)
				{
					__Error.StreamIsClosed();
				}
				return this._capacity - this._origin;
			}
			[__DynamicallyInvokable]
			set
			{
				if ((long)value < this.Length)
				{
					throw new ArgumentOutOfRangeException("value", Environment.GetResourceString("ArgumentOutOfRange_SmallCapacity"));
				}
				if (!this._isOpen)
				{
					__Error.StreamIsClosed();
				}
				if (!this._expandable && value != this.Capacity)
				{
					__Error.MemoryStreamNotExpandable();
				}
				if (this._expandable && value != this._capacity)
				{
					if (value > 0)
					{
						byte[] array = new byte[value];
						if (this._length > 0)
						{
							Buffer.InternalBlockCopy(this._buffer, 0, array, 0, this._length);
						}
						this._buffer = array;
					}
					else
					{
						this._buffer = null;
					}
					this._capacity = value;
				}
			}
		}

		// Token: 0x170002C4 RID: 708
		// (get) Token: 0x060018F7 RID: 6391 RVA: 0x00051B8F File Offset: 0x0004FD8F
		[__DynamicallyInvokable]
		public override long Length
		{
			[__DynamicallyInvokable]
			get
			{
				if (!this._isOpen)
				{
					__Error.StreamIsClosed();
				}
				return (long)(this._length - this._origin);
			}
		}

		// Token: 0x170002C5 RID: 709
		// (get) Token: 0x060018F8 RID: 6392 RVA: 0x00051BAC File Offset: 0x0004FDAC
		// (set) Token: 0x060018F9 RID: 6393 RVA: 0x00051BCC File Offset: 0x0004FDCC
		[__DynamicallyInvokable]
		public override long Position
		{
			[__DynamicallyInvokable]
			get
			{
				if (!this._isOpen)
				{
					__Error.StreamIsClosed();
				}
				return (long)(this._position - this._origin);
			}
			[__DynamicallyInvokable]
			set
			{
				if (value < 0L)
				{
					throw new ArgumentOutOfRangeException("value", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
				}
				if (!this._isOpen)
				{
					__Error.StreamIsClosed();
				}
				if (value > 2147483647L)
				{
					throw new ArgumentOutOfRangeException("value", Environment.GetResourceString("ArgumentOutOfRange_StreamLength"));
				}
				this._position = this._origin + (int)value;
			}
		}

		// Token: 0x060018FA RID: 6394 RVA: 0x00051C30 File Offset: 0x0004FE30
		[__DynamicallyInvokable]
		public override int Read([In] [Out] byte[] buffer, int offset, int count)
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
			int num = this._length - this._position;
			if (num > count)
			{
				num = count;
			}
			if (num <= 0)
			{
				return 0;
			}
			if (num <= 8)
			{
				int num2 = num;
				while (--num2 >= 0)
				{
					buffer[offset + num2] = this._buffer[this._position + num2];
				}
			}
			else
			{
				Buffer.InternalBlockCopy(this._buffer, this._position, buffer, offset, num);
			}
			this._position += num;
			return num;
		}

		// Token: 0x060018FB RID: 6395 RVA: 0x00051D10 File Offset: 0x0004FF10
		[ComVisible(false)]
		[__DynamicallyInvokable]
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
			catch (OperationCanceledException exception)
			{
				task = Task.FromCancellation<int>(exception);
			}
			catch (Exception exception2)
			{
				task = Task.FromException<int>(exception2);
			}
			return task;
		}

		// Token: 0x060018FC RID: 6396 RVA: 0x00051DF8 File Offset: 0x0004FFF8
		[__DynamicallyInvokable]
		public override int ReadByte()
		{
			if (!this._isOpen)
			{
				__Error.StreamIsClosed();
			}
			if (this._position >= this._length)
			{
				return -1;
			}
			byte[] buffer = this._buffer;
			int position = this._position;
			this._position = position + 1;
			return buffer[position];
		}

		// Token: 0x060018FD RID: 6397 RVA: 0x00051E3C File Offset: 0x0005003C
		[__DynamicallyInvokable]
		public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
		{
			if (destination == null)
			{
				throw new ArgumentNullException("destination");
			}
			if (bufferSize <= 0)
			{
				throw new ArgumentOutOfRangeException("bufferSize", Environment.GetResourceString("ArgumentOutOfRange_NeedPosNum"));
			}
			if (!this.CanRead && !this.CanWrite)
			{
				throw new ObjectDisposedException(null, Environment.GetResourceString("ObjectDisposed_StreamClosed"));
			}
			if (!destination.CanRead && !destination.CanWrite)
			{
				throw new ObjectDisposedException("destination", Environment.GetResourceString("ObjectDisposed_StreamClosed"));
			}
			if (!this.CanRead)
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_UnreadableStream"));
			}
			if (!destination.CanWrite)
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_UnwritableStream"));
			}
			if (base.GetType() != typeof(MemoryStream))
			{
				return base.CopyToAsync(destination, bufferSize, cancellationToken);
			}
			if (cancellationToken.IsCancellationRequested)
			{
				return Task.FromCancellation(cancellationToken);
			}
			int position = this._position;
			int count = this.InternalEmulateRead(this._length - this._position);
			MemoryStream memoryStream = destination as MemoryStream;
			if (memoryStream == null)
			{
				return destination.WriteAsync(this._buffer, position, count, cancellationToken);
			}
			Task result;
			try
			{
				memoryStream.Write(this._buffer, position, count);
				result = Task.CompletedTask;
			}
			catch (Exception exception)
			{
				result = Task.FromException(exception);
			}
			return result;
		}

		// Token: 0x060018FE RID: 6398 RVA: 0x00051F80 File Offset: 0x00050180
		[__DynamicallyInvokable]
		public override long Seek(long offset, SeekOrigin loc)
		{
			if (!this._isOpen)
			{
				__Error.StreamIsClosed();
			}
			if (offset > 2147483647L)
			{
				throw new ArgumentOutOfRangeException("offset", Environment.GetResourceString("ArgumentOutOfRange_StreamLength"));
			}
			switch (loc)
			{
			case SeekOrigin.Begin:
			{
				int num = this._origin + (int)offset;
				if (offset < 0L || num < this._origin)
				{
					throw new IOException(Environment.GetResourceString("IO.IO_SeekBeforeBegin"));
				}
				this._position = num;
				break;
			}
			case SeekOrigin.Current:
			{
				int num2 = this._position + (int)offset;
				if ((long)this._position + offset < (long)this._origin || num2 < this._origin)
				{
					throw new IOException(Environment.GetResourceString("IO.IO_SeekBeforeBegin"));
				}
				this._position = num2;
				break;
			}
			case SeekOrigin.End:
			{
				int num3 = this._length + (int)offset;
				if ((long)this._length + offset < (long)this._origin || num3 < this._origin)
				{
					throw new IOException(Environment.GetResourceString("IO.IO_SeekBeforeBegin"));
				}
				this._position = num3;
				break;
			}
			default:
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidSeekOrigin"));
			}
			return (long)this._position;
		}

		// Token: 0x060018FF RID: 6399 RVA: 0x00052098 File Offset: 0x00050298
		[__DynamicallyInvokable]
		public override void SetLength(long value)
		{
			if (value < 0L || value > 2147483647L)
			{
				throw new ArgumentOutOfRangeException("value", Environment.GetResourceString("ArgumentOutOfRange_StreamLength"));
			}
			this.EnsureWriteable();
			if (value > (long)(2147483647 - this._origin))
			{
				throw new ArgumentOutOfRangeException("value", Environment.GetResourceString("ArgumentOutOfRange_StreamLength"));
			}
			int num = this._origin + (int)value;
			if (!this.EnsureCapacity(num) && num > this._length)
			{
				Array.Clear(this._buffer, this._length, num - this._length);
			}
			this._length = num;
			if (this._position > num)
			{
				this._position = num;
			}
		}

		// Token: 0x06001900 RID: 6400 RVA: 0x00052144 File Offset: 0x00050344
		[__DynamicallyInvokable]
		public virtual byte[] ToArray()
		{
			byte[] array = new byte[this._length - this._origin];
			Buffer.InternalBlockCopy(this._buffer, this._origin, array, 0, this._length - this._origin);
			return array;
		}

		// Token: 0x06001901 RID: 6401 RVA: 0x00052188 File Offset: 0x00050388
		[__DynamicallyInvokable]
		public override void Write(byte[] buffer, int offset, int count)
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
			this.EnsureWriteable();
			int num = this._position + count;
			if (num < 0)
			{
				throw new IOException(Environment.GetResourceString("IO.IO_StreamTooLong"));
			}
			if (num > this._length)
			{
				bool flag = this._position > this._length;
				if (num > this._capacity)
				{
					bool flag2 = this.EnsureCapacity(num);
					if (flag2)
					{
						flag = false;
					}
				}
				if (flag)
				{
					Array.Clear(this._buffer, this._length, num - this._length);
				}
				this._length = num;
			}
			if (count <= 8 && buffer != this._buffer)
			{
				int num2 = count;
				while (--num2 >= 0)
				{
					this._buffer[this._position + num2] = buffer[offset + num2];
				}
			}
			else
			{
				Buffer.InternalBlockCopy(buffer, offset, this._buffer, this._position, count);
			}
			this._position = num;
		}

		// Token: 0x06001902 RID: 6402 RVA: 0x000522C4 File Offset: 0x000504C4
		[ComVisible(false)]
		[__DynamicallyInvokable]
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
			catch (OperationCanceledException exception)
			{
				result = Task.FromCancellation<VoidTaskResult>(exception);
			}
			catch (Exception exception2)
			{
				result = Task.FromException(exception2);
			}
			return result;
		}

		// Token: 0x06001903 RID: 6403 RVA: 0x00052388 File Offset: 0x00050588
		[__DynamicallyInvokable]
		public override void WriteByte(byte value)
		{
			if (!this._isOpen)
			{
				__Error.StreamIsClosed();
			}
			this.EnsureWriteable();
			if (this._position >= this._length)
			{
				int num = this._position + 1;
				bool flag = this._position > this._length;
				if (num >= this._capacity)
				{
					bool flag2 = this.EnsureCapacity(num);
					if (flag2)
					{
						flag = false;
					}
				}
				if (flag)
				{
					Array.Clear(this._buffer, this._length, this._position - this._length);
				}
				this._length = num;
			}
			byte[] buffer = this._buffer;
			int position = this._position;
			this._position = position + 1;
			buffer[position] = value;
		}

		// Token: 0x06001904 RID: 6404 RVA: 0x00052428 File Offset: 0x00050628
		[__DynamicallyInvokable]
		public virtual void WriteTo(Stream stream)
		{
			if (stream == null)
			{
				throw new ArgumentNullException("stream", Environment.GetResourceString("ArgumentNull_Stream"));
			}
			if (!this._isOpen)
			{
				__Error.StreamIsClosed();
			}
			stream.Write(this._buffer, this._origin, this._length - this._origin);
		}

		// Token: 0x040008B9 RID: 2233
		private byte[] _buffer;

		// Token: 0x040008BA RID: 2234
		private int _origin;

		// Token: 0x040008BB RID: 2235
		private int _position;

		// Token: 0x040008BC RID: 2236
		private int _length;

		// Token: 0x040008BD RID: 2237
		private int _capacity;

		// Token: 0x040008BE RID: 2238
		private bool _expandable;

		// Token: 0x040008BF RID: 2239
		private bool _writable;

		// Token: 0x040008C0 RID: 2240
		private bool _exposable;

		// Token: 0x040008C1 RID: 2241
		private bool _isOpen;

		// Token: 0x040008C2 RID: 2242
		[NonSerialized]
		private Task<int> _lastReadTask;

		// Token: 0x040008C3 RID: 2243
		private const int MemStreamMaxLength = 2147483647;
	}
}
