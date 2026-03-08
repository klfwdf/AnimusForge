using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace StbSharp
{
	// Token: 0x02000005 RID: 5
	public class ImageReader
	{
		// Token: 0x06000017 RID: 23 RVA: 0x00002410 File Offset: 0x00000610
		public ImageReader()
		{
			this._callbacks = new StbImage.stbi_io_callbacks
			{
				read = new StbImage.ReadCallback(this.ReadCallback),
				skip = new StbImage.SkipCallback(this.SkipCallback),
				eof = new StbImage.EofCallback(this.Eof)
			};
		}

		// Token: 0x06000018 RID: 24 RVA: 0x00002474 File Offset: 0x00000674
		private unsafe int SkipCallback(void* user, int i)
		{
			return (int)this._stream.Seek((long)i, SeekOrigin.Current);
		}

		// Token: 0x06000019 RID: 25 RVA: 0x00002485 File Offset: 0x00000685
		private unsafe int Eof(void* user)
		{
			if (!this._stream.CanRead)
			{
				return 0;
			}
			return 1;
		}

		// Token: 0x0600001A RID: 26 RVA: 0x00002498 File Offset: 0x00000698
		private unsafe int ReadCallback(void* user, sbyte* data, int size)
		{
			if (size > this._buffer.Length)
			{
				this._buffer = new byte[size * 2];
			}
			int result = this._stream.Read(this._buffer, 0, size);
			Marshal.Copy(this._buffer, 0, new IntPtr((void*)data), size);
			return result;
		}

		// Token: 0x0600001B RID: 27 RVA: 0x000024E4 File Offset: 0x000006E4
		public unsafe Image Read(Stream stream, int req_comp = 0)
		{
			this._stream = stream;
			Image result;
			try
			{
				int num;
				int num2;
				int num3;
				byte* ptr = StbImage.stbi_load_from_callbacks(this._callbacks, null, &num, &num2, &num3, req_comp);
				Image image = new Image
				{
					Width = num,
					Height = num2,
					SourceComp = num3,
					Comp = ((req_comp == 0) ? num3 : req_comp)
				};
				if (ptr == null)
				{
					throw new Exception(StbImage.LastError);
				}
				byte[] array = new byte[num * num2 * image.Comp];
				Marshal.Copy(new IntPtr((void*)ptr), array, 0, array.Length);
				CRuntime.free((void*)ptr);
				image.Data = array;
				result = image;
			}
			finally
			{
				this._stream = null;
			}
			return result;
		}

		// Token: 0x0600001C RID: 28 RVA: 0x00002598 File Offset: 0x00000798
		public unsafe ImageReader.AnimatedGifFrame[] ReadAnimatedGif(Stream stream, out int x, out int y, out int comp, int req_comp)
		{
			ImageReader.AnimatedGifFrame[] result;
			try
			{
				x = (y = (comp = 0));
				List<ImageReader.AnimatedGifFrame> list = new List<ImageReader.AnimatedGifFrame>();
				this._stream = stream;
				StbImage.stbi__context s = new StbImage.stbi__context();
				StbImage.stbi__start_callbacks(s, this._callbacks, null);
				if (StbImage.stbi__gif_test(s) == 0)
				{
					throw new Exception("Input stream is not GIF file.");
				}
				StbImage.stbi__gif stbi__gif = new StbImage.stbi__gif();
				for (;;)
				{
					int num;
					byte* ptr = StbImage.stbi__gif_load_next(s, stbi__gif, &num, req_comp);
					if (ptr == null)
					{
						break;
					}
					comp = num;
					int num2 = ((req_comp != 0) ? req_comp : comp);
					byte[] array = new byte[stbi__gif.w * stbi__gif.h * num2];
					Marshal.Copy(new IntPtr((void*)ptr), array, 0, array.Length);
					CRuntime.free((void*)ptr);
					ImageReader.AnimatedGifFrame item = new ImageReader.AnimatedGifFrame
					{
						Data = array,
						Delay = stbi__gif.delay
					};
					list.Add(item);
				}
				CRuntime.free((void*)stbi__gif._out_);
				if (list.Count > 0)
				{
					x = stbi__gif.w;
					y = stbi__gif.h;
				}
				result = list.ToArray();
			}
			finally
			{
				this._stream = null;
			}
			return result;
		}

		// Token: 0x04000014 RID: 20
		private Stream _stream;

		// Token: 0x04000015 RID: 21
		private byte[] _buffer = new byte[1024];

		// Token: 0x04000016 RID: 22
		private readonly StbImage.stbi_io_callbacks _callbacks;

		// Token: 0x0200000F RID: 15
		public class AnimatedGifFrame
		{
			// Token: 0x040000F4 RID: 244
			public byte[] Data;

			// Token: 0x040000F5 RID: 245
			public int Delay;
		}
	}
}
