namespace System.Reflection.PortableExecutable;

internal sealed class PEHeader
{
	internal const int OffsetOfChecksum = 64;

	public System.Reflection.PortableExecutable.PEMagic Magic { get; }

	public byte MajorLinkerVersion { get; }

	public byte MinorLinkerVersion { get; }

	public int SizeOfCode { get; }

	public int SizeOfInitializedData { get; }

	public int SizeOfUninitializedData { get; }

	public int AddressOfEntryPoint { get; }

	public int BaseOfCode { get; }

	public int BaseOfData { get; }

	public ulong ImageBase { get; }

	public int SectionAlignment { get; }

	public int FileAlignment { get; }

	public ushort MajorOperatingSystemVersion { get; }

	public ushort MinorOperatingSystemVersion { get; }

	public ushort MajorImageVersion { get; }

	public ushort MinorImageVersion { get; }

	public ushort MajorSubsystemVersion { get; }

	public ushort MinorSubsystemVersion { get; }

	public int SizeOfImage { get; }

	public int SizeOfHeaders { get; }

	public uint CheckSum { get; }

	public System.Reflection.PortableExecutable.Subsystem Subsystem { get; }

	public System.Reflection.PortableExecutable.DllCharacteristics DllCharacteristics { get; }

	public ulong SizeOfStackReserve { get; }

	public ulong SizeOfStackCommit { get; }

	public ulong SizeOfHeapReserve { get; }

	public ulong SizeOfHeapCommit { get; }

	public int NumberOfRvaAndSizes { get; }

	public System.Reflection.PortableExecutable.DirectoryEntry ExportTableDirectory { get; }

	public System.Reflection.PortableExecutable.DirectoryEntry ImportTableDirectory { get; }

	public System.Reflection.PortableExecutable.DirectoryEntry ResourceTableDirectory { get; }

	public System.Reflection.PortableExecutable.DirectoryEntry ExceptionTableDirectory { get; }

	public System.Reflection.PortableExecutable.DirectoryEntry CertificateTableDirectory { get; }

	public System.Reflection.PortableExecutable.DirectoryEntry BaseRelocationTableDirectory { get; }

	public System.Reflection.PortableExecutable.DirectoryEntry DebugTableDirectory { get; }

	public System.Reflection.PortableExecutable.DirectoryEntry CopyrightTableDirectory { get; }

	public System.Reflection.PortableExecutable.DirectoryEntry GlobalPointerTableDirectory { get; }

	public System.Reflection.PortableExecutable.DirectoryEntry ThreadLocalStorageTableDirectory { get; }

	public System.Reflection.PortableExecutable.DirectoryEntry LoadConfigTableDirectory { get; }

	public System.Reflection.PortableExecutable.DirectoryEntry BoundImportTableDirectory { get; }

	public System.Reflection.PortableExecutable.DirectoryEntry ImportAddressTableDirectory { get; }

	public System.Reflection.PortableExecutable.DirectoryEntry DelayImportTableDirectory { get; }

	public System.Reflection.PortableExecutable.DirectoryEntry CorHeaderTableDirectory { get; }

	internal static int Size(bool is32Bit)
	{
		return 72 + 4 * (is32Bit ? 4 : 8) + 4 + 4 + 128;
	}

	internal PEHeader(ref System.Reflection.PortableExecutable.PEBinaryReader reader)
	{
		System.Reflection.PortableExecutable.PEMagic pEMagic = (System.Reflection.PortableExecutable.PEMagic)reader.ReadUInt16();
		if (pEMagic != System.Reflection.PortableExecutable.PEMagic.PE32 && pEMagic != System.Reflection.PortableExecutable.PEMagic.PE32Plus)
		{
			throw new BadImageFormatException(System.SR.UnknownPEMagicValue);
		}
		Magic = pEMagic;
		MajorLinkerVersion = reader.ReadByte();
		MinorLinkerVersion = reader.ReadByte();
		SizeOfCode = reader.ReadInt32();
		SizeOfInitializedData = reader.ReadInt32();
		SizeOfUninitializedData = reader.ReadInt32();
		AddressOfEntryPoint = reader.ReadInt32();
		BaseOfCode = reader.ReadInt32();
		if (pEMagic == System.Reflection.PortableExecutable.PEMagic.PE32Plus)
		{
			BaseOfData = 0;
		}
		else
		{
			BaseOfData = reader.ReadInt32();
		}
		if (pEMagic == System.Reflection.PortableExecutable.PEMagic.PE32Plus)
		{
			ImageBase = reader.ReadUInt64();
		}
		else
		{
			ImageBase = reader.ReadUInt32();
		}
		SectionAlignment = reader.ReadInt32();
		FileAlignment = reader.ReadInt32();
		MajorOperatingSystemVersion = reader.ReadUInt16();
		MinorOperatingSystemVersion = reader.ReadUInt16();
		MajorImageVersion = reader.ReadUInt16();
		MinorImageVersion = reader.ReadUInt16();
		MajorSubsystemVersion = reader.ReadUInt16();
		MinorSubsystemVersion = reader.ReadUInt16();
		reader.ReadUInt32();
		SizeOfImage = reader.ReadInt32();
		SizeOfHeaders = reader.ReadInt32();
		CheckSum = reader.ReadUInt32();
		Subsystem = (System.Reflection.PortableExecutable.Subsystem)reader.ReadUInt16();
		DllCharacteristics = (System.Reflection.PortableExecutable.DllCharacteristics)reader.ReadUInt16();
		if (pEMagic == System.Reflection.PortableExecutable.PEMagic.PE32Plus)
		{
			SizeOfStackReserve = reader.ReadUInt64();
			SizeOfStackCommit = reader.ReadUInt64();
			SizeOfHeapReserve = reader.ReadUInt64();
			SizeOfHeapCommit = reader.ReadUInt64();
		}
		else
		{
			SizeOfStackReserve = reader.ReadUInt32();
			SizeOfStackCommit = reader.ReadUInt32();
			SizeOfHeapReserve = reader.ReadUInt32();
			SizeOfHeapCommit = reader.ReadUInt32();
		}
		reader.ReadUInt32();
		NumberOfRvaAndSizes = reader.ReadInt32();
		ExportTableDirectory = new System.Reflection.PortableExecutable.DirectoryEntry(ref reader);
		ImportTableDirectory = new System.Reflection.PortableExecutable.DirectoryEntry(ref reader);
		ResourceTableDirectory = new System.Reflection.PortableExecutable.DirectoryEntry(ref reader);
		ExceptionTableDirectory = new System.Reflection.PortableExecutable.DirectoryEntry(ref reader);
		CertificateTableDirectory = new System.Reflection.PortableExecutable.DirectoryEntry(ref reader);
		BaseRelocationTableDirectory = new System.Reflection.PortableExecutable.DirectoryEntry(ref reader);
		DebugTableDirectory = new System.Reflection.PortableExecutable.DirectoryEntry(ref reader);
		CopyrightTableDirectory = new System.Reflection.PortableExecutable.DirectoryEntry(ref reader);
		GlobalPointerTableDirectory = new System.Reflection.PortableExecutable.DirectoryEntry(ref reader);
		ThreadLocalStorageTableDirectory = new System.Reflection.PortableExecutable.DirectoryEntry(ref reader);
		LoadConfigTableDirectory = new System.Reflection.PortableExecutable.DirectoryEntry(ref reader);
		BoundImportTableDirectory = new System.Reflection.PortableExecutable.DirectoryEntry(ref reader);
		ImportAddressTableDirectory = new System.Reflection.PortableExecutable.DirectoryEntry(ref reader);
		DelayImportTableDirectory = new System.Reflection.PortableExecutable.DirectoryEntry(ref reader);
		CorHeaderTableDirectory = new System.Reflection.PortableExecutable.DirectoryEntry(ref reader);
		new System.Reflection.PortableExecutable.DirectoryEntry(ref reader);
	}
}
