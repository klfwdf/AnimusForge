namespace System.Reflection.Metadata;

internal static class HandleKindExtensions
{
	internal static bool IsHeapHandle(this System.Reflection.Metadata.HandleKind kind)
	{
		return (int)kind >= 124;
	}
}
