using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO
{
	// Token: 0x020001AA RID: 426
	internal sealed class UnmanagedMemoryStreamWrapper : MemoryStream
	{
		// Token: 0x06001AC3 RID: 6851 RVA: 0x0005A2E4 File Offset: 0x000584E4
		internal UnmanagedMemoryStreamWrapper(UnmanagedMemoryStream stream)
		{
			this._unmanagedStream = stream;
		}

		// Token: 0x170002FB RID: 763
		// (get) Token: 0x06001AC4 RID: 6852 RVA: 0x0005A2F3 File Offset: 0x000584F3
		public override bool CanRead
		{
			get
			{
				return this._unmanagedStream.CanRead;
			}
		}

		// Token: 0x170002FC RID: 764
		// (get) Token: 0x06001AC5 RID: 6853 RVA: 0x0005A300 File Offset: 0x00058500
		public override bool CanSeek
		{
			get
			{
				return this._unmanagedStream.CanSeek;
			}
		}

		// Token: 0x170002FD RID: 765
		// (get) Token: 0x06001AC6 RID: 6854 RVA: 0x0005A30D File Offset: 0x0005850D
		public override bool CanWrite
		{
			get
			{
				return this._unmanagedStream.CanWrite;
			}
		}

		// Token: 0x06001AC7 RID: 6855 RVA: 0x0005A31C File Offset: 0x0005851C
		protected override void Dispose(bool disposing)
		{
			try
			{
				if (disposing)
				{
					this._unmanagedStream.Close();
				}
			}
			finally
			{
				base.Dispose(disposing);
			}
		}

		// Token: 0x06001AC8 RID: 6856 RVA: 0x0005A354 File Offset: 0x00058554
		public override void Flush()
		{
			this._unmanagedStream.Flush();
		}

		// Token: 0x06001AC9 RID: 6857 RVA: 0x0005A361 File Offset: 0x00058561
		public override byte[] GetBuffer()
		{
			throw new UnauthorizedAccessException(Environment.GetResourceString("UnauthorizedAccess_MemStreamBuffer"));
		}

		// Token: 0x06001ACA RID: 6858 RVA: 0x0005A372 File Offset: 0x00058572
		public override bool TryGetBuffer(out ArraySegment<byte> buffer)
		{
			buffer = default(ArraySegment<byte>);
			return false;
		}

		// Token: 0x170002FE RID: 766
		// (get) Token: 0x06001ACB RID: 6859 RVA: 0x0005A37C File Offset: 0x0005857C
		// (set) Token: 0x06001ACC RID: 6860 RVA: 0x0005A38A File Offset: 0x0005858A
		public override int Capacity
		{
			get
			{
				return (int)this._unmanagedStream.Capacity;
			}
			set
			{
				throw new IOException(Environment.GetResourceString("IO.IO_FixedCapacity"));
			}
		}

		// Token: 0x170002FF RID: 767
		// (get) Token: 0x06001ACD RID: 6861 RVA: 0x0005A39B File Offset: 0x0005859B
		public override long Length
		{
			get
			{
				return this._unmanagedStream.Length;
			}
		}

		// Token: 0x17000300 RID: 768
		// (get) Token: 0x06001ACE RID: 6862 RVA: 0x0005A3A8 File Offset: 0x000585A8
		// (set) Token: 0x06001ACF RID: 6863 RVA: 0x0005A3B5 File Offset: 0x000585B5
		public override long Position
		{
			get
			{
				return this._unmanagedStream.Position;
			}
			set
			{
				this._unmanagedStream.Position = value;
			}
		}

		// Token: 0x06001AD0 RID: 6864 RVA: 0x0005A3C3 File Offset: 0x000585C3
		public override int Read([In] [Out] byte[] buffer, int offset, int count)
		{
			return this._unmanagedStream.Read(buffer, offset, count);
		}

		// Token: 0x06001AD1 RID: 6865 RVA: 0x0005A3D3 File Offset: 0x000585D3
		public override int ReadByte()
		{
			return this._unmanagedStream.ReadByte();
		}

		// Token: 0x06001AD2 RID: 6866 RVA: 0x0005A3E0 File Offset: 0x000585E0
		public override long Seek(long offset, SeekOrigin loc)
		{
			return this._unmanagedStream.Seek(offset, loc);
		}

		// Token: 0x06001AD3 RID: 6867 RVA: 0x0005A3F0 File Offset: 0x000585F0
		[SecuritySafeCritical]
		public override byte[] ToArray()
		{
			if (!this._unmanagedStream._isOpen)
			{
				__Error.StreamIsClosed();
			}
			if (!this._unmanagedStream.CanRead)
			{
				__Error.ReadNotSupported();
			}
			byte[] array = new byte[this._unmanagedStream.Length];
			Buffer.Memcpy(array, 0, this._unmanagedStream.Pointer, 0, (int)this._unmanagedStream.Length);
			return array;
		}

		// Token: 0x06001AD4 RID: 6868 RVA: 0x0005A453 File Offset: 0x00058653
		public override void Write(byte[] buffer, int offset, int count)
		{
			this._unmanagedStream.Write(buffer, offset, count);
		}

		// Token: 0x06001AD5 RID: 6869 RVA: 0x0005A463 File Offset: 0x00058663
		public override void WriteByte(byte value)
		{
			this._unmanagedStream.WriteByte(value);
		}

		// Token: 0x06001AD6 RID: 6870 RVA: 0x0005A474 File Offset: 0x00058674
		public override void WriteTo(Stream stream)
		{
			if (stream == null)
			{
				throw new ArgumentNullException("stream", Environment.GetResourceString("ArgumentNull_Stream"));
			}
			if (!this._unmanagedStream._isOpen)
			{
				__Error.StreamIsClosed();
			}
			if (!this.CanRead)
			{
				__Error.ReadNotSupported();
			}
			byte[] array = this.ToArray();
			stream.Write(array, 0, array.Length);
		}

		// Token: 0x06001AD7 RID: 6871 RVA: 0x0005A4CA File Offset: 0x000586CA
		public override void SetLength(long value)
		{
			base.SetLength(value);
		}

		// Token: 0x06001AD8 RID: 6872 RVA: 0x0005A4D4 File Offset: 0x000586D4
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
			return this._unmanagedStream.CopyToAsync(destination, bufferSize, cancellationToken);
		}

		// Token: 0x06001AD9 RID: 6873 RVA: 0x0005A58C File Offset: 0x0005878C
		public override Task FlushAsync(CancellationToken cancellationToken)
		{
			return this._unmanagedStream.FlushAsync(cancellationToken);
		}

		// Token: 0x06001ADA RID: 6874 RVA: 0x0005A59A File Offset: 0x0005879A
		public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			return this._unmanagedStream.ReadAsync(buffer, offset, count, cancellationToken);
		}

		// Token: 0x06001ADB RID: 6875 RVA: 0x0005A5AC File Offset: 0x000587AC
		public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			return this._unmanagedStream.WriteAsync(buffer, offset, count, cancellationToken);
		}

		// Token: 0x04000942 RID: 2370
		private UnmanagedMemoryStream _unmanagedStream;
	}
}
