namespace TaleWorlds.TwoDimension.Standalone.Native.Windows;

public struct BlendFunction(AlphaFormatFlags op, byte flags, byte alpha, AlphaFormatFlags format)
{
	public byte BlendOp = (byte)op;

	public byte BlendFlags = flags;

	public byte SourceConstantAlpha = alpha;

	public byte AlphaFormat = (byte)format;

	public static readonly BlendFunction Default = new BlendFunction(AlphaFormatFlags.Over, 0, byte.MaxValue, AlphaFormatFlags.Alpha);
}
