namespace System.Reflection.Metadata.Ecma335;

internal static class COR20Constants
{
	internal const int SizeOfCorHeader = 72;

	internal const uint COR20MetadataSignature = 1112167234u;

	internal const int MinimumSizeofMetadataHeader = 16;

	internal const int SizeofStorageHeader = 4;

	internal const int MinimumSizeofStreamHeader = 8;

	internal const string StringStreamName = "#Strings";

	internal const string BlobStreamName = "#Blob";

	internal const string GUIDStreamName = "#GUID";

	internal const string UserStringStreamName = "#US";

	internal const string CompressedMetadataTableStreamName = "#~";

	internal const string UncompressedMetadataTableStreamName = "#-";

	internal const string MinimalDeltaMetadataTableStreamName = "#JTD";

	internal const string StandalonePdbStreamName = "#Pdb";

	internal const int LargeStreamHeapSize = 4096;
}
