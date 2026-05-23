using System;
using System.IO;

namespace System.Security.Cryptography
{
	// Token: 0x02000270 RID: 624
	internal sealed class TailStream : Stream
	{
		// Token: 0x06002218 RID: 8728 RVA: 0x00078AAF File Offset: 0x00076CAF
		public TailStream(int bufferSize)
		{
			this._Buffer = new byte[bufferSize];
			this._BufferSize = bufferSize;
		}

		// Token: 0x06002219 RID: 8729 RVA: 0x00078ACA File Offset: 0x00076CCA
		public void Clear()
		{
			this.Close();
		}

		// Token: 0x0600221A RID: 8730 RVA: 0x00078AD4 File Offset: 0x00076CD4
		protected override void Dispose(bool disposing)
		{
			try
			{
				if (disposing)
				{
					if (this._Buffer != null)
					{
						Array.Clear(this._Buffer, 0, this._Buffer.Length);
					}
					this._Buffer = null;
				}
			}
			finally
			{
				base.Dispose(disposing);
			}
		}

		// Token: 0x1700043C RID: 1084
		// (get) Token: 0x0600221B RID: 8731 RVA: 0x00078B24 File Offset: 0x00076D24
		public byte[] Buffer
		{
			get
			{
				return (byte[])this._Buffer.Clone();
			}
		}

		// Token: 0x1700043D RID: 1085
		// (get) Token: 0x0600221C RID: 8732 RVA: 0x00078B36 File Offset: 0x00076D36
		public override bool CanRead
		{
			get
			{
				return false;
			}
		}

		// Token: 0x1700043E RID: 1086
		// (get) Token: 0x0600221D RID: 8733 RVA: 0x00078B39 File Offset: 0x00076D39
		public override bool CanSeek
		{
			get
			{
				return false;
			}
		}

		// Token: 0x1700043F RID: 1087
		// (get) Token: 0x0600221E RID: 8734 RVA: 0x00078B3C File Offset: 0x00076D3C
		public override bool CanWrite
		{
			get
			{
				return this._Buffer != null;
			}
		}

		// Token: 0x17000440 RID: 1088
		// (get) Token: 0x0600221F RID: 8735 RVA: 0x00078B47 File Offset: 0x00076D47
		public override long Length
		{
			get
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_UnseekableStream"));
			}
		}

		// Token: 0x17000441 RID: 1089
		// (get) Token: 0x06002220 RID: 8736 RVA: 0x00078B58 File Offset: 0x00076D58
		// (set) Token: 0x06002221 RID: 8737 RVA: 0x00078B69 File Offset: 0x00076D69
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

		// Token: 0x06002222 RID: 8738 RVA: 0x00078B7A File Offset: 0x00076D7A
		public override void Flush()
		{
		}

		// Token: 0x06002223 RID: 8739 RVA: 0x00078B7C File Offset: 0x00076D7C
		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_UnseekableStream"));
		}

		// Token: 0x06002224 RID: 8740 RVA: 0x00078B8D File Offset: 0x00076D8D
		public override void SetLength(long value)
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_UnseekableStream"));
		}

		// Token: 0x06002225 RID: 8741 RVA: 0x00078B9E File Offset: 0x00076D9E
		public override int Read(byte[] buffer, int offset, int count)
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_UnreadableStream"));
		}

		// Token: 0x06002226 RID: 8742 RVA: 0x00078BB0 File Offset: 0x00076DB0
		public override void Write(byte[] buffer, int offset, int count)
		{
			if (this._Buffer == null)
			{
				throw new ObjectDisposedException("TailStream");
			}
			if (count == 0)
			{
				return;
			}
			if (this._BufferFull)
			{
				if (count > this._BufferSize)
				{
					System.Buffer.InternalBlockCopy(buffer, offset + count - this._BufferSize, this._Buffer, 0, this._BufferSize);
					return;
				}
				System.Buffer.InternalBlockCopy(this._Buffer, this._BufferSize - count, this._Buffer, 0, this._BufferSize - count);
				System.Buffer.InternalBlockCopy(buffer, offset, this._Buffer, this._BufferSize - count, count);
				return;
			}
			else
			{
				if (count > this._BufferSize)
				{
					System.Buffer.InternalBlockCopy(buffer, offset + count - this._BufferSize, this._Buffer, 0, this._BufferSize);
					this._BufferFull = true;
					return;
				}
				if (count + this._BufferIndex >= this._BufferSize)
				{
					System.Buffer.InternalBlockCopy(this._Buffer, this._BufferIndex + count - this._BufferSize, this._Buffer, 0, this._BufferSize - count);
					System.Buffer.InternalBlockCopy(buffer, offset, this._Buffer, this._BufferIndex, count);
					this._BufferFull = true;
					return;
				}
				System.Buffer.InternalBlockCopy(buffer, offset, this._Buffer, this._BufferIndex, count);
				this._BufferIndex += count;
				return;
			}
		}

		// Token: 0x04000C66 RID: 3174
		private byte[] _Buffer;

		// Token: 0x04000C67 RID: 3175
		private int _BufferSize;

		// Token: 0x04000C68 RID: 3176
		private int _BufferIndex;

		// Token: 0x04000C69 RID: 3177
		private bool _BufferFull;
	}
}
