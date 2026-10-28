using System.Text;

namespace System.Reflection;

internal static class AssemblyNameFormatter
{
	public static string ComputeDisplayName(string name, Version? version, string? cultureName, byte[]? pkt, AssemblyNameFlags flags = AssemblyNameFlags.None, AssemblyContentType contentType = AssemblyContentType.Default, byte[]? pk = null)
	{
		Span<char> initialBuffer = stackalloc char[256];
		ValueStringBuilder vsb = new ValueStringBuilder(initialBuffer);
		vsb.AppendQuoted(name);
		if (version != null)
		{
			ushort num = (ushort)version.Major;
			if (num != ushort.MaxValue)
			{
				vsb.Append(", Version=");
				vsb.AppendSpanFormattable(num);
				ushort num2 = (ushort)version.Minor;
				if (num2 != ushort.MaxValue)
				{
					vsb.Append('.');
					vsb.AppendSpanFormattable(num2);
					ushort num3 = (ushort)version.Build;
					if (num3 != ushort.MaxValue)
					{
						vsb.Append('.');
						vsb.AppendSpanFormattable(num3);
						ushort num4 = (ushort)version.Revision;
						if (num4 != ushort.MaxValue)
						{
							vsb.Append('.');
							vsb.AppendSpanFormattable(num4);
						}
					}
				}
			}
		}
		if (cultureName != null)
		{
			if (cultureName.Length == 0)
			{
				cultureName = "neutral";
			}
			vsb.Append(", Culture=");
			vsb.AppendQuoted(cultureName);
		}
		byte[] array = pkt ?? pk;
		if (array != null)
		{
			if (pkt != null)
			{
				if (pkt.Length > 8)
				{
					throw new ArgumentException();
				}
				vsb.Append(", PublicKeyToken=");
			}
			else
			{
				vsb.Append(", PublicKey=");
			}
			if (array.Length == 0)
			{
				vsb.Append("null");
			}
			else
			{
				HexConverter.EncodeToUtf16(array, vsb.AppendSpan(array.Length * 2), HexConverter.Casing.Lower);
			}
		}
		if ((flags & AssemblyNameFlags.Retargetable) != AssemblyNameFlags.None)
		{
			vsb.Append(", Retargetable=Yes");
		}
		if (contentType == AssemblyContentType.WindowsRuntime)
		{
			vsb.Append(", ContentType=WindowsRuntime");
		}
		return vsb.ToString();
	}

	private static void AppendQuoted(this ref ValueStringBuilder vsb, string s)
	{
		bool flag = false;
		ReadOnlySpan<char> span = s.AsSpan();
		if (s.Length != span.Trim().Length || span.IndexOfAny('"', '\'') >= 0)
		{
			flag = true;
		}
		if (flag)
		{
			vsb.Append('"');
		}
		for (int i = 0; i < s.Length; i++)
		{
			switch (s[i])
			{
			case '"':
			case '\'':
			case ',':
			case '=':
			case '\\':
				vsb.Append('\\');
				break;
			case '\t':
				vsb.Append("\\t");
				continue;
			case '\r':
				vsb.Append("\\r");
				continue;
			case '\n':
				vsb.Append("\\n");
				continue;
			}
			vsb.Append(s[i]);
		}
		if (flag)
		{
			vsb.Append('"');
		}
	}

	private static void AppendSpanFormattable(this ref ValueStringBuilder vsb, ushort value)
	{
		vsb.Append(value.ToString());
	}
}
