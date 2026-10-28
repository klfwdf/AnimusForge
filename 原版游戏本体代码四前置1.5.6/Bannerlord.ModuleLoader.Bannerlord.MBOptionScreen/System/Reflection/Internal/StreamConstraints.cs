namespace System.Reflection.Internal;

internal readonly struct StreamConstraints(object? guardOpt, long startPosition, int imageSize)
{
	public readonly object? GuardOpt = guardOpt;

	public readonly long ImageStart = startPosition;

	public readonly int ImageSize = imageSize;
}
