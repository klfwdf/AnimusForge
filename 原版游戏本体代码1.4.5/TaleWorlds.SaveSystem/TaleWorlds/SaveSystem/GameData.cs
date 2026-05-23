using System;
using System.IO;
using System.Linq;
using TaleWorlds.Library;

namespace TaleWorlds.SaveSystem;

[Serializable]
public class GameData
{
	public byte[] Header { get; internal set; }

	public byte[] Strings { get; internal set; }

	public byte[][] ObjectData { get; internal set; }

	public byte[][] ContainerData { get; internal set; }

	public int TotalSize
	{
		get
		{
			int num = Header.Length;
			num += Strings.Length;
			for (int i = 0; i < ObjectData.Length; i++)
			{
				num += ObjectData[i].Length;
			}
			for (int j = 0; j < ContainerData.Length; j++)
			{
				num += ContainerData[j].Length;
			}
			return num;
		}
	}

	public GameData(byte[] header, byte[] strings, byte[][] objectData, byte[][] containerData)
	{
		Header = header;
		Strings = strings;
		ObjectData = objectData;
		ContainerData = containerData;
	}

	public GameData()
	{
	}

	public void Inspect()
	{
		Debug.Print($"Header Size: {Header.Length} Strings Size: {Strings.Length} Object Size: {ObjectData.Length} Container Size: {ContainerData.Length}");
		float num = (float)TotalSize / 1048576f;
		Debug.Print($"Total size: {num:##.00} MB");
	}

	public static GameData CreateFrom(byte[] readBytes)
	{
		TaleWorlds.Library.BinaryReader binaryReader = new TaleWorlds.Library.BinaryReader(readBytes);
		int length = binaryReader.ReadInt();
		byte[] header = binaryReader.ReadBytes(length);
		int length2 = binaryReader.ReadInt();
		byte[] strings = binaryReader.ReadBytes(length2);
		int num = binaryReader.ReadInt();
		byte[][] array = new byte[num][];
		for (int i = 0; i < num; i++)
		{
			int length3 = binaryReader.ReadInt();
			byte[] array2 = binaryReader.ReadBytes(length3);
			array[i] = array2;
		}
		int num2 = binaryReader.ReadInt();
		byte[][] array3 = new byte[num2][];
		for (int j = 0; j < num2; j++)
		{
			int length4 = binaryReader.ReadInt();
			byte[] array4 = binaryReader.ReadBytes(length4);
			array3[j] = array4;
		}
		return new GameData(header, strings, array, array3);
	}

	public byte[] GetData()
	{
		TaleWorlds.Library.BinaryWriter binaryWriter = new TaleWorlds.Library.BinaryWriter();
		binaryWriter.WriteInt(Header.Length);
		binaryWriter.WriteBytes(Header);
		binaryWriter.WriteInt(Strings.Length);
		binaryWriter.WriteBytes(Strings);
		binaryWriter.WriteInt(ObjectData.Length);
		byte[][] objectData = ObjectData;
		foreach (byte[] array in objectData)
		{
			binaryWriter.WriteInt(array.Length);
			binaryWriter.WriteBytes(array);
		}
		binaryWriter.WriteInt(ContainerData.Length);
		objectData = ContainerData;
		foreach (byte[] array2 in objectData)
		{
			binaryWriter.WriteInt(array2.Length);
			binaryWriter.WriteBytes(array2);
		}
		return binaryWriter.GetFinalData();
	}

	public static void Write(System.IO.BinaryWriter writer, GameData gameData)
	{
		Debug.Print("---------------SAVE STATISTICS---------------");
		Debug.Print($"[GameData.Write] WRITING LITTLE ENDIAN: {BitConverter.IsLittleEndian}");
		Debug.Print($"[GameData.Write] Header Length: {gameData.Header.Length}");
		writer.Write(gameData.Header.Length);
		writer.Write(gameData.Header);
		Debug.Print($"[GameData.Write] ObjectData Length: {gameData.ObjectData.Length}");
		Debug.Print($"[GameData.Write] ObjectData Total Size: {gameData.ObjectData.Sum((byte[] x) => x.Length)}");
		writer.Write(gameData.ObjectData.Length);
		byte[][] objectData = gameData.ObjectData;
		foreach (byte[] array in objectData)
		{
			writer.Write(array.Length);
			writer.Write(array);
		}
		Debug.Print($"[GameData.Write] ContainerData Length: {gameData.ContainerData.Length}");
		Debug.Print($"[GameData.Write] ContainerData Total Size: {gameData.ContainerData.Sum((byte[] x) => x.Length)}");
		writer.Write(gameData.ContainerData.Length);
		objectData = gameData.ContainerData;
		foreach (byte[] array2 in objectData)
		{
			writer.Write(array2.Length);
			writer.Write(array2);
		}
		Debug.Print($"[GameData.Write] Strings Length: {gameData.Strings.Length}");
		writer.Write(gameData.Strings.Length);
		writer.Write(gameData.Strings);
		Debug.Print("---------------SAVE STATISTICS---------------");
	}

	public static GameData Read(System.IO.BinaryReader reader)
	{
		Debug.Print("---------------LOAD STATISTICS---------------");
		int num = reader.ReadInt32();
		Debug.Print($"[GameData.Read] Header Length: {num}");
		byte[] header = reader.ReadBytes(num);
		int num2 = reader.ReadInt32();
		Debug.Print($"[GameData.Read] Object Length: {num2}");
		byte[][] array = new byte[num2][];
		for (int i = 0; i < num2; i++)
		{
			int count = reader.ReadInt32();
			array[i] = reader.ReadBytes(count);
		}
		Debug.Print($"[GameData.Read] ObjectData Total Size: {array.Sum((byte[] x) => x.Length)}");
		int num3 = reader.ReadInt32();
		Debug.Print($"[GameData.Read] Container Length: {num3}");
		byte[][] array2 = new byte[num3][];
		for (int num4 = 0; num4 < num3; num4++)
		{
			int count2 = reader.ReadInt32();
			array2[num4] = reader.ReadBytes(count2);
		}
		Debug.Print($"[GameData.Read] ContainerData Total Size: {array2.Sum((byte[] x) => x.Length)}");
		int num5 = reader.ReadInt32();
		Debug.Print($"[GameData.Read] String Length: {num5}");
		byte[] strings = reader.ReadBytes(num5);
		return new GameData(header, strings, array, array2);
	}

	public bool IsEqualTo(GameData gameData)
	{
		bool num = CompareByteArrays(Header, gameData.Header, "Header");
		bool flag = CompareByteArrays(Strings, gameData.Strings, "Strings");
		bool flag2 = CompareByteArrays(ObjectData, gameData.ObjectData, "ObjectData");
		bool flag3 = CompareByteArrays(ContainerData, gameData.ContainerData, "ContainerData");
		return num && flag && flag2 && flag3;
	}

	private bool CompareByteArrays(byte[] arr1, byte[] arr2, string name)
	{
		if (arr1.Length != arr2.Length)
		{
			Debug.FailedAssert(name + " failed length comparison.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.SaveSystem\\GameData.cs", "CompareByteArrays", 212);
			return false;
		}
		for (int i = 0; i < arr1.Length; i++)
		{
			if (arr1[i] != arr2[i])
			{
				Debug.FailedAssert($"{name} failed byte comparison at index {i}.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.SaveSystem\\GameData.cs", "CompareByteArrays", 220);
				return false;
			}
		}
		return true;
	}

	private bool CompareByteArrays(byte[][] arr1, byte[][] arr2, string name)
	{
		if (arr1.Length != arr2.Length)
		{
			Debug.FailedAssert(name + " failed length comparison.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.SaveSystem\\GameData.cs", "CompareByteArrays", 231);
			return false;
		}
		for (int i = 0; i < arr1.Length; i++)
		{
			if (!CompareByteArrays(arr1[i], arr2[i], name + $" Index: {i}"))
			{
				return false;
			}
		}
		return true;
	}
}
