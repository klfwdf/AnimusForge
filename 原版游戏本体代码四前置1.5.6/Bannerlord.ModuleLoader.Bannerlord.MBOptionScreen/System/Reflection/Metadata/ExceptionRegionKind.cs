namespace System.Reflection.Metadata;

internal enum ExceptionRegionKind : ushort
{
	Catch = 0,
	Filter = 1,
	Finally = 2,
	Fault = 4
}
