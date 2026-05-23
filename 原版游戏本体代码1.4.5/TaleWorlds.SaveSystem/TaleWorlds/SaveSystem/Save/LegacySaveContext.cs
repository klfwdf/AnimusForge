using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Library;
using TaleWorlds.SaveSystem.Definition;

namespace TaleWorlds.SaveSystem.Save;

public class LegacySaveContext : ISaveContext
{
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

	private List<object> _childObjects;

	private Dictionary<object, int> _idsOfChildObjects;

	private List<object> _childContainers;

	private Dictionary<object, int> _idsOfChildContainers;

	private List<string> _strings;

	private Dictionary<string, int> _idsOfStrings;

	private List<object> _temporaryCollectedObjects;

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

	public LegacySaveContext(DefinitionContext definitionContext)
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
			Debug.FailedAssert(message, "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.SaveSystem\\Save\\LegacySaveContext.cs", "CollectContainerObjects", 151);
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

	public void AddStrings(List<string> texts)
	{
		lock (_locker)
		{
			for (int i = 0; i < texts.Count; i++)
			{
				string text = texts[i];
				if (text != null && !_idsOfStrings.ContainsKey(text))
				{
					int count = _strings.Count;
					_idsOfStrings.Add(text, count);
					_strings.Add(text);
				}
			}
		}
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
			Debug.FailedAssert("SAVE ERROR. Cant find target object on save", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.SaveSystem\\Save\\LegacySaveContext.cs", "GetObjectId", 258);
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

	private static void SaveStringTo(SaveEntryFolder stringsFolder, int id, string value)
	{
		BinaryWriter binaryWriter = BinaryWriterFactory.GetBinaryWriter();
		binaryWriter.WriteString(value);
		stringsFolder.CreateEntry(new EntryId(id, SaveEntryExtension.Txt)).FillFrom(binaryWriter);
		BinaryWriterFactory.ReleaseBinaryWriter(binaryWriter);
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
				BinaryWriterFactory.Initialize();
				CollectObjects();
				ArchiveConcurrentSerializer headerSerializer = new ArchiveConcurrentSerializer();
				byte[][] objectData = new byte[_childObjects.Count][];
				using (new PerformanceTestBlock("SaveContext::Saving Objects"))
				{
					if (!EnableSaveStatistics)
					{
						TWParallel.ForWithoutRenderThread(0, _childObjects.Count, delegate(int startInclusive, int endExclusive)
						{
							for (int i = startInclusive; i < endExclusive; i++)
							{
								SaveSingleObject(headerSerializer, objectData, i);
							}
						});
					}
					else
					{
						for (int num = 0; num < _childObjects.Count; num++)
						{
							SaveSingleObject(headerSerializer, objectData, num);
						}
					}
				}
				byte[][] containerData = new byte[_childContainers.Count][];
				using (new PerformanceTestBlock("SaveContext::Saving Containers"))
				{
					if (!EnableSaveStatistics)
					{
						TWParallel.ForWithoutRenderThread(0, _childContainers.Count, delegate(int startInclusive, int endExclusive)
						{
							for (int i = startInclusive; i < endExclusive; i++)
							{
								SaveSingleContainer(headerSerializer, containerData, i);
							}
						});
					}
					else
					{
						for (int num2 = 0; num2 < _childContainers.Count; num2++)
						{
							SaveSingleContainer(headerSerializer, containerData, num2);
						}
					}
				}
				SaveEntryFolder saveEntryFolder = SaveEntryFolder.CreateRootFolder();
				BinaryWriter binaryWriter = BinaryWriterFactory.GetBinaryWriter();
				binaryWriter.WriteInt(_idsOfChildObjects.Count);
				binaryWriter.WriteInt(_strings.Count);
				binaryWriter.WriteInt(_idsOfChildContainers.Count);
				saveEntryFolder.CreateEntry(new EntryId(-1, SaveEntryExtension.Config)).FillFrom(binaryWriter);
				headerSerializer.SerializeFolderConcurrent(saveEntryFolder);
				BinaryWriterFactory.ReleaseBinaryWriter(binaryWriter);
				ArchiveSerializer archiveSerializer = new ArchiveSerializer();
				SaveEntryFolder saveEntryFolder2 = SaveEntryFolder.CreateRootFolder();
				SaveEntryFolder stringsFolder = archiveSerializer.CreateFolder(saveEntryFolder2, new FolderId(-1, SaveFolderExtension.Strings), _strings.Count);
				for (int num3 = 0; num3 < _strings.Count; num3++)
				{
					string value = _strings[num3];
					SaveStringTo(stringsFolder, num3, value);
				}
				archiveSerializer.SerializeFolder(saveEntryFolder2);
				byte[] array = null;
				byte[] array2 = null;
				array = headerSerializer.FinalizeAndGetBinaryDataConcurrent();
				array2 = archiveSerializer.FinalizeAndGetBinaryData();
				SaveData = new GameData(array, array2, objectData, containerData);
				BinaryWriterFactory.Release();
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

	private void SaveSingleObject(ArchiveConcurrentSerializer headerSerializer, byte[][] objectData, int id)
	{
		object target = _childObjects[id];
		ArchiveSerializer archiveSerializer = new ArchiveSerializer();
		SaveEntryFolder saveEntryFolder = SaveEntryFolder.CreateRootFolder();
		SaveEntryFolder saveEntryFolder2 = SaveEntryFolder.CreateRootFolder();
		ObjectSaveData objectSaveData = new ObjectSaveData(this, id, target, isClass: true);
		objectSaveData.CollectStructs();
		objectSaveData.CollectMembers();
		objectSaveData.CollectStrings();
		objectSaveData.SaveHeaderTo(saveEntryFolder2, headerSerializer);
		objectSaveData.SaveTo(saveEntryFolder, archiveSerializer);
		headerSerializer.SerializeFolderConcurrent(saveEntryFolder2);
		archiveSerializer.SerializeFolder(saveEntryFolder);
		byte[] array = (objectData[id] = archiveSerializer.FinalizeAndGetBinaryData());
		if (EnableSaveStatistics)
		{
			string name = objectSaveData.Type.Name;
			int num = array.Length;
			if (_typeStatistics.TryGetValue(name, out var value))
			{
				_typeStatistics[name] = (value.Item1 + 1, value.Item2, value.Item3, value.Item4 + num);
			}
			else
			{
				_typeStatistics[name] = (1, objectSaveData.FieldCount, objectSaveData.PropertyCount, num);
			}
		}
	}

	private void SaveSingleContainer(ArchiveConcurrentSerializer headerSerializer, byte[][] containerData, int id)
	{
		object obj = _childContainers[id];
		ArchiveSerializer archiveSerializer = new ArchiveSerializer();
		SaveEntryFolder saveEntryFolder = SaveEntryFolder.CreateRootFolder();
		SaveEntryFolder saveEntryFolder2 = SaveEntryFolder.CreateRootFolder();
		obj.GetType().IsContainer(out var containerType);
		ContainerSaveData containerSaveData = new ContainerSaveData(this, id, obj, containerType);
		containerSaveData.CollectChildren();
		containerSaveData.CollectStructs();
		containerSaveData.CollectMembers();
		containerSaveData.CollectStrings();
		containerSaveData.SaveHeaderTo(saveEntryFolder2, headerSerializer);
		containerSaveData.SaveTo(saveEntryFolder, archiveSerializer);
		headerSerializer.SerializeFolderConcurrent(saveEntryFolder2);
		archiveSerializer.SerializeFolder(saveEntryFolder);
		byte[] array = (containerData[id] = archiveSerializer.FinalizeAndGetBinaryData());
		if (EnableSaveStatistics)
		{
			string containerName = GetContainerName(containerSaveData.Type);
			long num = array.Length;
			if (_containerStatistics.TryGetValue(containerName, out var value))
			{
				_containerStatistics[containerName] = (value.Item1 + 1, value.Item2 + containerSaveData.GetElementCount(), value.Item3, value.Item4, _containerStatistics[containerName].Item5 + num);
			}
			else
			{
				_containerStatistics[containerName] = (1, containerSaveData.GetElementCount(), containerSaveData.ElementFieldCount, containerSaveData.ElementPropertyCount, num);
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
