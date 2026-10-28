namespace System.Reflection.Metadata;

[Flags]
internal enum MetadataReaderOptions
{
	None = 0,
	Default = 1,
	ApplyWindowsRuntimeProjections = Default
}
