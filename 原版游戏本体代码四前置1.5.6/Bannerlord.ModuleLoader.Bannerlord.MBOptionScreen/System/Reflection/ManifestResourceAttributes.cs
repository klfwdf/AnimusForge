namespace System.Reflection;

[Flags]
internal enum ManifestResourceAttributes
{
	Public = 1,
	Private = 2,
	VisibilityMask = 7
}
