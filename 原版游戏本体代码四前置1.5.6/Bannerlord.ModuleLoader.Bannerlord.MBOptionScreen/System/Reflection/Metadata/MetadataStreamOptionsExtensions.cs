namespace System.Reflection.Metadata;

internal static class MetadataStreamOptionsExtensions
{
	public static bool IsValid(this System.Reflection.Metadata.MetadataStreamOptions options)
	{
		return (options & ~(System.Reflection.Metadata.MetadataStreamOptions.LeaveOpen | System.Reflection.Metadata.MetadataStreamOptions.PrefetchMetadata)) == 0;
	}
}
