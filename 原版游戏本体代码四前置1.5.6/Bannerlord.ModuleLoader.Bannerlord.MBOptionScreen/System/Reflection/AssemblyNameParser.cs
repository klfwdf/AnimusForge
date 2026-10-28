using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.Reflection;

internal ref struct AssemblyNameParser
{
	public readonly struct AssemblyNameParts(string name, Version? version, string? cultureName, AssemblyNameFlags flags, byte[]? publicKeyOrToken)
	{
		public readonly string _name = name;

		public readonly Version? _version = version;

		public readonly string? _cultureName = cultureName;

		public readonly AssemblyNameFlags _flags = flags;

		public readonly byte[]? _publicKeyOrToken = publicKeyOrToken;
	}

	private enum Token
	{
		Equals = 1,
		Comma,
		String,
		End
	}

	private enum AttributeKind
	{
		Version = 1,
		Culture = 2,
		PublicKeyOrToken = 4,
		ProcessorArchitecture = 8,
		Retargetable = 0x10,
		ContentType = 0x20
	}

	private readonly ReadOnlySpan<char> _input;

	private int _index;

	private AssemblyNameParser(ReadOnlySpan<char> input)
	{
		_input = input;
		_index = 0;
	}

	internal static bool TryParse(ReadOnlySpan<char> name, ref AssemblyNameParts parts)
	{
		return new AssemblyNameParser(name).TryParse(ref parts);
	}

	private static bool TryRecordNewSeen(scoped ref AttributeKind seenAttributes, AttributeKind newAttribute)
	{
		if ((seenAttributes & newAttribute) != 0)
		{
			return false;
		}
		seenAttributes |= newAttribute;
		return true;
	}

	private bool TryParse(ref AssemblyNameParts result)
	{
		if (!TryGetNextToken(out var tokenString, out var token) || token != Token.String || string.IsNullOrEmpty(tokenString))
		{
			return false;
		}
		Version version = null;
		string result2 = null;
		byte[] result3 = null;
		AssemblyNameFlags assemblyNameFlags = AssemblyNameFlags.None;
		AttributeKind seenAttributes = (AttributeKind)0;
		if (!TryGetNextToken(out var tokenString2, out token))
		{
			return false;
		}
		do
		{
			switch (token)
			{
			default:
				return false;
			case Token.Comma:
			{
				if (!TryGetNextToken(out var tokenString3, out token) || token != Token.String)
				{
					return false;
				}
				if (!TryGetNextToken(out tokenString2, out token) || token != Token.Equals)
				{
					return false;
				}
				if (!TryGetNextToken(out var tokenString4, out token) || token != Token.String)
				{
					return false;
				}
				if (tokenString3 == string.Empty)
				{
					return false;
				}
				if (IsAttribute(tokenString3, "Version"))
				{
					if (!TryRecordNewSeen(ref seenAttributes, AttributeKind.Version))
					{
						return false;
					}
					if (!TryParseVersion(tokenString4, ref version))
					{
						return false;
					}
				}
				else if (IsAttribute(tokenString3, "Culture"))
				{
					if (!TryRecordNewSeen(ref seenAttributes, AttributeKind.Culture))
					{
						return false;
					}
					if (!TryParseCulture(tokenString4, out result2))
					{
						return false;
					}
				}
				else if (IsAttribute(tokenString3, "PublicKeyToken"))
				{
					if (!TryRecordNewSeen(ref seenAttributes, AttributeKind.PublicKeyOrToken))
					{
						return false;
					}
					if (!TryParsePKT(tokenString4, isToken: true, out result3))
					{
						return false;
					}
				}
				else if (IsAttribute(tokenString3, "PublicKey"))
				{
					if (!TryRecordNewSeen(ref seenAttributes, AttributeKind.PublicKeyOrToken))
					{
						return false;
					}
					if (!TryParsePKT(tokenString4, isToken: false, out result3))
					{
						return false;
					}
					assemblyNameFlags |= AssemblyNameFlags.PublicKey;
				}
				else if (IsAttribute(tokenString3, "ProcessorArchitecture"))
				{
					if (!TryRecordNewSeen(ref seenAttributes, AttributeKind.ProcessorArchitecture))
					{
						return false;
					}
					if (!TryParseProcessorArchitecture(tokenString4, out var result4))
					{
						return false;
					}
					assemblyNameFlags = (AssemblyNameFlags)((int)assemblyNameFlags | ((int)result4 << 4));
				}
				else if (IsAttribute(tokenString3, "Retargetable"))
				{
					if (!TryRecordNewSeen(ref seenAttributes, AttributeKind.Retargetable))
					{
						return false;
					}
					if (tokenString4.Equals("Yes", StringComparison.OrdinalIgnoreCase))
					{
						assemblyNameFlags |= AssemblyNameFlags.Retargetable;
					}
					else if (!tokenString4.Equals("No", StringComparison.OrdinalIgnoreCase))
					{
						return false;
					}
				}
				else if (IsAttribute(tokenString3, "ContentType"))
				{
					if (!TryRecordNewSeen(ref seenAttributes, AttributeKind.ContentType))
					{
						return false;
					}
					if (!tokenString4.Equals("WindowsRuntime", StringComparison.OrdinalIgnoreCase))
					{
						return false;
					}
					assemblyNameFlags |= (AssemblyNameFlags)0x200;
				}
				break;
			}
			case Token.End:
				result = new AssemblyNameParts(tokenString, version, result2, assemblyNameFlags, result3);
				return true;
			}
		}
		while (TryGetNextToken(out tokenString2, out token));
		return false;
	}

	private static bool IsAttribute(string candidate, string attributeKind)
	{
		return candidate.Equals(attributeKind, StringComparison.OrdinalIgnoreCase);
	}

	private static bool TryParseVersion(string attributeValue, ref Version version)
	{
		string[] array = attributeValue.Split('.');
		int num = array.Length;
		if ((num < 2 || num > 4) ? true : false)
		{
			return false;
		}
		Span<ushort> span = new Span<ushort>(new ushort[4] { 65535, 65535, 65535, 65535 });
		for (int i = 0; i < array.Length; i++)
		{
			if (!ushort.TryParse(array[i], NumberStyles.None, NumberFormatInfo.InvariantInfo, out span[i]))
			{
				return false;
			}
		}
		if (span[0] == ushort.MaxValue || span[1] == ushort.MaxValue)
		{
			return false;
		}
		version = ((span[2] == ushort.MaxValue) ? new Version(span[0], span[1]) : ((span[3] == ushort.MaxValue) ? new Version(span[0], span[1], span[2]) : new Version(span[0], span[1], span[2], span[3])));
		return true;
	}

	private static bool TryParseCulture(string attributeValue, out string result)
	{
		if (attributeValue.Equals("Neutral", StringComparison.OrdinalIgnoreCase))
		{
			result = "";
			return true;
		}
		result = attributeValue;
		return true;
	}

	private static bool TryParsePKT(string attributeValue, bool isToken, out byte[] result)
	{
		if (attributeValue.Equals("null", StringComparison.OrdinalIgnoreCase) || attributeValue == string.Empty)
		{
			result = Array.Empty<byte>();
			return true;
		}
		if (attributeValue.Length % 2 != 0 || (isToken && attributeValue.Length != 16))
		{
			result = null;
			return false;
		}
		byte[] array = new byte[attributeValue.Length / 2];
		if (!HexConverter.TryDecodeFromUtf16(attributeValue.AsSpan(), array, out var _))
		{
			result = null;
			return false;
		}
		result = array;
		return true;
	}

	private static bool TryParseProcessorArchitecture(string attributeValue, out ProcessorArchitecture result)
	{
		ProcessorArchitecture processorArchitecture = (attributeValue.Equals("msil", StringComparison.OrdinalIgnoreCase) ? ProcessorArchitecture.MSIL : (attributeValue.Equals("x86", StringComparison.OrdinalIgnoreCase) ? ProcessorArchitecture.X86 : (attributeValue.Equals("ia64", StringComparison.OrdinalIgnoreCase) ? ProcessorArchitecture.IA64 : (attributeValue.Equals("amd64", StringComparison.OrdinalIgnoreCase) ? ProcessorArchitecture.Amd64 : (attributeValue.Equals("arm", StringComparison.OrdinalIgnoreCase) ? ProcessorArchitecture.Arm : (attributeValue.Equals("msil", StringComparison.OrdinalIgnoreCase) ? ProcessorArchitecture.MSIL : ProcessorArchitecture.None))))));
		result = processorArchitecture;
		return result != ProcessorArchitecture.None;
	}

	private static bool IsWhiteSpace(char ch)
	{
		switch (ch)
		{
		case '\t':
		case '\n':
		case '\r':
		case ' ':
			return true;
		default:
			return false;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private bool TryGetNextChar(out char ch)
	{
		if (_index < _input.Length)
		{
			ch = _input[_index++];
			if (ch == '\0')
			{
				return false;
			}
		}
		else
		{
			ch = '\0';
		}
		return true;
	}

	private bool TryGetNextToken(out string tokenString, out Token token)
	{
		tokenString = string.Empty;
		char ch;
		do
		{
			if (!TryGetNextChar(out ch))
			{
				token = (Token)0;
				return false;
			}
			switch (ch)
			{
			case ',':
				token = Token.Comma;
				return true;
			case '=':
				token = Token.Equals;
				return true;
			case '\0':
				token = Token.End;
				return true;
			}
		}
		while (IsWhiteSpace(ch));
		Span<char> initialBuffer = stackalloc char[64];
		bool flag;
		using (ValueStringBuilder valueStringBuilder = new ValueStringBuilder(initialBuffer))
		{
			char c = '\0';
			flag = ((ch == '"' || ch == '\'') ? true : false);
			if (!flag)
			{
				goto IL_0085;
			}
			c = ch;
			if (TryGetNextChar(out ch))
			{
				goto IL_0085;
			}
			token = (Token)0;
			flag = false;
			goto end_IL_0056;
			IL_0085:
			while (true)
			{
				if (ch == '\0')
				{
					if (c != 0)
					{
						token = (Token)0;
						flag = false;
						break;
					}
				}
				else if (c == '\0' || ch != c)
				{
					bool flag2 = c == '\0';
					if (flag2)
					{
						bool flag3 = ((ch == ',' || ch == '=') ? true : false);
						flag2 = flag3;
					}
					if (!flag2)
					{
						flag2 = c == '\0';
						if (flag2)
						{
							bool flag3 = ((ch == '"' || ch == '\'') ? true : false);
							flag2 = flag3;
						}
						if (flag2)
						{
							token = (Token)0;
							flag = false;
							break;
						}
						if (ch == '\\')
						{
							if (!TryGetNextChar(out ch))
							{
								token = (Token)0;
								flag = false;
								break;
							}
							switch (ch)
							{
							case '"':
							case '\'':
							case ',':
							case '=':
							case '\\':
								valueStringBuilder.Append(ch);
								goto IL_01a0;
							case 't':
								valueStringBuilder.Append('\t');
								goto IL_01a0;
							case 'r':
								valueStringBuilder.Append('\r');
								goto IL_01a0;
							case 'n':
								valueStringBuilder.Append('\n');
								goto IL_01a0;
							default:
								token = (Token)0;
								flag = false;
								break;
							}
							break;
						}
						valueStringBuilder.Append(ch);
						goto IL_01a0;
					}
					_index--;
				}
				int num = valueStringBuilder.Length;
				if (c == '\0')
				{
					while (num > 0 && IsWhiteSpace(valueStringBuilder[num - 1]))
					{
						num--;
					}
				}
				tokenString = valueStringBuilder.AsSpan(0, num).ToString();
				token = Token.String;
				flag = true;
				break;
				IL_01a0:
				if (!TryGetNextChar(out ch))
				{
					token = (Token)0;
					flag = false;
					break;
				}
			}
			end_IL_0056:;
		}
		return flag;
	}
}
