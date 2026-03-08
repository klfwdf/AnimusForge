using System;
using System.IO;
using System.Runtime.InteropServices;

namespace StbSharp
{
	// Token: 0x02000009 RID: 9
	public class ImageWriter
	{
		// Token: 0x06000097 RID: 151 RVA: 0x00009334 File Offset: 0x00007534
		private unsafe int WriteCallback(void* context, void* data, int size)
		{
			if (data == null || size <= 0)
			{
				return 0;
			}
			if (this._buffer.Length < size)
			{
				this._buffer = new byte[size * 2];
			}
			Marshal.Copy(new IntPtr(data), this._buffer, 0, size);
			this._stream.Write(this._buffer, 0, size);
			return size;
		}

		// Token: 0x06000098 RID: 152 RVA: 0x00009390 File Offset: 0x00007590
		public unsafe void WriteBmp(Image image, Stream dest)
		{
			try
			{
				this._stream = dest;
				try
				{
					fixed (byte* ptr = &image.Data[0])
					{
						byte* data = ptr;
						StbImageWrite.stbi_write_bmp_to_func(new StbImageWrite.WriteCallback(this.WriteCallback), null, image.Width, image.Height, image.Comp, (void*)data);
					}
				}
				finally
				{
					byte* ptr = null;
				}
			}
			finally
			{
				this._stream = null;
			}
		}

		// Token: 0x06000099 RID: 153 RVA: 0x00009408 File Offset: 0x00007608
		public unsafe void WriteTga(Image image, Stream dest)
		{
			try
			{
				this._stream = dest;
				try
				{
					fixed (byte* ptr = &image.Data[0])
					{
						byte* data = ptr;
						StbImageWrite.stbi_write_tga_to_func(new StbImageWrite.WriteCallback(this.WriteCallback), null, image.Width, image.Height, image.Comp, (void*)data);
					}
				}
				finally
				{
					byte* ptr = null;
				}
			}
			finally
			{
				this._stream = null;
			}
		}

		// Token: 0x0600009A RID: 154 RVA: 0x00009480 File Offset: 0x00007680
		public unsafe void WriteHdr(Image image, Stream dest)
		{
			try
			{
				this._stream = dest;
				float[] array = new float[image.Data.Length];
				for (int i = 0; i < image.Data.Length; i++)
				{
					array[i] = (float)image.Data[i] / 255f;
				}
				try
				{
					float[] array2;
					float* data;
					if ((array2 = array) == null || array2.Length == 0)
					{
						data = null;
					}
					else
					{
						data = &array2[0];
					}
					StbImageWrite.stbi_write_hdr_to_func(new StbImageWrite.WriteCallback(this.WriteCallback), null, image.Width, image.Height, image.Comp, data);
				}
				finally
				{
					float[] array2 = null;
				}
			}
			finally
			{
				this._stream = null;
			}
		}

		// Token: 0x0600009B RID: 155 RVA: 0x00009530 File Offset: 0x00007730
		public unsafe void WritePng(Image image, Stream dest)
		{
			try
			{
				this._stream = dest;
				try
				{
					fixed (byte* ptr = &image.Data[0])
					{
						byte* data = ptr;
						StbImageWrite.stbi_write_png_to_func(new StbImageWrite.WriteCallback(this.WriteCallback), null, image.Width, image.Height, image.Comp, (void*)data, image.Width * image.Comp);
					}
				}
				finally
				{
					byte* ptr = null;
				}
			}
			finally
			{
				this._stream = null;
			}
		}

		// Token: 0x0600009C RID: 156 RVA: 0x000095B4 File Offset: 0x000077B4
		public unsafe void WriteJpg(Image image, Stream dest, int quality)
		{
			try
			{
				this._stream = dest;
				try
				{
					fixed (byte* ptr = &image.Data[0])
					{
						byte* data = ptr;
						StbImageWrite.stbi_write_jpg_to_func(new StbImageWrite.WriteCallback(this.WriteCallback), null, image.Width, image.Height, image.Comp, (void*)data, quality);
					}
				}
				finally
				{
					byte* ptr = null;
				}
			}
			finally
			{
				this._stream = null;
			}
		}

		// Token: 0x0400004C RID: 76
		private Stream _stream;

		// Token: 0x0400004D RID: 77
		private byte[] _buffer = new byte[1024];
	}
}
