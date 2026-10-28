namespace System.Reflection;

[Flags]
internal enum MethodImportAttributes : short
{
	None = 0,
	ExactSpelling = 1,
	BestFitMappingDisable = 0x20,
	BestFitMappingEnable = 0x10,
	BestFitMappingMask = BestFitMappingDisable | BestFitMappingEnable,
	CharSetAnsi = 2,
	CharSetUnicode = 4,
	CharSetAuto = CharSetAnsi | CharSetUnicode,
	CharSetMask = CharSetAuto,
	ThrowOnUnmappableCharEnable = 0x1000,
	ThrowOnUnmappableCharDisable = 0x2000,
	ThrowOnUnmappableCharMask = ThrowOnUnmappableCharEnable | ThrowOnUnmappableCharDisable,
	SetLastError = 0x40,
	CallingConventionWinApi = 0x100,
	CallingConventionCDecl = 0x200,
	CallingConventionStdCall = CallingConventionWinApi | CallingConventionCDecl,
	CallingConventionThisCall = 0x400,
	CallingConventionFastCall = CallingConventionWinApi | CallingConventionThisCall,
	CallingConventionMask = CallingConventionStdCall | CallingConventionThisCall
}
