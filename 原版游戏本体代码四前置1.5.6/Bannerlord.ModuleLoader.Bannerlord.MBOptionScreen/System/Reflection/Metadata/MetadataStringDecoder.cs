using System.Text;

namespace System.Reflection.Metadata;

internal class MetadataStringDecoder
{
	public Encoding Encoding { get; }

	public static MetadataStringDecoder DefaultUTF8 { get; } = new MetadataStringDecoder(System.Text.Encoding.UTF8);

	public MetadataStringDecoder(Encoding encoding)
	{
		if (encoding == null)
		{
			System.Reflection.Throw.ArgumentNull("encoding");
		}
		Encoding = encoding;
	}

	public unsafe virtual string GetString(byte* bytes, int byteCount)
	{
		return Encoding.GetString(bytes, byteCount);
	}
}
