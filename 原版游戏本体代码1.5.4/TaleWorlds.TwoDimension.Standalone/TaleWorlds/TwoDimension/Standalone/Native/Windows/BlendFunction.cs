namespace TaleWorlds.TwoDimension.Standalone.Native.Windows;

public struct BlendFunction
{
	public byte BlendOp;

	public byte BlendFlags;

	public byte SourceConstantAlpha;

	public byte AlphaFormat;

	public static readonly BlendFunction Default = new BlendFunction(AlphaFormatFlags.Over, 0, byte.MaxValue, AlphaFormatFlags.Alpha);

	public BlendFunction(AlphaFormatFlags op, byte flags, byte alpha, AlphaFormatFlags format)
	{
		BlendOp = (byte)op;
		BlendFlags = flags;
		SourceConstantAlpha = alpha;
		AlphaFormat = (byte)format;
	}
}
