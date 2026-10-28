namespace System.Reflection.Metadata.Ecma335;

[Flags]
internal enum TypeDefTreatment : byte
{
	None = 0,
	KindMask = 0xF,
	NormalNonAttribute = 1,
	NormalAttribute = 2,
	UnmangleWinRTName = 3,
	PrefixWinRTName = 4,
	RedirectedToClrType = 5,
	RedirectedToClrAttribute = 6,
	MarkAbstractFlag = 0x10,
	MarkInternalFlag = 0x20
}
