using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

namespace System.Reflection.Metadata;

[DebuggerDisplay("{_inputString}")]
internal ref struct TypeNameParser
{
	private static readonly TypeNameParseOptions s_defaults = new TypeNameParseOptions();

	private readonly bool _throwOnError;

	private readonly TypeNameParseOptions _parseOptions;

	private ReadOnlySpan<char> _inputString;

	private TypeNameParser(ReadOnlySpan<char> name, bool throwOnError, TypeNameParseOptions options)
	{
		this = default(TypeNameParser);
		_inputString = name;
		_throwOnError = throwOnError;
		_parseOptions = options ?? s_defaults;
	}

	internal static TypeName? Parse(ReadOnlySpan<char> typeName, bool throwOnError, TypeNameParseOptions? options = null)
	{
		ReadOnlySpan<char> name = typeName.TrimStart();
		if (name.IsEmpty)
		{
			if (throwOnError)
			{
				TypeNameParserHelpers.ThrowArgumentException_InvalidTypeName(0);
			}
			return null;
		}
		int recursiveDepth = 0;
		TypeNameParser typeNameParser = new TypeNameParser(name, throwOnError, options);
		TypeName typeName2 = typeNameParser.ParseNextTypeName(allowFullyQualifiedName: true, ref recursiveDepth);
		if (typeName2 == null || !typeNameParser._inputString.IsEmpty)
		{
			if (throwOnError)
			{
				if (TypeNameParserHelpers.IsMaxDepthExceeded(typeNameParser._parseOptions, recursiveDepth))
				{
					TypeNameParserHelpers.ThrowInvalidOperation_MaxNodesExceeded(typeNameParser._parseOptions.MaxNodes);
				}
				TypeNameParserHelpers.ThrowArgumentException_InvalidTypeName(typeName.Length - typeNameParser._inputString.Length);
			}
			return null;
		}
		return typeName2;
	}

	private TypeName ParseNextTypeName(bool allowFullyQualifiedName, ref int recursiveDepth)
	{
		if (!TypeNameParserHelpers.TryDive(_parseOptions, ref recursiveDepth))
		{
			return null;
		}
		List<int> nestedNameLengths = null;
		if (!TypeNameParserHelpers.TryGetTypeNameInfo(_parseOptions, ref _inputString, ref nestedNameLengths, ref recursiveDepth, out var totalLength))
		{
			return null;
		}
		ReadOnlySpan<char> readOnlySpan = _inputString.Slice(0, totalLength);
		_inputString = _inputString.Slice(totalLength);
		System.Collections.Immutable.ImmutableArray<TypeName>.Builder builder = null;
		ReadOnlySpan<char> input = _inputString;
		if (TypeNameParserHelpers.IsBeginningOfGenericArgs(ref _inputString, out var doubleBrackets))
		{
			while (true)
			{
				TypeName typeName = ParseNextTypeName(doubleBrackets, ref recursiveDepth);
				if (typeName == null)
				{
					return null;
				}
				if (doubleBrackets && !TypeNameParserHelpers.TryStripFirstCharAndTrailingSpaces(ref _inputString, ']'))
				{
					return null;
				}
				if (builder == null)
				{
					builder = System.Collections.Immutable.ImmutableArray.CreateBuilder<TypeName>(2);
				}
				builder.Add(typeName);
				if (!TypeNameParserHelpers.TryStripFirstCharAndTrailingSpaces(ref _inputString, ','))
				{
					break;
				}
				doubleBrackets = TypeNameParserHelpers.TryStripFirstCharAndTrailingSpaces(ref _inputString, '[');
			}
			if (!TypeNameParserHelpers.TryStripFirstCharAndTrailingSpaces(ref _inputString, ']'))
			{
				return null;
			}
		}
		if (builder == null)
		{
			_inputString = input;
		}
		else if (!TypeNameParserHelpers.TryDive(_parseOptions, ref recursiveDepth))
		{
			return null;
		}
		int num = 0;
		input = _inputString;
		int rankOrModifier;
		while (TypeNameParserHelpers.TryParseNextDecorator(ref _inputString, out rankOrModifier))
		{
			if (!TypeNameParserHelpers.TryDive(_parseOptions, ref recursiveDepth))
			{
				return null;
			}
			num = rankOrModifier;
		}
		AssemblyNameInfo assemblyName = null;
		if (allowFullyQualifiedName && !TryParseAssemblyName(ref assemblyName))
		{
			return null;
		}
		string text = readOnlySpan.ToString();
		TypeName declaringType = GetDeclaringType(text, nestedNameLengths, assemblyName);
		TypeName typeName2 = new TypeName(text, assemblyName, null, declaringType);
		if (builder != null)
		{
			typeName2 = new TypeName(null, assemblyName, typeName2, declaringType, builder);
		}
		if (num != 0)
		{
			int rankOrModifier2;
			while (TypeNameParserHelpers.TryParseNextDecorator(ref input, out rankOrModifier2))
			{
				typeName2 = new TypeName(null, assemblyName, typeName2, null, null, rankOrModifier2);
			}
		}
		return typeName2;
	}

	private bool TryParseAssemblyName(ref AssemblyNameInfo assemblyName)
	{
		ReadOnlySpan<char> inputString = _inputString;
		if (TypeNameParserHelpers.TryStripFirstCharAndTrailingSpaces(ref _inputString, ','))
		{
			if (_inputString.IsEmpty)
			{
				_inputString = inputString;
				return false;
			}
			ReadOnlySpan<char> assemblyNameCandidate = TypeNameParserHelpers.GetAssemblyNameCandidate(_inputString);
			if (!AssemblyNameInfo.TryParse(assemblyNameCandidate, out assemblyName))
			{
				return false;
			}
			_inputString = _inputString.Slice(assemblyNameCandidate.Length);
			return true;
		}
		return true;
	}

	private static TypeName GetDeclaringType(string fullTypeName, List<int> nestedNameLengths, AssemblyNameInfo assemblyName)
	{
		if (nestedNameLengths == null)
		{
			return null;
		}
		TypeName typeName = null;
		int num = 0;
		foreach (int nestedNameLength2 in nestedNameLengths)
		{
			int nestedNameLength = num + nestedNameLength2;
			typeName = new TypeName(fullTypeName, assemblyName, null, typeName, null, 0, nestedNameLength);
			num += nestedNameLength2 + 1;
		}
		return typeName;
	}
}
