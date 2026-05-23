using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO
{
	// Token: 0x02000179 RID: 377
	[ComVisible(true)]
	public sealed class BufferedStream : Stream
	{
		// Token: 0x060016D6 RID: 5846 RVA: 0x00048A23 File Offset: 0x00046C23
		private BufferedStream()
		{
		}

		// Token: 0x060016D7 RID: 5847 RVA: 0x00048A2B File Offset: 0x00046C2B
		public BufferedStream(Stream stream)
			: this(stream, 4096)
		{
		}

		// Token: 0x060016D8 RID: 5848 RVA: 0x00048A3C File Offset: 0x00046C3C
		public BufferedStream(Stream stream, int bufferSize)
		{
			if (stream == null)
			{
				throw new ArgumentNullException("stream");
			}
			if (bufferSize <= 0)
			{
				throw new ArgumentOutOfRangeException("bufferSize", Environment.GetResourceString("ArgumentOutOfRange_MustBePositive", new object[] { "bufferSize" }));
			}
			this._stream = stream;
			this._bufferSize = bufferSize;
			if (!this._stream.CanRead && !this._stream.CanWrite)
			{
				__Error.StreamIsClosed();
			}
		}

		// Token: 0x060016D9 RID: 5849 RVA: 0x00048AB1 File Offset: 0x00046CB1
		private void EnsureNotClosed()
		{
			if (this._stream == null)
			{
				__Error.StreamIsClosed();
			}
		}

		// Token: 0x060016DA RID: 5850 RVA: 0x00048AC0 File Offset: 0x00046CC0
		private void EnsureCanSeek()
		{
			if (!this._stream.CanSeek)
			{
				__Error.SeekNotSupported();
			}
		}

		// Token: 0x060016DB RID: 5851 RVA: 0x00048AD4 File Offset: 0x00046CD4
		private void EnsureCanRead()
		{
			if (!this._stream.CanRead)
			{
				__Error.ReadNotSupported();
			}
		}

		// Token: 0x060016DC RID: 5852 RVA: 0x00048AE8 File Offset: 0x00046CE8
		private void EnsureCanWrite()
		{
			if (!this._stream.CanWrite)
			{
				__Error.WriteNotSupported();
			}
		}

		// Token: 0x060016DD RID: 5853 RVA: 0x00048AFC File Offset: 0x00046CFC
		private void EnsureBeginEndAwaitableAllocated()
		{
			if (this._beginEndAwaitable == null)
			{
				this._beginEndAwaitable = new BeginEndAwaitableAdapter();
			}
		}

		// Token: 0x060016DE RID: 5854 RVA: 0x00048B14 File Offset: 0x00046D14
		private void EnsureShadowBufferAllocated()
		{
			if (this._buffer.Length != this._bufferSize || this._bufferSize >= 81920)
			{
				return;
			}
			byte[] array = new byte[Math.Min(this._bufferSize + this._bufferSize, 81920)];
			Buffer.InternalBlockCopy(this._buffer, 0, array, 0, this._writePos);
			this._buffer = array;
		}

		// Token: 0x060016DF RID: 5855 RVA: 0x00048B77 File Offset: 0x00046D77
		private void EnsureBufferAllocated()
		{
			if (this._buffer == null)
			{
				this._buffer = new byte[this._bufferSize];
			}
		}

		// Token: 0x1700027A RID: 634
		// (get) Token: 0x060016E0 RID: 5856 RVA: 0x00048B92 File Offset: 0x00046D92
		internal Stream UnderlyingStream
		{
			[FriendAccessAllowed]
			get
			{
				return this._stream;
			}
		}

		// Token: 0x1700027B RID: 635
		// (get) Token: 0x060016E1 RID: 5857 RVA: 0x00048B9A File Offset: 0x00046D9A
		internal int BufferSize
		{
			[FriendAccessAllowed]
			get
			{
				return this._bufferSize;
			}
		}

		// Token: 0x1700027C RID: 636
		// (get) Token: 0x060016E2 RID: 5858 RVA: 0x00048BA2 File Offset: 0x00046DA2
		public override bool CanRead
		{
			get
			{
				return this._stream != null && this._stream.CanRead;
			}
		}

		// Token: 0x1700027D RID: 637
		// (get) Token: 0x060016E3 RID: 5859 RVA: 0x00048BB9 File Offset: 0x00046DB9
		public override bool CanWrite
		{
			get
			{
				return this._stream != null && this._stream.CanWrite;
			}
		}

		// Token: 0x1700027E RID: 638
		// (get) Token: 0x060016E4 RID: 5860 RVA: 0x00048BD0 File Offset: 0x00046DD0
		public override bool CanSeek
		{
			get
			{
				return this._stream != null && this._stream.CanSeek;
			}
		}

		// Token: 0x1700027F RID: 639
		// (get) Token: 0x060016E5 RID: 5861 RVA: 0x00048BE7 File Offset: 0x00046DE7
		public override long Length
		{
			get
			{
				this.EnsureNotClosed();
				if (this._writePos > 0)
				{
					this.FlushWrite();
				}
				return this._stream.Length;
			}
		}

		// Token: 0x17000280 RID: 640
		// (get) Token: 0x060016E6 RID: 5862 RVA: 0x00048C09 File Offset: 0x00046E09
		// (set) Token: 0x060016E7 RID: 5863 RVA: 0x00048C38 File Offset: 0x00046E38
		public override long Position
		{
			get
			{
				this.EnsureNotClosed();
				this.EnsureCanSeek();
				return this._stream.Position + (long)(this._readPos - this._readLen + this._writePos);
			}
			set
			{
				if (value < 0L)
				{
					throw new ArgumentOutOfRangeException("value", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
				}
				this.EnsureNotClosed();
				this.EnsureCanSeek();
				if (this._writePos > 0)
				{
					this.FlushWrite();
				}
				this._readPos = 0;
				this._readLen = 0;
				this._stream.Seek(value, SeekOrigin.Begin);
			}
		}

		// Token: 0x060016E8 RID: 5864 RVA: 0x00048C98 File Offset: 0x00046E98
		protected override void Dispose(bool disposing)
		{
			try
			{
				if (disposing && this._stream != null)
				{
					try
					{
						this.Flush();
					}
					finally
					{
						this._stream.Close();
					}
				}
			}
			finally
			{
				this._stream = null;
				this._buffer = null;
				this._lastSyncCompletedReadTask = null;
				base.Dispose(disposing);
			}
		}

		// Token: 0x060016E9 RID: 5865 RVA: 0x00048D00 File Offset: 0x00046F00
		public override void Flush()
		{
			this.EnsureNotClosed();
			if (this._writePos > 0)
			{
				this.FlushWrite();
				return;
			}
			if (this._readPos >= this._readLen)
			{
				if (this._stream.CanWrite || this._stream is BufferedStream)
				{
					this._stream.Flush();
				}
				this._writePos = (this._readPos = (this._readLen = 0));
				return;
			}
			if (!this._stream.CanSeek)
			{
				return;
			}
			this.FlushRead();
			if (this._stream.CanWrite || this._stream is BufferedStream)
			{
				this._stream.Flush();
			}
		}

		// Token: 0x060016EA RID: 5866 RVA: 0x00048DA9 File Offset: 0x00046FA9
		public override Task FlushAsync(CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return Task.FromCancellation<int>(cancellationToken);
			}
			this.EnsureNotClosed();
			return BufferedStream.FlushAsyncInternal(cancellationToken, this, this._stream, this._writePos, this._readPos, this._readLen);
		}

		// Token: 0x060016EB RID: 5867 RVA: 0x00048DE0 File Offset: 0x00046FE0
		private static Task FlushAsyncInternal(CancellationToken cancellationToken, BufferedStream _this, Stream stream, int writePos, int readPos, int readLen)
		{
			BufferedStream.<FlushAsyncInternal>d__38 <FlushAsyncInternal>d__;
			<FlushAsyncInternal>d__.<>t__builder = AsyncTaskMethodBuilder.Create();
			<FlushAsyncInternal>d__.cancellationToken = cancellationToken;
			<FlushAsyncInternal>d__._this = _this;
			<FlushAsyncInternal>d__.stream = stream;
			<FlushAsyncInternal>d__.writePos = writePos;
			<FlushAsyncInternal>d__.readPos = readPos;
			<FlushAsyncInternal>d__.readLen = readLen;
			<FlushAsyncInternal>d__.<>1__state = -1;
			<FlushAsyncInternal>d__.<>t__builder.Start<BufferedStream.<FlushAsyncInternal>d__38>(ref <FlushAsyncInternal>d__);
			return <FlushAsyncInternal>d__.<>t__builder.Task;
		}

		// Token: 0x060016EC RID: 5868 RVA: 0x00048E4D File Offset: 0x0004704D
		private void FlushRead()
		{
			if (this._readPos - this._readLen != 0)
			{
				this._stream.Seek((long)(this._readPos - this._readLen), SeekOrigin.Current);
			}
			this._readPos = 0;
			this._readLen = 0;
		}

		// Token: 0x060016ED RID: 5869 RVA: 0x00048E88 File Offset: 0x00047088
		private void ClearReadBufferBeforeWrite()
		{
			if (this._readPos == this._readLen)
			{
				this._readPos = (this._readLen = 0);
				return;
			}
			if (!this._stream.CanSeek)
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_CannotWriteToBufferedStreamIfReadBufferCannotBeFlushed"));
			}
			this.FlushRead();
		}

		// Token: 0x060016EE RID: 5870 RVA: 0x00048ED7 File Offset: 0x000470D7
		private void FlushWrite()
		{
			this._stream.Write(this._buffer, 0, this._writePos);
			this._writePos = 0;
			this._stream.Flush();
		}

		// Token: 0x060016EF RID: 5871 RVA: 0x00048F04 File Offset: 0x00047104
		private async Task FlushWriteAsync(CancellationToken cancellationToken)
		{
			await this._stream.WriteAsync(this._buffer, 0, this._writePos, cancellationToken).ConfigureAwait(false);
			this._writePos = 0;
			await this._stream.FlushAsync(cancellationToken).ConfigureAwait(false);
		}

		// Token: 0x060016F0 RID: 5872 RVA: 0x00048F50 File Offset: 0x00047150
		private int ReadFromBuffer(byte[] array, int offset, int count)
		{
			int num = this._readLen - this._readPos;
			if (num == 0)
			{
				return 0;
			}
			if (num > count)
			{
				num = count;
			}
			Buffer.InternalBlockCopy(this._buffer, this._readPos, array, offset, num);
			this._readPos += num;
			return num;
		}

		// Token: 0x060016F1 RID: 5873 RVA: 0x00048F9C File Offset: 0x0004719C
		private int ReadFromBuffer(byte[] array, int offset, int count, out Exception error)
		{
			int result;
			try
			{
				error = null;
				result = this.ReadFromBuffer(array, offset, count);
			}
			catch (Exception ex)
			{
				error = ex;
				result = 0;
			}
			return result;
		}

		// Token: 0x060016F2 RID: 5874 RVA: 0x00048FD4 File Offset: 0x000471D4
		public override int Read([In] [Out] byte[] array, int offset, int count)
		{
			if (array == null)
			{
				throw new ArgumentNullException("array", Environment.GetResourceString("ArgumentNull_Buffer"));
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
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
			}
			this.EnsureNotClosed();
			this.EnsureCanRead();
			int num = this.ReadFromBuffer(array, offset, count);
			if (num == count)
			{
				return num;
			}
			int num2 = num;
			if (num > 0)
			{
				count -= num;
				offset += num;
			}
			this._readPos = (this._readLen = 0);
			if (this._writePos > 0)
			{
				this.FlushWrite();
			}
			if (count >= this._bufferSize)
			{
				return this._stream.Read(array, offset, count) + num2;
			}
			this.EnsureBufferAllocated();
			this._readLen = this._stream.Read(this._buffer, 0, this._bufferSize);
			num = this.ReadFromBuffer(array, offset, count);
			return num + num2;
		}

		// Token: 0x060016F3 RID: 5875 RVA: 0x000490DC File Offset: 0x000472DC
		public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
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
			if (this._stream == null)
			{
				__Error.ReadNotSupported();
			}
			this.EnsureCanRead();
			int num = 0;
			SemaphoreSlim semaphoreSlim = base.EnsureAsyncActiveSemaphoreInitialized();
			Task task = semaphoreSlim.WaitAsync();
			if (task.Status == TaskStatus.RanToCompletion)
			{
				bool flag = true;
				try
				{
					Exception ex;
					num = this.ReadFromBuffer(buffer, offset, count, out ex);
					flag = num == count || ex != null;
					if (flag)
					{
						Stream.SynchronousAsyncResult synchronousAsyncResult = ((ex == null) ? new Stream.SynchronousAsyncResult(num, state) : new Stream.SynchronousAsyncResult(ex, state, false));
						if (callback != null)
						{
							callback(synchronousAsyncResult);
						}
						return synchronousAsyncResult;
					}
				}
				finally
				{
					if (flag)
					{
						semaphoreSlim.Release();
					}
				}
			}
			return this.BeginReadFromUnderlyingStream(buffer, offset + num, count - num, callback, state, num, task);
		}

		// Token: 0x060016F4 RID: 5876 RVA: 0x000491F8 File Offset: 0x000473F8
		private IAsyncResult BeginReadFromUnderlyingStream(byte[] buffer, int offset, int count, AsyncCallback callback, object state, int bytesAlreadySatisfied, Task semaphoreLockTask)
		{
			Task<int> task = this.ReadFromUnderlyingStreamAsync(buffer, offset, count, CancellationToken.None, bytesAlreadySatisfied, semaphoreLockTask, true);
			return TaskToApm.Begin(task, callback, state);
		}

		// Token: 0x060016F5 RID: 5877 RVA: 0x00049224 File Offset: 0x00047424
		public override int EndRead(IAsyncResult asyncResult)
		{
			if (asyncResult == null)
			{
				throw new ArgumentNullException("asyncResult");
			}
			Stream.SynchronousAsyncResult synchronousAsyncResult = asyncResult as Stream.SynchronousAsyncResult;
			if (synchronousAsyncResult != null)
			{
				return Stream.SynchronousAsyncResult.EndRead(asyncResult);
			}
			return TaskToApm.End<int>(asyncResult);
		}

		// Token: 0x060016F6 RID: 5878 RVA: 0x00049258 File Offset: 0x00047458
		private Task<int> LastSyncCompletedReadTask(int val)
		{
			Task<int> task = this._lastSyncCompletedReadTask;
			if (task != null && task.Result == val)
			{
				return task;
			}
			task = Task.FromResult<int>(val);
			this._lastSyncCompletedReadTask = task;
			return task;
		}

		// Token: 0x060016F7 RID: 5879 RVA: 0x0004928C File Offset: 0x0004748C
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
			this.EnsureNotClosed();
			this.EnsureCanRead();
			int num = 0;
			SemaphoreSlim semaphoreSlim = base.EnsureAsyncActiveSemaphoreInitialized();
			Task task = semaphoreSlim.WaitAsync();
			if (task.Status == TaskStatus.RanToCompletion)
			{
				bool flag = true;
				try
				{
					Exception ex;
					num = this.ReadFromBuffer(buffer, offset, count, out ex);
					flag = num == count || ex != null;
					if (flag)
					{
						return (ex == null) ? this.LastSyncCompletedReadTask(num) : Task.FromException<int>(ex);
					}
				}
				finally
				{
					if (flag)
					{
						semaphoreSlim.Release();
					}
				}
			}
			return this.ReadFromUnderlyingStreamAsync(buffer, offset + num, count - num, cancellationToken, num, task, false);
		}

		// Token: 0x060016F8 RID: 5880 RVA: 0x0004939C File Offset: 0x0004759C
		private async Task<int> ReadFromUnderlyingStreamAsync(byte[] array, int offset, int count, CancellationToken cancellationToken, int bytesAlreadySatisfied, Task semaphoreLockTask, bool useApmPattern)
		{
			await semaphoreLockTask.ConfigureAwait(false);
			int result;
			try
			{
				int num = this.ReadFromBuffer(array, offset, count);
				if (num == count)
				{
					result = bytesAlreadySatisfied + num;
				}
				else
				{
					if (num > 0)
					{
						count -= num;
						offset += num;
						bytesAlreadySatisfied += num;
					}
					int num2 = 0;
					this._readLen = num2;
					this._readPos = num2;
					if (this._writePos > 0)
					{
						await this.FlushWriteAsync(cancellationToken).ConfigureAwait(false);
					}
					if (count >= this._bufferSize)
					{
						if (useApmPattern)
						{
							this.EnsureBeginEndAwaitableAllocated();
							this._stream.BeginRead(array, offset, count, BeginEndAwaitableAdapter.Callback, this._beginEndAwaitable);
							int num3 = bytesAlreadySatisfied;
							Stream stream = this._stream;
							result = num3 + stream.EndRead(await this._beginEndAwaitable);
						}
						else
						{
							int num3 = bytesAlreadySatisfied;
							result = num3 + await this._stream.ReadAsync(array, offset, count, cancellationToken).ConfigureAwait(false);
						}
					}
					else
					{
						this.EnsureBufferAllocated();
						if (useApmPattern)
						{
							this.EnsureBeginEndAwaitableAllocated();
							this._stream.BeginRead(this._buffer, 0, this._bufferSize, BeginEndAwaitableAdapter.Callback, this._beginEndAwaitable);
							Stream stream = this._stream;
							this._readLen = stream.EndRead(await this._beginEndAwaitable);
							stream = null;
						}
						else
						{
							this._readLen = await this._stream.ReadAsync(this._buffer, 0, this._bufferSize, cancellationToken).ConfigureAwait(false);
						}
						result = bytesAlreadySatisfied + this.ReadFromBuffer(array, offset, count);
					}
				}
			}
			finally
			{
				base.EnsureAsyncActiveSemaphoreInitialized().Release();
			}
			return result;
		}

		// Token: 0x060016F9 RID: 5881 RVA: 0x0004941C File Offset: 0x0004761C
		public override int ReadByte()
		{
			this.EnsureNotClosed();
			this.EnsureCanRead();
			if (this._readPos == this._readLen)
			{
				if (this._writePos > 0)
				{
					this.FlushWrite();
				}
				this.EnsureBufferAllocated();
				this._readLen = this._stream.Read(this._buffer, 0, this._bufferSize);
				this._readPos = 0;
			}
			if (this._readPos == this._readLen)
			{
				return -1;
			}
			byte[] buffer = this._buffer;
			int readPos = this._readPos;
			this._readPos = readPos + 1;
			return buffer[readPos];
		}

		// Token: 0x060016FA RID: 5882 RVA: 0x000494A8 File Offset: 0x000476A8
		private void WriteToBuffer(byte[] array, ref int offset, ref int count)
		{
			int num = Math.Min(this._bufferSize - this._writePos, count);
			if (num <= 0)
			{
				return;
			}
			this.EnsureBufferAllocated();
			Buffer.InternalBlockCopy(array, offset, this._buffer, this._writePos, num);
			this._writePos += num;
			count -= num;
			offset += num;
		}

		// Token: 0x060016FB RID: 5883 RVA: 0x00049504 File Offset: 0x00047704
		private void WriteToBuffer(byte[] array, ref int offset, ref int count, out Exception error)
		{
			try
			{
				error = null;
				this.WriteToBuffer(array, ref offset, ref count);
			}
			catch (Exception ex)
			{
				error = ex;
			}
		}

		// Token: 0x060016FC RID: 5884 RVA: 0x00049538 File Offset: 0x00047738
		public override void Write(byte[] array, int offset, int count)
		{
			if (array == null)
			{
				throw new ArgumentNullException("array", Environment.GetResourceString("ArgumentNull_Buffer"));
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
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
			}
			this.EnsureNotClosed();
			this.EnsureCanWrite();
			if (this._writePos == 0)
			{
				this.ClearReadBufferBeforeWrite();
			}
			int num;
			bool flag;
			checked
			{
				num = this._writePos + count;
				flag = num + count < this._bufferSize + this._bufferSize;
			}
			if (!flag)
			{
				if (this._writePos > 0)
				{
					if (num <= this._bufferSize + this._bufferSize && num <= 81920)
					{
						this.EnsureShadowBufferAllocated();
						Buffer.InternalBlockCopy(array, offset, this._buffer, this._writePos, count);
						this._stream.Write(this._buffer, 0, num);
						this._writePos = 0;
						return;
					}
					this._stream.Write(this._buffer, 0, this._writePos);
					this._writePos = 0;
				}
				this._stream.Write(array, offset, count);
				return;
			}
			this.WriteToBuffer(array, ref offset, ref count);
			if (this._writePos < this._bufferSize)
			{
				return;
			}
			this._stream.Write(this._buffer, 0, this._writePos);
			this._writePos = 0;
			this.WriteToBuffer(array, ref offset, ref count);
		}

		// Token: 0x060016FD RID: 5885 RVA: 0x000496A8 File Offset: 0x000478A8
		public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
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
			if (this._stream == null)
			{
				__Error.ReadNotSupported();
			}
			this.EnsureCanWrite();
			SemaphoreSlim semaphoreSlim = base.EnsureAsyncActiveSemaphoreInitialized();
			Task task = semaphoreSlim.WaitAsync();
			if (task.Status == TaskStatus.RanToCompletion)
			{
				bool flag = true;
				try
				{
					if (this._writePos == 0)
					{
						this.ClearReadBufferBeforeWrite();
					}
					flag = count < this._bufferSize - this._writePos;
					if (flag)
					{
						Exception ex;
						this.WriteToBuffer(buffer, ref offset, ref count, out ex);
						Stream.SynchronousAsyncResult synchronousAsyncResult = ((ex == null) ? new Stream.SynchronousAsyncResult(state) : new Stream.SynchronousAsyncResult(ex, state, true));
						if (callback != null)
						{
							callback(synchronousAsyncResult);
						}
						return synchronousAsyncResult;
					}
				}
				finally
				{
					if (flag)
					{
						semaphoreSlim.Release();
					}
				}
			}
			return this.BeginWriteToUnderlyingStream(buffer, offset, count, callback, state, task);
		}

		// Token: 0x060016FE RID: 5886 RVA: 0x000497CC File Offset: 0x000479CC
		private IAsyncResult BeginWriteToUnderlyingStream(byte[] buffer, int offset, int count, AsyncCallback callback, object state, Task semaphoreLockTask)
		{
			Task task = this.WriteToUnderlyingStreamAsync(buffer, offset, count, CancellationToken.None, semaphoreLockTask, true);
			return TaskToApm.Begin(task, callback, state);
		}

		// Token: 0x060016FF RID: 5887 RVA: 0x000497F8 File Offset: 0x000479F8
		public override void EndWrite(IAsyncResult asyncResult)
		{
			if (asyncResult == null)
			{
				throw new ArgumentNullException("asyncResult");
			}
			Stream.SynchronousAsyncResult synchronousAsyncResult = asyncResult as Stream.SynchronousAsyncResult;
			if (synchronousAsyncResult != null)
			{
				Stream.SynchronousAsyncResult.EndWrite(asyncResult);
				return;
			}
			TaskToApm.End(asyncResult);
		}

		// Token: 0x06001700 RID: 5888 RVA: 0x0004982C File Offset: 0x00047A2C
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
				return Task.FromCancellation<int>(cancellationToken);
			}
			this.EnsureNotClosed();
			this.EnsureCanWrite();
			SemaphoreSlim semaphoreSlim = base.EnsureAsyncActiveSemaphoreInitialized();
			Task task = semaphoreSlim.WaitAsync();
			if (task.Status == TaskStatus.RanToCompletion)
			{
				bool flag = true;
				try
				{
					if (this._writePos == 0)
					{
						this.ClearReadBufferBeforeWrite();
					}
					flag = count < this._bufferSize - this._writePos;
					if (flag)
					{
						Exception ex;
						this.WriteToBuffer(buffer, ref offset, ref count, out ex);
						return (ex == null) ? Task.CompletedTask : Task.FromException(ex);
					}
				}
				finally
				{
					if (flag)
					{
						semaphoreSlim.Release();
					}
				}
			}
			return this.WriteToUnderlyingStreamAsync(buffer, offset, count, cancellationToken, task, false);
		}

		// Token: 0x06001701 RID: 5889 RVA: 0x00049944 File Offset: 0x00047B44
		private async Task WriteToUnderlyingStreamAsync(byte[] array, int offset, int count, CancellationToken cancellationToken, Task semaphoreLockTask, bool useApmPattern)
		{
			await semaphoreLockTask.ConfigureAwait(false);
			try
			{
				if (this._writePos == 0)
				{
					this.ClearReadBufferBeforeWrite();
				}
				int num = checked(this._writePos + count);
				if (checked(num + count < this._bufferSize + this._bufferSize))
				{
					this.WriteToBuffer(array, ref offset, ref count);
					if (this._writePos >= this._bufferSize)
					{
						if (useApmPattern)
						{
							this.EnsureBeginEndAwaitableAllocated();
							this._stream.BeginWrite(this._buffer, 0, this._writePos, BeginEndAwaitableAdapter.Callback, this._beginEndAwaitable);
							Stream stream = this._stream;
							stream.EndWrite(await this._beginEndAwaitable);
							stream = null;
						}
						else
						{
							await this._stream.WriteAsync(this._buffer, 0, this._writePos, cancellationToken).ConfigureAwait(false);
						}
						this._writePos = 0;
						this.WriteToBuffer(array, ref offset, ref count);
					}
				}
				else
				{
					if (this._writePos > 0)
					{
						if (num <= this._bufferSize + this._bufferSize && num <= 81920)
						{
							this.EnsureShadowBufferAllocated();
							Buffer.InternalBlockCopy(array, offset, this._buffer, this._writePos, count);
							if (useApmPattern)
							{
								this.EnsureBeginEndAwaitableAllocated();
								this._stream.BeginWrite(this._buffer, 0, num, BeginEndAwaitableAdapter.Callback, this._beginEndAwaitable);
								Stream stream = this._stream;
								stream.EndWrite(await this._beginEndAwaitable);
								stream = null;
							}
							else
							{
								await this._stream.WriteAsync(this._buffer, 0, num, cancellationToken).ConfigureAwait(false);
							}
							this._writePos = 0;
							return;
						}
						if (useApmPattern)
						{
							this.EnsureBeginEndAwaitableAllocated();
							this._stream.BeginWrite(this._buffer, 0, this._writePos, BeginEndAwaitableAdapter.Callback, this._beginEndAwaitable);
							Stream stream = this._stream;
							stream.EndWrite(await this._beginEndAwaitable);
							stream = null;
						}
						else
						{
							await this._stream.WriteAsync(this._buffer, 0, this._writePos, cancellationToken).ConfigureAwait(false);
						}
						this._writePos = 0;
					}
					if (useApmPattern)
					{
						this.EnsureBeginEndAwaitableAllocated();
						this._stream.BeginWrite(array, offset, count, BeginEndAwaitableAdapter.Callback, this._beginEndAwaitable);
						Stream stream = this._stream;
						stream.EndWrite(await this._beginEndAwaitable);
						stream = null;
					}
					else
					{
						await this._stream.WriteAsync(array, offset, count, cancellationToken).ConfigureAwait(false);
					}
				}
			}
			finally
			{
				base.EnsureAsyncActiveSemaphoreInitialized().Release();
			}
		}

		// Token: 0x06001702 RID: 5890 RVA: 0x000499BC File Offset: 0x00047BBC
		public override void WriteByte(byte value)
		{
			this.EnsureNotClosed();
			if (this._writePos == 0)
			{
				this.EnsureCanWrite();
				this.ClearReadBufferBeforeWrite();
				this.EnsureBufferAllocated();
			}
			if (this._writePos >= this._bufferSize - 1)
			{
				this.FlushWrite();
			}
			byte[] buffer = this._buffer;
			int writePos = this._writePos;
			this._writePos = writePos + 1;
			buffer[writePos] = value;
		}

		// Token: 0x06001703 RID: 5891 RVA: 0x00049A18 File Offset: 0x00047C18
		public override long Seek(long offset, SeekOrigin origin)
		{
			this.EnsureNotClosed();
			this.EnsureCanSeek();
			if (this._writePos > 0)
			{
				this.FlushWrite();
				return this._stream.Seek(offset, origin);
			}
			if (this._readLen - this._readPos > 0 && origin == SeekOrigin.Current)
			{
				offset -= (long)(this._readLen - this._readPos);
			}
			long position = this.Position;
			long num = this._stream.Seek(offset, origin);
			this._readPos = (int)(num - (position - (long)this._readPos));
			if (0 <= this._readPos && this._readPos < this._readLen)
			{
				this._stream.Seek((long)(this._readLen - this._readPos), SeekOrigin.Current);
			}
			else
			{
				this._readPos = (this._readLen = 0);
			}
			return num;
		}

		// Token: 0x06001704 RID: 5892 RVA: 0x00049AE0 File Offset: 0x00047CE0
		public override void SetLength(long value)
		{
			if (value < 0L)
			{
				throw new ArgumentOutOfRangeException("value", Environment.GetResourceString("ArgumentOutOfRange_NegFileSize"));
			}
			this.EnsureNotClosed();
			this.EnsureCanSeek();
			this.EnsureCanWrite();
			this.Flush();
			this._stream.SetLength(value);
		}

		// Token: 0x0400080F RID: 2063
		private const int _DefaultBufferSize = 4096;

		// Token: 0x04000810 RID: 2064
		private Stream _stream;

		// Token: 0x04000811 RID: 2065
		private byte[] _buffer;

		// Token: 0x04000812 RID: 2066
		private readonly int _bufferSize;

		// Token: 0x04000813 RID: 2067
		private int _readPos;

		// Token: 0x04000814 RID: 2068
		private int _readLen;

		// Token: 0x04000815 RID: 2069
		private int _writePos;

		// Token: 0x04000816 RID: 2070
		private BeginEndAwaitableAdapter _beginEndAwaitable;

		// Token: 0x04000817 RID: 2071
		private Task<int> _lastSyncCompletedReadTask;

		// Token: 0x04000818 RID: 2072
		private const int MaxShadowBufferSize = 81920;
	}
}
