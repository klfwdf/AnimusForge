using System;
using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.SaveSystem.Definition;

namespace TaleWorlds.SaveSystem;

public class SaveableBasicTypeDefiner : SaveableTypeDefiner
{
	public SaveableBasicTypeDefiner()
		: base(30000)
	{
	}

	protected internal override void DefineBasicTypes()
	{
		AddBasicTypeDefinition(typeof(int), 1, new IntBasicTypeSerializer());
		AddBasicTypeDefinition(typeof(uint), 2, new UintBasicTypeSerializer());
		AddBasicTypeDefinition(typeof(short), 3, new ShortBasicTypeSerializer());
		AddBasicTypeDefinition(typeof(ushort), 4, new UshortBasicTypeSerializer());
		AddBasicTypeDefinition(typeof(byte), 5, new ByteBasicTypeSerializer());
		AddBasicTypeDefinition(typeof(sbyte), 6, new SbyteBasicTypeSerializer());
		AddBasicTypeDefinition(typeof(float), 7, new FloatBasicTypeSerializer());
		AddBasicTypeDefinition(typeof(double), 8, new DoubleBasicTypeSerializer());
		AddBasicTypeDefinition(typeof(long), 9, new LongBasicTypeSerializer());
		AddBasicTypeDefinition(typeof(ulong), 10, new UlongBasicTypeSerializer());
		AddBasicTypeDefinition(typeof(Vec2), 11, new Vec2BasicTypeSerializer());
		AddBasicTypeDefinition(typeof(Vec2i), 12, new Vec2iBasicTypeSerializer());
		AddBasicTypeDefinition(typeof(Vec3), 13, new Vec3BasicTypeSerializer());
		AddBasicTypeDefinition(typeof(Vec3i), 14, new Vec3iBasicTypeSerializer());
		AddBasicTypeDefinition(typeof(Mat2), 15, new Mat2BasicTypeSerializer());
		AddBasicTypeDefinition(typeof(Mat3), 16, new Mat3BasicTypeSerializer());
		AddBasicTypeDefinition(typeof(MatrixFrame), 17, new MatrixFrameBasicTypeSerializer());
		AddBasicTypeDefinition(typeof(Quaternion), 18, new QuaternionBasicTypeSerializer());
		AddBasicTypeDefinition(typeof(Color), 19, new ColorBasicTypeSerializer());
		AddBasicTypeDefinition(typeof(bool), 20, new BoolBasicTypeSerializer());
		AddBasicTypeDefinition(typeof(string), 21, new StringSerializer());
	}

	protected internal override void DefineClassTypes()
	{
		AddClassDefinition(typeof(object), 0);
		AddClassDefinitionWithCustomFields(typeof(Tuple<, >), 100, new Tuple<string, short>[2]
		{
			new Tuple<string, short>("m_Item1", 1),
			new Tuple<string, short>("m_Item2", 2)
		});
		AddClassDefinitionWithCustomFields(typeof(TaleWorlds.Library.PriorityQueue<, >), 103, new Tuple<string, short>[1]
		{
			new Tuple<string, short>("_baseHeap", 1)
		});
		AddClassDefinitionWithCustomFields(typeof(MBReadOnlyDictionary<, >), 105, new Tuple<string, short>[1]
		{
			new Tuple<string, short>("_dictionary", 1)
		});
		AddClassDefinition(typeof(TaleWorlds.Library.GenericComparer<>), 106);
	}

	protected internal override void DefineStructTypes()
	{
		AddStructDefinitionWithCustomFields(typeof(Nullable<>), 101, new Tuple<string, short>[1]
		{
			new Tuple<string, short>("value", 1)
		});
		AddStructDefinitionWithCustomFields(typeof(KeyValuePair<, >), 102, new Tuple<string, short>[2]
		{
			new Tuple<string, short>("key", 1),
			new Tuple<string, short>("value", 2)
		});
		AddStructDefinitionWithCustomFields(typeof(ValueTuple<, >), 107, new Tuple<string, short>[2]
		{
			new Tuple<string, short>("Item1", 1),
			new Tuple<string, short>("Item2", 2)
		});
	}

	protected internal override void DefineGenericStructDefinitions()
	{
		ConstructGenericStructDefinition(typeof(KeyValuePair<string, string>));
		ConstructGenericStructDefinition(typeof(KeyValuePair<string, int>));
		ConstructGenericStructDefinition(typeof(KeyValuePair<int, string>));
		ConstructGenericStructDefinition(typeof(KeyValuePair<int, int>));
		ConstructGenericStructDefinition(typeof((int, int)));
		ConstructGenericStructDefinition(typeof((string, int)));
	}

	protected internal override void DefineGenericClassDefinitions()
	{
		ConstructGenericClassDefinition(typeof(Tuple<string, int>));
		ConstructGenericClassDefinition(typeof(Tuple<bool, float>));
		ConstructGenericClassDefinition(typeof(TaleWorlds.Library.GenericComparer<int>));
		ConstructGenericClassDefinition(typeof(TaleWorlds.Library.GenericComparer<float>));
	}

	protected internal override void DefineContainerDefinitions()
	{
		ConstructContainerDefinition(typeof(List<int>));
		ConstructContainerDefinition(typeof(List<uint>));
		ConstructContainerDefinition(typeof(List<short>));
		ConstructContainerDefinition(typeof(List<ushort>));
		ConstructContainerDefinition(typeof(List<byte>));
		ConstructContainerDefinition(typeof(List<sbyte>));
		ConstructContainerDefinition(typeof(List<float>));
		ConstructContainerDefinition(typeof(List<double>));
		ConstructContainerDefinition(typeof(List<long>));
		ConstructContainerDefinition(typeof(List<ulong>));
		ConstructContainerDefinition(typeof(List<Vec2>));
		ConstructContainerDefinition(typeof(List<Vec2i>));
		ConstructContainerDefinition(typeof(List<Vec3>));
		ConstructContainerDefinition(typeof(List<Vec3i>));
		ConstructContainerDefinition(typeof(List<Mat2>));
		ConstructContainerDefinition(typeof(List<Mat3>));
		ConstructContainerDefinition(typeof(List<MatrixFrame>));
		ConstructContainerDefinition(typeof(List<Quaternion>));
		ConstructContainerDefinition(typeof(List<Color>));
		ConstructContainerDefinition(typeof(List<bool>));
		ConstructContainerDefinition(typeof(List<string>));
		ConstructContainerDefinition(typeof(List<KeyValuePair<string, string>>));
		ConstructContainerDefinition(typeof(List<KeyValuePair<string, int>>));
		ConstructContainerDefinition(typeof(List<KeyValuePair<int, string>>));
		ConstructContainerDefinition(typeof(List<KeyValuePair<int, int>>));
		ConstructContainerDefinition(typeof(List<Tuple<bool, float>>));
		ConstructContainerDefinition(typeof(Queue<int>));
		ConstructContainerDefinition(typeof(Queue<uint>));
		ConstructContainerDefinition(typeof(Queue<short>));
		ConstructContainerDefinition(typeof(Queue<ushort>));
		ConstructContainerDefinition(typeof(Queue<byte>));
		ConstructContainerDefinition(typeof(Queue<sbyte>));
		ConstructContainerDefinition(typeof(Queue<float>));
		ConstructContainerDefinition(typeof(Queue<double>));
		ConstructContainerDefinition(typeof(Queue<long>));
		ConstructContainerDefinition(typeof(Queue<ulong>));
		ConstructContainerDefinition(typeof(Queue<Vec2>));
		ConstructContainerDefinition(typeof(Queue<Vec2i>));
		ConstructContainerDefinition(typeof(Queue<Vec3>));
		ConstructContainerDefinition(typeof(Queue<Vec3i>));
		ConstructContainerDefinition(typeof(Queue<Mat2>));
		ConstructContainerDefinition(typeof(Queue<Mat3>));
		ConstructContainerDefinition(typeof(Queue<MatrixFrame>));
		ConstructContainerDefinition(typeof(Queue<Quaternion>));
		ConstructContainerDefinition(typeof(Queue<Color>));
		ConstructContainerDefinition(typeof(Queue<bool>));
		ConstructContainerDefinition(typeof(Queue<string>));
		ConstructContainerDefinition(typeof(int[]));
		ConstructContainerDefinition(typeof(uint[]));
		ConstructContainerDefinition(typeof(short[]));
		ConstructContainerDefinition(typeof(ushort[]));
		ConstructContainerDefinition(typeof(byte[]));
		ConstructContainerDefinition(typeof(sbyte[]));
		ConstructContainerDefinition(typeof(float[]));
		ConstructContainerDefinition(typeof(double[]));
		ConstructContainerDefinition(typeof(long[]));
		ConstructContainerDefinition(typeof(ulong[]));
		ConstructContainerDefinition(typeof(Vec2[]));
		ConstructContainerDefinition(typeof(Vec2i[]));
		ConstructContainerDefinition(typeof(Vec3[]));
		ConstructContainerDefinition(typeof(Vec3i[]));
		ConstructContainerDefinition(typeof(Mat2[]));
		ConstructContainerDefinition(typeof(Mat3[]));
		ConstructContainerDefinition(typeof(MatrixFrame[]));
		ConstructContainerDefinition(typeof(Quaternion[]));
		ConstructContainerDefinition(typeof(Color[]));
		ConstructContainerDefinition(typeof(bool[]));
		ConstructContainerDefinition(typeof(string[]));
		ConstructContainerDefinition(typeof(Dictionary<int, string>));
		ConstructContainerDefinition(typeof(Dictionary<string, int>));
		ConstructContainerDefinition(typeof(Dictionary<int, int>));
		ConstructContainerDefinition(typeof(Dictionary<string, string>));
		ConstructContainerDefinition(typeof(Dictionary<long, int>));
		ConstructContainerDefinition(typeof(Dictionary<string, object>));
		ConstructContainerDefinition(typeof(Dictionary<string, float>));
	}
}
