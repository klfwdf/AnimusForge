namespace System.Reflection.Metadata.Ecma335;

internal enum HeapSizeFlag : byte
{
	StringHeapLarge = 1,
	GuidHeapLarge = 2,
	BlobHeapLarge = 4,
	EncDeltas = 0x20,
	DeletedMarks = 0x80
}
