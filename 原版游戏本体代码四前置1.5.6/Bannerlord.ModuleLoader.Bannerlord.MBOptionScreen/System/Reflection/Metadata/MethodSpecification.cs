using System.Collections.Immutable;
using System.Reflection.Metadata.Ecma335;

namespace System.Reflection.Metadata;

internal readonly struct MethodSpecification
{
	private readonly System.Reflection.Metadata.MetadataReader _reader;

	private readonly int _rowId;

	private MethodSpecificationHandle Handle => MethodSpecificationHandle.FromRowId(_rowId);

	public EntityHandle Method => _reader.MethodSpecTable.GetMethod(Handle);

	public System.Reflection.Metadata.BlobHandle Signature => _reader.MethodSpecTable.GetInstantiation(Handle);

	internal MethodSpecification(System.Reflection.Metadata.MetadataReader reader, MethodSpecificationHandle handle)
	{
		_reader = reader;
		_rowId = handle.RowId;
	}

	public System.Collections.Immutable.ImmutableArray<TType> DecodeSignature<TType, TGenericContext>(ISignatureTypeProvider<TType, TGenericContext> provider, TGenericContext genericContext)
	{
		SignatureDecoder<TType, TGenericContext> signatureDecoder = new SignatureDecoder<TType, TGenericContext>(provider, _reader, genericContext);
		System.Reflection.Metadata.BlobReader blobReader = _reader.GetBlobReader(Signature);
		return signatureDecoder.DecodeMethodSpecificationSignature(ref blobReader);
	}

	public CustomAttributeHandleCollection GetCustomAttributes()
	{
		return new CustomAttributeHandleCollection(_reader, Handle);
	}
}
