namespace System.Reflection.PortableExecutable;

internal static class PEStreamOptionsExtensions
{
	public static bool IsValid(this System.Reflection.PortableExecutable.PEStreamOptions options)
	{
		return (options & ~(System.Reflection.PortableExecutable.PEStreamOptions.LeaveOpen | System.Reflection.PortableExecutable.PEStreamOptions.PrefetchMetadata | System.Reflection.PortableExecutable.PEStreamOptions.PrefetchEntireImage | System.Reflection.PortableExecutable.PEStreamOptions.IsLoadedImage)) == 0;
	}
}
