namespace System.Reflection.PortableExecutable;

[Flags]
internal enum PEStreamOptions
{
	Default = 0,
	LeaveOpen = 1,
	PrefetchMetadata = 2,
	PrefetchEntireImage = 4,
	IsLoadedImage = 8
}
