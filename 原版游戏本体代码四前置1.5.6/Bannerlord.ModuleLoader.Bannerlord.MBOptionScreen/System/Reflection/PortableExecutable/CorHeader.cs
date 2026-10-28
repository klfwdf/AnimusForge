namespace System.Reflection.PortableExecutable;

internal sealed class CorHeader
{
	public ushort MajorRuntimeVersion { get; }

	public ushort MinorRuntimeVersion { get; }

	public System.Reflection.PortableExecutable.DirectoryEntry MetadataDirectory { get; }

	public System.Reflection.PortableExecutable.CorFlags Flags { get; }

	public int EntryPointTokenOrRelativeVirtualAddress { get; }

	public System.Reflection.PortableExecutable.DirectoryEntry ResourcesDirectory { get; }

	public System.Reflection.PortableExecutable.DirectoryEntry StrongNameSignatureDirectory { get; }

	public System.Reflection.PortableExecutable.DirectoryEntry CodeManagerTableDirectory { get; }

	public System.Reflection.PortableExecutable.DirectoryEntry VtableFixupsDirectory { get; }

	public System.Reflection.PortableExecutable.DirectoryEntry ExportAddressTableJumpsDirectory { get; }

	public System.Reflection.PortableExecutable.DirectoryEntry ManagedNativeHeaderDirectory { get; }

	internal CorHeader(ref System.Reflection.PortableExecutable.PEBinaryReader reader)
	{
		reader.ReadInt32();
		MajorRuntimeVersion = reader.ReadUInt16();
		MinorRuntimeVersion = reader.ReadUInt16();
		MetadataDirectory = new System.Reflection.PortableExecutable.DirectoryEntry(ref reader);
		Flags = (System.Reflection.PortableExecutable.CorFlags)reader.ReadUInt32();
		EntryPointTokenOrRelativeVirtualAddress = reader.ReadInt32();
		ResourcesDirectory = new System.Reflection.PortableExecutable.DirectoryEntry(ref reader);
		StrongNameSignatureDirectory = new System.Reflection.PortableExecutable.DirectoryEntry(ref reader);
		CodeManagerTableDirectory = new System.Reflection.PortableExecutable.DirectoryEntry(ref reader);
		VtableFixupsDirectory = new System.Reflection.PortableExecutable.DirectoryEntry(ref reader);
		ExportAddressTableJumpsDirectory = new System.Reflection.PortableExecutable.DirectoryEntry(ref reader);
		ManagedNativeHeaderDirectory = new System.Reflection.PortableExecutable.DirectoryEntry(ref reader);
	}
}
