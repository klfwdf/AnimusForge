using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using TaleWorlds.Library;
using TaleWorlds.SaveSystem.Definition;

namespace TaleWorlds.SaveSystem.Save;

public class SaveContext : ISaveContext
{
	private struct SaveDataSizeRecord
	{
		public int HeaderSize;

		public int StringSize;

		public int ObjectSize;

		public int ContainerSize;

		public override string ToString()
		{
			float num = (float)(HeaderSize + StringSize + ObjectSize + ContainerSize) / 1048576f;
			_ = $"Total size: {num:##.00} MB";
			return $"[SaveDataSizeRecord] Header size: {HeaderSize}, String Size: {StringSize}, Object Size: {ObjectSize}, Container Size: {ContainerSize}.\nTotal Size: {num}";
		}
	}

	public struct SaveStatistics
	{
		private Dictionary<string, (int, int, int, long)> _typeStatistics;

		private Dictionary<string, (int, int, int, int, long)> _containerStatistics;

		public SaveStatistics(Dictionary<string, (int, int, int, long)> typeStatistics, Dictionary<string, (int, int, int, int, long)> containerStatistics)
		{
			_typeStatistics = typeStatistics;
			_containerStatistics = containerStatistics;
		}

		public (int, int, int, long) GetObjectCounts(string key)
		{
			if (_typeStatistics.ContainsKey(key))
			{
				return _typeStatistics[key];
			}
			return default((int, int, int, long));
		}

		public (int, int, int, int, long) GetContainerCounts(string key)
		{
			return _containerStatistics[key];
		}

		public long GetContainerSize(string key)
		{
			return _containerStatistics[key].Item5;
		}

		public List<string> GetTypeKeys()
		{
			return _typeStatistics.Keys.ToList();
		}

		public List<string> GetContainerKeys()
		{
			return _containerStatistics.Keys.ToList();
		}
	}

	private static SaveDataSizeRecord SizeRecord;

	private List<object> _childObjects;

	private Dictionary<object, int> _idsOfChildObjects;

	private List<object> _childContainers;

	private Dictionary<object, int> _idsOfChildContainers;

	private List<string> _strings;

	private Dictionary<string, int> _idsOfStrings;

	private List<object> _temporaryCollectedObjects;

	private ObjectSaveData[] _objectSaveDataList;

	private ContainerSaveData[] _containerSaveDataList;

	private object _locker;

	private static Dictionary<string, (int, int, int, long)> _typeStatistics;

	private static Dictionary<string, (int, int, int, int, long)> _containerStatistics;

	private Queue<object> _objectsToIterate;

	public object RootObject { get; private set; }

	public GameData SaveData { get; private set; }

	public DefinitionContext DefinitionContext { get; private set; }

	public static bool EnableSaveStatistics => false;

	public static SaveStatistics GetStatistics()
	{
		return new SaveStatistics(_typeStatistics, _containerStatistics);
	}

	public SaveContext(DefinitionContext definitionContext)
	{
		DefinitionContext = definitionContext;
		_childObjects = new List<object>(131072);
		_idsOfChildObjects = new Dictionary<object, int>(131072);
		_strings = new List<string>(131072);
		_idsOfStrings = new Dictionary<string, int>(131072);
		_childContainers = new List<object>(131072);
		_idsOfChildContainers = new Dictionary<object, int>(131072);
		_temporaryCollectedObjects = new List<object>(4096);
		_locker = new object();
	}

	private void CollectSaveDatas()
	{
		SizeRecord.HeaderSize = GetConfigEntrySize();
		SizeRecord.StringSize = GetStringFolderSize();
		SizeRecord.ObjectSize = 0;
		SizeRecord.ContainerSize = 0;
		using (new PerformanceTestBlock("SaveContext::CollectSaveDataForObject::Objects"))
		{
			if (!EnableSaveStatistics)
			{
				TWParallel.ForWithoutRenderThread(0, _childObjects.Count, delegate(int startInclusive, int endExclusive)
				{
					for (int i = startInclusive; i < endExclusive; i++)
					{
						CollectSaveDataForObject(i, ref SizeRecord);
					}
				});
			}
			else
			{
				for (int num = 0; num < _childObjects.Count; num++)
				{
					CollectSaveDataForObject(num, ref SizeRecord);
				}
			}
		}
		using (new PerformanceTestBlock("SaveContext::CollectSaveDataForObject::Containers"))
		{
			if (!EnableSaveStatistics)
			{
				TWParallel.ForWithoutRenderThread(0, _childContainers.Count, delegate(int startInclusive, int endExclusive)
				{
					for (int i = startInclusive; i < endExclusive; i++)
					{
						CollectSaveDataForContainer(i, ref SizeRecord);
					}
				});
			}
			else
			{
				for (int num2 = 0; num2 < _childContainers.Count; num2++)
				{
					CollectSaveDataForContainer(num2, ref SizeRecord);
				}
			}
		}
	}

	private void CollectObjects()
	{
		using (new PerformanceTestBlock("SaveContext::CollectObjects"))
		{
			_objectsToIterate = new Queue<object>(1024);
			_objectsToIterate.Enqueue(RootObject);
			while (_objectsToIterate.Count > 0)
			{
				object obj = _objectsToIterate.Dequeue();
				if (obj.GetType().IsContainer(out var containerType))
				{
					CollectContainerObjects(containerType, obj);
				}
				else
				{
					CollectObjects(obj);
				}
			}
		}
	}

	private void CollectContainerObjects(ContainerType containerType, object parent)
	{
		if (_idsOfChildContainers.ContainsKey(parent))
		{
			return;
		}
		int count = _childContainers.Count;
		_childContainers.Add(parent);
		_idsOfChildContainers.Add(parent, count);
		Type type = parent.GetType();
		ContainerDefinition containerDefinition = DefinitionContext.GetContainerDefinition(type);
		if (containerDefinition == null)
		{
			string message = "Cant find definition for " + type.FullName;
			Debug.Print(message, 0, Debug.DebugColor.Red);
			Debug.FailedAssert(message, "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.SaveSystem\\Save\\SaveContext.cs", "CollectContainerObjects", 222);
		}
		ContainerSaveData.GetChildObjects(this, containerDefinition, containerType, parent, _temporaryCollectedObjects);
		for (int i = 0; i < _temporaryCollectedObjects.Count; i++)
		{
			object obj = _temporaryCollectedObjects[i];
			if (obj != null)
			{
				_objectsToIterate.Enqueue(obj);
			}
		}
		_temporaryCollectedObjects.Clear();
	}

	private void CollectObjects(object parent)
	{
		if (_idsOfChildObjects.ContainsKey(parent))
		{
			return;
		}
		int count = _childObjects.Count;
		_childObjects.Add(parent);
		_idsOfChildObjects.Add(parent, count);
		Type type = parent.GetType();
		TypeDefinition classDefinition = DefinitionContext.GetClassDefinition(type);
		if (classDefinition == null)
		{
			throw new Exception("Could not find type definition of type: " + type);
		}
		ObjectSaveData.GetChildObjects(this, classDefinition, parent, _temporaryCollectedObjects);
		for (int i = 0; i < _temporaryCollectedObjects.Count; i++)
		{
			object obj = _temporaryCollectedObjects[i];
			if (obj != null)
			{
				_objectsToIterate.Enqueue(obj);
			}
		}
		_temporaryCollectedObjects.Clear();
	}

	public int AddOrGetStringId(string text)
	{
		int num = -1;
		if (text == null)
		{
			num = -1;
		}
		else
		{
			lock (_locker)
			{
				if (_idsOfStrings.TryGetValue(text, out var value))
				{
					num = value;
				}
				else
				{
					num = _strings.Count;
					_idsOfStrings.Add(text, num);
					_strings.Add(text);
					int stringSizeWithOverhead = GetStringSizeWithOverhead(text);
					Interlocked.Add(ref SizeRecord.StringSize, stringSizeWithOverhead);
				}
			}
		}
		return num;
	}

	public int GetObjectId(object target)
	{
		if (!_idsOfChildObjects.TryGetValue(target, out var value))
		{
			Debug.Print($"SAVE ERROR. Cant find {target} with type {target.GetType()}");
			Debug.FailedAssert("SAVE ERROR. Cant find target object on save", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.SaveSystem\\Save\\SaveContext.cs", "GetObjectId", 310);
		}
		return value;
	}

	public int GetContainerId(object target)
	{
		return _idsOfChildContainers[target];
	}

	public int GetStringId(string target)
	{
		if (target == null)
		{
			return -1;
		}
		int num = -1;
		lock (_locker)
		{
			return _idsOfStrings[target];
		}
	}

	private static void SaveStringTo(BinaryWriter stringWriter, int id, string text)
	{
		stringWriter.Write3ByteInt(0);
		stringWriter.Write3ByteInt(id);
		stringWriter.WriteByte(10);
		int stringSizeInBytes = GetStringSizeInBytes(text);
		stringWriter.WriteShort((short)stringSizeInBytes);
		stringWriter.WriteString(text);
	}

	public static int GetStringSizeInBytes(string text)
	{
		return 4 + Encoding.UTF8.GetByteCount(text);
	}

	private static int GetStringSizeWithOverhead(string text)
	{
		return GetStringSizeInBytes(text) + 9;
	}

	public bool Save(object target, MetaData metaData, out string errorMessage)
	{
		errorMessage = "";
		bool flag = false;
		if (EnableSaveStatistics)
		{
			_typeStatistics = new Dictionary<string, (int, int, int, long)>();
			_containerStatistics = new Dictionary<string, (int, int, int, int, long)>();
		}
		try
		{
			RootObject = target;
			using (new PerformanceTestBlock("SaveContext::Save"))
			{
				CollectObjects();
				_objectSaveDataList = new ObjectSaveData[_childObjects.Count];
				_containerSaveDataList = new ContainerSaveData[_childContainers.Count];
				SizeRecord = default(SaveDataSizeRecord);
				CollectSaveDatas();
				byte[][] objectData = WriteObjects();
				byte[][] containerData = WriteContainers();
				new List<int>();
				byte[] header = WriteHeaders(_objectSaveDataList, _containerSaveDataList, SizeRecord.HeaderSize, _strings.Count);
				byte[] strings = WriteAllStrings(_strings, SizeRecord.StringSize);
				SaveData = new GameData(header, strings, objectData, containerData);
				Debug.Print(SizeRecord.ToString());
			}
			return true;
		}
		catch (Exception ex)
		{
			errorMessage = "SaveContext Error\n";
			errorMessage += ex.Message;
			return false;
		}
	}

	private byte[][] WriteObjects()
	{
		byte[][] objectData = new byte[_childObjects.Count][];
		using (new PerformanceTestBlock("SaveContext::Saving Objects"))
		{
			if (!EnableSaveStatistics)
			{
				TWParallel.ForWithoutRenderThread(0, _childObjects.Count, delegate(int startInclusive, int endExclusive)
				{
					for (int i = startInclusive; i < endExclusive; i++)
					{
						SaveSingleObject(objectData, i);
					}
				});
			}
			else
			{
				for (int num = 0; num < _childObjects.Count; num++)
				{
					SaveSingleObject(objectData, num);
				}
			}
		}
		return objectData;
	}

	private byte[][] WriteContainers()
	{
		byte[][] containerData = new byte[_childContainers.Count][];
		using (new PerformanceTestBlock("SaveContext::Saving Containers"))
		{
			if (!EnableSaveStatistics)
			{
				TWParallel.ForWithoutRenderThread(0, _childContainers.Count, delegate(int startInclusive, int endExclusive)
				{
					for (int i = startInclusive; i < endExclusive; i++)
					{
						SaveSingleContainer(containerData, i);
					}
				});
			}
			else
			{
				for (int num = 0; num < _childContainers.Count; num++)
				{
					SaveSingleContainer(containerData, num);
				}
			}
		}
		return containerData;
	}

	private static byte[] WriteHeaders(ObjectSaveData[] objects, ContainerSaveData[] containers, int headerSize, int stringCount)
	{
		BinaryWriter binaryWriter = new BinaryWriter(SizeRecord.HeaderSize);
		binaryWriter.WriteInt(objects.Length + containers.Length);
		for (int i = 0; i < objects.Length; i++)
		{
			objects[i].SaveHeaderFolderTo(binaryWriter, i);
		}
		for (int j = 0; j < containers.Length; j++)
		{
			containers[j].SaveHeaderFolderTo(binaryWriter, objects.Length + j);
		}
		binaryWriter.WriteInt(objects.Length + containers.Length + 1);
		for (int k = 0; k < objects.Length; k++)
		{
			objects[k].SaveHeaderDataTo(binaryWriter, k);
		}
		for (int l = 0; l < containers.Length; l++)
		{
			containers[l].SaveHeaderDataTo(binaryWriter, objects.Length + l);
		}
		WriteConfigEntry(binaryWriter, objects.Length, stringCount, containers.Length);
		return binaryWriter.Data;
	}

	private static byte[] WriteAllStrings(List<string> strings, int stringSize)
	{
		BinaryWriter binaryWriter = new BinaryWriter(stringSize);
		WriteStringsEntry(binaryWriter, strings.Count);
		for (int i = 0; i < strings.Count; i++)
		{
			string text = strings[i];
			SaveStringTo(binaryWriter, i, text);
		}
		return binaryWriter.Data;
	}

	private static void WriteConfigEntry(BinaryWriter headerWriter, int objects, int strings, int containers)
	{
		headerWriter.Write3ByteInt(-1);
		headerWriter.Write3ByteInt(-1);
		headerWriter.WriteByte(7);
		headerWriter.WriteShort(12);
		headerWriter.WriteInt(objects);
		headerWriter.WriteInt(strings);
		headerWriter.WriteInt(containers);
	}

	private static void WriteStringsEntry(BinaryWriter headerWriter, int strings)
	{
		headerWriter.WriteInt(1);
		headerWriter.Write3ByteInt(-1);
		headerWriter.Write3ByteInt(0);
		headerWriter.Write3ByteInt(-1);
		headerWriter.WriteByte(4);
		headerWriter.WriteInt(strings);
	}

	private static int GetConfigEntrySize()
	{
		return 29;
	}

	private static int GetStringFolderSize()
	{
		return 18;
	}

	private void CollectSaveDataForObject(int id, ref SaveDataSizeRecord headerSize)
	{
		object target = _childObjects[id];
		ObjectSaveData objectSaveData = new ObjectSaveData(this, id, target, isClass: true);
		objectSaveData.CollectStructs();
		objectSaveData.CollectMembers();
		objectSaveData.CollectStrings();
		_objectSaveDataList[id] = objectSaveData;
		Interlocked.Add(ref headerSize.HeaderSize, objectSaveData.GetHeaderSize());
		Interlocked.Add(ref headerSize.ObjectSize, objectSaveData.GetDataSize());
	}

	private void CollectSaveDataForContainer(int id, ref SaveDataSizeRecord headerSize)
	{
		object obj = _childContainers[id];
		obj.GetType().IsContainer(out var containerType);
		ContainerSaveData containerSaveData = new ContainerSaveData(this, id, obj, containerType);
		containerSaveData.CollectChildren();
		containerSaveData.CollectStructs();
		containerSaveData.CollectMembers();
		containerSaveData.CollectStrings();
		_containerSaveDataList[id] = containerSaveData;
		Interlocked.Add(ref headerSize.HeaderSize, containerSaveData.GetHeaderSize());
		Interlocked.Add(ref headerSize.ContainerSize, containerSaveData.GetDataSize());
	}

	private void SaveSingleObject(byte[][] objectData, int id)
	{
		_ = _childObjects[id];
		ObjectSaveData objectSaveData = _objectSaveDataList[id];
		int dataSize = objectSaveData.GetDataSize();
		BinaryWriter binaryWriter = new BinaryWriter(dataSize);
		int folderCount = objectSaveData.GetFolderCount();
		binaryWriter.WriteInt(folderCount);
		int folderId = 0;
		objectSaveData.SaveDataFolder(binaryWriter, -1, ref folderId);
		int entryCount = objectSaveData.GetEntryCount();
		binaryWriter.WriteInt(entryCount);
		folderId = 0;
		objectSaveData.SaveTo(binaryWriter, ref folderId);
		objectData[id] = binaryWriter.Data;
		if (EnableSaveStatistics)
		{
			string name = objectSaveData.Type.Name;
			if (_typeStatistics.TryGetValue(name, out var value))
			{
				_typeStatistics[name] = (value.Item1 + 1, value.Item2, value.Item3, value.Item4 + dataSize);
			}
			else
			{
				_typeStatistics[name] = (1, objectSaveData.FieldCount, objectSaveData.PropertyCount, dataSize);
			}
		}
	}

	private void SaveSingleContainer(byte[][] containerData, int id)
	{
		_ = _childContainers[id];
		ContainerSaveData containerSaveData = _containerSaveDataList[id];
		int dataSize = containerSaveData.GetDataSize();
		BinaryWriter binaryWriter = new BinaryWriter(dataSize);
		binaryWriter.WriteInt(containerSaveData.GetFolderCount());
		int folderId = 0;
		containerSaveData.SaveDataFolder(binaryWriter, ref folderId);
		int entryCount = containerSaveData.GetEntryCount();
		binaryWriter.WriteInt(entryCount);
		folderId = 0;
		containerSaveData.SaveTo(binaryWriter, ref folderId);
		containerData[id] = binaryWriter.Data;
		if (EnableSaveStatistics)
		{
			string containerName = GetContainerName(containerSaveData.Type);
			if (_containerStatistics.TryGetValue(containerName, out var value))
			{
				_containerStatistics[containerName] = (value.Item1 + 1, value.Item2 + containerSaveData.GetElementCount(), value.Item3, value.Item4, _containerStatistics[containerName].Item5 + dataSize);
			}
			else
			{
				_containerStatistics[containerName] = (1, containerSaveData.GetElementCount(), containerSaveData.ElementFieldCount, containerSaveData.ElementPropertyCount, dataSize);
			}
		}
	}

	private string GetContainerName(Type t)
	{
		string text = t.Name;
		Type[] genericArguments = t.GetGenericArguments();
		foreach (Type type in genericArguments)
		{
			text = ((!t.IsContainer()) ? (text + type.Name + ".") : (text + GetContainerName(type)));
		}
		return text.TrimEnd(new char[1] { '.' });
	}
}
