using System.Collections.Generic;
using System.Collections.Immutable;

namespace System.Reflection.Metadata.Ecma335;

internal sealed class PortablePdbBuilder
{
	private Blob _pdbIdBlob;

	private readonly System.Reflection.Metadata.MethodDefinitionHandle _entryPoint;

	private readonly MetadataBuilder _builder;

	private readonly SerializedMetadata _serializedMetadata;

	public string MetadataVersion => "PDB v1.0";

	public ushort FormatVersion => 256;

	public Func<IEnumerable<Blob>, System.Reflection.Metadata.BlobContentId> IdProvider { get; }

	public PortablePdbBuilder(MetadataBuilder tablesAndHeaps, System.Collections.Immutable.ImmutableArray<int> typeSystemRowCounts, System.Reflection.Metadata.MethodDefinitionHandle entryPoint, Func<IEnumerable<Blob>, System.Reflection.Metadata.BlobContentId>? idProvider = null)
	{
		if (tablesAndHeaps == null)
		{
			System.Reflection.Throw.ArgumentNull("tablesAndHeaps");
		}
		ValidateTypeSystemRowCounts(typeSystemRowCounts);
		_builder = tablesAndHeaps;
		_entryPoint = entryPoint;
		_serializedMetadata = tablesAndHeaps.GetSerializedMetadata(typeSystemRowCounts, MetadataVersion.Length, isStandaloneDebugMetadata: true);
		IdProvider = idProvider ?? System.Reflection.Metadata.BlobContentId.GetTimeBasedProvider();
	}

	private static void ValidateTypeSystemRowCounts(System.Collections.Immutable.ImmutableArray<int> typeSystemRowCounts)
	{
		if (typeSystemRowCounts.IsDefault)
		{
			System.Reflection.Throw.ArgumentNull("typeSystemRowCounts");
		}
		if (typeSystemRowCounts.Length != System.Reflection.Metadata.Ecma335.MetadataTokens.TableCount)
		{
			throw new ArgumentException(System.SR.Format(System.SR.ExpectedArrayOfSize, System.Reflection.Metadata.Ecma335.MetadataTokens.TableCount), "typeSystemRowCounts");
		}
		for (int i = 0; i < typeSystemRowCounts.Length; i++)
		{
			if (typeSystemRowCounts[i] != 0)
			{
				if ((typeSystemRowCounts[i] & -16777216) != 0)
				{
					throw new ArgumentOutOfRangeException("typeSystemRowCounts", System.SR.Format(System.SR.RowCountOutOfRange, i));
				}
				if (((1L << i) & 0x1FC93FB7FF57L) == 0L)
				{
					throw new ArgumentException(System.SR.Format(System.SR.RowCountMustBeZero, i), "typeSystemRowCounts");
				}
			}
		}
	}

	private void SerializeStandalonePdbStream(BlobBuilder builder)
	{
		_ = builder.Count;
		_pdbIdBlob = builder.ReserveBytes(20);
		builder.WriteInt32((!_entryPoint.IsNil) ? System.Reflection.Metadata.Ecma335.MetadataTokens.GetToken(_entryPoint) : 0);
		builder.WriteUInt64(_serializedMetadata.Sizes.ExternalTablesMask);
		MetadataWriterUtilities.SerializeRowCounts(builder, _serializedMetadata.Sizes.ExternalRowCounts);
		_ = builder.Count;
	}

	public System.Reflection.Metadata.BlobContentId Serialize(BlobBuilder builder)
	{
		if (builder == null)
		{
			System.Reflection.Throw.ArgumentNull("builder");
		}
		MetadataBuilder.SerializeMetadataHeader(builder, MetadataVersion, _serializedMetadata.Sizes);
		SerializeStandalonePdbStream(builder);
		_builder.SerializeMetadataTables(builder, _serializedMetadata.Sizes, _serializedMetadata.StringMap, 0, 0);
		_builder.WriteHeapsTo(builder, _serializedMetadata.StringHeap);
		System.Reflection.Metadata.BlobContentId result = IdProvider(builder.GetBlobs());
		BlobWriter blobWriter = new BlobWriter(_pdbIdBlob);
		blobWriter.WriteGuid(result.Guid);
		blobWriter.WriteUInt32(result.Stamp);
		return result;
	}
}
