using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Security.Cryptography
{
	// Token: 0x02000258 RID: 600
	[ComVisible(true)]
	public class CryptoStream : Stream, IDisposable
	{
		// Token: 0x0600213E RID: 8510 RVA: 0x0007542B File Offset: 0x0007362B
		public CryptoStream(Stream stream, ICryptoTransform transform, CryptoStreamMode mode)
			: this(stream, transform, mode, false)
		{
		}

		// Token: 0x0600213F RID: 8511 RVA: 0x00075438 File Offset: 0x00073638
		public CryptoStream(Stream stream, ICryptoTransform transform, CryptoStreamMode mode, bool leaveOpen)
		{
			this._stream = stream;
			this._transformMode = mode;
			this._Transform = transform;
			this._leaveOpen = leaveOpen;
			CryptoStreamMode transformMode = this._transformMode;
			if (transformMode != CryptoStreamMode.Read)
			{
				if (transformMode != CryptoStreamMode.Write)
				{
					throw new ArgumentException(Environment.GetResourceString("Argument_InvalidValue"));
				}
				if (!this._stream.CanWrite)
				{
					throw new ArgumentException(Environment.GetResourceString("Argument_StreamNotWritable"), "stream");
				}
				this._canWrite = true;
			}
			else
			{
				if (!this._stream.CanRead)
				{
					throw new ArgumentException(Environment.GetResourceString("Argument_StreamNotReadable"), "stream");
				}
				this._canRead = true;
			}
			this.InitializeBuffer();
		}

		// Token: 0x1700040B RID: 1035
		// (get) Token: 0x06002140 RID: 8512 RVA: 0x000754E4 File Offset: 0x000736E4
		public override bool CanRead
		{
			get
			{
				return this._canRead;
			}
		}

		// Token: 0x1700040C RID: 1036
		// (get) Token: 0x06002141 RID: 8513 RVA: 0x000754EC File Offset: 0x000736EC
		public override bool CanSeek
		{
			get
			{
				return false;
			}
		}

		// Token: 0x1700040D RID: 1037
		// (get) Token: 0x06002142 RID: 8514 RVA: 0x000754EF File Offset: 0x000736EF
		public override bool CanWrite
		{
			get
			{
				return this._canWrite;
			}
		}

		// Token: 0x1700040E RID: 1038
		// (get) Token: 0x06002143 RID: 8515 RVA: 0x000754F7 File Offset: 0x000736F7
		public override long Length
		{
			get
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_UnseekableStream"));
			}
		}

		// Token: 0x1700040F RID: 1039
		// (get) Token: 0x06002144 RID: 8516 RVA: 0x00075508 File Offset: 0x00073708
		// (set) Token: 0x06002145 RID: 8517 RVA: 0x00075519 File Offset: 0x00073719
		public override long Position
		{
			get
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_UnseekableStream"));
			}
			set
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_UnseekableStream"));
			}
		}

		// Token: 0x17000410 RID: 1040
		// (get) Token: 0x06002146 RID: 8518 RVA: 0x0007552A File Offset: 0x0007372A
		public bool HasFlushedFinalBlock
		{
			get
			{
				return this._finalBlockTransformed;
			}
		}

		// Token: 0x06002147 RID: 8519 RVA: 0x00075534 File Offset: 0x00073734
		public void FlushFinalBlock()
		{
			if (this._finalBlockTransformed)
			{
				throw new NotSupportedException(Environment.GetResourceString("Cryptography_CryptoStream_FlushFinalBlockTwice"));
			}
			byte[] array = this._Transform.TransformFinalBlock(this._InputBuffer, 0, this._InputBufferIndex);
			this._finalBlockTransformed = true;
			if (this._canWrite && this._OutputBufferIndex > 0)
			{
				this._stream.Write(this._OutputBuffer, 0, this._OutputBufferIndex);
				this._OutputBufferIndex = 0;
			}
			if (this._canWrite)
			{
				this._stream.Write(array, 0, array.Length);
			}
			CryptoStream cryptoStream = this._stream as CryptoStream;
			if (cryptoStream != null)
			{
				if (!cryptoStream.HasFlushedFinalBlock)
				{
					cryptoStream.FlushFinalBlock();
				}
			}
			else
			{
				this._stream.Flush();
			}
			if (this._InputBuffer != null)
			{
				Array.Clear(this._InputBuffer, 0, this._InputBuffer.Length);
			}
			if (this._OutputBuffer != null)
			{
				Array.Clear(this._OutputBuffer, 0, this._OutputBuffer.Length);
			}
		}

		// Token: 0x06002148 RID: 8520 RVA: 0x00075623 File Offset: 0x00073823
		public override void Flush()
		{
		}

		// Token: 0x06002149 RID: 8521 RVA: 0x00075625 File Offset: 0x00073825
		public override Task FlushAsync(CancellationToken cancellationToken)
		{
			if (base.GetType() != typeof(CryptoStream))
			{
				return base.FlushAsync(cancellationToken);
			}
			if (!cancellationToken.IsCancellationRequested)
			{
				return Task.CompletedTask;
			}
			return Task.FromCancellation(cancellationToken);
		}

		// Token: 0x0600214A RID: 8522 RVA: 0x0007565B File Offset: 0x0007385B
		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_UnseekableStream"));
		}

		// Token: 0x0600214B RID: 8523 RVA: 0x0007566C File Offset: 0x0007386C
		public override void SetLength(long value)
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_UnseekableStream"));
		}

		// Token: 0x0600214C RID: 8524 RVA: 0x00075680 File Offset: 0x00073880
		public override int Read([In] [Out] byte[] buffer, int offset, int count)
		{
			if (!this.CanRead)
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_UnreadableStream"));
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
			int i = count;
			int num = offset;
			if (this._OutputBufferIndex != 0)
			{
				if (this._OutputBufferIndex > count)
				{
					Buffer.InternalBlockCopy(this._OutputBuffer, 0, buffer, offset, count);
					Buffer.InternalBlockCopy(this._OutputBuffer, count, this._OutputBuffer, 0, this._OutputBufferIndex - count);
					this._OutputBufferIndex -= count;
					return count;
				}
				Buffer.InternalBlockCopy(this._OutputBuffer, 0, buffer, offset, this._OutputBufferIndex);
				i -= this._OutputBufferIndex;
				num += this._OutputBufferIndex;
				this._OutputBufferIndex = 0;
			}
			if (this._finalBlockTransformed)
			{
				return count - i;
			}
			if (i > this._OutputBlockSize && this._Transform.CanTransformMultipleBlocks)
			{
				int num2 = i / this._OutputBlockSize;
				int num3 = num2 * this._InputBlockSize;
				byte[] array = new byte[num3];
				Buffer.InternalBlockCopy(this._InputBuffer, 0, array, 0, this._InputBufferIndex);
				int num4 = this._InputBufferIndex;
				num4 += this._stream.Read(array, this._InputBufferIndex, num3 - this._InputBufferIndex);
				this._InputBufferIndex = 0;
				if (num4 <= this._InputBlockSize)
				{
					this._InputBuffer = array;
					this._InputBufferIndex = num4;
				}
				else
				{
					int num5 = num4 / this._InputBlockSize * this._InputBlockSize;
					int num6 = num4 - num5;
					if (num6 != 0)
					{
						this._InputBufferIndex = num6;
						Buffer.InternalBlockCopy(array, num5, this._InputBuffer, 0, num6);
					}
					byte[] array2 = new byte[num5 / this._InputBlockSize * this._OutputBlockSize];
					int num7 = this._Transform.TransformBlock(array, 0, num5, array2, 0);
					Buffer.InternalBlockCopy(array2, 0, buffer, num, num7);
					Array.Clear(array, 0, array.Length);
					Array.Clear(array2, 0, array2.Length);
					i -= num7;
					num += num7;
				}
			}
			while (i > 0)
			{
				while (this._InputBufferIndex < this._InputBlockSize)
				{
					int num4 = this._stream.Read(this._InputBuffer, this._InputBufferIndex, this._InputBlockSize - this._InputBufferIndex);
					if (num4 != 0)
					{
						this._InputBufferIndex += num4;
					}
					else
					{
						byte[] array3 = this._Transform.TransformFinalBlock(this._InputBuffer, 0, this._InputBufferIndex);
						this._OutputBuffer = array3;
						this._OutputBufferIndex = array3.Length;
						this._finalBlockTransformed = true;
						if (i < this._OutputBufferIndex)
						{
							Buffer.InternalBlockCopy(this._OutputBuffer, 0, buffer, num, i);
							this._OutputBufferIndex -= i;
							Buffer.InternalBlockCopy(this._OutputBuffer, i, this._OutputBuffer, 0, this._OutputBufferIndex);
							return count;
						}
						Buffer.InternalBlockCopy(this._OutputBuffer, 0, buffer, num, this._OutputBufferIndex);
						i -= this._OutputBufferIndex;
						this._OutputBufferIndex = 0;
						return count - i;
					}
				}
				int num7 = this._Transform.TransformBlock(this._InputBuffer, 0, this._InputBlockSize, this._OutputBuffer, 0);
				this._InputBufferIndex = 0;
				if (i < num7)
				{
					Buffer.InternalBlockCopy(this._OutputBuffer, 0, buffer, num, i);
					this._OutputBufferIndex = num7 - i;
					Buffer.InternalBlockCopy(this._OutputBuffer, i, this._OutputBuffer, 0, this._OutputBufferIndex);
					return count;
				}
				Buffer.InternalBlockCopy(this._OutputBuffer, 0, buffer, num, num7);
				num += num7;
				i -= num7;
			}
			return count;
		}

		// Token: 0x0600214D RID: 8525 RVA: 0x00075A08 File Offset: 0x00073C08
		public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			if (!this.CanRead)
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_UnreadableStream"));
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
			if (base.GetType() != typeof(CryptoStream))
			{
				return base.ReadAsync(buffer, offset, count, cancellationToken);
			}
			if (cancellationToken.IsCancellationRequested)
			{
				return Task.FromCancellation<int>(cancellationToken);
			}
			return this.ReadAsyncInternal(buffer, offset, count, cancellationToken);
		}

		// Token: 0x0600214E RID: 8526 RVA: 0x00075AB8 File Offset: 0x00073CB8
		private Task<int> ReadAsyncInternal(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			CryptoStream.<ReadAsyncInternal>d__36 <ReadAsyncInternal>d__;
			<ReadAsyncInternal>d__.<>t__builder = AsyncTaskMethodBuilder<int>.Create();
			<ReadAsyncInternal>d__.<>4__this = this;
			<ReadAsyncInternal>d__.buffer = buffer;
			<ReadAsyncInternal>d__.offset = offset;
			<ReadAsyncInternal>d__.count = count;
			<ReadAsyncInternal>d__.cancellationToken = cancellationToken;
			<ReadAsyncInternal>d__.<>1__state = -1;
			<ReadAsyncInternal>d__.<>t__builder.Start<CryptoStream.<ReadAsyncInternal>d__36>(ref <ReadAsyncInternal>d__);
			return <ReadAsyncInternal>d__.<>t__builder.Task;
		}

		// Token: 0x0600214F RID: 8527 RVA: 0x00075B1C File Offset: 0x00073D1C
		public override void Write(byte[] buffer, int offset, int count)
		{
			if (!this.CanWrite)
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_UnwritableStream"));
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
			int i = count;
			int num = offset;
			if (this._InputBufferIndex > 0)
			{
				if (count < this._InputBlockSize - this._InputBufferIndex)
				{
					Buffer.InternalBlockCopy(buffer, offset, this._InputBuffer, this._InputBufferIndex, count);
					this._InputBufferIndex += count;
					return;
				}
				Buffer.InternalBlockCopy(buffer, offset, this._InputBuffer, this._InputBufferIndex, this._InputBlockSize - this._InputBufferIndex);
				num += this._InputBlockSize - this._InputBufferIndex;
				i -= this._InputBlockSize - this._InputBufferIndex;
				this._InputBufferIndex = this._InputBlockSize;
			}
			if (this._OutputBufferIndex > 0)
			{
				this._stream.Write(this._OutputBuffer, 0, this._OutputBufferIndex);
				this._OutputBufferIndex = 0;
			}
			if (this._InputBufferIndex == this._InputBlockSize)
			{
				int count2 = this._Transform.TransformBlock(this._InputBuffer, 0, this._InputBlockSize, this._OutputBuffer, 0);
				this._stream.Write(this._OutputBuffer, 0, count2);
				this._InputBufferIndex = 0;
			}
			while (i > 0)
			{
				if (i < this._InputBlockSize)
				{
					Buffer.InternalBlockCopy(buffer, num, this._InputBuffer, 0, i);
					this._InputBufferIndex += i;
					return;
				}
				if (this._Transform.CanTransformMultipleBlocks)
				{
					int num2 = i / this._InputBlockSize;
					int num3 = num2 * this._InputBlockSize;
					byte[] array = new byte[num2 * this._OutputBlockSize];
					int count2 = this._Transform.TransformBlock(buffer, num, num3, array, 0);
					this._stream.Write(array, 0, count2);
					num += num3;
					i -= num3;
				}
				else
				{
					int count2 = this._Transform.TransformBlock(buffer, num, this._InputBlockSize, this._OutputBuffer, 0);
					this._stream.Write(this._OutputBuffer, 0, count2);
					num += this._InputBlockSize;
					i -= this._InputBlockSize;
				}
			}
		}

		// Token: 0x06002150 RID: 8528 RVA: 0x00075D64 File Offset: 0x00073F64
		public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			if (!this.CanWrite)
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_UnwritableStream"));
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
			if (base.GetType() != typeof(CryptoStream))
			{
				return base.WriteAsync(buffer, offset, count, cancellationToken);
			}
			if (cancellationToken.IsCancellationRequested)
			{
				return Task.FromCancellation(cancellationToken);
			}
			return this.WriteAsyncInternal(buffer, offset, count, cancellationToken);
		}

		// Token: 0x06002151 RID: 8529 RVA: 0x00075E14 File Offset: 0x00074014
		private Task WriteAsyncInternal(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			CryptoStream.<WriteAsyncInternal>d__39 <WriteAsyncInternal>d__;
			<WriteAsyncInternal>d__.<>t__builder = AsyncTaskMethodBuilder.Create();
			<WriteAsyncInternal>d__.<>4__this = this;
			<WriteAsyncInternal>d__.buffer = buffer;
			<WriteAsyncInternal>d__.offset = offset;
			<WriteAsyncInternal>d__.count = count;
			<WriteAsyncInternal>d__.cancellationToken = cancellationToken;
			<WriteAsyncInternal>d__.<>1__state = -1;
			<WriteAsyncInternal>d__.<>t__builder.Start<CryptoStream.<WriteAsyncInternal>d__39>(ref <WriteAsyncInternal>d__);
			return <WriteAsyncInternal>d__.<>t__builder.Task;
		}

		// Token: 0x06002152 RID: 8530 RVA: 0x00075E78 File Offset: 0x00074078
		public void Clear()
		{
			this.Close();
		}

		// Token: 0x06002153 RID: 8531 RVA: 0x00075E80 File Offset: 0x00074080
		protected override void Dispose(bool disposing)
		{
			try
			{
				if (disposing)
				{
					if (!this._finalBlockTransformed)
					{
						this.FlushFinalBlock();
					}
					if (!this._leaveOpen)
					{
						this._stream.Close();
					}
				}
			}
			finally
			{
				try
				{
					this._finalBlockTransformed = true;
					if (this._InputBuffer != null)
					{
						Array.Clear(this._InputBuffer, 0, this._InputBuffer.Length);
					}
					if (this._OutputBuffer != null)
					{
						Array.Clear(this._OutputBuffer, 0, this._OutputBuffer.Length);
					}
					this._InputBuffer = null;
					this._OutputBuffer = null;
					this._canRead = false;
					this._canWrite = false;
				}
				finally
				{
					base.Dispose(disposing);
				}
			}
		}

		// Token: 0x06002154 RID: 8532 RVA: 0x00075F38 File Offset: 0x00074138
		private void InitializeBuffer()
		{
			if (this._Transform != null)
			{
				this._InputBlockSize = this._Transform.InputBlockSize;
				this._InputBuffer = new byte[this._InputBlockSize];
				this._OutputBlockSize = this._Transform.OutputBlockSize;
				this._OutputBuffer = new byte[this._OutputBlockSize];
			}
		}

		// Token: 0x04000C21 RID: 3105
		private Stream _stream;

		// Token: 0x04000C22 RID: 3106
		private ICryptoTransform _Transform;

		// Token: 0x04000C23 RID: 3107
		private byte[] _InputBuffer;

		// Token: 0x04000C24 RID: 3108
		private int _InputBufferIndex;

		// Token: 0x04000C25 RID: 3109
		private int _InputBlockSize;

		// Token: 0x04000C26 RID: 3110
		private byte[] _OutputBuffer;

		// Token: 0x04000C27 RID: 3111
		private int _OutputBufferIndex;

		// Token: 0x04000C28 RID: 3112
		private int _OutputBlockSize;

		// Token: 0x04000C29 RID: 3113
		private CryptoStreamMode _transformMode;

		// Token: 0x04000C2A RID: 3114
		private bool _canRead;

		// Token: 0x04000C2B RID: 3115
		private bool _canWrite;

		// Token: 0x04000C2C RID: 3116
		private bool _finalBlockTransformed;

		// Token: 0x04000C2D RID: 3117
		private bool _leaveOpen;

		// Token: 0x02000B43 RID: 2883
		private struct HopToThreadPoolAwaitable : INotifyCompletion
		{
			// Token: 0x06006B8A RID: 27530 RVA: 0x00173AF6 File Offset: 0x00171CF6
			public CryptoStream.HopToThreadPoolAwaitable GetAwaiter()
			{
				return this;
			}

			// Token: 0x17001224 RID: 4644
			// (get) Token: 0x06006B8B RID: 27531 RVA: 0x00173AFE File Offset: 0x00171CFE
			public bool IsCompleted
			{
				get
				{
					return false;
				}
			}

			// Token: 0x06006B8C RID: 27532 RVA: 0x00173B01 File Offset: 0x00171D01
			public void OnCompleted(Action continuation)
			{
				Task.Run(continuation);
			}

			// Token: 0x06006B8D RID: 27533 RVA: 0x00173B0A File Offset: 0x00171D0A
			public void GetResult()
			{
			}
		}
	}
}
