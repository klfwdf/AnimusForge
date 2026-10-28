using System.Collections.Immutable;

namespace System.Reflection.Metadata;

internal sealed class DebugMetadataHeader
{
	public System.Collections.Immutable.ImmutableArray<byte> Id { get; }

	public System.Reflection.Metadata.MethodDefinitionHandle EntryPoint { get; }

	public int IdStartOffset { get; }

	internal DebugMetadataHeader(System.Collections.Immutable.ImmutableArray<byte> id, System.Reflection.Metadata.MethodDefinitionHandle entryPoint, int idStartOffset)
	{
		Id = id;
		EntryPoint = entryPoint;
		IdStartOffset = idStartOffset;
	}
}
