using System;

namespace StbSharp
{
	// Token: 0x02000004 RID: 4
	public class Image
	{
		// Token: 0x17000003 RID: 3
		// (get) Token: 0x0600000B RID: 11 RVA: 0x000022F7 File Offset: 0x000004F7
		// (set) Token: 0x0600000C RID: 12 RVA: 0x000022FF File Offset: 0x000004FF
		public int Width { get; set; }

		// Token: 0x17000004 RID: 4
		// (get) Token: 0x0600000D RID: 13 RVA: 0x00002308 File Offset: 0x00000508
		// (set) Token: 0x0600000E RID: 14 RVA: 0x00002310 File Offset: 0x00000510
		public int Height { get; set; }

		// Token: 0x17000005 RID: 5
		// (get) Token: 0x0600000F RID: 15 RVA: 0x00002319 File Offset: 0x00000519
		// (set) Token: 0x06000010 RID: 16 RVA: 0x00002321 File Offset: 0x00000521
		public int SourceComp { get; set; }

		// Token: 0x17000006 RID: 6
		// (get) Token: 0x06000011 RID: 17 RVA: 0x0000232A File Offset: 0x0000052A
		// (set) Token: 0x06000012 RID: 18 RVA: 0x00002332 File Offset: 0x00000532
		public int Comp { get; set; }

		// Token: 0x17000007 RID: 7
		// (get) Token: 0x06000013 RID: 19 RVA: 0x0000233B File Offset: 0x0000053B
		// (set) Token: 0x06000014 RID: 20 RVA: 0x00002343 File Offset: 0x00000543
		public byte[] Data { get; set; }

		// Token: 0x06000015 RID: 21 RVA: 0x0000234C File Offset: 0x0000054C
		public unsafe Image CreateResized(int newWidth, int newHeight)
		{
			Image image = new Image();
			image.Comp = this.Comp;
			image.SourceComp = this.SourceComp;
			image.Data = new byte[newWidth * newHeight * this.Comp];
			image.Width = newWidth;
			image.Height = newHeight;
			byte[] array;
			byte* input_pixels;
			if ((array = this.Data) == null || array.Length == 0)
			{
				input_pixels = null;
			}
			else
			{
				input_pixels = &array[0];
			}
			byte[] array2;
			byte* output_pixels;
			if ((array2 = image.Data) == null || array2.Length == 0)
			{
				output_pixels = null;
			}
			else
			{
				output_pixels = &array2[0];
			}
			StbImageResize.stbir_resize_uint8(input_pixels, this.Width, this.Height, this.Width * this.Comp, output_pixels, newWidth, newHeight, newWidth * this.Comp, this.Comp);
			array2 = null;
			array = null;
			return image;
		}
	}
}
