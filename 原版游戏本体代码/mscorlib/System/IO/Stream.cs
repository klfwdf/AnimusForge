using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO
{
	// Token: 0x020001A1 RID: 417
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public abstract class Stream : MarshalByRefObject, IDisposable
	{
		// Token: 0x0600196D RID: 6509 RVA: 0x00054AA7 File Offset: 0x00052CA7
		internal SemaphoreSlim EnsureAsyncActiveSemaphoreInitialized()
		{
			return LazyInitializer.EnsureInitialized<SemaphoreSlim>(ref this._asyncActiveSemaphore, () => new SemaphoreSlim(1, 1));
		}

		// Token: 0x170002C9 RID: 713
		// (get) Token: 0x0600196E RID: 6510
		[__DynamicallyInvokable]
		public abstract bool CanRead
		{
			[__DynamicallyInvokable]
			get;
		}

		// Token: 0x170002CA RID: 714
		// (get) Token: 0x0600196F RID: 6511
		[__DynamicallyInvokable]
		public abstract bool CanSeek
		{
			[__DynamicallyInvokable]
			get;
		}

		// Token: 0x170002CB RID: 715
		// (get) Token: 0x06001970 RID: 6512 RVA: 0x00054AD3 File Offset: 0x00052CD3
		[ComVisible(false)]
		[__DynamicallyInvokable]
		public virtual bool CanTimeout
		{
			[__DynamicallyInvokable]
			get
			{
				return false;
			}
		}

		// Token: 0x170002CC RID: 716
		// (get) Token: 0x06001971 RID: 6513
		[__DynamicallyInvokable]
		public abstract bool CanWrite
		{
			[__DynamicallyInvokable]
			get;
		}

		// Token: 0x170002CD RID: 717
		// (get) Token: 0x06001972 RID: 6514
		[__DynamicallyInvokable]
		public abstract long Length
		{
			[__DynamicallyInvokable]
			get;
		}

		// Token: 0x170002CE RID: 718
		// (get) Token: 0x06001973 RID: 6515
		// (set) Token: 0x06001974 RID: 6516
		[__DynamicallyInvokable]
		public abstract long Position
		{
			[__DynamicallyInvokable]
			get;
			[__DynamicallyInvokable]
			set;
		}

		// Token: 0x170002CF RID: 719
		// (get) Token: 0x06001975 RID: 6517 RVA: 0x00054AD6 File Offset: 0x00052CD6
		// (set) Token: 0x06001976 RID: 6518 RVA: 0x00054AE7 File Offset: 0x00052CE7
		[ComVisible(false)]
		[__DynamicallyInvokable]
		public virtual int ReadTimeout
		{
			[__DynamicallyInvokable]
			get
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_TimeoutsNotSupported"));
			}
			[__DynamicallyInvokable]
			set
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_TimeoutsNotSupported"));
			}
		}

		// Token: 0x170002D0 RID: 720
		// (get) Token: 0x06001977 RID: 6519 RVA: 0x00054AF8 File Offset: 0x00052CF8
		// (set) Token: 0x06001978 RID: 6520 RVA: 0x00054B09 File Offset: 0x00052D09
		[ComVisible(false)]
		[__DynamicallyInvokable]
		public virtual int WriteTimeout
		{
			[__DynamicallyInvokable]
			get
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_TimeoutsNotSupported"));
			}
			[__DynamicallyInvokable]
			set
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_TimeoutsNotSupported"));
			}
		}

		// Token: 0x06001979 RID: 6521 RVA: 0x00054B1A File Offset: 0x00052D1A
		[ComVisible(false)]
		[__DynamicallyInvokable]
		[HostProtection(SecurityAction.LinkDemand, ExternalThreading = true)]
		public Task CopyToAsync(Stream destination)
		{
			return this.CopyToAsync(destination, 81920);
		}

		// Token: 0x0600197A RID: 6522 RVA: 0x00054B28 File Offset: 0x00052D28
		[ComVisible(false)]
		[__DynamicallyInvokable]
		[HostProtection(SecurityAction.LinkDemand, ExternalThreading = true)]
		public Task CopyToAsync(Stream destination, int bufferSize)
		{
			return this.CopyToAsync(destination, bufferSize, CancellationToken.None);
		}

		// Token: 0x0600197B RID: 6523 RVA: 0x00054B38 File Offset: 0x00052D38
		[ComVisible(false)]
		[__DynamicallyInvokable]
		[HostProtection(SecurityAction.LinkDemand, ExternalThreading = true)]
		public virtual Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
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
			return this.CopyToAsyncInternal(destination, bufferSize, cancellationToken);
		}

		// Token: 0x0600197C RID: 6524 RVA: 0x00054BEC File Offset: 0x00052DEC
		private Task CopyToAsyncInternal(Stream destination, int bufferSize, CancellationToken cancellationToken)
		{
			Stream.<CopyToAsyncInternal>d__27 <CopyToAsyncInternal>d__;
			<CopyToAsyncInternal>d__.<>t__builder = AsyncTaskMethodBuilder.Create();
			<CopyToAsyncInternal>d__.<>4__this = this;
			<CopyToAsyncInternal>d__.destination = destination;
			<CopyToAsyncInternal>d__.bufferSize = bufferSize;
			<CopyToAsyncInternal>d__.cancellationToken = cancellationToken;
			<CopyToAsyncInternal>d__.<>1__state = -1;
			<CopyToAsyncInternal>d__.<>t__builder.Start<Stream.<CopyToAsyncInternal>d__27>(ref <CopyToAsyncInternal>d__);
			return <CopyToAsyncInternal>d__.<>t__builder.Task;
		}

		// Token: 0x0600197D RID: 6525 RVA: 0x00054C48 File Offset: 0x00052E48
		[__DynamicallyInvokable]
		public void CopyTo(Stream destination)
		{
			if (destination == null)
			{
				throw new ArgumentNullException("destination");
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
			this.InternalCopyTo(destination, 81920);
		}

		// Token: 0x0600197E RID: 6526 RVA: 0x00054CE8 File Offset: 0x00052EE8
		[__DynamicallyInvokable]
		public void CopyTo(Stream destination, int bufferSize)
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
			this.InternalCopyTo(destination, bufferSize);
		}

		// Token: 0x0600197F RID: 6527 RVA: 0x00054D9C File Offset: 0x00052F9C
		private void InternalCopyTo(Stream destination, int bufferSize)
		{
			byte[] array = new byte[bufferSize];
			int count;
			while ((count = this.Read(array, 0, array.Length)) != 0)
			{
				destination.Write(array, 0, count);
			}
		}

		// Token: 0x06001980 RID: 6528 RVA: 0x00054DCA File Offset: 0x00052FCA
		public virtual void Close()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		// Token: 0x06001981 RID: 6529 RVA: 0x00054DD9 File Offset: 0x00052FD9
		[__DynamicallyInvokable]
		public void Dispose()
		{
			this.Close();
		}

		// Token: 0x06001982 RID: 6530 RVA: 0x00054DE1 File Offset: 0x00052FE1
		[__DynamicallyInvokable]
		protected virtual void Dispose(bool disposing)
		{
		}

		// Token: 0x06001983 RID: 6531
		[__DynamicallyInvokable]
		public abstract void Flush();

		// Token: 0x06001984 RID: 6532 RVA: 0x00054DE3 File Offset: 0x00052FE3
		[ComVisible(false)]
		[__DynamicallyInvokable]
		[HostProtection(SecurityAction.LinkDemand, ExternalThreading = true)]
		public Task FlushAsync()
		{
			return this.FlushAsync(CancellationToken.None);
		}

		// Token: 0x06001985 RID: 6533 RVA: 0x00054DF0 File Offset: 0x00052FF0
		[ComVisible(false)]
		[__DynamicallyInvokable]
		[HostProtection(SecurityAction.LinkDemand, ExternalThreading = true)]
		public virtual Task FlushAsync(CancellationToken cancellationToken)
		{
			return Task.Factory.StartNew(delegate(object state)
			{
				((Stream)state).Flush();
			}, this, cancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
		}

		// Token: 0x06001986 RID: 6534 RVA: 0x00054E23 File Offset: 0x00053023
		[Obsolete("CreateWaitHandle will be removed eventually.  Please use \"new ManualResetEvent(false)\" instead.")]
		protected virtual WaitHandle CreateWaitHandle()
		{
			return new ManualResetEvent(false);
		}

		// Token: 0x06001987 RID: 6535 RVA: 0x00054E2B File Offset: 0x0005302B
		[__DynamicallyInvokable]
		[HostProtection(SecurityAction.LinkDemand, ExternalThreading = true)]
		public virtual IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
		{
			return this.BeginReadInternal(buffer, offset, count, callback, state, false);
		}

		// Token: 0x06001988 RID: 6536 RVA: 0x00054E3C File Offset: 0x0005303C
		[HostProtection(SecurityAction.LinkDemand, ExternalThreading = true)]
		internal IAsyncResult BeginReadInternal(byte[] buffer, int offset, int count, AsyncCallback callback, object state, bool serializeAsynchronously)
		{
			if (!this.CanRead)
			{
				__Error.ReadNotSupported();
			}
			if (CompatibilitySwitches.IsAppEarlierThanWindowsPhone8)
			{
				return this.BlockingBeginRead(buffer, offset, count, callback, state);
			}
			SemaphoreSlim semaphoreSlim = this.EnsureAsyncActiveSemaphoreInitialized();
			Task task = null;
			if (serializeAsynchronously)
			{
				task = semaphoreSlim.WaitAsync();
			}
			else
			{
				semaphoreSlim.Wait();
			}
			Stream.ReadWriteTask readWriteTask = new Stream.ReadWriteTask(true, delegate(object <p0>)
			{
				Stream.ReadWriteTask readWriteTask2 = Task.InternalCurrent as Stream.ReadWriteTask;
				int result = readWriteTask2._stream.Read(readWriteTask2._buffer, readWriteTask2._offset, readWriteTask2._count);
				readWriteTask2.ClearBeginState();
				return result;
			}, state, this, buffer, offset, count, callback);
			if (task != null)
			{
				this.RunReadWriteTaskWhenReady(task, readWriteTask);
			}
			else
			{
				this.RunReadWriteTask(readWriteTask);
			}
			return readWriteTask;
		}

		// Token: 0x06001989 RID: 6537 RVA: 0x00054ECC File Offset: 0x000530CC
		[__DynamicallyInvokable]
		public virtual int EndRead(IAsyncResult asyncResult)
		{
			if (asyncResult == null)
			{
				throw new ArgumentNullException("asyncResult");
			}
			if (CompatibilitySwitches.IsAppEarlierThanWindowsPhone8)
			{
				return Stream.BlockingEndRead(asyncResult);
			}
			Stream.ReadWriteTask activeReadWriteTask = this._activeReadWriteTask;
			if (activeReadWriteTask == null)
			{
				throw new ArgumentException(Environment.GetResourceString("InvalidOperation_WrongAsyncResultOrEndReadCalledMultiple"));
			}
			if (activeReadWriteTask != asyncResult)
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_WrongAsyncResultOrEndReadCalledMultiple"));
			}
			if (!activeReadWriteTask._isRead)
			{
				throw new ArgumentException(Environment.GetResourceString("InvalidOperation_WrongAsyncResultOrEndReadCalledMultiple"));
			}
			int result;
			try
			{
				result = activeReadWriteTask.GetAwaiter().GetResult();
			}
			finally
			{
				this._activeReadWriteTask = null;
				this._asyncActiveSemaphore.Release();
			}
			return result;
		}

		// Token: 0x0600198A RID: 6538 RVA: 0x00054F74 File Offset: 0x00053174
		[ComVisible(false)]
		[__DynamicallyInvokable]
		[HostProtection(SecurityAction.LinkDemand, ExternalThreading = true)]
		public Task<int> ReadAsync(byte[] buffer, int offset, int count)
		{
			return this.ReadAsync(buffer, offset, count, CancellationToken.None);
		}

		// Token: 0x0600198B RID: 6539 RVA: 0x00054F84 File Offset: 0x00053184
		[ComVisible(false)]
		[__DynamicallyInvokable]
		[HostProtection(SecurityAction.LinkDemand, ExternalThreading = true)]
		public virtual Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			if (!cancellationToken.IsCancellationRequested)
			{
				return this.BeginEndReadAsync(buffer, offset, count);
			}
			return Task.FromCancellation<int>(cancellationToken);
		}

		// Token: 0x0600198C RID: 6540 RVA: 0x00054FA0 File Offset: 0x000531A0
		private Task<int> BeginEndReadAsync(byte[] buffer, int offset, int count)
		{
			return TaskFactory<int>.FromAsyncTrim<Stream, Stream.ReadWriteParameters>(this, new Stream.ReadWriteParameters
			{
				Buffer = buffer,
				Offset = offset,
				Count = count
			}, (Stream stream, Stream.ReadWriteParameters args, AsyncCallback callback, object state) => stream.BeginRead(args.Buffer, args.Offset, args.Count, callback, state), (Stream stream, IAsyncResult asyncResult) => stream.EndRead(asyncResult));
		}

		// Token: 0x0600198D RID: 6541 RVA: 0x00055012 File Offset: 0x00053212
		[__DynamicallyInvokable]
		[HostProtection(SecurityAction.LinkDemand, ExternalThreading = true)]
		public virtual IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
		{
			return this.BeginWriteInternal(buffer, offset, count, callback, state, false);
		}

		// Token: 0x0600198E RID: 6542 RVA: 0x00055024 File Offset: 0x00053224
		[HostProtection(SecurityAction.LinkDemand, ExternalThreading = true)]
		internal IAsyncResult BeginWriteInternal(byte[] buffer, int offset, int count, AsyncCallback callback, object state, bool serializeAsynchronously)
		{
			if (!this.CanWrite)
			{
				__Error.WriteNotSupported();
			}
			if (CompatibilitySwitches.IsAppEarlierThanWindowsPhone8)
			{
				return this.BlockingBeginWrite(buffer, offset, count, callback, state);
			}
			SemaphoreSlim semaphoreSlim = this.EnsureAsyncActiveSemaphoreInitialized();
			Task task = null;
			if (serializeAsynchronously)
			{
				task = semaphoreSlim.WaitAsync();
			}
			else
			{
				semaphoreSlim.Wait();
			}
			Stream.ReadWriteTask readWriteTask = new Stream.ReadWriteTask(false, delegate(object <p0>)
			{
				Stream.ReadWriteTask readWriteTask2 = Task.InternalCurrent as Stream.ReadWriteTask;
				readWriteTask2._stream.Write(readWriteTask2._buffer, readWriteTask2._offset, readWriteTask2._count);
				readWriteTask2.ClearBeginState();
				return 0;
			}, state, this, buffer, offset, count, callback);
			if (task != null)
			{
				this.RunReadWriteTaskWhenReady(task, readWriteTask);
			}
			else
			{
				this.RunReadWriteTask(readWriteTask);
			}
			return readWriteTask;
		}

		// Token: 0x0600198F RID: 6543 RVA: 0x000550B4 File Offset: 0x000532B4
		private void RunReadWriteTaskWhenReady(Task asyncWaiter, Stream.ReadWriteTask readWriteTask)
		{
			if (asyncWaiter.IsCompleted)
			{
				this.RunReadWriteTask(readWriteTask);
				return;
			}
			asyncWaiter.ContinueWith(delegate(Task t, object state)
			{
				Tuple<Stream, Stream.ReadWriteTask> tuple = (Tuple<Stream, Stream.ReadWriteTask>)state;
				tuple.Item1.RunReadWriteTask(tuple.Item2);
			}, Tuple.Create<Stream, Stream.ReadWriteTask>(this, readWriteTask), default(CancellationToken), TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
		}

		// Token: 0x06001990 RID: 6544 RVA: 0x00055111 File Offset: 0x00053311
		private void RunReadWriteTask(Stream.ReadWriteTask readWriteTask)
		{
			this._activeReadWriteTask = readWriteTask;
			readWriteTask.m_taskScheduler = TaskScheduler.Default;
			readWriteTask.ScheduleAndStart(false);
		}

		// Token: 0x06001991 RID: 6545 RVA: 0x0005512C File Offset: 0x0005332C
		[__DynamicallyInvokable]
		public virtual void EndWrite(IAsyncResult asyncResult)
		{
			if (asyncResult == null)
			{
				throw new ArgumentNullException("asyncResult");
			}
			if (CompatibilitySwitches.IsAppEarlierThanWindowsPhone8)
			{
				Stream.BlockingEndWrite(asyncResult);
				return;
			}
			Stream.ReadWriteTask activeReadWriteTask = this._activeReadWriteTask;
			if (activeReadWriteTask == null)
			{
				throw new ArgumentException(Environment.GetResourceString("InvalidOperation_WrongAsyncResultOrEndWriteCalledMultiple"));
			}
			if (activeReadWriteTask != asyncResult)
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_WrongAsyncResultOrEndWriteCalledMultiple"));
			}
			if (activeReadWriteTask._isRead)
			{
				throw new ArgumentException(Environment.GetResourceString("InvalidOperation_WrongAsyncResultOrEndWriteCalledMultiple"));
			}
			try
			{
				activeReadWriteTask.GetAwaiter().GetResult();
			}
			finally
			{
				this._activeReadWriteTask = null;
				this._asyncActiveSemaphore.Release();
			}
		}

		// Token: 0x06001992 RID: 6546 RVA: 0x000551D4 File Offset: 0x000533D4
		[ComVisible(false)]
		[__DynamicallyInvokable]
		[HostProtection(SecurityAction.LinkDemand, ExternalThreading = true)]
		public Task WriteAsync(byte[] buffer, int offset, int count)
		{
			return this.WriteAsync(buffer, offset, count, CancellationToken.None);
		}

		// Token: 0x06001993 RID: 6547 RVA: 0x000551E4 File Offset: 0x000533E4
		[ComVisible(false)]
		[__DynamicallyInvokable]
		[HostProtection(SecurityAction.LinkDemand, ExternalThreading = true)]
		public virtual Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			if (!cancellationToken.IsCancellationRequested)
			{
				return this.BeginEndWriteAsync(buffer, offset, count);
			}
			return Task.FromCancellation(cancellationToken);
		}

		// Token: 0x06001994 RID: 6548 RVA: 0x00055200 File Offset: 0x00053400
		private Task BeginEndWriteAsync(byte[] buffer, int offset, int count)
		{
			return TaskFactory<VoidTaskResult>.FromAsyncTrim<Stream, Stream.ReadWriteParameters>(this, new Stream.ReadWriteParameters
			{
				Buffer = buffer,
				Offset = offset,
				Count = count
			}, (Stream stream, Stream.ReadWriteParameters args, AsyncCallback callback, object state) => stream.BeginWrite(args.Buffer, args.Offset, args.Count, callback, state), delegate(Stream stream, IAsyncResult asyncResult)
			{
				stream.EndWrite(asyncResult);
				return default(VoidTaskResult);
			});
		}

		// Token: 0x06001995 RID: 6549
		[__DynamicallyInvokable]
		public abstract long Seek(long offset, SeekOrigin origin);

		// Token: 0x06001996 RID: 6550
		[__DynamicallyInvokable]
		public abstract void SetLength(long value);

		// Token: 0x06001997 RID: 6551
		[__DynamicallyInvokable]
		public abstract int Read([In] [Out] byte[] buffer, int offset, int count);

		// Token: 0x06001998 RID: 6552 RVA: 0x00055274 File Offset: 0x00053474
		[__DynamicallyInvokable]
		public virtual int ReadByte()
		{
			byte[] array = new byte[1];
			if (this.Read(array, 0, 1) == 0)
			{
				return -1;
			}
			return (int)array[0];
		}

		// Token: 0x06001999 RID: 6553
		[__DynamicallyInvokable]
		public abstract void Write(byte[] buffer, int offset, int count);

		// Token: 0x0600199A RID: 6554 RVA: 0x0005529C File Offset: 0x0005349C
		[__DynamicallyInvokable]
		public virtual void WriteByte(byte value)
		{
			this.Write(new byte[] { value }, 0, 1);
		}

		// Token: 0x0600199B RID: 6555 RVA: 0x000552BD File Offset: 0x000534BD
		[HostProtection(SecurityAction.LinkDemand, Synchronization = true)]
		public static Stream Synchronized(Stream stream)
		{
			if (stream == null)
			{
				throw new ArgumentNullException("stream");
			}
			if (stream is Stream.SyncStream)
			{
				return stream;
			}
			return new Stream.SyncStream(stream);
		}

		// Token: 0x0600199C RID: 6556 RVA: 0x000552DD File Offset: 0x000534DD
		[Obsolete("Do not call or override this method.")]
		protected virtual void ObjectInvariant()
		{
		}

		// Token: 0x0600199D RID: 6557 RVA: 0x000552E0 File Offset: 0x000534E0
		internal IAsyncResult BlockingBeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
		{
			Stream.SynchronousAsyncResult synchronousAsyncResult;
			try
			{
				int bytesRead = this.Read(buffer, offset, count);
				synchronousAsyncResult = new Stream.SynchronousAsyncResult(bytesRead, state);
			}
			catch (IOException ex)
			{
				synchronousAsyncResult = new Stream.SynchronousAsyncResult(ex, state, false);
			}
			if (callback != null)
			{
				callback(synchronousAsyncResult);
			}
			return synchronousAsyncResult;
		}

		// Token: 0x0600199E RID: 6558 RVA: 0x0005532C File Offset: 0x0005352C
		internal static int BlockingEndRead(IAsyncResult asyncResult)
		{
			return Stream.SynchronousAsyncResult.EndRead(asyncResult);
		}

		// Token: 0x0600199F RID: 6559 RVA: 0x00055334 File Offset: 0x00053534
		internal IAsyncResult BlockingBeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
		{
			Stream.SynchronousAsyncResult synchronousAsyncResult;
			try
			{
				this.Write(buffer, offset, count);
				synchronousAsyncResult = new Stream.SynchronousAsyncResult(state);
			}
			catch (IOException ex)
			{
				synchronousAsyncResult = new Stream.SynchronousAsyncResult(ex, state, true);
			}
			if (callback != null)
			{
				callback(synchronousAsyncResult);
			}
			return synchronousAsyncResult;
		}

		// Token: 0x060019A0 RID: 6560 RVA: 0x00055380 File Offset: 0x00053580
		internal static void BlockingEndWrite(IAsyncResult asyncResult)
		{
			Stream.SynchronousAsyncResult.EndWrite(asyncResult);
		}

		// Token: 0x060019A1 RID: 6561 RVA: 0x00055388 File Offset: 0x00053588
		[__DynamicallyInvokable]
		protected Stream()
		{
		}

		// Token: 0x040008F4 RID: 2292
		[__DynamicallyInvokable]
		public static readonly Stream Null = new Stream.NullStream();

		// Token: 0x040008F5 RID: 2293
		private const int _DefaultCopyBufferSize = 81920;

		// Token: 0x040008F6 RID: 2294
		[NonSerialized]
		private Stream.ReadWriteTask _activeReadWriteTask;

		// Token: 0x040008F7 RID: 2295
		[NonSerialized]
		private SemaphoreSlim _asyncActiveSemaphore;

		// Token: 0x02000B12 RID: 2834
		private struct ReadWriteParameters
		{
			// Token: 0x040032AD RID: 12973
			internal byte[] Buffer;

			// Token: 0x040032AE RID: 12974
			internal int Offset;

			// Token: 0x040032AF RID: 12975
			internal int Count;
		}

		// Token: 0x02000B13 RID: 2835
		private sealed class ReadWriteTask : Task<int>, ITaskCompletionAction
		{
			// Token: 0x06006A9C RID: 27292 RVA: 0x00170A14 File Offset: 0x0016EC14
			internal void ClearBeginState()
			{
				this._stream = null;
				this._buffer = null;
			}

			// Token: 0x06006A9D RID: 27293 RVA: 0x00170A24 File Offset: 0x0016EC24
			[SecuritySafeCritical]
			[MethodImpl(MethodImplOptions.NoInlining)]
			public ReadWriteTask(bool isRead, Func<object, int> function, object state, Stream stream, byte[] buffer, int offset, int count, AsyncCallback callback)
				: base(function, state, CancellationToken.None, TaskCreationOptions.DenyChildAttach)
			{
				StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
				this._isRead = isRead;
				this._stream = stream;
				this._buffer = buffer;
				this._offset = offset;
				this._count = count;
				if (callback != null)
				{
					this._callback = callback;
					this._context = ExecutionContext.Capture(ref stackCrawlMark, ExecutionContext.CaptureOptions.IgnoreSyncCtx | ExecutionContext.CaptureOptions.OptimizeDefaultCase);
					base.AddCompletionAction(this);
				}
			}

			// Token: 0x06006A9E RID: 27294 RVA: 0x00170A8C File Offset: 0x0016EC8C
			[SecurityCritical]
			private static void InvokeAsyncCallback(object completedTask)
			{
				Stream.ReadWriteTask readWriteTask = (Stream.ReadWriteTask)completedTask;
				AsyncCallback callback = readWriteTask._callback;
				readWriteTask._callback = null;
				callback(readWriteTask);
			}

			// Token: 0x06006A9F RID: 27295 RVA: 0x00170AB8 File Offset: 0x0016ECB8
			[SecuritySafeCritical]
			void ITaskCompletionAction.Invoke(Task completingTask)
			{
				ExecutionContext context = this._context;
				if (context == null)
				{
					AsyncCallback callback = this._callback;
					this._callback = null;
					callback(completingTask);
					return;
				}
				this._context = null;
				ContextCallback contextCallback = Stream.ReadWriteTask.s_invokeAsyncCallback;
				if (contextCallback == null)
				{
					contextCallback = (Stream.ReadWriteTask.s_invokeAsyncCallback = new ContextCallback(Stream.ReadWriteTask.InvokeAsyncCallback));
				}
				using (context)
				{
					ExecutionContext.Run(context, contextCallback, this, true);
				}
			}

			// Token: 0x040032B0 RID: 12976
			internal readonly bool _isRead;

			// Token: 0x040032B1 RID: 12977
			internal Stream _stream;

			// Token: 0x040032B2 RID: 12978
			internal byte[] _buffer;

			// Token: 0x040032B3 RID: 12979
			internal int _offset;

			// Token: 0x040032B4 RID: 12980
			internal int _count;

			// Token: 0x040032B5 RID: 12981
			private AsyncCallback _callback;

			// Token: 0x040032B6 RID: 12982
			private ExecutionContext _context;

			// Token: 0x040032B7 RID: 12983
			[SecurityCritical]
			private static ContextCallback s_invokeAsyncCallback;
		}

		// Token: 0x02000B14 RID: 2836
		[Serializable]
		private sealed class NullStream : Stream
		{
			// Token: 0x06006AA0 RID: 27296 RVA: 0x00170B30 File Offset: 0x0016ED30
			internal NullStream()
			{
			}

			// Token: 0x17001207 RID: 4615
			// (get) Token: 0x06006AA1 RID: 27297 RVA: 0x00170B38 File Offset: 0x0016ED38
			public override bool CanRead
			{
				get
				{
					return true;
				}
			}

			// Token: 0x17001208 RID: 4616
			// (get) Token: 0x06006AA2 RID: 27298 RVA: 0x00170B3B File Offset: 0x0016ED3B
			public override bool CanWrite
			{
				get
				{
					return true;
				}
			}

			// Token: 0x17001209 RID: 4617
			// (get) Token: 0x06006AA3 RID: 27299 RVA: 0x00170B3E File Offset: 0x0016ED3E
			public override bool CanSeek
			{
				get
				{
					return true;
				}
			}

			// Token: 0x1700120A RID: 4618
			// (get) Token: 0x06006AA4 RID: 27300 RVA: 0x00170B41 File Offset: 0x0016ED41
			public override long Length
			{
				get
				{
					return 0L;
				}
			}

			// Token: 0x1700120B RID: 4619
			// (get) Token: 0x06006AA5 RID: 27301 RVA: 0x00170B45 File Offset: 0x0016ED45
			// (set) Token: 0x06006AA6 RID: 27302 RVA: 0x00170B49 File Offset: 0x0016ED49
			public override long Position
			{
				get
				{
					return 0L;
				}
				set
				{
				}
			}

			// Token: 0x06006AA7 RID: 27303 RVA: 0x00170B4B File Offset: 0x0016ED4B
			protected override void Dispose(bool disposing)
			{
			}

			// Token: 0x06006AA8 RID: 27304 RVA: 0x00170B4D File Offset: 0x0016ED4D
			public override void Flush()
			{
			}

			// Token: 0x06006AA9 RID: 27305 RVA: 0x00170B4F File Offset: 0x0016ED4F
			[ComVisible(false)]
			public override Task FlushAsync(CancellationToken cancellationToken)
			{
				if (!cancellationToken.IsCancellationRequested)
				{
					return Task.CompletedTask;
				}
				return Task.FromCancellation(cancellationToken);
			}

			// Token: 0x06006AAA RID: 27306 RVA: 0x00170B66 File Offset: 0x0016ED66
			[HostProtection(SecurityAction.LinkDemand, ExternalThreading = true)]
			public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
			{
				if (!this.CanRead)
				{
					__Error.ReadNotSupported();
				}
				return base.BlockingBeginRead(buffer, offset, count, callback, state);
			}

			// Token: 0x06006AAB RID: 27307 RVA: 0x00170B82 File Offset: 0x0016ED82
			public override int EndRead(IAsyncResult asyncResult)
			{
				if (asyncResult == null)
				{
					throw new ArgumentNullException("asyncResult");
				}
				return Stream.BlockingEndRead(asyncResult);
			}

			// Token: 0x06006AAC RID: 27308 RVA: 0x00170B98 File Offset: 0x0016ED98
			[HostProtection(SecurityAction.LinkDemand, ExternalThreading = true)]
			public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
			{
				if (!this.CanWrite)
				{
					__Error.WriteNotSupported();
				}
				return base.BlockingBeginWrite(buffer, offset, count, callback, state);
			}

			// Token: 0x06006AAD RID: 27309 RVA: 0x00170BB4 File Offset: 0x0016EDB4
			public override void EndWrite(IAsyncResult asyncResult)
			{
				if (asyncResult == null)
				{
					throw new ArgumentNullException("asyncResult");
				}
				Stream.BlockingEndWrite(asyncResult);
			}

			// Token: 0x06006AAE RID: 27310 RVA: 0x00170BCA File Offset: 0x0016EDCA
			public override int Read([In] [Out] byte[] buffer, int offset, int count)
			{
				return 0;
			}

			// Token: 0x06006AAF RID: 27311 RVA: 0x00170BD0 File Offset: 0x0016EDD0
			[ComVisible(false)]
			public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
			{
				Task<int> task = Stream.NullStream.s_nullReadTask;
				if (task == null)
				{
					task = (Stream.NullStream.s_nullReadTask = new Task<int>(false, 0, (TaskCreationOptions)16384, CancellationToken.None));
				}
				return task;
			}

			// Token: 0x06006AB0 RID: 27312 RVA: 0x00170BFF File Offset: 0x0016EDFF
			public override int ReadByte()
			{
				return -1;
			}

			// Token: 0x06006AB1 RID: 27313 RVA: 0x00170C02 File Offset: 0x0016EE02
			public override void Write(byte[] buffer, int offset, int count)
			{
			}

			// Token: 0x06006AB2 RID: 27314 RVA: 0x00170C04 File Offset: 0x0016EE04
			[ComVisible(false)]
			public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
			{
				if (!cancellationToken.IsCancellationRequested)
				{
					return Task.CompletedTask;
				}
				return Task.FromCancellation(cancellationToken);
			}

			// Token: 0x06006AB3 RID: 27315 RVA: 0x00170C1C File Offset: 0x0016EE1C
			public override void WriteByte(byte value)
			{
			}

			// Token: 0x06006AB4 RID: 27316 RVA: 0x00170C1E File Offset: 0x0016EE1E
			public override long Seek(long offset, SeekOrigin origin)
			{
				return 0L;
			}

			// Token: 0x06006AB5 RID: 27317 RVA: 0x00170C22 File Offset: 0x0016EE22
			public override void SetLength(long length)
			{
			}

			// Token: 0x040032B8 RID: 12984
			private static Task<int> s_nullReadTask;
		}

		// Token: 0x02000B15 RID: 2837
		internal sealed class SynchronousAsyncResult : IAsyncResult
		{
			// Token: 0x06006AB6 RID: 27318 RVA: 0x00170C24 File Offset: 0x0016EE24
			internal SynchronousAsyncResult(int bytesRead, object asyncStateObject)
			{
				this._bytesRead = bytesRead;
				this._stateObject = asyncStateObject;
			}

			// Token: 0x06006AB7 RID: 27319 RVA: 0x00170C3A File Offset: 0x0016EE3A
			internal SynchronousAsyncResult(object asyncStateObject)
			{
				this._stateObject = asyncStateObject;
				this._isWrite = true;
			}

			// Token: 0x06006AB8 RID: 27320 RVA: 0x00170C50 File Offset: 0x0016EE50
			internal SynchronousAsyncResult(Exception ex, object asyncStateObject, bool isWrite)
			{
				this._exceptionInfo = ExceptionDispatchInfo.Capture(ex);
				this._stateObject = asyncStateObject;
				this._isWrite = isWrite;
			}

			// Token: 0x1700120C RID: 4620
			// (get) Token: 0x06006AB9 RID: 27321 RVA: 0x00170C72 File Offset: 0x0016EE72
			public bool IsCompleted
			{
				get
				{
					return true;
				}
			}

			// Token: 0x1700120D RID: 4621
			// (get) Token: 0x06006ABA RID: 27322 RVA: 0x00170C75 File Offset: 0x0016EE75
			public WaitHandle AsyncWaitHandle
			{
				get
				{
					return LazyInitializer.EnsureInitialized<ManualResetEvent>(ref this._waitHandle, () => new ManualResetEvent(true));
				}
			}

			// Token: 0x1700120E RID: 4622
			// (get) Token: 0x06006ABB RID: 27323 RVA: 0x00170CA1 File Offset: 0x0016EEA1
			public object AsyncState
			{
				get
				{
					return this._stateObject;
				}
			}

			// Token: 0x1700120F RID: 4623
			// (get) Token: 0x06006ABC RID: 27324 RVA: 0x00170CA9 File Offset: 0x0016EEA9
			public bool CompletedSynchronously
			{
				get
				{
					return true;
				}
			}

			// Token: 0x06006ABD RID: 27325 RVA: 0x00170CAC File Offset: 0x0016EEAC
			internal void ThrowIfError()
			{
				if (this._exceptionInfo != null)
				{
					this._exceptionInfo.Throw();
				}
			}

			// Token: 0x06006ABE RID: 27326 RVA: 0x00170CC4 File Offset: 0x0016EEC4
			internal static int EndRead(IAsyncResult asyncResult)
			{
				Stream.SynchronousAsyncResult synchronousAsyncResult = asyncResult as Stream.SynchronousAsyncResult;
				if (synchronousAsyncResult == null || synchronousAsyncResult._isWrite)
				{
					__Error.WrongAsyncResult();
				}
				if (synchronousAsyncResult._endXxxCalled)
				{
					__Error.EndReadCalledTwice();
				}
				synchronousAsyncResult._endXxxCalled = true;
				synchronousAsyncResult.ThrowIfError();
				return synchronousAsyncResult._bytesRead;
			}

			// Token: 0x06006ABF RID: 27327 RVA: 0x00170D08 File Offset: 0x0016EF08
			internal static void EndWrite(IAsyncResult asyncResult)
			{
				Stream.SynchronousAsyncResult synchronousAsyncResult = asyncResult as Stream.SynchronousAsyncResult;
				if (synchronousAsyncResult == null || !synchronousAsyncResult._isWrite)
				{
					__Error.WrongAsyncResult();
				}
				if (synchronousAsyncResult._endXxxCalled)
				{
					__Error.EndWriteCalledTwice();
				}
				synchronousAsyncResult._endXxxCalled = true;
				synchronousAsyncResult.ThrowIfError();
			}

			// Token: 0x040032B9 RID: 12985
			private readonly object _stateObject;

			// Token: 0x040032BA RID: 12986
			private readonly bool _isWrite;

			// Token: 0x040032BB RID: 12987
			private ManualResetEvent _waitHandle;

			// Token: 0x040032BC RID: 12988
			private ExceptionDispatchInfo _exceptionInfo;

			// Token: 0x040032BD RID: 12989
			private bool _endXxxCalled;

			// Token: 0x040032BE RID: 12990
			private int _bytesRead;
		}

		// Token: 0x02000B16 RID: 2838
		[Serializable]
		internal sealed class SyncStream : Stream, IDisposable
		{
			// Token: 0x06006AC0 RID: 27328 RVA: 0x00170D46 File Offset: 0x0016EF46
			internal SyncStream(Stream stream)
			{
				if (stream == null)
				{
					throw new ArgumentNullException("stream");
				}
				this._stream = stream;
			}

			// Token: 0x17001210 RID: 4624
			// (get) Token: 0x06006AC1 RID: 27329 RVA: 0x00170D63 File Offset: 0x0016EF63
			public override bool CanRead
			{
				get
				{
					return this._stream.CanRead;
				}
			}

			// Token: 0x17001211 RID: 4625
			// (get) Token: 0x06006AC2 RID: 27330 RVA: 0x00170D70 File Offset: 0x0016EF70
			public override bool CanWrite
			{
				get
				{
					return this._stream.CanWrite;
				}
			}

			// Token: 0x17001212 RID: 4626
			// (get) Token: 0x06006AC3 RID: 27331 RVA: 0x00170D7D File Offset: 0x0016EF7D
			public override bool CanSeek
			{
				get
				{
					return this._stream.CanSeek;
				}
			}

			// Token: 0x17001213 RID: 4627
			// (get) Token: 0x06006AC4 RID: 27332 RVA: 0x00170D8A File Offset: 0x0016EF8A
			[ComVisible(false)]
			public override bool CanTimeout
			{
				get
				{
					return this._stream.CanTimeout;
				}
			}

			// Token: 0x17001214 RID: 4628
			// (get) Token: 0x06006AC5 RID: 27333 RVA: 0x00170D98 File Offset: 0x0016EF98
			public override long Length
			{
				get
				{
					Stream stream = this._stream;
					long length;
					lock (stream)
					{
						length = this._stream.Length;
					}
					return length;
				}
			}

			// Token: 0x17001215 RID: 4629
			// (get) Token: 0x06006AC6 RID: 27334 RVA: 0x00170DE0 File Offset: 0x0016EFE0
			// (set) Token: 0x06006AC7 RID: 27335 RVA: 0x00170E28 File Offset: 0x0016F028
			public override long Position
			{
				get
				{
					Stream stream = this._stream;
					long position;
					lock (stream)
					{
						position = this._stream.Position;
					}
					return position;
				}
				set
				{
					Stream stream = this._stream;
					lock (stream)
					{
						this._stream.Position = value;
					}
				}
			}

			// Token: 0x17001216 RID: 4630
			// (get) Token: 0x06006AC8 RID: 27336 RVA: 0x00170E70 File Offset: 0x0016F070
			// (set) Token: 0x06006AC9 RID: 27337 RVA: 0x00170E7D File Offset: 0x0016F07D
			[ComVisible(false)]
			public override int ReadTimeout
			{
				get
				{
					return this._stream.ReadTimeout;
				}
				set
				{
					this._stream.ReadTimeout = value;
				}
			}

			// Token: 0x17001217 RID: 4631
			// (get) Token: 0x06006ACA RID: 27338 RVA: 0x00170E8B File Offset: 0x0016F08B
			// (set) Token: 0x06006ACB RID: 27339 RVA: 0x00170E98 File Offset: 0x0016F098
			[ComVisible(false)]
			public override int WriteTimeout
			{
				get
				{
					return this._stream.WriteTimeout;
				}
				set
				{
					this._stream.WriteTimeout = value;
				}
			}

			// Token: 0x06006ACC RID: 27340 RVA: 0x00170EA8 File Offset: 0x0016F0A8
			public override void Close()
			{
				Stream stream = this._stream;
				lock (stream)
				{
					try
					{
						this._stream.Close();
					}
					finally
					{
						base.Dispose(true);
					}
				}
			}

			// Token: 0x06006ACD RID: 27341 RVA: 0x00170F04 File Offset: 0x0016F104
			protected override void Dispose(bool disposing)
			{
				Stream stream = this._stream;
				lock (stream)
				{
					try
					{
						if (disposing)
						{
							((IDisposable)this._stream).Dispose();
						}
					}
					finally
					{
						base.Dispose(disposing);
					}
				}
			}

			// Token: 0x06006ACE RID: 27342 RVA: 0x00170F60 File Offset: 0x0016F160
			public override void Flush()
			{
				Stream stream = this._stream;
				lock (stream)
				{
					this._stream.Flush();
				}
			}

			// Token: 0x06006ACF RID: 27343 RVA: 0x00170FA8 File Offset: 0x0016F1A8
			public override int Read([In] [Out] byte[] bytes, int offset, int count)
			{
				Stream stream = this._stream;
				int result;
				lock (stream)
				{
					result = this._stream.Read(bytes, offset, count);
				}
				return result;
			}

			// Token: 0x06006AD0 RID: 27344 RVA: 0x00170FF4 File Offset: 0x0016F1F4
			public override int ReadByte()
			{
				Stream stream = this._stream;
				int result;
				lock (stream)
				{
					result = this._stream.ReadByte();
				}
				return result;
			}

			// Token: 0x06006AD1 RID: 27345 RVA: 0x0017103C File Offset: 0x0016F23C
			private static bool OverridesBeginMethod(Stream stream, string methodName)
			{
				MethodInfo[] methods = stream.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public);
				foreach (MethodInfo methodInfo in methods)
				{
					if (methodInfo.DeclaringType == typeof(Stream) && methodInfo.Name == methodName)
					{
						return false;
					}
				}
				return true;
			}

			// Token: 0x06006AD2 RID: 27346 RVA: 0x00171094 File Offset: 0x0016F294
			[HostProtection(SecurityAction.LinkDemand, ExternalThreading = true)]
			public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
			{
				if (this._overridesBeginRead == null)
				{
					this._overridesBeginRead = new bool?(Stream.SyncStream.OverridesBeginMethod(this._stream, "BeginRead"));
				}
				Stream stream = this._stream;
				IAsyncResult result;
				lock (stream)
				{
					result = (this._overridesBeginRead.Value ? this._stream.BeginRead(buffer, offset, count, callback, state) : this._stream.BeginReadInternal(buffer, offset, count, callback, state, true));
				}
				return result;
			}

			// Token: 0x06006AD3 RID: 27347 RVA: 0x0017112C File Offset: 0x0016F32C
			public override int EndRead(IAsyncResult asyncResult)
			{
				if (asyncResult == null)
				{
					throw new ArgumentNullException("asyncResult");
				}
				Stream stream = this._stream;
				int result;
				lock (stream)
				{
					result = this._stream.EndRead(asyncResult);
				}
				return result;
			}

			// Token: 0x06006AD4 RID: 27348 RVA: 0x00171184 File Offset: 0x0016F384
			public override long Seek(long offset, SeekOrigin origin)
			{
				Stream stream = this._stream;
				long result;
				lock (stream)
				{
					result = this._stream.Seek(offset, origin);
				}
				return result;
			}

			// Token: 0x06006AD5 RID: 27349 RVA: 0x001711D0 File Offset: 0x0016F3D0
			public override void SetLength(long length)
			{
				Stream stream = this._stream;
				lock (stream)
				{
					this._stream.SetLength(length);
				}
			}

			// Token: 0x06006AD6 RID: 27350 RVA: 0x00171218 File Offset: 0x0016F418
			public override void Write(byte[] bytes, int offset, int count)
			{
				Stream stream = this._stream;
				lock (stream)
				{
					this._stream.Write(bytes, offset, count);
				}
			}

			// Token: 0x06006AD7 RID: 27351 RVA: 0x00171260 File Offset: 0x0016F460
			public override void WriteByte(byte b)
			{
				Stream stream = this._stream;
				lock (stream)
				{
					this._stream.WriteByte(b);
				}
			}

			// Token: 0x06006AD8 RID: 27352 RVA: 0x001712A8 File Offset: 0x0016F4A8
			[HostProtection(SecurityAction.LinkDemand, ExternalThreading = true)]
			public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
			{
				if (this._overridesBeginWrite == null)
				{
					this._overridesBeginWrite = new bool?(Stream.SyncStream.OverridesBeginMethod(this._stream, "BeginWrite"));
				}
				Stream stream = this._stream;
				IAsyncResult result;
				lock (stream)
				{
					result = (this._overridesBeginWrite.Value ? this._stream.BeginWrite(buffer, offset, count, callback, state) : this._stream.BeginWriteInternal(buffer, offset, count, callback, state, true));
				}
				return result;
			}

			// Token: 0x06006AD9 RID: 27353 RVA: 0x00171340 File Offset: 0x0016F540
			public override void EndWrite(IAsyncResult asyncResult)
			{
				if (asyncResult == null)
				{
					throw new ArgumentNullException("asyncResult");
				}
				Stream stream = this._stream;
				lock (stream)
				{
					this._stream.EndWrite(asyncResult);
				}
			}

			// Token: 0x040032BF RID: 12991
			private Stream _stream;

			// Token: 0x040032C0 RID: 12992
			[NonSerialized]
			private bool? _overridesBeginRead;

			// Token: 0x040032C1 RID: 12993
			[NonSerialized]
			private bool? _overridesBeginWrite;
		}
	}
}
