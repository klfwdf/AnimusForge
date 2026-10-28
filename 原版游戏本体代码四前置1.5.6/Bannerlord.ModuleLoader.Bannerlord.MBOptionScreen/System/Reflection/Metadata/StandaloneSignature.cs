using System.Collections.Immutable;
using System.Reflection.Metadata.Ecma335;

namespace System.Reflection.Metadata;

internal readonly struct StandaloneSignature
{
	private readonly System.Reflection.Metadata.MetadataReader _reader;

	private readonly int _rowId;

	private StandaloneSignatureHandle Handle => StandaloneSignatureHandle.FromRowId(_rowId);

	public System.Reflection.Metadata.BlobHandle Signature => _reader.StandAloneSigTable.GetSignature(_rowId);

	internal StandaloneSignature(System.Reflection.Metadata.MetadataReader reader, StandaloneSignatureHandle handle)
	{
		_reader = reader;
		_rowId = handle.RowId;
	}

	public MethodSignature<TType> DecodeMethodSignature<TType, TGenericContext>(ISignatureTypeProvider<TType, TGenericContext> provider, TGenericContext genericContext)
	{
		SignatureDecoder<TType, TGenericContext> signatureDecoder = new SignatureDecoder<TType, TGenericContext>(provider, _reader, genericContext);
		System.Reflection.Metadata.BlobReader blobReader = _reader.GetBlobReader(Signature);
		return signatureDecoder.DecodeMethodSignature(ref blobReader);
	}

	public System.Collections.Immutable.ImmutableArray<TType> DecodeLocalSignature<TType, TGenericContext>(ISignatureTypeProvider<TType, TGenericContext> provider, TGenericContext genericContext)
	{
		SignatureDecoder<TType, TGenericContext> signatureDecoder = new SignatureDecoder<TType, TGenericContext>(provider, _reader, genericContext);
		System.Reflection.Metadata.BlobReader blobReader = _reader.GetBlobReader(Signature);
		return signatureDecoder.DecodeLocalSignature(ref blobReader);
	}

	public CustomAttributeHandleCollection GetCustomAttributes()
	{
		return new CustomAttributeHandleCollection(_reader, Handle);
	}

	public StandaloneSignatureKind GetKind()
	{
		return _reader.GetBlobReader(Signature).ReadSignatureHeader().Kind switch
		{
			SignatureKind.Method => StandaloneSignatureKind.Method, 
			SignatureKind.LocalVariables => StandaloneSignatureKind.LocalVariables, 
			_ => throw new BadImageFormatException(), 
		};
	}
}
