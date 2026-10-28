using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace System.Reflection.Metadata;

[DebuggerDisplay("{AssemblyQualifiedName}")]
internal sealed class TypeName
{
	private readonly int _rankOrModifier;

	private readonly int _nestedNameLength;

	private readonly TypeName _elementOrGenericType;

	private readonly TypeName _declaringType;

	private readonly System.Collections.Immutable.ImmutableArray<TypeName> _genericArguments;

	private string _name;

	private string _fullName;

	private string _assemblyQualifiedName;

	public string AssemblyQualifiedName => _assemblyQualifiedName ?? (_assemblyQualifiedName = ((AssemblyName == null) ? FullName : (FullName + ", " + AssemblyName.FullName)));

	public AssemblyNameInfo? AssemblyName { get; }

	public TypeName DeclaringType
	{
		get
		{
			if (_declaringType == null)
			{
				TypeNameParserHelpers.ThrowInvalidOperation_NotNestedType();
			}
			return _declaringType;
		}
	}

	public string FullName
	{
		get
		{
			if (_fullName == null)
			{
				if (IsConstructedGenericType)
				{
					_fullName = TypeNameParserHelpers.GetGenericTypeFullName(GetGenericTypeDefinition().FullName.AsSpan(), _genericArguments.AsSpan());
				}
				else if (IsArray || IsPointer || IsByRef)
				{
					Span<char> initialBuffer = stackalloc char[128];
					ValueStringBuilder builder = new ValueStringBuilder(initialBuffer);
					builder.Append(GetElementType().FullName);
					_fullName = TypeNameParserHelpers.GetRankOrModifierStringRepresentation(_rankOrModifier, ref builder);
				}
			}
			else if (_nestedNameLength > 0 && _fullName.Length > _nestedNameLength)
			{
				_fullName = _fullName.Substring(0, _nestedNameLength);
			}
			return _fullName;
		}
	}

	public bool IsArray
	{
		get
		{
			if (_rankOrModifier != -1)
			{
				return _rankOrModifier > 0;
			}
			return true;
		}
	}

	public bool IsConstructedGenericType => _genericArguments.Length > 0;

	public bool IsSimple => _elementOrGenericType == null;

	public bool IsByRef => _rankOrModifier == -3;

	public bool IsNested => _declaringType != null;

	public bool IsSZArray => _rankOrModifier == -1;

	public bool IsPointer => _rankOrModifier == -2;

	public bool IsVariableBoundArrayType => _rankOrModifier >= 1;

	public string Name
	{
		get
		{
			if (_name == null)
			{
				if (IsConstructedGenericType)
				{
					_name = TypeNameParserHelpers.GetName(GetGenericTypeDefinition().FullName.AsSpan()).ToString();
				}
				else if (IsPointer || IsByRef || IsArray)
				{
					Span<char> initialBuffer = stackalloc char[64];
					ValueStringBuilder builder = new ValueStringBuilder(initialBuffer);
					builder.Append(GetElementType().Name);
					_name = TypeNameParserHelpers.GetRankOrModifierStringRepresentation(_rankOrModifier, ref builder);
				}
				else if (_nestedNameLength > 0 && _fullName != null)
				{
					_name = TypeNameParserHelpers.GetName(_fullName.AsSpan(0, _nestedNameLength)).ToString();
				}
				else
				{
					_name = TypeNameParserHelpers.GetName(FullName.AsSpan()).ToString();
				}
			}
			return _name;
		}
	}

	internal TypeName(string? fullName, AssemblyNameInfo? assemblyName, TypeName? elementOrGenericType = null, TypeName? declaringType = null, System.Collections.Immutable.ImmutableArray<TypeName>.Builder? genericTypeArguments = null, int rankOrModifier = 0, int nestedNameLength = -1)
	{
		_fullName = fullName;
		AssemblyName = assemblyName;
		_rankOrModifier = rankOrModifier;
		_elementOrGenericType = elementOrGenericType;
		_declaringType = declaringType;
		_nestedNameLength = nestedNameLength;
		_genericArguments = ((genericTypeArguments == null) ? System.Collections.Immutable.ImmutableArray<TypeName>.Empty : ((genericTypeArguments.Count == genericTypeArguments.Capacity) ? genericTypeArguments.MoveToImmutable() : genericTypeArguments.ToImmutableArray()));
	}

	private TypeName(string fullName, AssemblyNameInfo assemblyName, TypeName elementOrGenericType, TypeName declaringType, System.Collections.Immutable.ImmutableArray<TypeName> genericTypeArguments, int rankOrModifier = 0, int nestedNameLength = -1)
	{
		_fullName = fullName;
		AssemblyName = assemblyName;
		_elementOrGenericType = elementOrGenericType;
		_declaringType = declaringType;
		_genericArguments = genericTypeArguments;
		_rankOrModifier = rankOrModifier;
		_nestedNameLength = nestedNameLength;
	}

	public int GetNodeCount()
	{
		int num = 1;
		checked
		{
			if (IsArray || IsPointer || IsByRef)
			{
				num += GetElementType().GetNodeCount();
			}
			else if (IsConstructedGenericType)
			{
				num += GetGenericTypeDefinition().GetNodeCount();
				foreach (TypeName genericArgument in GetGenericArguments())
				{
					num += genericArgument.GetNodeCount();
				}
			}
			else if (IsNested)
			{
				num += DeclaringType.GetNodeCount();
			}
			return num;
		}
	}

	public TypeName GetElementType()
	{
		if (!IsArray && !IsPointer && !IsByRef)
		{
			TypeNameParserHelpers.ThrowInvalidOperation_NoElement();
		}
		return _elementOrGenericType;
	}

	public TypeName GetGenericTypeDefinition()
	{
		if (!IsConstructedGenericType)
		{
			TypeNameParserHelpers.ThrowInvalidOperation_NotGenericType();
		}
		return _elementOrGenericType;
	}

	public static TypeName Parse(ReadOnlySpan<char> typeName, TypeNameParseOptions? options = null)
	{
		return TypeNameParser.Parse(typeName, throwOnError: true, options);
	}

	public static bool TryParse(ReadOnlySpan<char> typeName, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out TypeName? result, TypeNameParseOptions? options = null)
	{
		result = TypeNameParser.Parse(typeName, throwOnError: false, options);
		return result != null;
	}

	public int GetArrayRank()
	{
		if (_rankOrModifier != -1 && _rankOrModifier <= 0)
		{
			TypeNameParserHelpers.ThrowInvalidOperation_HasToBeArrayClass();
		}
		if (_rankOrModifier != -1)
		{
			return _rankOrModifier;
		}
		return 1;
	}

	public System.Collections.Immutable.ImmutableArray<TypeName> GetGenericArguments()
	{
		return _genericArguments;
	}

	public TypeName WithAssemblyName(AssemblyNameInfo? assemblyName)
	{
		if (!IsSimple)
		{
			TypeNameParserHelpers.ThrowInvalidOperation_NotSimpleName(FullName);
		}
		TypeName declaringType = (IsNested ? DeclaringType.WithAssemblyName(assemblyName) : null);
		return new TypeName(_fullName, assemblyName, null, declaringType, System.Collections.Immutable.ImmutableArray<TypeName>.Empty, 0, _nestedNameLength);
	}

	public TypeName MakeSZArrayTypeName()
	{
		return MakeElementTypeName(-1);
	}

	public TypeName MakeArrayTypeName(int rank)
	{
		if (rank > 0)
		{
			return MakeElementTypeName(rank);
		}
		throw new ArgumentOutOfRangeException("rank");
	}

	public TypeName MakePointerTypeName()
	{
		return MakeElementTypeName(-2);
	}

	public TypeName MakeByRefTypeName()
	{
		return MakeElementTypeName(-3);
	}

	public TypeName MakeGenericTypeName(System.Collections.Immutable.ImmutableArray<TypeName> typeArguments)
	{
		if (!IsSimple)
		{
			TypeNameParserHelpers.ThrowInvalidOperation_NotSimpleName(FullName);
		}
		return new TypeName(null, AssemblyName, this, _declaringType, typeArguments);
	}

	private TypeName MakeElementTypeName(int rankOrModifier)
	{
		return new TypeName(null, AssemblyName, this, null, System.Collections.Immutable.ImmutableArray<TypeName>.Empty, rankOrModifier);
	}
}
