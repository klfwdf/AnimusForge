using System;
using System.Numerics;
using TaleWorlds.Library;

namespace TaleWorlds.GauntletUI
{
	// Token: 0x0200002B RID: 43
	public class ImageFit
	{
		// Token: 0x170000F7 RID: 247
		// (get) Token: 0x0600033A RID: 826 RVA: 0x0000E905 File Offset: 0x0000CB05
		// (set) Token: 0x0600033B RID: 827 RVA: 0x0000E90D File Offset: 0x0000CB0D
		public ImageFit.ImageFitTypes Type { get; set; }

		// Token: 0x170000F8 RID: 248
		// (get) Token: 0x0600033C RID: 828 RVA: 0x0000E916 File Offset: 0x0000CB16
		// (set) Token: 0x0600033D RID: 829 RVA: 0x0000E91E File Offset: 0x0000CB1E
		public ImageFit.ImageHorizontalAlignments HorizontalAlignment { get; set; }

		// Token: 0x170000F9 RID: 249
		// (get) Token: 0x0600033E RID: 830 RVA: 0x0000E927 File Offset: 0x0000CB27
		// (set) Token: 0x0600033F RID: 831 RVA: 0x0000E92F File Offset: 0x0000CB2F
		public ImageFit.ImageVerticalAlignments VerticalAlignment { get; set; }

		// Token: 0x170000FA RID: 250
		// (get) Token: 0x06000340 RID: 832 RVA: 0x0000E938 File Offset: 0x0000CB38
		// (set) Token: 0x06000341 RID: 833 RVA: 0x0000E940 File Offset: 0x0000CB40
		public float OffsetX { get; set; }

		// Token: 0x170000FB RID: 251
		// (get) Token: 0x06000342 RID: 834 RVA: 0x0000E949 File Offset: 0x0000CB49
		// (set) Token: 0x06000343 RID: 835 RVA: 0x0000E951 File Offset: 0x0000CB51
		public float OffsetY { get; set; }

		// Token: 0x06000344 RID: 836 RVA: 0x0000E95A File Offset: 0x0000CB5A
		public ImageFit()
		{
			this.Type = ImageFit.ImageFitTypes.StretchToFit;
			this.HorizontalAlignment = ImageFit.ImageHorizontalAlignments.Center;
			this.VerticalAlignment = ImageFit.ImageVerticalAlignments.Center;
		}

		// Token: 0x06000345 RID: 837 RVA: 0x0000E978 File Offset: 0x0000CB78
		public ImageFitResult GetFittedRectangle(in Vector2 containerSize, in Vector2 imageSize)
		{
			switch (this.Type)
			{
			case ImageFit.ImageFitTypes.StretchToFit:
				return new ImageFitResult(0f, 0f, containerSize.X, containerSize.Y);
			case ImageFit.ImageFitTypes.Cover:
				return this.GetRectangleForCover(containerSize, imageSize);
			case ImageFit.ImageFitTypes.Contain:
				return this.GetRectangleForContain(containerSize, imageSize);
			default:
				Debug.FailedAssert(string.Format("Image fit type not handled: {0}", this.Type), "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\ImageFit.cs", "GetFittedRectangle", 55);
				return new ImageFitResult(0f, 0f, containerSize.X, containerSize.Y);
			}
		}

		// Token: 0x06000346 RID: 838 RVA: 0x0000EA10 File Offset: 0x0000CC10
		private ImageFitResult GetRectangleForCover(in Vector2 containerSize, in Vector2 imageSize)
		{
			float a = containerSize.X / imageSize.X;
			float b = containerSize.Y / imageSize.Y;
			float num = MathF.Max(a, b);
			float num2 = imageSize.X * num;
			float num3 = imageSize.Y * num;
			Vector2 vector = new Vector2(num2, num3);
			float offsetX;
			float offsetY;
			this.GetImageAlignment(containerSize, vector, out offsetX, out offsetY);
			return new ImageFitResult(offsetX, offsetY, num2, num3);
		}

		// Token: 0x06000347 RID: 839 RVA: 0x0000EA74 File Offset: 0x0000CC74
		private ImageFitResult GetRectangleForContain(in Vector2 containerSize, in Vector2 imageSize)
		{
			float a = containerSize.X / imageSize.X;
			float b = containerSize.Y / imageSize.Y;
			float num = MathF.Min(a, b);
			float num2 = imageSize.X * num;
			float num3 = imageSize.Y * num;
			Vector2 vector = new Vector2(num2, num3);
			float offsetX;
			float offsetY;
			this.GetImageAlignment(containerSize, vector, out offsetX, out offsetY);
			return new ImageFitResult(offsetX, offsetY, num2, num3);
		}

		// Token: 0x06000348 RID: 840 RVA: 0x0000EAD8 File Offset: 0x0000CCD8
		private void GetImageAlignment(in Vector2 containerSize, in Vector2 imageSize, out float x, out float y)
		{
			x = 0f;
			y = 0f;
			switch (this.HorizontalAlignment)
			{
			case ImageFit.ImageHorizontalAlignments.Left:
				x = 0f;
				break;
			case ImageFit.ImageHorizontalAlignments.Center:
				x = (containerSize.X - imageSize.X) * 0.5f;
				break;
			case ImageFit.ImageHorizontalAlignments.Right:
				x = containerSize.X - imageSize.X;
				break;
			}
			switch (this.VerticalAlignment)
			{
			case ImageFit.ImageVerticalAlignments.Top:
				y = 0f;
				break;
			case ImageFit.ImageVerticalAlignments.Center:
				y = (containerSize.Y - imageSize.Y) * 0.5f;
				break;
			case ImageFit.ImageVerticalAlignments.Bottom:
				y = containerSize.Y - imageSize.Y;
				break;
			}
			x += this.OffsetX;
			y += this.OffsetY;
		}

		// Token: 0x0200007C RID: 124
		public enum ImageFitTypes : byte
		{
			// Token: 0x04000432 RID: 1074
			StretchToFit,
			// Token: 0x04000433 RID: 1075
			Cover,
			// Token: 0x04000434 RID: 1076
			Contain
		}

		// Token: 0x0200007D RID: 125
		public enum ImageHorizontalAlignments : byte
		{
			// Token: 0x04000436 RID: 1078
			Left,
			// Token: 0x04000437 RID: 1079
			Center,
			// Token: 0x04000438 RID: 1080
			Right
		}

		// Token: 0x0200007E RID: 126
		public enum ImageVerticalAlignments : byte
		{
			// Token: 0x0400043A RID: 1082
			Top,
			// Token: 0x0400043B RID: 1083
			Center,
			// Token: 0x0400043C RID: 1084
			Bottom
		}
	}
}
