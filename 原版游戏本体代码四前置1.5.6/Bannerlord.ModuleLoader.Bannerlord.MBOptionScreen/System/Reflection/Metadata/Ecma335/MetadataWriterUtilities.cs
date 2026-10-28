using System.Collections.Immutable;

namespace System.Reflection.Metadata.Ecma335;

internal static class MetadataWriterUtilities
{
	public static SignatureTypeCode GetConstantTypeCode(object? value)
	{
		if (value == null)
		{
			return (SignatureTypeCode)18;
		}
		if (value.GetType() == typeof(int))
		{
			return SignatureTypeCode.Int32;
		}
		if (value.GetType() == typeof(string))
		{
			return SignatureTypeCode.String;
		}
		if (value.GetType() == typeof(bool))
		{
			return SignatureTypeCode.Boolean;
		}
		if (value.GetType() == typeof(char))
		{
			return SignatureTypeCode.Char;
		}
		if (value.GetType() == typeof(byte))
		{
			return SignatureTypeCode.Byte;
		}
		if (value.GetType() == typeof(long))
		{
			return SignatureTypeCode.Int64;
		}
		if (value.GetType() == typeof(double))
		{
			return SignatureTypeCode.Double;
		}
		if (value.GetType() == typeof(short))
		{
			return SignatureTypeCode.Int16;
		}
		if (value.GetType() == typeof(ushort))
		{
			return SignatureTypeCode.UInt16;
		}
		if (value.GetType() == typeof(uint))
		{
			return SignatureTypeCode.UInt32;
		}
		if (value.GetType() == typeof(sbyte))
		{
			return SignatureTypeCode.SByte;
		}
		if (value.GetType() == typeof(ulong))
		{
			return SignatureTypeCode.UInt64;
		}
		if (value.GetType() == typeof(float))
		{
			return SignatureTypeCode.Single;
		}
		throw new ArgumentException(System.SR.Format(System.SR.InvalidConstantValueOfType, value.GetType()), "value");
	}

	internal static void SerializeRowCounts(BlobBuilder writer, System.Collections.Immutable.ImmutableArray<int> rowCounts)
	{
		for (int i = 0; i < rowCounts.Length; i++)
		{
			int num = rowCounts[i];
			if (num > 0)
			{
				writer.WriteInt32(num);
			}
		}
	}
}
