using System.Collections.Immutable;
using System.Reflection.Metadata.Ecma335;

namespace System.Reflection.Metadata;

internal readonly struct PropertyDefinition
{
	private readonly System.Reflection.Metadata.MetadataReader _reader;

	private readonly int _rowId;

	private PropertyDefinitionHandle Handle => PropertyDefinitionHandle.FromRowId(_rowId);

	public StringHandle Name => _reader.PropertyTable.GetName(Handle);

	public PropertyAttributes Attributes => _reader.PropertyTable.GetFlags(Handle);

	public System.Reflection.Metadata.BlobHandle Signature => _reader.PropertyTable.GetSignature(Handle);

	internal PropertyDefinition(System.Reflection.Metadata.MetadataReader reader, PropertyDefinitionHandle handle)
	{
		_reader = reader;
		_rowId = handle.RowId;
	}

	public MethodSignature<TType> DecodeSignature<TType, TGenericContext>(ISignatureTypeProvider<TType, TGenericContext> provider, TGenericContext genericContext)
	{
		SignatureDecoder<TType, TGenericContext> signatureDecoder = new SignatureDecoder<TType, TGenericContext>(provider, _reader, genericContext);
		System.Reflection.Metadata.BlobReader blobReader = _reader.GetBlobReader(Signature);
		return signatureDecoder.DecodeMethodSignature(ref blobReader);
	}

	public ConstantHandle GetDefaultValue()
	{
		return _reader.ConstantTable.FindConstant(Handle);
	}

	public CustomAttributeHandleCollection GetCustomAttributes()
	{
		return new CustomAttributeHandleCollection(_reader, Handle);
	}

	public PropertyAccessors GetAccessors()
	{
		int getterRowId = 0;
		int setterRowId = 0;
		System.Collections.Immutable.ImmutableArray<System.Reflection.Metadata.MethodDefinitionHandle>.Builder builder = null;
		int num = _reader.MethodSemanticsTable.FindSemanticMethodsForProperty(Handle, out var methodCount);
		for (ushort num2 = 0; num2 < methodCount; num2++)
		{
			int rowId = num + num2;
			switch (_reader.MethodSemanticsTable.GetSemantics(rowId))
			{
			case System.Reflection.MethodSemanticsAttributes.Getter:
				getterRowId = _reader.MethodSemanticsTable.GetMethod(rowId).RowId;
				break;
			case System.Reflection.MethodSemanticsAttributes.Setter:
				setterRowId = _reader.MethodSemanticsTable.GetMethod(rowId).RowId;
				break;
			case System.Reflection.MethodSemanticsAttributes.Other:
				if (builder == null)
				{
					builder = System.Collections.Immutable.ImmutableArray.CreateBuilder<System.Reflection.Metadata.MethodDefinitionHandle>();
				}
				builder.Add(_reader.MethodSemanticsTable.GetMethod(rowId));
				break;
			}
		}
		System.Collections.Immutable.ImmutableArray<System.Reflection.Metadata.MethodDefinitionHandle> others = builder?.ToImmutable() ?? System.Collections.Immutable.ImmutableArray<System.Reflection.Metadata.MethodDefinitionHandle>.Empty;
		return new PropertyAccessors(getterRowId, setterRowId, others);
	}
}
