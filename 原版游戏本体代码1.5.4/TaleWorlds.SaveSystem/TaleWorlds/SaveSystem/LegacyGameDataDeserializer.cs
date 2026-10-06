using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace TaleWorlds.SaveSystem;

public static class LegacyGameDataDeserializer
{
	public static GameData Deserialize(Stream stream)
	{
		Dictionary<int, object> dictionary = new Dictionary<int, object>();
		using MemoryStream memoryStream = new MemoryStream();
		using (DeflateStream deflateStream = new DeflateStream(stream, CompressionMode.Decompress, leaveOpen: true))
		{
			deflateStream.CopyTo(memoryStream);
		}
		memoryStream.Position = 0L;
		using BinaryReader binaryReader = new BinaryReader(memoryStream);
		if (binaryReader.ReadByte() != 0)
		{
			throw new InvalidDataException("Expected SerializationHeaderRecord.");
		}
		binaryReader.BaseStream.Position += 16L;
		if (binaryReader.ReadByte() != 12)
		{
			throw new InvalidDataException("Expected BinaryLibrary record.");
		}
		binaryReader.ReadInt32();
		binaryReader.ReadString();
		if (binaryReader.ReadByte() != 5)
		{
			throw new InvalidDataException("Expected GameData ClassWithMembersAndTypes record.");
		}
		binaryReader.ReadInt32();
		binaryReader.ReadString();
		int num = binaryReader.ReadInt32();
		for (int i = 0; i < num; i++)
		{
			binaryReader.ReadString();
		}
		binaryReader.ReadBytes(4);
		binaryReader.ReadBytes(2);
		binaryReader.ReadString();
		binaryReader.ReadString();
		binaryReader.ReadInt32();
		if (binaryReader.ReadByte() != 9)
		{
			throw new InvalidDataException("Expected MemberReference.");
		}
		int key = binaryReader.ReadInt32();
		if (binaryReader.ReadByte() != 9)
		{
			throw new InvalidDataException("Expected MemberReference.");
		}
		int key2 = binaryReader.ReadInt32();
		if (binaryReader.ReadByte() != 9)
		{
			throw new InvalidDataException("Expected MemberReference.");
		}
		int key3 = binaryReader.ReadInt32();
		if (binaryReader.ReadByte() != 9)
		{
			throw new InvalidDataException("Expected MemberReference.");
		}
		int key4 = binaryReader.ReadInt32();
		while (binaryReader.BaseStream.Position < binaryReader.BaseStream.Length)
		{
			byte b = binaryReader.ReadByte();
			if (b == 11)
			{
				break;
			}
			int key5 = binaryReader.ReadInt32();
			switch (b)
			{
			case 15:
			{
				int count = binaryReader.ReadInt32();
				binaryReader.ReadByte();
				dictionary[key5] = binaryReader.ReadBytes(count);
				break;
			}
			case 7:
			{
				binaryReader.ReadByte();
				binaryReader.ReadInt32();
				int num2 = binaryReader.ReadInt32();
				if (binaryReader.ReadByte() == 7)
				{
					binaryReader.ReadByte();
				}
				int[] array = new int[num2];
				for (int j = 0; j < num2; j++)
				{
					if (binaryReader.ReadByte() != 9)
					{
						throw new InvalidDataException("Expected MemberReference for jagged array element.");
					}
					array[j] = binaryReader.ReadInt32();
				}
				dictionary[key5] = array;
				break;
			}
			}
		}
		GameData gameData = new GameData
		{
			Header = (byte[])dictionary[key],
			Strings = (byte[])dictionary[key2]
		};
		int[] array2 = (int[])dictionary[key3];
		gameData.ObjectData = new byte[array2.Length][];
		for (int k = 0; k < array2.Length; k++)
		{
			gameData.ObjectData[k] = (byte[])dictionary[array2[k]];
		}
		int[] array3 = (int[])dictionary[key4];
		gameData.ContainerData = new byte[array3.Length][];
		for (int l = 0; l < array3.Length; l++)
		{
			gameData.ContainerData[l] = (byte[])dictionary[array3[l]];
		}
		return gameData;
	}
}
