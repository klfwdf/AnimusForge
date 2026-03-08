using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO
{
	// Token: 0x020001A6 RID: 422
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public abstract class TextReader : MarshalByRefObject, IDisposable
	{
		// Token: 0x06001A2C RID: 6700 RVA: 0x00057826 File Offset: 0x00055A26
		[__DynamicallyInvokable]
		protected TextReader()
		{
		}

		// Token: 0x06001A2D RID: 6701 RVA: 0x0005782E File Offset: 0x00055A2E
		public virtual void Close()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		// Token: 0x06001A2E RID: 6702 RVA: 0x0005783D File Offset: 0x00055A3D
		[__DynamicallyInvokable]
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		// Token: 0x06001A2F RID: 6703 RVA: 0x0005784C File Offset: 0x00055A4C
		[__DynamicallyInvokable]
		protected virtual void Dispose(bool disposing)
		{
		}

		// Token: 0x06001A30 RID: 6704 RVA: 0x0005784E File Offset: 0x00055A4E
		[__DynamicallyInvokable]
		public virtual int Peek()
		{
			return -1;
		}

		// Token: 0x06001A31 RID: 6705 RVA: 0x00057851 File Offset: 0x00055A51
		[__DynamicallyInvokable]
		public virtual int Read()
		{
			return -1;
		}

		// Token: 0x06001A32 RID: 6706 RVA: 0x00057854 File Offset: 0x00055A54
		[__DynamicallyInvokable]
		public virtual int Read([In] [Out] char[] buffer, int index, int count)
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
			int num = 0;
			do
			{
				int num2 = this.Read();
				if (num2 == -1)
				{
					break;
				}
				buffer[index + num++] = (char)num2;
			}
			while (num < count);
			return num;
		}

		// Token: 0x06001A33 RID: 6707 RVA: 0x000578E0 File Offset: 0x00055AE0
		[__DynamicallyInvokable]
		public virtual string ReadToEnd()
		{
			char[] array = new char[4096];
			StringBuilder stringBuilder = new StringBuilder(4096);
			int charCount;
			while ((charCount = this.Read(array, 0, array.Length)) != 0)
			{
				stringBuilder.Append(array, 0, charCount);
			}
			return stringBuilder.ToString();
		}

		// Token: 0x06001A34 RID: 6708 RVA: 0x00057924 File Offset: 0x00055B24
		[__DynamicallyInvokable]
		public virtual int ReadBlock([In] [Out] char[] buffer, int index, int count)
		{
			int num = 0;
			int num2;
			do
			{
				num += (num2 = this.Read(buffer, index + num, count - num));
			}
			while (num2 > 0 && num < count);
			return num;
		}

		// Token: 0x06001A35 RID: 6709 RVA: 0x00057950 File Offset: 0x00055B50
		[__DynamicallyInvokable]
		public virtual string ReadLine()
		{
			StringBuilder stringBuilder = new StringBuilder();
			int num;
			for (;;)
			{
				num = this.Read();
				if (num == -1)
				{
					goto IL_43;
				}
				if (num == 13 || num == 10)
				{
					break;
				}
				stringBuilder.Append((char)num);
			}
			if (num == 13 && this.Peek() == 10)
			{
				this.Read();
			}
			return stringBuilder.ToString();
			IL_43:
			if (stringBuilder.Length > 0)
			{
				return stringBuilder.ToString();
			}
			return null;
		}

		// Token: 0x06001A36 RID: 6710 RVA: 0x000579B1 File Offset: 0x00055BB1
		[ComVisible(false)]
		[__DynamicallyInvokable]
		[HostProtection(SecurityAction.LinkDemand, ExternalThreading = true)]
		public virtual Task<string> ReadLineAsync()
		{
			return Task<string>.Factory.StartNew(TextReader._ReadLineDelegate, this, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
		}

		// Token: 0x06001A37 RID: 6711 RVA: 0x000579D0 File Offset: 0x00055BD0
		[ComVisible(false)]
		[__DynamicallyInvokable]
		[HostProtection(SecurityAction.LinkDemand, ExternalThreading = true)]
		public virtual Task<string> ReadToEndAsync()
		{
			TextReader.<ReadToEndAsync>d__14 <ReadToEndAsync>d__;
			<ReadToEndAsync>d__.<>t__builder = AsyncTaskMethodBuilder<string>.Create();
			<ReadToEndAsync>d__.<>4__this = this;
			<ReadToEndAsync>d__.<>1__state = -1;
			<ReadToEndAsync>d__.<>t__builder.Start<TextReader.<ReadToEndAsync>d__14>(ref <ReadToEndAsync>d__);
			return <ReadToEndAsync>d__.<>t__builder.Task;
		}

		// Token: 0x06001A38 RID: 6712 RVA: 0x00057A14 File Offset: 0x00055C14
		[ComVisible(false)]
		[__DynamicallyInvokable]
		[HostProtection(SecurityAction.LinkDemand, ExternalThreading = true)]
		public virtual Task<int> ReadAsync(char[] buffer, int index, int count)
		{
			if (buffer == null)
			{
				throw new ArgumentNullException("buffer", Environment.GetResourceString("ArgumentNull_Buffer"));
			}
			if (index < 0 || count < 0)
			{
				throw new ArgumentOutOfRangeException((index < 0) ? "index" : "count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			if (buffer.Length - index < count)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
			}
			return this.ReadAsyncInternal(buffer, index, count);
		}

		// Token: 0x06001A39 RID: 6713 RVA: 0x00057A84 File Offset: 0x00055C84
		internal virtual Task<int> ReadAsyncInternal(char[] buffer, int index, int count)
		{
			Tuple<TextReader, char[], int, int> state = new Tuple<TextReader, char[], int, int>(this, buffer, index, count);
			return Task<int>.Factory.StartNew(TextReader._ReadDelegate, state, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
		}

		// Token: 0x06001A3A RID: 6714 RVA: 0x00057AB8 File Offset: 0x00055CB8
		[ComVisible(false)]
		[__DynamicallyInvokable]
		[HostProtection(SecurityAction.LinkDemand, ExternalThreading = true)]
		public virtual Task<int> ReadBlockAsync(char[] buffer, int index, int count)
		{
			if (buffer == null)
			{
				throw new ArgumentNullException("buffer", Environment.GetResourceString("ArgumentNull_Buffer"));
			}
			if (index < 0 || count < 0)
			{
				throw new ArgumentOutOfRangeException((index < 0) ? "index" : "count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			if (buffer.Length - index < count)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
			}
			return this.ReadBlockAsyncInternal(buffer, index, count);
		}

		// Token: 0x06001A3B RID: 6715 RVA: 0x00057B28 File Offset: 0x00055D28
		[HostProtection(SecurityAction.LinkDemand, ExternalThreading = true)]
		private async Task<int> ReadBlockAsyncInternal(char[] buffer, int index, int count)
		{
			int i = 0;
			int num2;
			do
			{
				int num = await this.ReadAsyncInternal(buffer, index + i, count - i).ConfigureAwait(false);
				num2 = num;
				i += num2;
			}
			while (num2 > 0 && i < count);
			return i;
		}

		// Token: 0x06001A3C RID: 6716 RVA: 0x00057B83 File Offset: 0x00055D83
		[HostProtection(SecurityAction.LinkDemand, Synchronization = true)]
		public static TextReader Synchronized(TextReader reader)
		{
			if (reader == null)
			{
				throw new ArgumentNullException("reader");
			}
			if (reader is TextReader.SyncTextReader)
			{
				return reader;
			}
			return new TextReader.SyncTextReader(reader);
		}

		// Token: 0x04000923 RID: 2339
		[NonSerialized]
		private static Func<object, string> _ReadLineDelegate = (object state) => ((TextReader)state).ReadLine();

		// Token: 0x04000924 RID: 2340
		[NonSerialized]
		private static Func<object, int> _ReadDelegate = delegate(object state)
		{
			Tuple<TextReader, char[], int, int> tuple = (Tuple<TextReader, char[], int, int>)state;
			return tuple.Item1.Read(tuple.Item2, tuple.Item3, tuple.Item4);
		};

		// Token: 0x04000925 RID: 2341
		[__DynamicallyInvokable]
		public static readonly TextReader Null = new TextReader.NullTextReader();

		// Token: 0x02000B23 RID: 2851
		[Serializable]
		private sealed class NullTextReader : TextReader
		{
			// Token: 0x06006B04 RID: 27396 RVA: 0x00172F3E File Offset: 0x0017113E
			public override int Read(char[] buffer, int index, int count)
			{
				return 0;
			}

			// Token: 0x06006B05 RID: 27397 RVA: 0x00172F41 File Offset: 0x00171141
			public override string ReadLine()
			{
				return null;
			}
		}

		// Token: 0x02000B24 RID: 2852
		[Serializable]
		internal sealed class SyncTextReader : TextReader
		{
			// Token: 0x06006B06 RID: 27398 RVA: 0x00172F44 File Offset: 0x00171144
			internal SyncTextReader(TextReader t)
			{
				this._in = t;
			}

			// Token: 0x06006B07 RID: 27399 RVA: 0x00172F53 File Offset: 0x00171153
			[MethodImpl(MethodImplOptions.Synchronized)]
			public override void Close()
			{
				this._in.Close();
			}

			// Token: 0x06006B08 RID: 27400 RVA: 0x00172F60 File Offset: 0x00171160
			[MethodImpl(MethodImplOptions.Synchronized)]
			protected override void Dispose(bool disposing)
			{
				if (disposing)
				{
					((IDisposable)this._in).Dispose();
				}
			}

			// Token: 0x06006B09 RID: 27401 RVA: 0x00172F70 File Offset: 0x00171170
			[MethodImpl(MethodImplOptions.Synchronized)]
			public override int Peek()
			{
				return this._in.Peek();
			}

			// Token: 0x06006B0A RID: 27402 RVA: 0x00172F7D File Offset: 0x0017117D
			[MethodImpl(MethodImplOptions.Synchronized)]
			public override int Read()
			{
				return this._in.Read();
			}

			// Token: 0x06006B0B RID: 27403 RVA: 0x00172F8A File Offset: 0x0017118A
			[MethodImpl(MethodImplOptions.Synchronized)]
			public override int Read([In] [Out] char[] buffer, int index, int count)
			{
				return this._in.Read(buffer, index, count);
			}

			// Token: 0x06006B0C RID: 27404 RVA: 0x00172F9A File Offset: 0x0017119A
			[MethodImpl(MethodImplOptions.Synchronized)]
			public override int ReadBlock([In] [Out] char[] buffer, int index, int count)
			{
				return this._in.ReadBlock(buffer, index, count);
			}

			// Token: 0x06006B0D RID: 27405 RVA: 0x00172FAA File Offset: 0x001711AA
			[MethodImpl(MethodImplOptions.Synchronized)]
			public override string ReadLine()
			{
				return this._in.ReadLine();
			}

			// Token: 0x06006B0E RID: 27406 RVA: 0x00172FB7 File Offset: 0x001711B7
			[MethodImpl(MethodImplOptions.Synchronized)]
			public override string ReadToEnd()
			{
				return this._in.ReadToEnd();
			}

			// Token: 0x06006B0F RID: 27407 RVA: 0x00172FC4 File Offset: 0x001711C4
			[ComVisible(false)]
			[MethodImpl(MethodImplOptions.Synchronized)]
			public override Task<string> ReadLineAsync()
			{
				return Task.FromResult<string>(this.ReadLine());
			}

			// Token: 0x06006B10 RID: 27408 RVA: 0x00172FD1 File Offset: 0x001711D1
			[ComVisible(false)]
			[MethodImpl(MethodImplOptions.Synchronized)]
			public override Task<string> ReadToEndAsync()
			{
				return Task.FromResult<string>(this.ReadToEnd());
			}

			// Token: 0x06006B11 RID: 27409 RVA: 0x00172FE0 File Offset: 0x001711E0
			[ComVisible(false)]
			[MethodImpl(MethodImplOptions.Synchronized)]
			public override Task<int> ReadBlockAsync(char[] buffer, int index, int count)
			{
				if (buffer == null)
				{
					throw new ArgumentNullException("buffer", Environment.GetResourceString("ArgumentNull_Buffer"));
				}
				if (index < 0 || count < 0)
				{
					throw new ArgumentOutOfRangeException((index < 0) ? "index" : "count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
				}
				if (buffer.Length - index < count)
				{
					throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
				}
				return Task.FromResult<int>(this.ReadBlock(buffer, index, count));
			}

			// Token: 0x06006B12 RID: 27410 RVA: 0x00173054 File Offset: 0x00171254
			[ComVisible(false)]
			[MethodImpl(MethodImplOptions.Synchronized)]
			public override Task<int> ReadAsync(char[] buffer, int index, int count)
			{
				if (buffer == null)
				{
					throw new ArgumentNullException("buffer", Environment.GetResourceString("ArgumentNull_Buffer"));
				}
				if (index < 0 || count < 0)
				{
					throw new ArgumentOutOfRangeException((index < 0) ? "index" : "count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
				}
				if (buffer.Length - index < count)
				{
					throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
				}
				return Task.FromResult<int>(this.Read(buffer, index, count));
			}

			// Token: 0x04003329 RID: 13097
			internal TextReader _in;
		}
	}
}
