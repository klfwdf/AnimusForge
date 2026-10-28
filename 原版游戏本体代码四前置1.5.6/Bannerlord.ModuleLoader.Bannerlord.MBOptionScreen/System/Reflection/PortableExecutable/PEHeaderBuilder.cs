using System.Reflection.Internal;

namespace System.Reflection.PortableExecutable;

internal sealed class PEHeaderBuilder
{
	public System.Reflection.PortableExecutable.Machine Machine { get; }

	public System.Reflection.PortableExecutable.Characteristics ImageCharacteristics { get; }

	public byte MajorLinkerVersion { get; }

	public byte MinorLinkerVersion { get; }

	public ulong ImageBase { get; }

	public int SectionAlignment { get; }

	public int FileAlignment { get; }

	public ushort MajorOperatingSystemVersion { get; }

	public ushort MinorOperatingSystemVersion { get; }

	public ushort MajorImageVersion { get; }

	public ushort MinorImageVersion { get; }

	public ushort MajorSubsystemVersion { get; }

	public ushort MinorSubsystemVersion { get; }

	public System.Reflection.PortableExecutable.Subsystem Subsystem { get; }

	public System.Reflection.PortableExecutable.DllCharacteristics DllCharacteristics { get; }

	public ulong SizeOfStackReserve { get; }

	public ulong SizeOfStackCommit { get; }

	public ulong SizeOfHeapReserve { get; }

	public ulong SizeOfHeapCommit { get; }

	internal bool Is32Bit
	{
		get
		{
			if (Machine != System.Reflection.PortableExecutable.Machine.Amd64 && Machine != System.Reflection.PortableExecutable.Machine.IA64 && Machine != System.Reflection.PortableExecutable.Machine.Arm64 && Machine != System.Reflection.PortableExecutable.Machine.LoongArch64)
			{
				return Machine != System.Reflection.PortableExecutable.Machine.RiscV64;
			}
			return false;
		}
	}

	public PEHeaderBuilder(System.Reflection.PortableExecutable.Machine machine = System.Reflection.PortableExecutable.Machine.Unknown, int sectionAlignment = 8192, int fileAlignment = 512, ulong imageBase = 4194304uL, byte majorLinkerVersion = 48, byte minorLinkerVersion = 0, ushort majorOperatingSystemVersion = 4, ushort minorOperatingSystemVersion = 0, ushort majorImageVersion = 0, ushort minorImageVersion = 0, ushort majorSubsystemVersion = 4, ushort minorSubsystemVersion = 0, System.Reflection.PortableExecutable.Subsystem subsystem = System.Reflection.PortableExecutable.Subsystem.WindowsCui, System.Reflection.PortableExecutable.DllCharacteristics dllCharacteristics = System.Reflection.PortableExecutable.DllCharacteristics.DynamicBase | System.Reflection.PortableExecutable.DllCharacteristics.NxCompatible | System.Reflection.PortableExecutable.DllCharacteristics.NoSeh | System.Reflection.PortableExecutable.DllCharacteristics.TerminalServerAware, System.Reflection.PortableExecutable.Characteristics imageCharacteristics = System.Reflection.PortableExecutable.Characteristics.Dll, ulong sizeOfStackReserve = 1048576uL, ulong sizeOfStackCommit = 4096uL, ulong sizeOfHeapReserve = 1048576uL, ulong sizeOfHeapCommit = 4096uL)
	{
		if (fileAlignment < 512 || fileAlignment > 65536 || BitArithmetic.CountBits(fileAlignment) != 1)
		{
			System.Reflection.Throw.ArgumentOutOfRange("fileAlignment");
		}
		if (sectionAlignment < fileAlignment || BitArithmetic.CountBits(sectionAlignment) != 1)
		{
			System.Reflection.Throw.ArgumentOutOfRange("sectionAlignment");
		}
		Machine = machine;
		SectionAlignment = sectionAlignment;
		FileAlignment = fileAlignment;
		ImageBase = imageBase;
		MajorLinkerVersion = majorLinkerVersion;
		MinorLinkerVersion = minorLinkerVersion;
		MajorOperatingSystemVersion = majorOperatingSystemVersion;
		MinorOperatingSystemVersion = minorOperatingSystemVersion;
		MajorImageVersion = majorImageVersion;
		MinorImageVersion = minorImageVersion;
		MajorSubsystemVersion = majorSubsystemVersion;
		MinorSubsystemVersion = minorSubsystemVersion;
		Subsystem = subsystem;
		DllCharacteristics = dllCharacteristics;
		ImageCharacteristics = imageCharacteristics;
		SizeOfStackReserve = sizeOfStackReserve;
		SizeOfStackCommit = sizeOfStackCommit;
		SizeOfHeapReserve = sizeOfHeapReserve;
		SizeOfHeapCommit = sizeOfHeapCommit;
	}

	public static PEHeaderBuilder CreateExecutableHeader()
	{
		return new PEHeaderBuilder(System.Reflection.PortableExecutable.Machine.Unknown, 8192, 512, 4194304uL, 48, 0, 4, 0, 0, 0, 4, 0, System.Reflection.PortableExecutable.Subsystem.WindowsCui, System.Reflection.PortableExecutable.DllCharacteristics.DynamicBase | System.Reflection.PortableExecutable.DllCharacteristics.NxCompatible | System.Reflection.PortableExecutable.DllCharacteristics.NoSeh | System.Reflection.PortableExecutable.DllCharacteristics.TerminalServerAware, System.Reflection.PortableExecutable.Characteristics.ExecutableImage, 1048576uL, 4096uL, 1048576uL, 4096uL);
	}

	public static PEHeaderBuilder CreateLibraryHeader()
	{
		return new PEHeaderBuilder(System.Reflection.PortableExecutable.Machine.Unknown, 8192, 512, 4194304uL, 48, 0, 4, 0, 0, 0, 4, 0, System.Reflection.PortableExecutable.Subsystem.WindowsCui, System.Reflection.PortableExecutable.DllCharacteristics.DynamicBase | System.Reflection.PortableExecutable.DllCharacteristics.NxCompatible | System.Reflection.PortableExecutable.DllCharacteristics.NoSeh | System.Reflection.PortableExecutable.DllCharacteristics.TerminalServerAware, System.Reflection.PortableExecutable.Characteristics.ExecutableImage | System.Reflection.PortableExecutable.Characteristics.Dll, 1048576uL, 4096uL, 1048576uL, 4096uL);
	}

	internal int ComputeSizeOfPEHeaders(int sectionCount)
	{
		return 152 + System.Reflection.PortableExecutable.PEHeader.Size(Is32Bit) + 40 * sectionCount;
	}
}
