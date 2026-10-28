using System.Numerics;
using TaleWorlds.Library;

namespace TaleWorlds.GauntletUI;

public class ImageFit
{
	public enum ImageFitTypes : byte
	{
		StretchToFit,
		Cover,
		Contain
	}

	public enum ImageHorizontalAlignments : byte
	{
		Left,
		Center,
		Right
	}

	public enum ImageVerticalAlignments : byte
	{
		Top,
		Center,
		Bottom
	}

	public ImageFitTypes Type { get; set; }

	public ImageHorizontalAlignments HorizontalAlignment { get; set; }

	public ImageVerticalAlignments VerticalAlignment { get; set; }

	public float OffsetX { get; set; }

	public float OffsetY { get; set; }

	public ImageFit()
	{
		Type = ImageFitTypes.StretchToFit;
		HorizontalAlignment = ImageHorizontalAlignments.Center;
		VerticalAlignment = ImageVerticalAlignments.Center;
	}

	public ImageFitResult GetFittedRectangle(in Vector2 containerSize, in Vector2 imageSize)
	{
		switch (Type)
		{
		case ImageFitTypes.StretchToFit:
			return new ImageFitResult(0f, 0f, containerSize.X, containerSize.Y);
		case ImageFitTypes.Cover:
			return GetRectangleForCover(in containerSize, in imageSize);
		case ImageFitTypes.Contain:
			return GetRectangleForContain(in containerSize, in imageSize);
		default:
			Debug.FailedAssert($"Image fit type not handled: {Type}", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\ImageFit.cs", "GetFittedRectangle", 55);
			return new ImageFitResult(0f, 0f, containerSize.X, containerSize.Y);
		}
	}

	private ImageFitResult GetRectangleForCover(in Vector2 containerSize, in Vector2 imageSize)
	{
		float a = containerSize.X / imageSize.X;
		float b = containerSize.Y / imageSize.Y;
		float num = MathF.Max(a, b);
		float num2 = imageSize.X * num;
		float num3 = imageSize.Y * num;
		GetImageAlignment(in containerSize, new Vector2(num2, num3), out var x, out var y);
		return new ImageFitResult(x, y, num2, num3);
	}

	private ImageFitResult GetRectangleForContain(in Vector2 containerSize, in Vector2 imageSize)
	{
		float a = containerSize.X / imageSize.X;
		float b = containerSize.Y / imageSize.Y;
		float num = MathF.Min(a, b);
		float num2 = imageSize.X * num;
		float num3 = imageSize.Y * num;
		GetImageAlignment(in containerSize, new Vector2(num2, num3), out var x, out var y);
		return new ImageFitResult(x, y, num2, num3);
	}

	private void GetImageAlignment(in Vector2 containerSize, in Vector2 imageSize, out float x, out float y)
	{
		x = 0f;
		y = 0f;
		switch (HorizontalAlignment)
		{
		case ImageHorizontalAlignments.Left:
			x = 0f;
			break;
		case ImageHorizontalAlignments.Center:
			x = (containerSize.X - imageSize.X) * 0.5f;
			break;
		case ImageHorizontalAlignments.Right:
			x = containerSize.X - imageSize.X;
			break;
		}
		switch (VerticalAlignment)
		{
		case ImageVerticalAlignments.Top:
			y = 0f;
			break;
		case ImageVerticalAlignments.Center:
			y = (containerSize.Y - imageSize.Y) * 0.5f;
			break;
		case ImageVerticalAlignments.Bottom:
			y = containerSize.Y - imageSize.Y;
			break;
		}
		x += OffsetX;
		y += OffsetY;
	}
}
