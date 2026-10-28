using System.Collections.Immutable;

namespace System.Reflection.PortableExecutable;

internal readonly struct PdbChecksumDebugDirectoryData
{
	public string AlgorithmName { get; }

	public System.Collections.Immutable.ImmutableArray<byte> Checksum { get; }

	internal PdbChecksumDebugDirectoryData(string algorithmName, System.Collections.Immutable.ImmutableArray<byte> checksum)
	{
		AlgorithmName = algorithmName;
		Checksum = checksum;
	}
}
