using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection.Internal;
using System.Reflection.Metadata;

namespace System.Reflection.PortableExecutable;

internal abstract class PEBuilder
{
	protected readonly struct Section
	{
		public readonly string Name;

		public readonly System.Reflection.PortableExecutable.SectionCharacteristics Characteristics;

		public Section(string name, System.Reflection.PortableExecutable.SectionCharacteristics characteristics)
		{
			if (name == null)
			{
				System.Reflection.Throw.ArgumentNull("name");
			}
			Name = name;
			Characteristics = characteristics;
		}
	}

	private readonly struct SerializedSection(BlobBuilder builder, string name, System.Reflection.PortableExecutable.SectionCharacteristics characteristics, int relativeVirtualAddress, int sizeOfRawData, int pointerToRawData)
	{
		public readonly BlobBuilder Builder = builder;

		public readonly string Name = name;

		public readonly System.Reflection.PortableExecutable.SectionCharacteristics Characteristics = characteristics;

		public readonly int RelativeVirtualAddress = relativeVirtualAddress;

		public readonly int SizeOfRawData = sizeOfRawData;

		public readonly int PointerToRawData = pointerToRawData;

		public int VirtualSize => Builder.Count;
	}

	private readonly Lazy<System.Collections.Immutable.ImmutableArray<Section>> _lazySections;

	private Blob _lazyChecksum;

	internal const int DosHeaderSize = 128;

	public PEHeaderBuilder Header { get; }

	public Func<IEnumerable<Blob>, System.Reflection.Metadata.BlobContentId> IdProvider { get; }

	public bool IsDeterministic { get; }

	private static ReadOnlySpan<byte> DosHeader => new byte[128]
	{
		77, 90, 144, 0, 3, 0, 0, 0, 4, 0,
		0, 0, 255, 255, 0, 0, 184, 0, 0, 0,
		0, 0, 0, 0, 64, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		128, 0, 0, 0, 14, 31, 186, 14, 0, 180,
		9, 205, 33, 184, 1, 76, 205, 33, 84, 104,
		105, 115, 32, 112, 114, 111, 103, 114, 97, 109,
		32, 99, 97, 110, 110, 111, 116, 32, 98, 101,
		32, 114, 117, 110, 32, 105, 110, 32, 68, 79,
		83, 32, 109, 111, 100, 101, 46, 13, 13, 10,
		36, 0, 0, 0, 0, 0, 0, 0
	};

	protected PEBuilder(PEHeaderBuilder header, Func<IEnumerable<Blob>, System.Reflection.Metadata.BlobContentId>? deterministicIdProvider)
	{
		if (header == null)
		{
			System.Reflection.Throw.ArgumentNull("header");
		}
		IdProvider = deterministicIdProvider ?? System.Reflection.Metadata.BlobContentId.GetTimeBasedProvider();
		IsDeterministic = deterministicIdProvider != null;
		Header = header;
		_lazySections = new Lazy<System.Collections.Immutable.ImmutableArray<Section>>(CreateSections);
	}

	protected System.Collections.Immutable.ImmutableArray<Section> GetSections()
	{
		System.Collections.Immutable.ImmutableArray<Section> value = _lazySections.Value;
		if (value.IsDefault)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.MustNotReturnNull, "CreateSections"));
		}
		return value;
	}

	protected abstract System.Collections.Immutable.ImmutableArray<Section> CreateSections();

	protected abstract BlobBuilder SerializeSection(string name, SectionLocation location);

	protected internal abstract PEDirectoriesBuilder GetDirectories();

	public System.Reflection.Metadata.BlobContentId Serialize(BlobBuilder builder)
	{
		System.Collections.Immutable.ImmutableArray<SerializedSection> immutableArray = SerializeSections();
		PEDirectoriesBuilder directories = GetDirectories();
		WritePESignature(builder);
		WriteCoffHeader(builder, immutableArray, out var stampFixup);
		WritePEHeader(builder, directories, immutableArray);
		WriteSectionHeaders(builder, immutableArray);
		builder.Align(Header.FileAlignment);
		foreach (SerializedSection item in immutableArray)
		{
			builder.LinkSuffix(item.Builder);
			builder.Align(Header.FileAlignment);
		}
		System.Reflection.Metadata.BlobContentId result = IdProvider(builder.GetBlobs());
		new BlobWriter(stampFixup).WriteUInt32(result.Stamp);
		return result;
	}

	private System.Collections.Immutable.ImmutableArray<SerializedSection> SerializeSections()
	{
		System.Collections.Immutable.ImmutableArray<Section> sections = GetSections();
		System.Collections.Immutable.ImmutableArray<SerializedSection>.Builder builder = System.Collections.Immutable.ImmutableArray.CreateBuilder<SerializedSection>(sections.Length);
		int position = Header.ComputeSizeOfPEHeaders(sections.Length);
		int relativeVirtualAddress = BitArithmetic.Align(position, Header.SectionAlignment);
		int pointerToRawData = BitArithmetic.Align(position, Header.FileAlignment);
		foreach (Section item2 in sections)
		{
			BlobBuilder blobBuilder = SerializeSection(item2.Name, new SectionLocation(relativeVirtualAddress, pointerToRawData));
			SerializedSection item = new SerializedSection(blobBuilder, item2.Name, item2.Characteristics, relativeVirtualAddress, BitArithmetic.Align(blobBuilder.Count, Header.FileAlignment), pointerToRawData);
			builder.Add(item);
			relativeVirtualAddress = BitArithmetic.Align(item.RelativeVirtualAddress + item.VirtualSize, Header.SectionAlignment);
			pointerToRawData = item.PointerToRawData + item.SizeOfRawData;
		}
		return builder.MoveToImmutable();
	}

	private unsafe static void WritePESignature(BlobBuilder builder)
	{
		ReadOnlySpan<byte> dosHeader = DosHeader;
		fixed (byte* buffer = dosHeader)
		{
			builder.WriteBytes(buffer, dosHeader.Length);
		}
		builder.WriteUInt32(17744u);
	}

	private void WriteCoffHeader(BlobBuilder builder, System.Collections.Immutable.ImmutableArray<SerializedSection> sections, out Blob stampFixup)
	{
		builder.WriteUInt16((ushort)((Header.Machine == System.Reflection.PortableExecutable.Machine.Unknown) ? System.Reflection.PortableExecutable.Machine.I386 : Header.Machine));
		builder.WriteUInt16((ushort)sections.Length);
		stampFixup = builder.ReserveBytes(4);
		builder.WriteUInt32(0u);
		builder.WriteUInt32(0u);
		builder.WriteUInt16((ushort)System.Reflection.PortableExecutable.PEHeader.Size(Header.Is32Bit));
		builder.WriteUInt16((ushort)Header.ImageCharacteristics);
	}

	private void WritePEHeader(BlobBuilder builder, PEDirectoriesBuilder directories, System.Collections.Immutable.ImmutableArray<SerializedSection> sections)
	{
		builder.WriteUInt16((ushort)(Header.Is32Bit ? 267 : 523));
		builder.WriteByte(Header.MajorLinkerVersion);
		builder.WriteByte(Header.MinorLinkerVersion);
		builder.WriteUInt32((uint)SumRawDataSizes(sections, System.Reflection.PortableExecutable.SectionCharacteristics.ContainsCode));
		builder.WriteUInt32((uint)SumRawDataSizes(sections, System.Reflection.PortableExecutable.SectionCharacteristics.ContainsInitializedData));
		builder.WriteUInt32((uint)SumRawDataSizes(sections, System.Reflection.PortableExecutable.SectionCharacteristics.ContainsUninitializedData));
		builder.WriteUInt32((uint)directories.AddressOfEntryPoint);
		int num = IndexOfSection(sections, System.Reflection.PortableExecutable.SectionCharacteristics.ContainsCode);
		builder.WriteUInt32((num != -1) ? ((uint)sections[num].RelativeVirtualAddress) : 0u);
		if (Header.Is32Bit)
		{
			int num2 = IndexOfSection(sections, System.Reflection.PortableExecutable.SectionCharacteristics.ContainsInitializedData);
			builder.WriteUInt32((num2 != -1) ? ((uint)sections[num2].RelativeVirtualAddress) : 0u);
			builder.WriteUInt32((uint)Header.ImageBase);
		}
		else
		{
			builder.WriteUInt64(Header.ImageBase);
		}
		builder.WriteUInt32((uint)Header.SectionAlignment);
		builder.WriteUInt32((uint)Header.FileAlignment);
		builder.WriteUInt16(Header.MajorOperatingSystemVersion);
		builder.WriteUInt16(Header.MinorOperatingSystemVersion);
		builder.WriteUInt16(Header.MajorImageVersion);
		builder.WriteUInt16(Header.MinorImageVersion);
		builder.WriteUInt16(Header.MajorSubsystemVersion);
		builder.WriteUInt16(Header.MinorSubsystemVersion);
		builder.WriteUInt32(0u);
		SerializedSection serializedSection = sections[sections.Length - 1];
		builder.WriteUInt32((uint)BitArithmetic.Align(serializedSection.RelativeVirtualAddress + serializedSection.VirtualSize, Header.SectionAlignment));
		builder.WriteUInt32((uint)BitArithmetic.Align(Header.ComputeSizeOfPEHeaders(sections.Length), Header.FileAlignment));
		_lazyChecksum = builder.ReserveBytes(4);
		new BlobWriter(_lazyChecksum).WriteUInt32(0u);
		builder.WriteUInt16((ushort)Header.Subsystem);
		builder.WriteUInt16((ushort)Header.DllCharacteristics);
		if (Header.Is32Bit)
		{
			builder.WriteUInt32((uint)Header.SizeOfStackReserve);
			builder.WriteUInt32((uint)Header.SizeOfStackCommit);
			builder.WriteUInt32((uint)Header.SizeOfHeapReserve);
			builder.WriteUInt32((uint)Header.SizeOfHeapCommit);
		}
		else
		{
			builder.WriteUInt64(Header.SizeOfStackReserve);
			builder.WriteUInt64(Header.SizeOfStackCommit);
			builder.WriteUInt64(Header.SizeOfHeapReserve);
			builder.WriteUInt64(Header.SizeOfHeapCommit);
		}
		builder.WriteUInt32(0u);
		builder.WriteUInt32(16u);
		builder.WriteUInt32((uint)directories.ExportTable.RelativeVirtualAddress);
		builder.WriteUInt32((uint)directories.ExportTable.Size);
		builder.WriteUInt32((uint)directories.ImportTable.RelativeVirtualAddress);
		builder.WriteUInt32((uint)directories.ImportTable.Size);
		builder.WriteUInt32((uint)directories.ResourceTable.RelativeVirtualAddress);
		builder.WriteUInt32((uint)directories.ResourceTable.Size);
		builder.WriteUInt32((uint)directories.ExceptionTable.RelativeVirtualAddress);
		builder.WriteUInt32((uint)directories.ExceptionTable.Size);
		builder.WriteUInt32(0u);
		builder.WriteUInt32(0u);
		builder.WriteUInt32((uint)directories.BaseRelocationTable.RelativeVirtualAddress);
		builder.WriteUInt32((uint)directories.BaseRelocationTable.Size);
		builder.WriteUInt32((uint)directories.DebugTable.RelativeVirtualAddress);
		builder.WriteUInt32((uint)directories.DebugTable.Size);
		builder.WriteUInt32((uint)directories.CopyrightTable.RelativeVirtualAddress);
		builder.WriteUInt32((uint)directories.CopyrightTable.Size);
		builder.WriteUInt32((uint)directories.GlobalPointerTable.RelativeVirtualAddress);
		builder.WriteUInt32((uint)directories.GlobalPointerTable.Size);
		builder.WriteUInt32((uint)directories.ThreadLocalStorageTable.RelativeVirtualAddress);
		builder.WriteUInt32((uint)directories.ThreadLocalStorageTable.Size);
		builder.WriteUInt32((uint)directories.LoadConfigTable.RelativeVirtualAddress);
		builder.WriteUInt32((uint)directories.LoadConfigTable.Size);
		builder.WriteUInt32((uint)directories.BoundImportTable.RelativeVirtualAddress);
		builder.WriteUInt32((uint)directories.BoundImportTable.Size);
		builder.WriteUInt32((uint)directories.ImportAddressTable.RelativeVirtualAddress);
		builder.WriteUInt32((uint)directories.ImportAddressTable.Size);
		builder.WriteUInt32((uint)directories.DelayImportTable.RelativeVirtualAddress);
		builder.WriteUInt32((uint)directories.DelayImportTable.Size);
		builder.WriteUInt32((uint)directories.CorHeaderTable.RelativeVirtualAddress);
		builder.WriteUInt32((uint)directories.CorHeaderTable.Size);
		builder.WriteUInt64(0uL);
	}

	private static void WriteSectionHeaders(BlobBuilder builder, System.Collections.Immutable.ImmutableArray<SerializedSection> serializedSections)
	{
		foreach (SerializedSection item in serializedSections)
		{
			WriteSectionHeader(builder, item);
		}
	}

	private static void WriteSectionHeader(BlobBuilder builder, SerializedSection serializedSection)
	{
		if (serializedSection.VirtualSize == 0)
		{
			return;
		}
		int i = 0;
		int length = serializedSection.Name.Length;
		for (; i < 8; i++)
		{
			if (i < length)
			{
				builder.WriteByte((byte)serializedSection.Name[i]);
			}
			else
			{
				builder.WriteByte(0);
			}
		}
		builder.WriteUInt32((uint)serializedSection.VirtualSize);
		builder.WriteUInt32((uint)serializedSection.RelativeVirtualAddress);
		builder.WriteUInt32((uint)serializedSection.SizeOfRawData);
		builder.WriteUInt32((uint)serializedSection.PointerToRawData);
		builder.WriteUInt32(0u);
		builder.WriteUInt32(0u);
		builder.WriteUInt16(0);
		builder.WriteUInt16(0);
		builder.WriteUInt32((uint)serializedSection.Characteristics);
	}

	private static int IndexOfSection(System.Collections.Immutable.ImmutableArray<SerializedSection> sections, System.Reflection.PortableExecutable.SectionCharacteristics characteristics)
	{
		for (int i = 0; i < sections.Length; i++)
		{
			if ((sections[i].Characteristics & characteristics) == characteristics)
			{
				return i;
			}
		}
		return -1;
	}

	private static int SumRawDataSizes(System.Collections.Immutable.ImmutableArray<SerializedSection> sections, System.Reflection.PortableExecutable.SectionCharacteristics characteristics)
	{
		int num = 0;
		for (int i = 0; i < sections.Length; i++)
		{
			if ((sections[i].Characteristics & characteristics) == characteristics)
			{
				num += sections[i].SizeOfRawData;
			}
		}
		return num;
	}

	internal static IEnumerable<Blob> GetContentToSign(BlobBuilder peImage, int peHeadersSize, int peHeaderAlignment, Blob strongNameSignatureFixup)
	{
		int remainingHeaderToSign = peHeadersSize;
		int remainingHeader = BitArithmetic.Align(peHeadersSize, peHeaderAlignment);
		foreach (Blob blob in peImage.GetBlobs())
		{
			int blobStart = blob.Start;
			int blobLength = blob.Length;
			while (blobLength > 0)
			{
				if (remainingHeader > 0)
				{
					int length;
					if (remainingHeaderToSign > 0)
					{
						length = Math.Min(remainingHeaderToSign, blobLength);
						yield return new Blob(blob.Buffer, blobStart, length);
						remainingHeaderToSign -= length;
					}
					else
					{
						length = Math.Min(remainingHeader, blobLength);
					}
					remainingHeader -= length;
					blobStart += length;
					blobLength -= length;
					continue;
				}
				if (blob.Buffer == strongNameSignatureFixup.Buffer)
				{
					yield return GetPrefixBlob(new Blob(blob.Buffer, blobStart, blobLength), strongNameSignatureFixup);
					yield return GetSuffixBlob(new Blob(blob.Buffer, blobStart, blobLength), strongNameSignatureFixup);
				}
				else
				{
					yield return new Blob(blob.Buffer, blobStart, blobLength);
				}
				break;
			}
		}
	}

	internal static Blob GetPrefixBlob(Blob container, Blob blob)
	{
		return new Blob(container.Buffer, container.Start, blob.Start - container.Start);
	}

	internal static Blob GetSuffixBlob(Blob container, Blob blob)
	{
		return new Blob(container.Buffer, blob.Start + blob.Length, container.Start + container.Length - blob.Start - blob.Length);
	}

	internal static IEnumerable<Blob> GetContentToChecksum(BlobBuilder peImage, Blob checksumFixup)
	{
		foreach (Blob blob in peImage.GetBlobs())
		{
			if (blob.Buffer == checksumFixup.Buffer)
			{
				yield return GetPrefixBlob(blob, checksumFixup);
				yield return GetSuffixBlob(blob, checksumFixup);
			}
			else
			{
				yield return blob;
			}
		}
	}

	internal void Sign(BlobBuilder peImage, Blob strongNameSignatureFixup, Func<IEnumerable<Blob>, byte[]> signatureProvider)
	{
		int peHeadersSize = Header.ComputeSizeOfPEHeaders(GetSections().Length);
		byte[] array = signatureProvider(GetContentToSign(peImage, peHeadersSize, Header.FileAlignment, strongNameSignatureFixup));
		if (array == null || array.Length > strongNameSignatureFixup.Length)
		{
			throw new InvalidOperationException(System.SR.SignatureProviderReturnedInvalidSignature);
		}
		new BlobWriter(strongNameSignatureFixup).WriteBytes(array);
		uint value = CalculateChecksum(peImage, _lazyChecksum);
		new BlobWriter(_lazyChecksum).WriteUInt32(value);
	}

	internal static uint CalculateChecksum(BlobBuilder peImage, Blob checksumFixup)
	{
		return CalculateChecksum(GetContentToChecksum(peImage, checksumFixup)) + (uint)peImage.Count;
	}

	private unsafe static uint CalculateChecksum(IEnumerable<Blob> blobs)
	{
		uint num = 0u;
		int num2 = -1;
		foreach (Blob blob in blobs)
		{
			ArraySegment<byte> bytes = blob.GetBytes();
			fixed (byte* array = bytes.Array)
			{
				byte* ptr = array + bytes.Offset;
				byte* ptr2 = ptr + bytes.Count;
				if (num2 >= 0)
				{
					num = AggregateChecksum(num, (ushort)((*ptr << 8) | num2));
					ptr++;
				}
				if ((ptr2 - ptr) % 2 != 0L)
				{
					ptr2--;
					num2 = *ptr2;
				}
				else
				{
					num2 = -1;
				}
				for (; ptr < ptr2; ptr += 2)
				{
					num = AggregateChecksum(num, (ushort)((ptr[1] << 8) | *ptr));
				}
			}
		}
		if (num2 >= 0)
		{
			num = AggregateChecksum(num, (ushort)num2);
		}
		return num;
	}

	private static uint AggregateChecksum(uint checksum, ushort value)
	{
		uint num = checksum + value;
		return (num >> 16) + (ushort)num;
	}
}
