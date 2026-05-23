using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;

namespace System.IO
{
	// Token: 0x020001A2 RID: 418
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public class StreamReader : TextReader
	{
		// Token: 0x170002D1 RID: 721
		// (get) Token: 0x060019A3 RID: 6563 RVA: 0x0005539C File Offset: 0x0005359C
		internal static int DefaultBufferSize
		{
			get
			{
				return 1024;
			}
		}

		// Token: 0x060019A4 RID: 6564 RVA: 0x000553A4 File Offset: 0x000535A4
		private void CheckAsyncTaskInProgress()
		{
			Task asyncReadTask = this._asyncReadTask;
			if (asyncReadTask != null && !asyncReadTask.IsCompleted)
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_AsyncIOInProgress"));
			}
		}

		// Token: 0x060019A5 RID: 6565 RVA: 0x000553D5 File Offset: 0x000535D5
		internal StreamReader()
		{
		}

		// Token: 0x060019A6 RID: 6566 RVA: 0x000553DD File Offset: 0x000535DD
		[__DynamicallyInvokable]
		public StreamReader(Stream stream)
			: this(stream, true)
		{
		}

		// Token: 0x060019A7 RID: 6567 RVA: 0x000553E7 File Offset: 0x000535E7
		[__DynamicallyInvokable]
		public StreamReader(Stream stream, bool detectEncodingFromByteOrderMarks)
			: this(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks, StreamReader.DefaultBufferSize, false)
		{
		}

		// Token: 0x060019A8 RID: 6568 RVA: 0x000553FC File Offset: 0x000535FC
		[__DynamicallyInvokable]
		public StreamReader(Stream stream, Encoding encoding)
			: this(stream, encoding, true, StreamReader.DefaultBufferSize, false)
		{
		}

		// Token: 0x060019A9 RID: 6569 RVA: 0x0005540D File Offset: 0x0005360D
		[__DynamicallyInvokable]
		public StreamReader(Stream stream, Encoding encoding, bool detectEncodingFromByteOrderMarks)
			: this(stream, encoding, detectEncodingFromByteOrderMarks, StreamReader.DefaultBufferSize, false)
		{
		}

		// Token: 0x060019AA RID: 6570 RVA: 0x0005541E File Offset: 0x0005361E
		[__DynamicallyInvokable]
		public StreamReader(Stream stream, Encoding encoding, bool detectEncodingFromByteOrderMarks, int bufferSize)
			: this(stream, encoding, detectEncodingFromByteOrderMarks, bufferSize, false)
		{
		}

		// Token: 0x060019AB RID: 6571 RVA: 0x0005542C File Offset: 0x0005362C
		[__DynamicallyInvokable]
		public StreamReader(Stream stream, Encoding encoding, bool detectEncodingFromByteOrderMarks, int bufferSize, bool leaveOpen)
		{
			if (stream == null || encoding == null)
			{
				throw new ArgumentNullException((stream == null) ? "stream" : "encoding");
			}
			if (!stream.CanRead)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_StreamNotReadable"));
			}
			if (bufferSize <= 0)
			{
				throw new ArgumentOutOfRangeException("bufferSize", Environment.GetResourceString("ArgumentOutOfRange_NeedPosNum"));
			}
			this.Init(stream, encoding, detectEncodingFromByteOrderMarks, bufferSize, leaveOpen);
		}

		// Token: 0x060019AC RID: 6572 RVA: 0x00055499 File Offset: 0x00053699
		public StreamReader(string path)
			: this(path, true)
		{
		}

		// Token: 0x060019AD RID: 6573 RVA: 0x000554A3 File Offset: 0x000536A3
		public StreamReader(string path, bool detectEncodingFromByteOrderMarks)
			: this(path, Encoding.UTF8, detectEncodingFromByteOrderMarks, StreamReader.DefaultBufferSize)
		{
		}

		// Token: 0x060019AE RID: 6574 RVA: 0x000554B7 File Offset: 0x000536B7
		public StreamReader(string path, Encoding encoding)
			: this(path, encoding, true, StreamReader.DefaultBufferSize)
		{
		}

		// Token: 0x060019AF RID: 6575 RVA: 0x000554C7 File Offset: 0x000536C7
		public StreamReader(string path, Encoding encoding, bool detectEncodingFromByteOrderMarks)
			: this(path, encoding, detectEncodingFromByteOrderMarks, StreamReader.DefaultBufferSize)
		{
		}

		// Token: 0x060019B0 RID: 6576 RVA: 0x000554D7 File Offset: 0x000536D7
		[SecuritySafeCritical]
		public StreamReader(string path, Encoding encoding, bool detectEncodingFromByteOrderMarks, int bufferSize)
			: this(path, encoding, detectEncodingFromByteOrderMarks, bufferSize, true)
		{
		}

		// Token: 0x060019B1 RID: 6577 RVA: 0x000554E8 File Offset: 0x000536E8
		[SecurityCritical]
		internal StreamReader(string path, Encoding encoding, bool detectEncodingFromByteOrderMarks, int bufferSize, bool checkHost)
		{
			if (path == null || encoding == null)
			{
				throw new ArgumentNullException((path == null) ? "path" : "encoding");
			}
			if (path.Length == 0)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"));
			}
			if (bufferSize <= 0)
			{
				throw new ArgumentOutOfRangeException("bufferSize", Environment.GetResourceString("ArgumentOutOfRange_NeedPosNum"));
			}
			Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan, Path.GetFileName(path), false, false, checkHost);
			this.Init(stream, encoding, detectEncodingFromByteOrderMarks, bufferSize, false);
		}

		// Token: 0x060019B2 RID: 6578 RVA: 0x00055574 File Offset: 0x00053774
		private void Init(Stream stream, Encoding encoding, bool detectEncodingFromByteOrderMarks, int bufferSize, bool leaveOpen)
		{
			this.stream = stream;
			this.encoding = encoding;
			this.decoder = encoding.GetDecoder();
			if (bufferSize < 128)
			{
				bufferSize = 128;
			}
			this.byteBuffer = new byte[bufferSize];
			this._maxCharsPerBuffer = encoding.GetMaxCharCount(bufferSize);
			this.charBuffer = new char[this._maxCharsPerBuffer];
			this.byteLen = 0;
			this.bytePos = 0;
			this._detectEncoding = detectEncodingFromByteOrderMarks;
			this._preamble = encoding.GetPreamble();
			this._checkPreamble = this._preamble.Length != 0;
			this._isBlocked = false;
			this._closable = !leaveOpen;
		}

		// Token: 0x060019B3 RID: 6579 RVA: 0x0005561A File Offset: 0x0005381A
		internal void Init(Stream stream)
		{
			this.stream = stream;
			this._closable = true;
		}

		// Token: 0x060019B4 RID: 6580 RVA: 0x0005562A File Offset: 0x0005382A
		public override void Close()
		{
			this.Dispose(true);
		}

		// Token: 0x060019B5 RID: 6581 RVA: 0x00055634 File Offset: 0x00053834
		[__DynamicallyInvokable]
		protected override void Dispose(bool disposing)
		{
			try
			{
				if (!this.LeaveOpen && disposing && this.stream != null)
				{
					this.stream.Close();
				}
			}
			finally
			{
				if (!this.LeaveOpen && this.stream != null)
				{
					this.stream = null;
					this.encoding = null;
					this.decoder = null;
					this.byteBuffer = null;
					this.charBuffer = null;
					this.charPos = 0;
					this.charLen = 0;
					base.Dispose(disposing);
				}
			}
		}

		// Token: 0x170002D2 RID: 722
		// (get) Token: 0x060019B6 RID: 6582 RVA: 0x000556BC File Offset: 0x000538BC
		[__DynamicallyInvokable]
		public virtual Encoding CurrentEncoding
		{
			[__DynamicallyInvokable]
			get
			{
				return this.encoding;
			}
		}

		// Token: 0x170002D3 RID: 723
		// (get) Token: 0x060019B7 RID: 6583 RVA: 0x000556C4 File Offset: 0x000538C4
		[__DynamicallyInvokable]
		public virtual Stream BaseStream
		{
			[__DynamicallyInvokable]
			get
			{
				return this.stream;
			}
		}

		// Token: 0x170002D4 RID: 724
		// (get) Token: 0x060019B8 RID: 6584 RVA: 0x000556CC File Offset: 0x000538CC
		internal bool LeaveOpen
		{
			get
			{
				return !this._closable;
			}
		}

		// Token: 0x060019B9 RID: 6585 RVA: 0x000556D7 File Offset: 0x000538D7
		[__DynamicallyInvokable]
		public void DiscardBufferedData()
		{
			this.CheckAsyncTaskInProgress();
			this.byteLen = 0;
			this.charLen = 0;
			this.charPos = 0;
			if (this.encoding != null)
			{
				this.decoder = this.encoding.GetDecoder();
			}
			this._isBlocked = false;
		}

		// Token: 0x170002D5 RID: 725
		// (get) Token: 0x060019BA RID: 6586 RVA: 0x00055714 File Offset: 0x00053914
		[__DynamicallyInvokable]
		public bool EndOfStream
		{
			[__DynamicallyInvokable]
			get
			{
				if (this.stream == null)
				{
					__Error.ReaderClosed();
				}
				this.CheckAsyncTaskInProgress();
				if (this.charPos < this.charLen)
				{
					return false;
				}
				int num = this.ReadBuffer();
				return num == 0;
			}
		}

		// Token: 0x060019BB RID: 6587 RVA: 0x00055750 File Offset: 0x00053950
		[__DynamicallyInvokable]
		public override int Peek()
		{
			if (this.stream == null)
			{
				__Error.ReaderClosed();
			}
			this.CheckAsyncTaskInProgress();
			if (this.charPos == this.charLen && (this._isBlocked || this.ReadBuffer() == 0))
			{
				return -1;
			}
			return (int)this.charBuffer[this.charPos];
		}

		// Token: 0x060019BC RID: 6588 RVA: 0x000557A0 File Offset: 0x000539A0
		[__DynamicallyInvokable]
		public override int Read()
		{
			if (this.stream == null)
			{
				__Error.ReaderClosed();
			}
			this.CheckAsyncTaskInProgress();
			if (this.charPos == this.charLen && this.ReadBuffer() == 0)
			{
				return -1;
			}
			int result = (int)this.charBuffer[this.charPos];
			this.charPos++;
			return result;
		}

		// Token: 0x060019BD RID: 6589 RVA: 0x000557F8 File Offset: 0x000539F8
		[__DynamicallyInvokable]
		public override int Read([In] [Out] char[] buffer, int index, int count)
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
			if (this.stream == null)
			{
				__Error.ReaderClosed();
			}
			this.CheckAsyncTaskInProgress();
			int num = 0;
			bool flag = false;
			while (count > 0)
			{
				int num2 = this.charLen - this.charPos;
				if (num2 == 0)
				{
					num2 = this.ReadBuffer(buffer, index + num, count, out flag);
				}
				if (num2 == 0)
				{
					break;
				}
				if (num2 > count)
				{
					num2 = count;
				}
				if (!flag)
				{
					Buffer.InternalBlockCopy(this.charBuffer, this.charPos * 2, buffer, (index + num) * 2, num2 * 2);
					this.charPos += num2;
				}
				num += num2;
				count -= num2;
				if (this._isBlocked)
				{
					break;
				}
			}
			return num;
		}

		// Token: 0x060019BE RID: 6590 RVA: 0x000558E4 File Offset: 0x00053AE4
		[__DynamicallyInvokable]
		public override string ReadToEnd()
		{
			if (this.stream == null)
			{
				__Error.ReaderClosed();
			}
			this.CheckAsyncTaskInProgress();
			StringBuilder stringBuilder = new StringBuilder(this.charLen - this.charPos);
			do
			{
				stringBuilder.Append(this.charBuffer, this.charPos, this.charLen - this.charPos);
				this.charPos = this.charLen;
				this.ReadBuffer();
			}
			while (this.charLen > 0);
			return stringBuilder.ToString();
		}

		// Token: 0x060019BF RID: 6591 RVA: 0x0005595C File Offset: 0x00053B5C
		[__DynamicallyInvokable]
		public override int ReadBlock([In] [Out] char[] buffer, int index, int count)
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
			if (this.stream == null)
			{
				__Error.ReaderClosed();
			}
			this.CheckAsyncTaskInProgress();
			return base.ReadBlock(buffer, index, count);
		}

		// Token: 0x060019C0 RID: 6592 RVA: 0x000559DD File Offset: 0x00053BDD
		private void CompressBuffer(int n)
		{
			Buffer.InternalBlockCopy(this.byteBuffer, n, this.byteBuffer, 0, this.byteLen - n);
			this.byteLen -= n;
		}

		// Token: 0x060019C1 RID: 6593 RVA: 0x00055A08 File Offset: 0x00053C08
		private void DetectEncoding()
		{
			if (this.byteLen < 2)
			{
				return;
			}
			this._detectEncoding = false;
			bool flag = false;
			if (this.byteBuffer[0] == 254 && this.byteBuffer[1] == 255)
			{
				this.encoding = new UnicodeEncoding(true, true);
				this.CompressBuffer(2);
				flag = true;
			}
			else if (this.byteBuffer[0] == 255 && this.byteBuffer[1] == 254)
			{
				if (this.byteLen < 4 || this.byteBuffer[2] != 0 || this.byteBuffer[3] != 0)
				{
					this.encoding = new UnicodeEncoding(false, true);
					this.CompressBuffer(2);
					flag = true;
				}
				else
				{
					this.encoding = new UTF32Encoding(false, true);
					this.CompressBuffer(4);
					flag = true;
				}
			}
			else if (this.byteLen >= 3 && this.byteBuffer[0] == 239 && this.byteBuffer[1] == 187 && this.byteBuffer[2] == 191)
			{
				this.encoding = Encoding.UTF8;
				this.CompressBuffer(3);
				flag = true;
			}
			else if (this.byteLen >= 4 && this.byteBuffer[0] == 0 && this.byteBuffer[1] == 0 && this.byteBuffer[2] == 254 && this.byteBuffer[3] == 255)
			{
				this.encoding = new UTF32Encoding(true, true);
				this.CompressBuffer(4);
				flag = true;
			}
			else if (this.byteLen == 2)
			{
				this._detectEncoding = true;
			}
			if (flag)
			{
				this.decoder = this.encoding.GetDecoder();
				this._maxCharsPerBuffer = this.encoding.GetMaxCharCount(this.byteBuffer.Length);
				this.charBuffer = new char[this._maxCharsPerBuffer];
			}
		}

		// Token: 0x060019C2 RID: 6594 RVA: 0x00055BC0 File Offset: 0x00053DC0
		private bool IsPreamble()
		{
			if (!this._checkPreamble)
			{
				return this._checkPreamble;
			}
			int num = ((this.byteLen >= this._preamble.Length) ? (this._preamble.Length - this.bytePos) : (this.byteLen - this.bytePos));
			int i = 0;
			while (i < num)
			{
				if (this.byteBuffer[this.bytePos] != this._preamble[this.bytePos])
				{
					this.bytePos = 0;
					this._checkPreamble = false;
					break;
				}
				i++;
				this.bytePos++;
			}
			if (this._checkPreamble && this.bytePos == this._preamble.Length)
			{
				this.CompressBuffer(this._preamble.Length);
				this.bytePos = 0;
				this._checkPreamble = false;
				this._detectEncoding = false;
			}
			return this._checkPreamble;
		}

		// Token: 0x060019C3 RID: 6595 RVA: 0x00055C94 File Offset: 0x00053E94
		internal virtual int ReadBuffer()
		{
			this.charLen = 0;
			this.charPos = 0;
			if (!this._checkPreamble)
			{
				this.byteLen = 0;
			}
			for (;;)
			{
				if (this._checkPreamble)
				{
					int num = this.stream.Read(this.byteBuffer, this.bytePos, this.byteBuffer.Length - this.bytePos);
					if (num == 0)
					{
						break;
					}
					this.byteLen += num;
				}
				else
				{
					this.byteLen = this.stream.Read(this.byteBuffer, 0, this.byteBuffer.Length);
					if (this.byteLen == 0)
					{
						goto Block_5;
					}
				}
				this._isBlocked = this.byteLen < this.byteBuffer.Length;
				if (!this.IsPreamble())
				{
					if (this._detectEncoding && this.byteLen >= 2)
					{
						this.DetectEncoding();
					}
					this.charLen += this.decoder.GetChars(this.byteBuffer, 0, this.byteLen, this.charBuffer, this.charLen);
				}
				if (this.charLen != 0)
				{
					goto Block_9;
				}
			}
			if (this.byteLen > 0)
			{
				this.charLen += this.decoder.GetChars(this.byteBuffer, 0, this.byteLen, this.charBuffer, this.charLen);
				this.bytePos = (this.byteLen = 0);
			}
			return this.charLen;
			Block_5:
			return this.charLen;
			Block_9:
			return this.charLen;
		}

		// Token: 0x060019C4 RID: 6596 RVA: 0x00055DFC File Offset: 0x00053FFC
		private int ReadBuffer(char[] userBuffer, int userOffset, int desiredChars, out bool readToUserBuffer)
		{
			this.charLen = 0;
			this.charPos = 0;
			if (!this._checkPreamble)
			{
				this.byteLen = 0;
			}
			int num = 0;
			readToUserBuffer = desiredChars >= this._maxCharsPerBuffer;
			for (;;)
			{
				if (this._checkPreamble)
				{
					int num2 = this.stream.Read(this.byteBuffer, this.bytePos, this.byteBuffer.Length - this.bytePos);
					if (num2 == 0)
					{
						break;
					}
					this.byteLen += num2;
				}
				else
				{
					this.byteLen = this.stream.Read(this.byteBuffer, 0, this.byteBuffer.Length);
					if (this.byteLen == 0)
					{
						goto IL_1B1;
					}
				}
				this._isBlocked = this.byteLen < this.byteBuffer.Length;
				if (!this.IsPreamble())
				{
					if (this._detectEncoding && this.byteLen >= 2)
					{
						this.DetectEncoding();
						readToUserBuffer = desiredChars >= this._maxCharsPerBuffer;
					}
					this.charPos = 0;
					if (readToUserBuffer)
					{
						num += this.decoder.GetChars(this.byteBuffer, 0, this.byteLen, userBuffer, userOffset + num);
						this.charLen = 0;
					}
					else
					{
						num = this.decoder.GetChars(this.byteBuffer, 0, this.byteLen, this.charBuffer, num);
						this.charLen += num;
					}
				}
				if (num != 0)
				{
					goto IL_1B1;
				}
			}
			if (this.byteLen > 0)
			{
				if (readToUserBuffer)
				{
					num = this.decoder.GetChars(this.byteBuffer, 0, this.byteLen, userBuffer, userOffset + num);
					this.charLen = 0;
				}
				else
				{
					num = this.decoder.GetChars(this.byteBuffer, 0, this.byteLen, this.charBuffer, num);
					this.charLen += num;
				}
			}
			return num;
			IL_1B1:
			this._isBlocked &= num < desiredChars;
			return num;
		}

		// Token: 0x060019C5 RID: 6597 RVA: 0x00055FCC File Offset: 0x000541CC
		[__DynamicallyInvokable]
		public override string ReadLine()
		{
			if (this.stream == null)
			{
				__Error.ReaderClosed();
			}
			this.CheckAsyncTaskInProgress();
			if (this.charPos == this.charLen && this.ReadBuffer() == 0)
			{
				return null;
			}
			StringBuilder stringBuilder = null;
			int num;
			char c;
			for (;;)
			{
				num = this.charPos;
				do
				{
					c = this.charBuffer[num];
					if (c == '\r' || c == '\n')
					{
						goto IL_4A;
					}
					num++;
				}
				while (num < this.charLen);
				num = this.charLen - this.charPos;
				if (stringBuilder == null)
				{
					stringBuilder = new StringBuilder(num + 80);
				}
				stringBuilder.Append(this.charBuffer, this.charPos, num);
				if (this.ReadBuffer() <= 0)
				{
					goto Block_11;
				}
			}
			IL_4A:
			string result;
			if (stringBuilder != null)
			{
				stringBuilder.Append(this.charBuffer, this.charPos, num - this.charPos);
				result = stringBuilder.ToString();
			}
			else
			{
				result = new string(this.charBuffer, this.charPos, num - this.charPos);
			}
			this.charPos = num + 1;
			if (c == '\r' && (this.charPos < this.charLen || this.ReadBuffer() > 0) && this.charBuffer[this.charPos] == '\n')
			{
				this.charPos++;
			}
			return result;
			Block_11:
			return stringBuilder.ToString();
		}

		// Token: 0x060019C6 RID: 6598 RVA: 0x000560FC File Offset: 0x000542FC
		[ComVisible(false)]
		[__DynamicallyInvokable]
		[HostProtection(SecurityAction.LinkDemand, ExternalThreading = true)]
		public override Task<string> ReadLineAsync()
		{
			if (base.GetType() != typeof(StreamReader))
			{
				return base.ReadLineAsync();
			}
			if (this.stream == null)
			{
				__Error.ReaderClosed();
			}
			this.CheckAsyncTaskInProgress();
			Task<string> task = this.ReadLineAsyncInternal();
			this._asyncReadTask = task;
			return task;
		}

		// Token: 0x060019C7 RID: 6599 RVA: 0x0005614C File Offset: 0x0005434C
		private Task<string> ReadLineAsyncInternal()
		{
			StreamReader.<ReadLineAsyncInternal>d__60 <ReadLineAsyncInternal>d__;
			<ReadLineAsyncInternal>d__.<>t__builder = AsyncTaskMethodBuilder<string>.Create();
			<ReadLineAsyncInternal>d__.<>4__this = this;
			<ReadLineAsyncInternal>d__.<>1__state = -1;
			<ReadLineAsyncInternal>d__.<>t__builder.Start<StreamReader.<ReadLineAsyncInternal>d__60>(ref <ReadLineAsyncInternal>d__);
			return <ReadLineAsyncInternal>d__.<>t__builder.Task;
		}

		// Token: 0x060019C8 RID: 6600 RVA: 0x00056190 File Offset: 0x00054390
		[ComVisible(false)]
		[__DynamicallyInvokable]
		[HostProtection(SecurityAction.LinkDemand, ExternalThreading = true)]
		public override Task<string> ReadToEndAsync()
		{
			if (base.GetType() != typeof(StreamReader))
			{
				return base.ReadToEndAsync();
			}
			if (this.stream == null)
			{
				__Error.ReaderClosed();
			}
			this.CheckAsyncTaskInProgress();
			Task<string> task = this.ReadToEndAsyncInternal();
			this._asyncReadTask = task;
			return task;
		}

		// Token: 0x060019C9 RID: 6601 RVA: 0x000561E0 File Offset: 0x000543E0
		private Task<string> ReadToEndAsyncInternal()
		{
			StreamReader.<ReadToEndAsyncInternal>d__62 <ReadToEndAsyncInternal>d__;
			<ReadToEndAsyncInternal>d__.<>t__builder = AsyncTaskMethodBuilder<string>.Create();
			<ReadToEndAsyncInternal>d__.<>4__this = this;
			<ReadToEndAsyncInternal>d__.<>1__state = -1;
			<ReadToEndAsyncInternal>d__.<>t__builder.Start<StreamReader.<ReadToEndAsyncInternal>d__62>(ref <ReadToEndAsyncInternal>d__);
			return <ReadToEndAsyncInternal>d__.<>t__builder.Task;
		}

		// Token: 0x060019CA RID: 6602 RVA: 0x00056224 File Offset: 0x00054424
		[ComVisible(false)]
		[__DynamicallyInvokable]
		[HostProtection(SecurityAction.LinkDemand, ExternalThreading = true)]
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
			if (base.GetType() != typeof(StreamReader))
			{
				return base.ReadAsync(buffer, index, count);
			}
			if (this.stream == null)
			{
				__Error.ReaderClosed();
			}
			this.CheckAsyncTaskInProgress();
			Task<int> task = this.ReadAsyncInternal(buffer, index, count);
			this._asyncReadTask = task;
			return task;
		}

		// Token: 0x060019CB RID: 6603 RVA: 0x000562D4 File Offset: 0x000544D4
		internal override Task<int> ReadAsyncInternal(char[] buffer, int index, int count)
		{
			StreamReader.<ReadAsyncInternal>d__64 <ReadAsyncInternal>d__;
			<ReadAsyncInternal>d__.<>t__builder = AsyncTaskMethodBuilder<int>.Create();
			<ReadAsyncInternal>d__.<>4__this = this;
			<ReadAsyncInternal>d__.buffer = buffer;
			<ReadAsyncInternal>d__.index = index;
			<ReadAsyncInternal>d__.count = count;
			<ReadAsyncInternal>d__.<>1__state = -1;
			<ReadAsyncInternal>d__.<>t__builder.Start<StreamReader.<ReadAsyncInternal>d__64>(ref <ReadAsyncInternal>d__);
			return <ReadAsyncInternal>d__.<>t__builder.Task;
		}

		// Token: 0x060019CC RID: 6604 RVA: 0x00056330 File Offset: 0x00054530
		[ComVisible(false)]
		[__DynamicallyInvokable]
		[HostProtection(SecurityAction.LinkDemand, ExternalThreading = true)]
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
			if (base.GetType() != typeof(StreamReader))
			{
				return base.ReadBlockAsync(buffer, index, count);
			}
			if (this.stream == null)
			{
				__Error.ReaderClosed();
			}
			this.CheckAsyncTaskInProgress();
			Task<int> task = base.ReadBlockAsync(buffer, index, count);
			this._asyncReadTask = task;
			return task;
		}

		// Token: 0x170002D6 RID: 726
		// (get) Token: 0x060019CD RID: 6605 RVA: 0x000563DD File Offset: 0x000545DD
		// (set) Token: 0x060019CE RID: 6606 RVA: 0x000563E5 File Offset: 0x000545E5
		private int CharLen_Prop
		{
			get
			{
				return this.charLen;
			}
			set
			{
				this.charLen = value;
			}
		}

		// Token: 0x170002D7 RID: 727
		// (get) Token: 0x060019CF RID: 6607 RVA: 0x000563EE File Offset: 0x000545EE
		// (set) Token: 0x060019D0 RID: 6608 RVA: 0x000563F6 File Offset: 0x000545F6
		private int CharPos_Prop
		{
			get
			{
				return this.charPos;
			}
			set
			{
				this.charPos = value;
			}
		}

		// Token: 0x170002D8 RID: 728
		// (get) Token: 0x060019D1 RID: 6609 RVA: 0x000563FF File Offset: 0x000545FF
		// (set) Token: 0x060019D2 RID: 6610 RVA: 0x00056407 File Offset: 0x00054607
		private int ByteLen_Prop
		{
			get
			{
				return this.byteLen;
			}
			set
			{
				this.byteLen = value;
			}
		}

		// Token: 0x170002D9 RID: 729
		// (get) Token: 0x060019D3 RID: 6611 RVA: 0x00056410 File Offset: 0x00054610
		// (set) Token: 0x060019D4 RID: 6612 RVA: 0x00056418 File Offset: 0x00054618
		private int BytePos_Prop
		{
			get
			{
				return this.bytePos;
			}
			set
			{
				this.bytePos = value;
			}
		}

		// Token: 0x170002DA RID: 730
		// (get) Token: 0x060019D5 RID: 6613 RVA: 0x00056421 File Offset: 0x00054621
		private byte[] Preamble_Prop
		{
			get
			{
				return this._preamble;
			}
		}

		// Token: 0x170002DB RID: 731
		// (get) Token: 0x060019D6 RID: 6614 RVA: 0x00056429 File Offset: 0x00054629
		private bool CheckPreamble_Prop
		{
			get
			{
				return this._checkPreamble;
			}
		}

		// Token: 0x170002DC RID: 732
		// (get) Token: 0x060019D7 RID: 6615 RVA: 0x00056431 File Offset: 0x00054631
		private Decoder Decoder_Prop
		{
			get
			{
				return this.decoder;
			}
		}

		// Token: 0x170002DD RID: 733
		// (get) Token: 0x060019D8 RID: 6616 RVA: 0x00056439 File Offset: 0x00054639
		private bool DetectEncoding_Prop
		{
			get
			{
				return this._detectEncoding;
			}
		}

		// Token: 0x170002DE RID: 734
		// (get) Token: 0x060019D9 RID: 6617 RVA: 0x00056441 File Offset: 0x00054641
		private char[] CharBuffer_Prop
		{
			get
			{
				return this.charBuffer;
			}
		}

		// Token: 0x170002DF RID: 735
		// (get) Token: 0x060019DA RID: 6618 RVA: 0x00056449 File Offset: 0x00054649
		private byte[] ByteBuffer_Prop
		{
			get
			{
				return this.byteBuffer;
			}
		}

		// Token: 0x170002E0 RID: 736
		// (get) Token: 0x060019DB RID: 6619 RVA: 0x00056451 File Offset: 0x00054651
		// (set) Token: 0x060019DC RID: 6620 RVA: 0x00056459 File Offset: 0x00054659
		private bool IsBlocked_Prop
		{
			get
			{
				return this._isBlocked;
			}
			set
			{
				this._isBlocked = value;
			}
		}

		// Token: 0x170002E1 RID: 737
		// (get) Token: 0x060019DD RID: 6621 RVA: 0x00056462 File Offset: 0x00054662
		private Stream Stream_Prop
		{
			get
			{
				return this.stream;
			}
		}

		// Token: 0x170002E2 RID: 738
		// (get) Token: 0x060019DE RID: 6622 RVA: 0x0005646A File Offset: 0x0005466A
		private int MaxCharsPerBuffer_Prop
		{
			get
			{
				return this._maxCharsPerBuffer;
			}
		}

		// Token: 0x060019DF RID: 6623 RVA: 0x00056474 File Offset: 0x00054674
		private Task<int> ReadBufferAsync()
		{
			StreamReader.<ReadBufferAsync>d__97 <ReadBufferAsync>d__;
			<ReadBufferAsync>d__.<>t__builder = AsyncTaskMethodBuilder<int>.Create();
			<ReadBufferAsync>d__.<>4__this = this;
			<ReadBufferAsync>d__.<>1__state = -1;
			<ReadBufferAsync>d__.<>t__builder.Start<StreamReader.<ReadBufferAsync>d__97>(ref <ReadBufferAsync>d__);
			return <ReadBufferAsync>d__.<>t__builder.Task;
		}

		// Token: 0x040008F8 RID: 2296
		[__DynamicallyInvokable]
		public new static readonly StreamReader Null = new StreamReader.NullStreamReader();

		// Token: 0x040008F9 RID: 2297
		private const int DefaultFileStreamBufferSize = 4096;

		// Token: 0x040008FA RID: 2298
		private const int MinBufferSize = 128;

		// Token: 0x040008FB RID: 2299
		private Stream stream;

		// Token: 0x040008FC RID: 2300
		private Encoding encoding;

		// Token: 0x040008FD RID: 2301
		private Decoder decoder;

		// Token: 0x040008FE RID: 2302
		private byte[] byteBuffer;

		// Token: 0x040008FF RID: 2303
		private char[] charBuffer;

		// Token: 0x04000900 RID: 2304
		private byte[] _preamble;

		// Token: 0x04000901 RID: 2305
		private int charPos;

		// Token: 0x04000902 RID: 2306
		private int charLen;

		// Token: 0x04000903 RID: 2307
		private int byteLen;

		// Token: 0x04000904 RID: 2308
		private int bytePos;

		// Token: 0x04000905 RID: 2309
		private int _maxCharsPerBuffer;

		// Token: 0x04000906 RID: 2310
		private bool _detectEncoding;

		// Token: 0x04000907 RID: 2311
		private bool _checkPreamble;

		// Token: 0x04000908 RID: 2312
		private bool _isBlocked;

		// Token: 0x04000909 RID: 2313
		private bool _closable;

		// Token: 0x0400090A RID: 2314
		[NonSerialized]
		private volatile Task _asyncReadTask;

		// Token: 0x02000B19 RID: 2841
		private class NullStreamReader : StreamReader
		{
			// Token: 0x06006AE7 RID: 27367 RVA: 0x00171662 File Offset: 0x0016F862
			internal NullStreamReader()
			{
				base.Init(Stream.Null);
			}

			// Token: 0x17001218 RID: 4632
			// (get) Token: 0x06006AE8 RID: 27368 RVA: 0x00171675 File Offset: 0x0016F875
			public override Stream BaseStream
			{
				get
				{
					return Stream.Null;
				}
			}

			// Token: 0x17001219 RID: 4633
			// (get) Token: 0x06006AE9 RID: 27369 RVA: 0x0017167C File Offset: 0x0016F87C
			public override Encoding CurrentEncoding
			{
				get
				{
					return Encoding.Unicode;
				}
			}

			// Token: 0x06006AEA RID: 27370 RVA: 0x00171683 File Offset: 0x0016F883
			protected override void Dispose(bool disposing)
			{
			}

			// Token: 0x06006AEB RID: 27371 RVA: 0x00171685 File Offset: 0x0016F885
			public override int Peek()
			{
				return -1;
			}

			// Token: 0x06006AEC RID: 27372 RVA: 0x00171688 File Offset: 0x0016F888
			public override int Read()
			{
				return -1;
			}

			// Token: 0x06006AED RID: 27373 RVA: 0x0017168B File Offset: 0x0016F88B
			public override int Read(char[] buffer, int index, int count)
			{
				return 0;
			}

			// Token: 0x06006AEE RID: 27374 RVA: 0x0017168E File Offset: 0x0016F88E
			public override string ReadLine()
			{
				return null;
			}

			// Token: 0x06006AEF RID: 27375 RVA: 0x00171691 File Offset: 0x0016F891
			public override string ReadToEnd()
			{
				return string.Empty;
			}

			// Token: 0x06006AF0 RID: 27376 RVA: 0x00171698 File Offset: 0x0016F898
			internal override int ReadBuffer()
			{
				return 0;
			}
		}
	}
}
