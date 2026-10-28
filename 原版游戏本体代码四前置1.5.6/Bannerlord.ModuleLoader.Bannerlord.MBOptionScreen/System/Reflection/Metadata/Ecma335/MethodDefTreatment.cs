namespace System.Reflection.Metadata.Ecma335;

[Flags]
internal enum MethodDefTreatment : byte
{
	None = 0,
	KindMask = 0xF,
	Other = 1,
	DelegateMethod = 2,
	AttributeMethod = 3,
	InterfaceMethod = 4,
	Implementation = 5,
	HiddenInterfaceImplementation = 6,
	DisposeMethod = 7,
	MarkAbstractFlag = 0x10,
	MarkPublicFlag = 0x20
}
