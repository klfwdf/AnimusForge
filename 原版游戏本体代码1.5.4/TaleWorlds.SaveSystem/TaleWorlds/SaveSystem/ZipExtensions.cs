using System.IO;
using System.IO.Compression;
using TaleWorlds.Library;

namespace TaleWorlds.SaveSystem;

internal static class ZipExtensions
{
	public static void FillFrom(this ZipArchiveEntry entry, byte[] data)
	{
		using Stream stream = entry.Open();
		stream.Write(data, 0, data.Length);
	}

	public static void FillFrom(this ZipArchiveEntry entry, TaleWorlds.Library.BinaryWriter writer)
	{
		using Stream stream = entry.Open();
		byte[] finalData = writer.GetFinalData();
		stream.Write(finalData, 0, finalData.Length);
	}

	public static TaleWorlds.Library.BinaryReader GetBinaryReader(this ZipArchiveEntry entry)
	{
		TaleWorlds.Library.BinaryReader binaryReader = null;
		using Stream stream = entry.Open();
		using MemoryStream memoryStream = new MemoryStream();
		stream.CopyTo(memoryStream);
		return new TaleWorlds.Library.BinaryReader(memoryStream.ToArray());
	}

	public static byte[] GetBinaryData(this ZipArchiveEntry entry)
	{
		byte[] array = null;
		using Stream stream = entry.Open();
		using MemoryStream memoryStream = new MemoryStream();
		stream.CopyTo(memoryStream);
		return memoryStream.ToArray();
	}
}
