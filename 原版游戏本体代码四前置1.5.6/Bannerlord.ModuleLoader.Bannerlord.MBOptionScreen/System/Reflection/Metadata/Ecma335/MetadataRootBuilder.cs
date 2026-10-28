using System.Collections.Immutable;

namespace System.Reflection.Metadata.Ecma335;

internal sealed class MetadataRootBuilder
{
	private const string DefaultMetadataVersionString = "v4.0.30319";

	internal static readonly System.Collections.Immutable.ImmutableArray<int> EmptyRowCounts = System.Collections.Immutable.ImmutableArray.Create(new int[System.Reflection.Metadata.Ecma335.MetadataTokens.TableCount]);

	private readonly MetadataBuilder _tablesAndHeaps;

	private readonly SerializedMetadata _serializedMetadata;

	public string MetadataVersion { get; }

	public bool SuppressValidation { get; }

	public MetadataSizes Sizes => _serializedMetadata.Sizes;

	public MetadataRootBuilder(MetadataBuilder tablesAndHeaps, string? metadataVersion = null, bool suppressValidation = false)
	{
		if (tablesAndHeaps == null)
		{
			System.Reflection.Throw.ArgumentNull("tablesAndHeaps");
		}
		int num = ((metadataVersion != null) ? System.Reflection.BlobUtilities.GetUTF8ByteCount(metadataVersion) : "v4.0.30319".Length);
		if (num > 254)
		{
			System.Reflection.Throw.InvalidArgument(System.SR.MetadataVersionTooLong, "metadataVersion");
		}
		_tablesAndHeaps = tablesAndHeaps;
		MetadataVersion = metadataVersion ?? "v4.0.30319";
		SuppressValidation = suppressValidation;
		_serializedMetadata = tablesAndHeaps.GetSerializedMetadata(EmptyRowCounts, num, isStandaloneDebugMetadata: false);
	}

	public void Serialize(BlobBuilder builder, int methodBodyStreamRva, int mappedFieldDataStreamRva)
	{
		if (builder == null)
		{
			System.Reflection.Throw.ArgumentNull("builder");
		}
		if (methodBodyStreamRva < 0)
		{
			System.Reflection.Throw.ArgumentOutOfRange("methodBodyStreamRva");
		}
		if (mappedFieldDataStreamRva < 0)
		{
			System.Reflection.Throw.ArgumentOutOfRange("mappedFieldDataStreamRva");
		}
		if (!SuppressValidation)
		{
			_tablesAndHeaps.ValidateOrder();
		}
		MetadataBuilder.SerializeMetadataHeader(builder, MetadataVersion, _serializedMetadata.Sizes);
		_tablesAndHeaps.SerializeMetadataTables(builder, _serializedMetadata.Sizes, _serializedMetadata.StringMap, methodBodyStreamRva, mappedFieldDataStreamRva);
		_tablesAndHeaps.WriteHeapsTo(builder, _serializedMetadata.StringHeap);
	}
}
