using System.ComponentModel;
using System.Reflection.PortableExecutable;

namespace System.Reflection.Metadata;

[EditorBrowsable(EditorBrowsableState.Never)]
internal static class PEReaderExtensions
{
	public static MethodBodyBlock GetMethodBody(this System.Reflection.PortableExecutable.PEReader peReader, int relativeVirtualAddress)
	{
		if (peReader == null)
		{
			System.Reflection.Throw.ArgumentNull("peReader");
		}
		System.Reflection.PortableExecutable.PEMemoryBlock sectionData = peReader.GetSectionData(relativeVirtualAddress);
		if (sectionData.Length == 0)
		{
			throw new BadImageFormatException(System.SR.Format(System.SR.InvalidMethodRva, relativeVirtualAddress));
		}
		return MethodBodyBlock.Create(sectionData.GetReader());
	}

	public static System.Reflection.Metadata.MetadataReader GetMetadataReader(this System.Reflection.PortableExecutable.PEReader peReader)
	{
		return peReader.GetMetadataReader(System.Reflection.Metadata.MetadataReaderOptions.Default, null);
	}

	public static System.Reflection.Metadata.MetadataReader GetMetadataReader(this System.Reflection.PortableExecutable.PEReader peReader, System.Reflection.Metadata.MetadataReaderOptions options)
	{
		return peReader.GetMetadataReader(options, null);
	}

	public unsafe static System.Reflection.Metadata.MetadataReader GetMetadataReader(this System.Reflection.PortableExecutable.PEReader peReader, System.Reflection.Metadata.MetadataReaderOptions options, MetadataStringDecoder? utf8Decoder)
	{
		if (peReader == null)
		{
			System.Reflection.Throw.ArgumentNull("peReader");
		}
		System.Reflection.PortableExecutable.PEMemoryBlock metadata = peReader.GetMetadata();
		return new System.Reflection.Metadata.MetadataReader(metadata.Pointer, metadata.Length, options, utf8Decoder, peReader);
	}
}
