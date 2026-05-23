using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO
{
	// Token: 0x020001A3 RID: 419
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public class StreamWriter : TextWriter
	{
		// Token: 0x060019E1 RID: 6625 RVA: 0x000564C4 File Offset: 0x000546C4
		private void CheckAsyncTaskInProgress()
		{
			Task asyncWriteTask = this._asyncWriteTask;
			if (asyncWriteTask != null && !asyncWriteTask.IsCompleted)
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_AsyncIOInProgress"));
			}
		}

		// Token: 0x170002E3 RID: 739
		// (get) Token: 0x060019E2 RID: 6626 RVA: 0x000564F8 File Offset: 0x000546F8
		internal static Encoding UTF8NoBOM
		{
			[FriendAccessAllowed]
			get
			{
				if (StreamWriter._UTF8NoBOM == null)
				{
					UTF8Encoding utf8NoBOM = new UTF8Encoding(false, true);
					Thread.MemoryBarrier();
					StreamWriter._UTF8NoBOM = utf8NoBOM;
				}
				return StreamWriter._UTF8NoBOM;
			}
		}

		// Token: 0x060019E3 RID: 6627 RVA: 0x0005652A File Offset: 0x0005472A
		internal StreamWriter()
			: base(null)
		{
		}

		// Token: 0x060019E4 RID: 6628 RVA: 0x00056533 File Offset: 0x00054733
		[__DynamicallyInvokable]
		public StreamWriter(Stream stream)
			: this(stream, StreamWriter.UTF8NoBOM, 1024, false)
		{
		}

		// Token: 0x060019E5 RID: 6629 RVA: 0x00056547 File Offset: 0x00054747
		[__DynamicallyInvokable]
		public StreamWriter(Stream stream, Encoding encoding)
			: this(stream, encoding, 1024, false)
		{
		}

		// Token: 0x060019E6 RID: 6630 RVA: 0x00056557 File Offset: 0x00054757
		[__DynamicallyInvokable]
		public StreamWriter(Stream stream, Encoding encoding, int bufferSize)
			: this(stream, encoding, bufferSize, false)
		{
		}

		// Token: 0x060019E7 RID: 6631 RVA: 0x00056564 File Offset: 0x00054764
		[__DynamicallyInvokable]
		public StreamWriter(Stream stream, Encoding encoding, int bufferSize, bool leaveOpen)
			: base(null)
		{
			if (stream == null || encoding == null)
			{
				throw new ArgumentNullException((stream == null) ? "stream" : "encoding");
			}
			if (!stream.CanWrite)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_StreamNotWritable"));
			}
			if (bufferSize <= 0)
			{
				throw new ArgumentOutOfRangeException("bufferSize", Environment.GetResourceString("ArgumentOutOfRange_NeedPosNum"));
			}
			this.Init(stream, encoding, bufferSize, leaveOpen);
		}

		// Token: 0x060019E8 RID: 6632 RVA: 0x000565CF File Offset: 0x000547CF
		public StreamWriter(string path)
			: this(path, false, StreamWriter.UTF8NoBOM, 1024)
		{
		}

		// Token: 0x060019E9 RID: 6633 RVA: 0x000565E3 File Offset: 0x000547E3
		public StreamWriter(string path, bool append)
			: this(path, append, StreamWriter.UTF8NoBOM, 1024)
		{
		}

		// Token: 0x060019EA RID: 6634 RVA: 0x000565F7 File Offset: 0x000547F7
		public StreamWriter(string path, bool append, Encoding encoding)
			: this(path, append, encoding, 1024)
		{
		}

		// Token: 0x060019EB RID: 6635 RVA: 0x00056607 File Offset: 0x00054807
		[SecuritySafeCritical]
		public StreamWriter(string path, bool append, Encoding encoding, int bufferSize)
			: this(path, append, encoding, bufferSize, true)
		{
		}

		// Token: 0x060019EC RID: 6636 RVA: 0x00056618 File Offset: 0x00054818
		[SecurityCritical]
		internal StreamWriter(string path, bool append, Encoding encoding, int bufferSize, bool checkHost)
			: base(null)
		{
			if (path == null)
			{
				throw new ArgumentNullException("path");
			}
			if (encoding == null)
			{
				throw new ArgumentNullException("encoding");
			}
			if (path.Length == 0)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"));
			}
			if (bufferSize <= 0)
			{
				throw new ArgumentOutOfRangeException("bufferSize", Environment.GetResourceString("ArgumentOutOfRange_NeedPosNum"));
			}
			Stream streamArg = StreamWriter.CreateFile(path, append, checkHost);
			this.Init(streamArg, encoding, bufferSize, false);
		}

		// Token: 0x060019ED RID: 6637 RVA: 0x00056690 File Offset: 0x00054890
		[SecuritySafeCritical]
		private void Init(Stream streamArg, Encoding encodingArg, int bufferSize, bool shouldLeaveOpen)
		{
			this.stream = streamArg;
			this.encoding = encodingArg;
			this.encoder = this.encoding.GetEncoder();
			if (bufferSize < 128)
			{
				bufferSize = 128;
			}
			this.charBuffer = new char[bufferSize];
			this.byteBuffer = new byte[this.encoding.GetMaxByteCount(bufferSize)];
			this.charLen = bufferSize;
			if (this.stream.CanSeek && this.stream.Position > 0L)
			{
				this.haveWrittenPreamble = true;
			}
			this.closable = !shouldLeaveOpen;
			if (Mda.StreamWriterBufferedDataLost.Enabled)
			{
				string cs = null;
				if (Mda.StreamWriterBufferedDataLost.CaptureAllocatedCallStack)
				{
					cs = Environment.GetStackTrace(null, false);
				}
				this.mdaHelper = new StreamWriter.MdaHelper(this, cs);
			}
		}

		// Token: 0x060019EE RID: 6638 RVA: 0x00056748 File Offset: 0x00054948
		[SecurityCritical]
		private static Stream CreateFile(string path, bool append, bool checkHost)
		{
			FileMode mode = (append ? FileMode.Append : FileMode.Create);
			return new FileStream(path, mode, FileAccess.Write, FileShare.Read, 4096, FileOptions.SequentialScan, Path.GetFileName(path), false, false, checkHost);
		}

		// Token: 0x060019EF RID: 6639 RVA: 0x0005677B File Offset: 0x0005497B
		public override void Close()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		// Token: 0x060019F0 RID: 6640 RVA: 0x0005678C File Offset: 0x0005498C
		[__DynamicallyInvokable]
		protected override void Dispose(bool disposing)
		{
			try
			{
				if (this.stream != null && (disposing || (this.LeaveOpen && this.stream is __ConsoleStream)))
				{
					this.CheckAsyncTaskInProgress();
					this.Flush(true, true);
					if (this.mdaHelper != null)
					{
						GC.SuppressFinalize(this.mdaHelper);
					}
				}
			}
			finally
			{
				if (!this.LeaveOpen && this.stream != null)
				{
					try
					{
						if (disposing)
						{
							this.stream.Close();
						}
					}
					finally
					{
						this.stream = null;
						this.byteBuffer = null;
						this.charBuffer = null;
						this.encoding = null;
						this.encoder = null;
						this.charLen = 0;
						base.Dispose(disposing);
					}
				}
			}
		}

		// Token: 0x060019F1 RID: 6641 RVA: 0x0005684C File Offset: 0x00054A4C
		[__DynamicallyInvokable]
		public override void Flush()
		{
			this.CheckAsyncTaskInProgress();
			this.Flush(true, true);
		}

		// Token: 0x060019F2 RID: 6642 RVA: 0x0005685C File Offset: 0x00054A5C
		private void Flush(bool flushStream, bool flushEncoder)
		{
			if (this.stream == null)
			{
				__Error.WriterClosed();
			}
			if (this.charPos == 0 && ((!flushStream && !flushEncoder) || CompatibilitySwitches.IsAppEarlierThanWindowsPhone8))
			{
				return;
			}
			if (!this.haveWrittenPreamble)
			{
				this.haveWrittenPreamble = true;
				byte[] preamble = this.encoding.GetPreamble();
				if (preamble.Length != 0)
				{
					this.stream.Write(preamble, 0, preamble.Length);
				}
			}
			int bytes = this.encoder.GetBytes(this.charBuffer, 0, this.charPos, this.byteBuffer, 0, flushEncoder);
			this.charPos = 0;
			if (bytes > 0)
			{
				this.stream.Write(this.byteBuffer, 0, bytes);
			}
			if (flushStream)
			{
				this.stream.Flush();
			}
		}

		// Token: 0x170002E4 RID: 740
		// (get) Token: 0x060019F3 RID: 6643 RVA: 0x00056908 File Offset: 0x00054B08
		// (set) Token: 0x060019F4 RID: 6644 RVA: 0x00056910 File Offset: 0x00054B10
		[__DynamicallyInvokable]
		public virtual bool AutoFlush
		{
			[__DynamicallyInvokable]
			get
			{
				return this.autoFlush;
			}
			[__DynamicallyInvokable]
			set
			{
				this.CheckAsyncTaskInProgress();
				this.autoFlush = value;
				if (value)
				{
					this.Flush(true, false);
				}
			}
		}

		// Token: 0x170002E5 RID: 741
		// (get) Token: 0x060019F5 RID: 6645 RVA: 0x0005692A File Offset: 0x00054B2A
		[__DynamicallyInvokable]
		public virtual Stream BaseStream
		{
			[__DynamicallyInvokable]
			get
			{
				return this.stream;
			}
		}

		// Token: 0x170002E6 RID: 742
		// (get) Token: 0x060019F6 RID: 6646 RVA: 0x00056932 File Offset: 0x00054B32
		internal bool LeaveOpen
		{
			get
			{
				return !this.closable;
			}
		}

		// Token: 0x170002E7 RID: 743
		// (set) Token: 0x060019F7 RID: 6647 RVA: 0x0005693D File Offset: 0x00054B3D
		internal bool HaveWrittenPreamble
		{
			set
			{
				this.haveWrittenPreamble = value;
			}
		}

		// Token: 0x170002E8 RID: 744
		// (get) Token: 0x060019F8 RID: 6648 RVA: 0x00056946 File Offset: 0x00054B46
		[__DynamicallyInvokable]
		public override Encoding Encoding
		{
			[__DynamicallyInvokable]
			get
			{
				return this.encoding;
			}
		}

		// Token: 0x060019F9 RID: 6649 RVA: 0x00056950 File Offset: 0x00054B50
		[__DynamicallyInvokable]
		public override void Write(char value)
		{
			this.CheckAsyncTaskInProgress();
			if (this.charPos == this.charLen)
			{
				this.Flush(false, false);
			}
			this.charBuffer[this.charPos] = value;
			this.charPos++;
			if (this.autoFlush)
			{
				this.Flush(true, false);
			}
		}

		// Token: 0x060019FA RID: 6650 RVA: 0x000569A8 File Offset: 0x00054BA8
		[__DynamicallyInvokable]
		public override void Write(char[] buffer)
		{
			if (buffer == null)
			{
				return;
			}
			this.CheckAsyncTaskInProgress();
			int num = 0;
			int num2;
			for (int i = buffer.Length; i > 0; i -= num2)
			{
				if (this.charPos == this.charLen)
				{
					this.Flush(false, false);
				}
				num2 = this.charLen - this.charPos;
				if (num2 > i)
				{
					num2 = i;
				}
				Buffer.InternalBlockCopy(buffer, num * 2, this.charBuffer, this.charPos * 2, num2 * 2);
				this.charPos += num2;
				num += num2;
			}
			if (this.autoFlush)
			{
				this.Flush(true, false);
			}
		}

		// Token: 0x060019FB RID: 6651 RVA: 0x00056A38 File Offset: 0x00054C38
		[__DynamicallyInvokable]
		public override void Write(char[] buffer, int index, int count)
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
			this.CheckAsyncTaskInProgress();
			while (count > 0)
			{
				if (this.charPos == this.charLen)
				{
					this.Flush(false, false);
				}
				int num = this.charLen - this.charPos;
				if (num > count)
				{
					num = count;
				}
				Buffer.InternalBlockCopy(buffer, index * 2, this.charBuffer, this.charPos * 2, num * 2);
				this.charPos += num;
				index += num;
				count -= num;
			}
			if (this.autoFlush)
			{
				this.Flush(true, false);
			}
		}

		// Token: 0x060019FC RID: 6652 RVA: 0x00056B20 File Offset: 0x00054D20
		[__DynamicallyInvokable]
		public override void Write(string value)
		{
			if (value != null)
			{
				this.CheckAsyncTaskInProgress();
				int i = value.Length;
				int num = 0;
				while (i > 0)
				{
					if (this.charPos == this.charLen)
					{
						this.Flush(false, false);
					}
					int num2 = this.charLen - this.charPos;
					if (num2 > i)
					{
						num2 = i;
					}
					value.CopyTo(num, this.charBuffer, this.charPos, num2);
					this.charPos += num2;
					num += num2;
					i -= num2;
				}
				if (this.autoFlush)
				{
					this.Flush(true, false);
				}
			}
		}

		// Token: 0x060019FD RID: 6653 RVA: 0x00056BAC File Offset: 0x00054DAC
		[ComVisible(false)]
		[__DynamicallyInvokable]
		[HostProtection(SecurityAction.LinkDemand, ExternalThreading = true)]
		public override Task WriteAsync(char value)
		{
			if (base.GetType() != typeof(StreamWriter))
			{
				return base.WriteAsync(value);
			}
			if (this.stream == null)
			{
				__Error.WriterClosed();
			}
			this.CheckAsyncTaskInProgress();
			Task task = StreamWriter.WriteAsyncInternal(this, value, this.charBuffer, this.charPos, this.charLen, this.CoreNewLine, this.autoFlush, false);
			this._asyncWriteTask = task;
			return task;
		}

		// Token: 0x060019FE RID: 6654 RVA: 0x00056C1C File Offset: 0x00054E1C
		private static async Task WriteAsyncInternal(StreamWriter _this, char value, char[] charBuffer, int charPos, int charLen, char[] coreNewLine, bool autoFlush, bool appendNewLine)
		{
			if (charPos == charLen)
			{
				await _this.FlushAsyncInternal(false, false, charBuffer, charPos).ConfigureAwait(false);
				charPos = 0;
			}
			charBuffer[charPos] = value;
			charPos++;
			if (appendNewLine)
			{
				for (int i = 0; i < coreNewLine.Length; i++)
				{
					if (charPos == charLen)
					{
						await _this.FlushAsyncInternal(false, false, charBuffer, charPos).ConfigureAwait(false);
						charPos = 0;
					}
					charBuffer[charPos] = coreNewLine[i];
					charPos++;
				}
			}
			if (autoFlush)
			{
				await _this.FlushAsyncInternal(true, false, charBuffer, charPos).ConfigureAwait(false);
				charPos = 0;
			}
			_this.CharPos_Prop = charPos;
		}

		// Token: 0x060019FF RID: 6655 RVA: 0x00056C9C File Offset: 0x00054E9C
		[ComVisible(false)]
		[__DynamicallyInvokable]
		[HostProtection(SecurityAction.LinkDemand, ExternalThreading = true)]
		public override Task WriteAsync(string value)
		{
			if (base.GetType() != typeof(StreamWriter))
			{
				return base.WriteAsync(value);
			}
			if (value != null)
			{
				if (this.stream == null)
				{
					__Error.WriterClosed();
				}
				this.CheckAsyncTaskInProgress();
				Task task = StreamWriter.WriteAsyncInternal(this, value, this.charBuffer, this.charPos, this.charLen, this.CoreNewLine, this.autoFlush, false);
				this._asyncWriteTask = task;
				return task;
			}
			return Task.CompletedTask;
		}

		// Token: 0x06001A00 RID: 6656 RVA: 0x00056D18 File Offset: 0x00054F18
		private static async Task WriteAsyncInternal(StreamWriter _this, string value, char[] charBuffer, int charPos, int charLen, char[] coreNewLine, bool autoFlush, bool appendNewLine)
		{
			int count = value.Length;
			int index = 0;
			while (count > 0)
			{
				if (charPos == charLen)
				{
					await _this.FlushAsyncInternal(false, false, charBuffer, charPos).ConfigureAwait(false);
					charPos = 0;
				}
				int num = charLen - charPos;
				if (num > count)
				{
					num = count;
				}
				value.CopyTo(index, charBuffer, charPos, num);
				charPos += num;
				index += num;
				count -= num;
			}
			if (appendNewLine)
			{
				for (int i = 0; i < coreNewLine.Length; i++)
				{
					if (charPos == charLen)
					{
						await _this.FlushAsyncInternal(false, false, charBuffer, charPos).ConfigureAwait(false);
						charPos = 0;
					}
					charBuffer[charPos] = coreNewLine[i];
					charPos++;
				}
			}
			if (autoFlush)
			{
				await _this.FlushAsyncInternal(true, false, charBuffer, charPos).ConfigureAwait(false);
				charPos = 0;
			}
			_this.CharPos_Prop = charPos;
		}

		// Token: 0x06001A01 RID: 6657 RVA: 0x00056D98 File Offset: 0x00054F98
		[ComVisible(false)]
		[__DynamicallyInvokable]
		[HostProtection(SecurityAction.LinkDemand, ExternalThreading = true)]
		public override Task WriteAsync(char[] buffer, int index, int count)
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
			if (base.GetType() != typeof(StreamWriter))
			{
				return base.WriteAsync(buffer, index, count);
			}
			if (this.stream == null)
			{
				__Error.WriterClosed();
			}
			this.CheckAsyncTaskInProgress();
			Task task = StreamWriter.WriteAsyncInternal(this, buffer, index, count, this.charBuffer, this.charPos, this.charLen, this.CoreNewLine, this.autoFlush, false);
			this._asyncWriteTask = task;
			return task;
		}

		// Token: 0x06001A02 RID: 6658 RVA: 0x00056E70 File Offset: 0x00055070
		private static async Task WriteAsyncInternal(StreamWriter _this, char[] buffer, int index, int count, char[] charBuffer, int charPos, int charLen, char[] coreNewLine, bool autoFlush, bool appendNewLine)
		{
			while (count > 0)
			{
				if (charPos == charLen)
				{
					await _this.FlushAsyncInternal(false, false, charBuffer, charPos).ConfigureAwait(false);
					charPos = 0;
				}
				int num = charLen - charPos;
				if (num > count)
				{
					num = count;
				}
				Buffer.InternalBlockCopy(buffer, index * 2, charBuffer, charPos * 2, num * 2);
				charPos += num;
				index += num;
				count -= num;
			}
			if (appendNewLine)
			{
				for (int i = 0; i < coreNewLine.Length; i++)
				{
					if (charPos == charLen)
					{
						await _this.FlushAsyncInternal(false, false, charBuffer, charPos).ConfigureAwait(false);
						charPos = 0;
					}
					charBuffer[charPos] = coreNewLine[i];
					charPos++;
				}
			}
			if (autoFlush)
			{
				await _this.FlushAsyncInternal(true, false, charBuffer, charPos).ConfigureAwait(false);
				charPos = 0;
			}
			_this.CharPos_Prop = charPos;
		}

		// Token: 0x06001A03 RID: 6659 RVA: 0x00056F04 File Offset: 0x00055104
		[ComVisible(false)]
		[__DynamicallyInvokable]
		[HostProtection(SecurityAction.LinkDemand, ExternalThreading = true)]
		public override Task WriteLineAsync()
		{
			if (base.GetType() != typeof(StreamWriter))
			{
				return base.WriteLineAsync();
			}
			if (this.stream == null)
			{
				__Error.WriterClosed();
			}
			this.CheckAsyncTaskInProgress();
			Task task = StreamWriter.WriteAsyncInternal(this, null, 0, 0, this.charBuffer, this.charPos, this.charLen, this.CoreNewLine, this.autoFlush, true);
			this._asyncWriteTask = task;
			return task;
		}

		// Token: 0x06001A04 RID: 6660 RVA: 0x00056F78 File Offset: 0x00055178
		[ComVisible(false)]
		[__DynamicallyInvokable]
		[HostProtection(SecurityAction.LinkDemand, ExternalThreading = true)]
		public override Task WriteLineAsync(char value)
		{
			if (base.GetType() != typeof(StreamWriter))
			{
				return base.WriteLineAsync(value);
			}
			if (this.stream == null)
			{
				__Error.WriterClosed();
			}
			this.CheckAsyncTaskInProgress();
			Task task = StreamWriter.WriteAsyncInternal(this, value, this.charBuffer, this.charPos, this.charLen, this.CoreNewLine, this.autoFlush, true);
			this._asyncWriteTask = task;
			return task;
		}

		// Token: 0x06001A05 RID: 6661 RVA: 0x00056FE8 File Offset: 0x000551E8
		[ComVisible(false)]
		[__DynamicallyInvokable]
		[HostProtection(SecurityAction.LinkDemand, ExternalThreading = true)]
		public override Task WriteLineAsync(string value)
		{
			if (base.GetType() != typeof(StreamWriter))
			{
				return base.WriteLineAsync(value);
			}
			if (this.stream == null)
			{
				__Error.WriterClosed();
			}
			this.CheckAsyncTaskInProgress();
			Task task = StreamWriter.WriteAsyncInternal(this, value, this.charBuffer, this.charPos, this.charLen, this.CoreNewLine, this.autoFlush, true);
			this._asyncWriteTask = task;
			return task;
		}

		// Token: 0x06001A06 RID: 6662 RVA: 0x00057058 File Offset: 0x00055258
		[ComVisible(false)]
		[__DynamicallyInvokable]
		[HostProtection(SecurityAction.LinkDemand, ExternalThreading = true)]
		public override Task WriteLineAsync(char[] buffer, int index, int count)
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
			if (base.GetType() != typeof(StreamWriter))
			{
				return base.WriteLineAsync(buffer, index, count);
			}
			if (this.stream == null)
			{
				__Error.WriterClosed();
			}
			this.CheckAsyncTaskInProgress();
			Task task = StreamWriter.WriteAsyncInternal(this, buffer, index, count, this.charBuffer, this.charPos, this.charLen, this.CoreNewLine, this.autoFlush, true);
			this._asyncWriteTask = task;
			return task;
		}

		// Token: 0x06001A07 RID: 6663 RVA: 0x00057130 File Offset: 0x00055330
		[ComVisible(false)]
		[__DynamicallyInvokable]
		[HostProtection(SecurityAction.LinkDemand, ExternalThreading = true)]
		public override Task FlushAsync()
		{
			if (base.GetType() != typeof(StreamWriter))
			{
				return base.FlushAsync();
			}
			if (this.stream == null)
			{
				__Error.WriterClosed();
			}
			this.CheckAsyncTaskInProgress();
			Task task = this.FlushAsyncInternal(true, true, this.charBuffer, this.charPos);
			this._asyncWriteTask = task;
			return task;
		}

		// Token: 0x170002E9 RID: 745
		// (set) Token: 0x06001A08 RID: 6664 RVA: 0x0005718D File Offset: 0x0005538D
		private int CharPos_Prop
		{
			set
			{
				this.charPos = value;
			}
		}

		// Token: 0x170002EA RID: 746
		// (set) Token: 0x06001A09 RID: 6665 RVA: 0x00057196 File Offset: 0x00055396
		private bool HaveWrittenPreamble_Prop
		{
			set
			{
				this.haveWrittenPreamble = value;
			}
		}

		// Token: 0x06001A0A RID: 6666 RVA: 0x000571A0 File Offset: 0x000553A0
		private Task FlushAsyncInternal(bool flushStream, bool flushEncoder, char[] sCharBuffer, int sCharPos)
		{
			if (sCharPos == 0 && !flushStream && !flushEncoder)
			{
				return Task.CompletedTask;
			}
			Task result = StreamWriter.FlushAsyncInternal(this, flushStream, flushEncoder, sCharBuffer, sCharPos, this.haveWrittenPreamble, this.encoding, this.encoder, this.byteBuffer, this.stream);
			this.charPos = 0;
			return result;
		}

		// Token: 0x06001A0B RID: 6667 RVA: 0x000571F0 File Offset: 0x000553F0
		private static async Task FlushAsyncInternal(StreamWriter _this, bool flushStream, bool flushEncoder, char[] charBuffer, int charPos, bool haveWrittenPreamble, Encoding encoding, Encoder encoder, byte[] byteBuffer, Stream stream)
		{
			if (!haveWrittenPreamble)
			{
				_this.HaveWrittenPreamble_Prop = true;
				byte[] preamble = encoding.GetPreamble();
				if (preamble.Length != 0)
				{
					await stream.WriteAsync(preamble, 0, preamble.Length).ConfigureAwait(false);
				}
			}
			int bytes = encoder.GetBytes(charBuffer, 0, charPos, byteBuffer, 0, flushEncoder);
			if (bytes > 0)
			{
				await stream.WriteAsync(byteBuffer, 0, bytes).ConfigureAwait(false);
			}
			if (flushStream)
			{
				await stream.FlushAsync().ConfigureAwait(false);
			}
		}

		// Token: 0x0400090B RID: 2315
		internal const int DefaultBufferSize = 1024;

		// Token: 0x0400090C RID: 2316
		private const int DefaultFileStreamBufferSize = 4096;

		// Token: 0x0400090D RID: 2317
		private const int MinBufferSize = 128;

		// Token: 0x0400090E RID: 2318
		private const int DontCopyOnWriteLineThreshold = 512;

		// Token: 0x0400090F RID: 2319
		[__DynamicallyInvokable]
		public new static readonly StreamWriter Null = new StreamWriter(Stream.Null, new UTF8Encoding(false, true), 128, true);

		// Token: 0x04000910 RID: 2320
		private Stream stream;

		// Token: 0x04000911 RID: 2321
		private Encoding encoding;

		// Token: 0x04000912 RID: 2322
		private Encoder encoder;

		// Token: 0x04000913 RID: 2323
		private byte[] byteBuffer;

		// Token: 0x04000914 RID: 2324
		private char[] charBuffer;

		// Token: 0x04000915 RID: 2325
		private int charPos;

		// Token: 0x04000916 RID: 2326
		private int charLen;

		// Token: 0x04000917 RID: 2327
		private bool autoFlush;

		// Token: 0x04000918 RID: 2328
		private bool haveWrittenPreamble;

		// Token: 0x04000919 RID: 2329
		private bool closable;

		// Token: 0x0400091A RID: 2330
		[NonSerialized]
		private StreamWriter.MdaHelper mdaHelper;

		// Token: 0x0400091B RID: 2331
		[NonSerialized]
		private volatile Task _asyncWriteTask;

		// Token: 0x0400091C RID: 2332
		private static volatile Encoding _UTF8NoBOM;

		// Token: 0x02000B1E RID: 2846
		private sealed class MdaHelper
		{
			// Token: 0x06006AF9 RID: 27385 RVA: 0x00172322 File Offset: 0x00170522
			internal MdaHelper(StreamWriter sw, string cs)
			{
				this.streamWriter = sw;
				this.allocatedCallstack = cs;
			}

			// Token: 0x06006AFA RID: 27386 RVA: 0x00172338 File Offset: 0x00170538
			protected override void Finalize()
			{
				try
				{
					if (this.streamWriter.charPos != 0 && this.streamWriter.stream != null && this.streamWriter.stream != Stream.Null)
					{
						string text = ((this.streamWriter.stream is FileStream) ? ((FileStream)this.streamWriter.stream).NameInternal : "<unknown>");
						string resourceString = this.allocatedCallstack;
						if (resourceString == null)
						{
							resourceString = Environment.GetResourceString("IO_StreamWriterBufferedDataLostCaptureAllocatedFromCallstackNotEnabled");
						}
						string resourceString2 = Environment.GetResourceString("IO_StreamWriterBufferedDataLost", new object[]
						{
							this.streamWriter.stream.GetType().FullName,
							text,
							resourceString
						});
						Mda.StreamWriterBufferedDataLost.ReportError(resourceString2);
					}
				}
				finally
				{
					base.Finalize();
				}
			}

			// Token: 0x040032F2 RID: 13042
			private StreamWriter streamWriter;

			// Token: 0x040032F3 RID: 13043
			private string allocatedCallstack;
		}
	}
}
