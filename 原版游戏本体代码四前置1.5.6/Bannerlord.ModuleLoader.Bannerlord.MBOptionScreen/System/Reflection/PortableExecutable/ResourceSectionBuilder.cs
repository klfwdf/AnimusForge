using System.Reflection.Metadata;

namespace System.Reflection.PortableExecutable;

internal abstract class ResourceSectionBuilder
{
	protected internal abstract void Serialize(BlobBuilder builder, SectionLocation location);
}
