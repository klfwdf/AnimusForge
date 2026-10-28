namespace System.Reflection.Metadata;

[Flags]
internal enum MetadataStreamOptions
{
	Default = 0,
	LeaveOpen = 1,
	PrefetchMetadata = 2
}
