namespace System.Reflection.PortableExecutable;

internal sealed class CoffHeader
{
	internal const int Size = 20;

	public System.Reflection.PortableExecutable.Machine Machine { get; }

	public short NumberOfSections { get; }

	public int TimeDateStamp { get; }

	public int PointerToSymbolTable { get; }

	public int NumberOfSymbols { get; }

	public short SizeOfOptionalHeader { get; }

	public System.Reflection.PortableExecutable.Characteristics Characteristics { get; }

	internal CoffHeader(ref System.Reflection.PortableExecutable.PEBinaryReader reader)
	{
		Machine = (System.Reflection.PortableExecutable.Machine)reader.ReadUInt16();
		NumberOfSections = reader.ReadInt16();
		TimeDateStamp = reader.ReadInt32();
		PointerToSymbolTable = reader.ReadInt32();
		NumberOfSymbols = reader.ReadInt32();
		SizeOfOptionalHeader = reader.ReadInt16();
		Characteristics = (System.Reflection.PortableExecutable.Characteristics)reader.ReadUInt16();
	}
}
