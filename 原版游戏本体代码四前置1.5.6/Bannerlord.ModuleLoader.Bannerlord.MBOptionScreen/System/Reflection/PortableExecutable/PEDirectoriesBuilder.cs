namespace System.Reflection.PortableExecutable;

internal sealed class PEDirectoriesBuilder
{
	public int AddressOfEntryPoint { get; set; }

	public System.Reflection.PortableExecutable.DirectoryEntry ExportTable { get; set; }

	public System.Reflection.PortableExecutable.DirectoryEntry ImportTable { get; set; }

	public System.Reflection.PortableExecutable.DirectoryEntry ResourceTable { get; set; }

	public System.Reflection.PortableExecutable.DirectoryEntry ExceptionTable { get; set; }

	public System.Reflection.PortableExecutable.DirectoryEntry BaseRelocationTable { get; set; }

	public System.Reflection.PortableExecutable.DirectoryEntry DebugTable { get; set; }

	public System.Reflection.PortableExecutable.DirectoryEntry CopyrightTable { get; set; }

	public System.Reflection.PortableExecutable.DirectoryEntry GlobalPointerTable { get; set; }

	public System.Reflection.PortableExecutable.DirectoryEntry ThreadLocalStorageTable { get; set; }

	public System.Reflection.PortableExecutable.DirectoryEntry LoadConfigTable { get; set; }

	public System.Reflection.PortableExecutable.DirectoryEntry BoundImportTable { get; set; }

	public System.Reflection.PortableExecutable.DirectoryEntry ImportAddressTable { get; set; }

	public System.Reflection.PortableExecutable.DirectoryEntry DelayImportTable { get; set; }

	public System.Reflection.PortableExecutable.DirectoryEntry CorHeaderTable { get; set; }
}
