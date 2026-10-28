using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace System.Reflection.Metadata;

internal static class TypeNameParserHelpers
{
	internal const int SZArray = -1;

	internal const int Pointer = -2;

	internal const int ByRef = -3;

	private const char EscapeCharacter = '\\';

	internal static string GetGenericTypeFullName(ReadOnlySpan<char> fullTypeName, ReadOnlySpan<TypeName> genericArgs)
	{
		Span<char> initialBuffer = stackalloc char[128];
		ValueStringBuilder valueStringBuilder = new ValueStringBuilder(initialBuffer);
		valueStringBuilder.Append(fullTypeName);
		valueStringBuilder.Append('[');
		ReadOnlySpan<TypeName> readOnlySpan = genericArgs;
		for (int i = 0; i < readOnlySpan.Length; i++)
		{
			TypeName typeName = readOnlySpan[i];
			valueStringBuilder.Append('[');
			valueStringBuilder.Append(typeName.AssemblyQualifiedName);
			valueStringBuilder.Append(']');
			valueStringBuilder.Append(',');
		}
		valueStringBuilder[valueStringBuilder.Length - 1] = ']';
		return valueStringBuilder.ToString();
	}

	internal static int GetFullTypeNameLength(ReadOnlySpan<char> input, out bool isNestedType)
	{
		isNestedType = false;
		int num = GetUnescapedOffset(input, 0);
		isNestedType = num > 0 && num < input.Length && input[num] == '+';
		return num;
		static int GetUnescapedOffset(ReadOnlySpan<char> readOnlySpan, int startOffset)
		{
			int i;
			for (i = startOffset; i < readOnlySpan.Length; i++)
			{
				char c = readOnlySpan[i];
				if (c == '\\')
				{
					i++;
					if (i == readOnlySpan.Length || !NeedsEscaping(readOnlySpan[i]))
					{
						return -1;
					}
				}
				else if (NeedsEscaping(c))
				{
					break;
				}
			}
			return i;
		}
		static bool NeedsEscaping(char c)
		{
			switch (c)
			{
			case '&':
			case '*':
			case '+':
			case ',':
			case '[':
			case '\\':
			case ']':
				return true;
			default:
				return false;
			}
		}
	}

	internal static ReadOnlySpan<char> GetName(ReadOnlySpan<char> fullName)
	{
		int num = fullName.LastIndexOfAny('.', '+');
		if (num > 0 && fullName[num - 1] == '\\')
		{
			num = GetUnescapedOffset(fullName, num);
		}
		if (num >= 0)
		{
			return fullName.Slice(num + 1);
		}
		return fullName;
		static int GetUnescapedOffset(ReadOnlySpan<char> readOnlySpan, int startIndex)
		{
			int num2;
			for (num2 = startIndex; num2 >= 0; num2--)
			{
				char c = readOnlySpan[num2];
				if ((c == '+' || c == '.') ? true : false)
				{
					if (num2 == 0 || readOnlySpan[num2 - 1] != '\\')
					{
						break;
					}
					num2--;
				}
			}
			return num2;
		}
	}

	internal static ReadOnlySpan<char> GetAssemblyNameCandidate(ReadOnlySpan<char> input)
	{
		int num = input.IndexOf(']');
		if (num > 0 && input[num - 1] == '\\')
		{
			num = GetUnescapedOffset(input, num);
		}
		if (num >= 0)
		{
			return input.Slice(0, num);
		}
		return input;
		static int GetUnescapedOffset(ReadOnlySpan<char> readOnlySpan, int startIndex)
		{
			int i;
			for (i = startIndex; i < readOnlySpan.Length && (readOnlySpan[i] != ']' || readOnlySpan[i - 1] == '\\'); i++)
			{
			}
			return i;
		}
	}

	internal static string GetRankOrModifierStringRepresentation(int rankOrModifier, ref ValueStringBuilder builder)
	{
		switch (rankOrModifier)
		{
		case -3:
			builder.Append('&');
			break;
		case -2:
			builder.Append('*');
			break;
		case -1:
			builder.Append("[]");
			break;
		case 1:
			builder.Append("[*]");
			break;
		default:
			builder.Append('[');
			builder.Append(',', rankOrModifier - 1);
			builder.Append(']');
			break;
		}
		return builder.ToString();
	}

	internal static bool IsBeginningOfGenericArgs(ref ReadOnlySpan<char> span, out bool doubleBrackets)
	{
		doubleBrackets = false;
		if (!span.IsEmpty && span[0] == '[')
		{
			ReadOnlySpan<char> readOnlySpan = span.Slice(1).TrimStart();
			if (!readOnlySpan.IsEmpty)
			{
				if (readOnlySpan[0] == '[')
				{
					doubleBrackets = true;
					span = readOnlySpan.Slice(1).TrimStart();
					return true;
				}
				char c = readOnlySpan[0];
				if ((c != '*' && c != ',' && c != ']') || 1 == 0)
				{
					span = readOnlySpan;
					return true;
				}
			}
		}
		return false;
	}

	internal static bool TryGetTypeNameInfo(TypeNameParseOptions options, ref ReadOnlySpan<char> input, ref List<int>? nestedNameLengths, ref int recursiveDepth, out int totalLength)
	{
		totalLength = 0;
		bool isNestedType;
		do
		{
			int fullTypeNameLength = GetFullTypeNameLength(input.Slice(totalLength), out isNestedType);
			if (fullTypeNameLength <= 0)
			{
				return false;
			}
			if (isNestedType)
			{
				if (!TryDive(options, ref recursiveDepth))
				{
					return false;
				}
				(nestedNameLengths ?? (nestedNameLengths = new List<int>())).Add(fullTypeNameLength);
				totalLength++;
			}
			totalLength += fullTypeNameLength;
		}
		while (isNestedType);
		return true;
	}

	internal static bool TryParseNextDecorator(ref ReadOnlySpan<char> input, out int rankOrModifier)
	{
		ReadOnlySpan<char> readOnlySpan = input;
		if (TryStripFirstCharAndTrailingSpaces(ref input, '*'))
		{
			rankOrModifier = -2;
			return true;
		}
		if (TryStripFirstCharAndTrailingSpaces(ref input, '&'))
		{
			rankOrModifier = -3;
			return true;
		}
		if (TryStripFirstCharAndTrailingSpaces(ref input, '['))
		{
			int num = 1;
			bool flag = false;
			while (true)
			{
				if (TryStripFirstCharAndTrailingSpaces(ref input, ']'))
				{
					rankOrModifier = ((num == 1 && !flag) ? (-1) : num);
					return true;
				}
				if (flag)
				{
					break;
				}
				if (num == 1 && TryStripFirstCharAndTrailingSpaces(ref input, '*'))
				{
					flag = true;
					continue;
				}
				if (!TryStripFirstCharAndTrailingSpaces(ref input, ','))
				{
					break;
				}
				num = checked(num + 1);
			}
		}
		input = readOnlySpan;
		rankOrModifier = 0;
		return false;
	}

	internal static bool TryStripFirstCharAndTrailingSpaces(ref ReadOnlySpan<char> span, char value)
	{
		if (!span.IsEmpty && span[0] == value)
		{
			span = span.Slice(1).TrimStart();
			return true;
		}
		return false;
	}

	[System.Diagnostics.CodeAnalysis.DoesNotReturn]
	internal static void ThrowArgumentException_InvalidTypeName(int errorIndex)
	{
		throw new ArgumentException(System.SR.Argument_InvalidTypeName, $"typeName@{errorIndex}");
	}

	[System.Diagnostics.CodeAnalysis.DoesNotReturn]
	internal static void ThrowInvalidOperation_MaxNodesExceeded(int limit)
	{
		throw new InvalidOperationException(System.SR.Format(System.SR.InvalidOperation_MaxNodesExceeded, limit));
	}

	[System.Diagnostics.CodeAnalysis.DoesNotReturn]
	internal static void ThrowInvalidOperation_NotGenericType()
	{
		throw new InvalidOperationException(System.SR.InvalidOperation_NotGenericType);
	}

	[System.Diagnostics.CodeAnalysis.DoesNotReturn]
	internal static void ThrowInvalidOperation_NotNestedType()
	{
		throw new InvalidOperationException(System.SR.InvalidOperation_NotNestedType);
	}

	[System.Diagnostics.CodeAnalysis.DoesNotReturn]
	internal static void ThrowInvalidOperation_NoElement()
	{
		throw new InvalidOperationException(System.SR.InvalidOperation_NoElement);
	}

	[System.Diagnostics.CodeAnalysis.DoesNotReturn]
	internal static void ThrowInvalidOperation_HasToBeArrayClass()
	{
		throw new InvalidOperationException(System.SR.Argument_HasToBeArrayClass);
	}

	internal static bool IsMaxDepthExceeded(TypeNameParseOptions options, int depth)
	{
		return depth > options.MaxNodes;
	}

	internal static bool TryDive(TypeNameParseOptions options, ref int depth)
	{
		depth++;
		return !IsMaxDepthExceeded(options, depth);
	}

	[System.Diagnostics.CodeAnalysis.DoesNotReturn]
	internal static void ThrowInvalidOperation_NotSimpleName(string fullName)
	{
		throw new InvalidOperationException(System.SR.Format(System.SR.Arg_NotSimpleTypeName, fullName));
	}
}
