using System.Collections.Immutable;

namespace System.Reflection.Metadata.Ecma335;

internal sealed class SerializedMetadata
{
	internal readonly System.Collections.Immutable.ImmutableArray<int> StringMap;

	internal readonly BlobBuilder StringHeap;

	internal readonly MetadataSizes Sizes;

	public SerializedMetadata(MetadataSizes sizes, BlobBuilder stringHeap, System.Collections.Immutable.ImmutableArray<int> stringMap)
	{
		Sizes = sizes;
		StringHeap = stringHeap;
		StringMap = stringMap;
	}
}
