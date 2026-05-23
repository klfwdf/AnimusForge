using System;
using System.Collections;
using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.SaveSystem.Definition;

namespace TaleWorlds.SaveSystem.Save;

internal class ContainerSaveData
{
	private ContainerType _containerType;

	private ElementSaveData[] _keys;

	private ElementSaveData[] _values;

	private ContainerDefinition _typeDefinition;

	private int _elementCount;

	private List<ObjectSaveData> _childStructs;

	public int ObjectId { get; private set; }

	public ISaveContext Context { get; private set; }

	public object Target { get; private set; }

	public Type Type { get; private set; }

	internal int ElementPropertyCount
	{
		get
		{
			if (_childStructs.Count <= 0)
			{
				return 0;
			}
			return _childStructs[0].PropertyCount;
		}
	}

	internal int ElementFieldCount
	{
		get
		{
			if (_childStructs.Count <= 0)
			{
				return 0;
			}
			return _childStructs[0].FieldCount;
		}
	}

	public ContainerSaveData(ISaveContext context, int objectId, object target, ContainerType containerType)
	{
		ObjectId = objectId;
		Context = context;
		Target = target;
		_containerType = containerType;
		Type = target.GetType();
		_elementCount = GetElementCount();
		_childStructs = new List<ObjectSaveData>();
		_typeDefinition = context.DefinitionContext.GetContainerDefinition(Type);
		if (_typeDefinition == null)
		{
			throw new Exception("Could not find type definition of container type: " + Type);
		}
	}

	public void CollectChildren()
	{
		_keys = new ElementSaveData[_elementCount];
		_values = new ElementSaveData[_elementCount];
		if (_containerType == ContainerType.Dictionary)
		{
			IDictionary obj = (IDictionary)Target;
			int num = 0;
			{
				foreach (DictionaryEntry item in obj)
				{
					object key = item.Key;
					object value = item.Value;
					ElementSaveData elementSaveData = new ElementSaveData(this, key, num);
					ElementSaveData elementSaveData2 = new ElementSaveData(this, value, _elementCount + num);
					_keys[num] = elementSaveData;
					_values[num] = elementSaveData2;
					num++;
				}
				return;
			}
		}
		if (_containerType == ContainerType.List || _containerType == ContainerType.CustomList || _containerType == ContainerType.CustomReadOnlyList)
		{
			IList list = (IList)Target;
			for (int i = 0; i < _elementCount; i++)
			{
				object value2 = list[i];
				ElementSaveData elementSaveData3 = new ElementSaveData(this, value2, i);
				_values[i] = elementSaveData3;
			}
			return;
		}
		if (_containerType == ContainerType.Queue)
		{
			ICollection obj2 = (ICollection)Target;
			int num2 = 0;
			{
				foreach (object item2 in obj2)
				{
					ElementSaveData elementSaveData4 = new ElementSaveData(this, item2, num2);
					_values[num2] = elementSaveData4;
					num2++;
				}
				return;
			}
		}
		if (_containerType == ContainerType.Array)
		{
			Array array = (Array)Target;
			for (int j = 0; j < _elementCount; j++)
			{
				object value3 = array.GetValue(j);
				ElementSaveData elementSaveData5 = new ElementSaveData(this, value3, j);
				_values[j] = elementSaveData5;
			}
		}
	}

	public void SaveHeaderTo(SaveEntryFolder parentFolder, IArchiveContext archiveContext)
	{
		SaveEntryFolder saveEntryFolder = archiveContext.CreateFolder(parentFolder, new FolderId(ObjectId, SaveFolderExtension.Container), 1);
		BinaryWriter binaryWriter = BinaryWriterFactory.GetBinaryWriter();
		_typeDefinition.SaveId.WriteTo(binaryWriter);
		binaryWriter.WriteByte((byte)_containerType);
		binaryWriter.WriteInt(GetElementCount());
		saveEntryFolder.CreateEntry(new EntryId(-1, SaveEntryExtension.Object)).FillFrom(binaryWriter);
		BinaryWriterFactory.ReleaseBinaryWriter(binaryWriter);
	}

	public void SaveHeaderDataTo(BinaryWriter headerWriter, int folderId)
	{
		headerWriter.Write3ByteInt(folderId);
		headerWriter.Write3ByteInt(-1);
		headerWriter.WriteByte(9);
		headerWriter.WriteShort((short)GetHeaderDataSize());
		_typeDefinition.SaveId.WriteTo(headerWriter);
		headerWriter.WriteByte((byte)_containerType);
		headerWriter.WriteInt(GetElementCount());
	}

	public void SaveHeaderFolderTo(BinaryWriter headerWriter, int folderId)
	{
		headerWriter.Write3ByteInt(-1);
		headerWriter.Write3ByteInt(folderId);
		headerWriter.Write3ByteInt(ObjectId);
		headerWriter.WriteByte(3);
	}

	private int GetHeaderDataSize()
	{
		return 5 + _typeDefinition.SaveId.GetSizeInBytes();
	}

	public int GetHeaderSize()
	{
		return GetHeaderDataSize() + 19;
	}

	public int GetDataSize()
	{
		int num = 18;
		for (int i = 0; i < _elementCount; i++)
		{
			num += _values[i].GetDataSize();
			num += GetMemberEntrySize();
			if (_containerType == ContainerType.Dictionary)
			{
				num += _keys[i].GetDataSize();
				num += GetMemberEntrySize();
			}
		}
		foreach (ObjectSaveData childStruct in _childStructs)
		{
			TypeDefinition structDefinition = Context.DefinitionContext.GetStructDefinition(childStruct.Type);
			if (ShouldSaveStruct(structDefinition, childStruct))
			{
				num += childStruct.GetDataSize() - 8;
			}
		}
		return num;
	}

	private bool ShouldSaveStruct(TypeDefinition structDefinition, ObjectSaveData objectSaveData)
	{
		if (structDefinition == null || !(structDefinition is StructDefinition) || !(objectSaveData.Target is ISavedStruct savedStruct) || !savedStruct.IsDefault())
		{
			return true;
		}
		ContainerSaveId containerSaveId = (ContainerSaveId)_typeDefinition.SaveId;
		if (_containerType != ContainerType.Dictionary)
		{
			return containerSaveId.KeyId != structDefinition.SaveId;
		}
		SaveId keyId = containerSaveId.KeyId;
		SaveId valueId = containerSaveId.ValueId;
		if (IsKey(objectSaveData))
		{
			return keyId != structDefinition.SaveId;
		}
		return valueId != structDefinition.SaveId;
	}

	private bool IsKey(ObjectSaveData objectSaveData)
	{
		for (int i = 0; i < _elementCount; i++)
		{
			ElementSaveData elementSaveData = _keys[i];
			ElementSaveData elementSaveData2 = _values[i];
			if (elementSaveData != null && objectSaveData.ObjectId == elementSaveData.ElementIndex)
			{
				return true;
			}
			if (elementSaveData2 != null && objectSaveData.ObjectId == elementSaveData2.ElementIndex)
			{
				return false;
			}
		}
		return false;
	}

	private int GetMemberEntrySize()
	{
		return 9;
	}

	public int GetEntryCount()
	{
		int num = ((_containerType == ContainerType.Dictionary) ? (_elementCount * 2) : _elementCount);
		foreach (ObjectSaveData childStruct in _childStructs)
		{
			TypeDefinition structDefinition = Context.DefinitionContext.GetStructDefinition(childStruct.Type);
			if (ShouldSaveStruct(structDefinition, childStruct))
			{
				num += childStruct.GetEntryCount();
			}
		}
		return num;
	}

	public int GetFolderCount()
	{
		int num = 1;
		foreach (ObjectSaveData childStruct in _childStructs)
		{
			TypeDefinition structDefinition = Context.DefinitionContext.GetStructDefinition(childStruct.Type);
			if (ShouldSaveStruct(structDefinition, childStruct))
			{
				num += childStruct.GetFolderCount();
			}
		}
		return num;
	}

	public void SaveDataFolder(BinaryWriter writer, ref int folderId)
	{
		writer.Write3ByteInt(-1);
		writer.Write3ByteInt(0);
		writer.Write3ByteInt(ObjectId);
		writer.WriteByte(3);
		folderId++;
		foreach (ObjectSaveData childStruct in _childStructs)
		{
			TypeDefinition structDefinition = Context.DefinitionContext.GetStructDefinition(childStruct.Type);
			if (ShouldSaveStruct(structDefinition, childStruct))
			{
				childStruct.SaveDataFolder(writer, 0, ref folderId);
			}
		}
	}

	public void SaveTo(BinaryWriter writer, ref int folderId)
	{
		for (int i = 0; i < _elementCount; i++)
		{
			WriteElementEntry(writer, _values[i], folderId, i, SaveEntryExtension.Value);
			_values[i].SaveTo(writer);
			if (_containerType == ContainerType.Dictionary)
			{
				WriteElementEntry(writer, _keys[i], folderId, i, SaveEntryExtension.Key);
				_keys[i].SaveTo(writer);
			}
		}
		folderId++;
		foreach (ObjectSaveData childStruct in _childStructs)
		{
			TypeDefinition structDefinition = Context.DefinitionContext.GetStructDefinition(childStruct.Type);
			if (ShouldSaveStruct(structDefinition, childStruct))
			{
				childStruct.SaveTo(writer, ref folderId);
			}
		}
	}

	public void SaveTo(SaveEntryFolder parentFolder, IArchiveContext archiveContext)
	{
		int entryCount = ((_containerType == ContainerType.Dictionary) ? (_elementCount * 2) : _elementCount);
		SaveEntryFolder saveEntryFolder = archiveContext.CreateFolder(parentFolder, new FolderId(ObjectId, SaveFolderExtension.Container), entryCount);
		for (int i = 0; i < _elementCount; i++)
		{
			ElementSaveData obj = _values[i];
			BinaryWriter binaryWriter = BinaryWriterFactory.GetBinaryWriter();
			obj.SaveTo(binaryWriter);
			saveEntryFolder.CreateEntry(new EntryId(i, SaveEntryExtension.Value)).FillFrom(binaryWriter);
			BinaryWriterFactory.ReleaseBinaryWriter(binaryWriter);
			if (_containerType == ContainerType.Dictionary)
			{
				ElementSaveData obj2 = _keys[i];
				BinaryWriter binaryWriter2 = BinaryWriterFactory.GetBinaryWriter();
				obj2.SaveTo(binaryWriter2);
				saveEntryFolder.CreateEntry(new EntryId(i, SaveEntryExtension.Key)).FillFrom(binaryWriter2);
				BinaryWriterFactory.ReleaseBinaryWriter(binaryWriter2);
			}
		}
		foreach (ObjectSaveData childStruct in _childStructs)
		{
			TypeDefinition structDefinition = Context.DefinitionContext.GetStructDefinition(childStruct.Type);
			if (ShouldSaveStruct(structDefinition, childStruct))
			{
				childStruct.SaveTo(saveEntryFolder, archiveContext);
			}
		}
	}

	private void WriteElementEntry(BinaryWriter writer, ElementSaveData data, int parentFolderId, int id, SaveEntryExtension extension)
	{
		writer.Write3ByteInt(parentFolderId);
		writer.Write3ByteInt(id);
		writer.WriteByte((byte)extension);
		writer.WriteShort((short)data.GetDataSize());
	}

	internal int GetElementCount()
	{
		if (_containerType == ContainerType.List || _containerType == ContainerType.CustomList || _containerType == ContainerType.CustomReadOnlyList)
		{
			return ((IList)Target).Count;
		}
		if (_containerType == ContainerType.Queue)
		{
			return ((ICollection)Target).Count;
		}
		if (_containerType == ContainerType.Dictionary)
		{
			return ((IDictionary)Target).Count;
		}
		if (_containerType == ContainerType.Array)
		{
			return ((Array)Target).GetLength(0);
		}
		return 0;
	}

	public void CollectStrings()
	{
		foreach (string item in GetChildString())
		{
			Context.AddOrGetStringId(item);
		}
		foreach (ObjectSaveData childStruct in _childStructs)
		{
			childStruct.CollectStrings();
		}
	}

	public void CollectStringsInto(List<string> collection)
	{
		foreach (string item in GetChildString())
		{
			collection.Add(item);
		}
	}

	public void CollectStructs()
	{
		foreach (ElementSaveData childElementSaveData in GetChildElementSaveDatas())
		{
			if (childElementSaveData.MemberType == SavedMemberType.CustomStruct)
			{
				object elementValue = childElementSaveData.ElementValue;
				ObjectSaveData item = new ObjectSaveData(Context, childElementSaveData.ElementIndex, elementValue, isClass: false);
				_childStructs.Add(item);
			}
		}
		foreach (ObjectSaveData childStruct in _childStructs)
		{
			childStruct.CollectStructs();
		}
	}

	public void CollectMembers()
	{
		foreach (ObjectSaveData childStruct in _childStructs)
		{
			childStruct.CollectMembers();
		}
	}

	public IEnumerable<ElementSaveData> GetChildElementSaveDatas()
	{
		for (int i = 0; i < _elementCount; i++)
		{
			ElementSaveData elementSaveData = _keys[i];
			ElementSaveData value = _values[i];
			if (elementSaveData != null)
			{
				yield return elementSaveData;
			}
			if (value != null)
			{
				yield return value;
			}
		}
	}

	public IEnumerable<object> GetChildElements()
	{
		return GetChildElements(_containerType, Target);
	}

	public static IEnumerable<object> GetChildElements(ContainerType containerType, object target)
	{
		switch (containerType)
		{
		case ContainerType.List:
		case ContainerType.CustomList:
		case ContainerType.CustomReadOnlyList:
		{
			IList list = (IList)target;
			for (int i = 0; i < list.Count; i++)
			{
				object obj = list[i];
				if (obj != null)
				{
					yield return obj;
				}
			}
			break;
		}
		case ContainerType.Queue:
		{
			ICollection collection = (ICollection)target;
			foreach (object item in collection)
			{
				if (item != null)
				{
					yield return item;
				}
			}
			break;
		}
		case ContainerType.Dictionary:
		{
			IDictionary dictionary = (IDictionary)target;
			foreach (DictionaryEntry entry in dictionary)
			{
				yield return entry.Key;
				object value2 = entry.Value;
				if (value2 != null)
				{
					yield return value2;
				}
			}
			break;
		}
		case ContainerType.Array:
		{
			Array array = (Array)target;
			for (int i = 0; i < array.Length; i++)
			{
				object value = array.GetValue(i);
				if (value != null)
				{
					yield return value;
				}
			}
			break;
		}
		}
	}

	public IEnumerable<object> GetChildObjects(ISaveContext context)
	{
		List<object> list = new List<object>();
		GetChildObjects(context, _typeDefinition, _containerType, Target, list);
		return list;
	}

	public static void GetChildObjects(ISaveContext context, ContainerDefinition containerDefinition, ContainerType containerType, object target, List<object> collectedObjects)
	{
		if (containerDefinition.CollectObjectsMethod != null)
		{
			if (!containerDefinition.HasNoChildObject)
			{
				containerDefinition.CollectObjectsMethod(target, collectedObjects);
			}
			return;
		}
		switch (containerType)
		{
		case ContainerType.List:
		case ContainerType.CustomList:
		case ContainerType.CustomReadOnlyList:
		{
			IList list = (IList)target;
			for (int j = 0; j < list.Count; j++)
			{
				object obj = list[j];
				if (obj != null)
				{
					ProcessChildObjectElement(obj, context, collectedObjects);
				}
			}
			return;
		}
		case ContainerType.Queue:
		{
			foreach (object item in (ICollection)target)
			{
				if (item != null)
				{
					ProcessChildObjectElement(item, context, collectedObjects);
				}
			}
			return;
		}
		case ContainerType.Dictionary:
		{
			foreach (DictionaryEntry item2 in (IDictionary)target)
			{
				ProcessChildObjectElement(item2.Key, context, collectedObjects);
				object value2 = item2.Value;
				if (value2 != null)
				{
					ProcessChildObjectElement(value2, context, collectedObjects);
				}
			}
			return;
		}
		case ContainerType.Array:
		{
			Array array = (Array)target;
			for (int i = 0; i < array.Length; i++)
			{
				object value = array.GetValue(i);
				if (value != null)
				{
					ProcessChildObjectElement(value, context, collectedObjects);
				}
			}
			return;
		}
		}
		foreach (object childElement in GetChildElements(containerType, target))
		{
			ProcessChildObjectElement(childElement, context, collectedObjects);
		}
	}

	private static void ProcessChildObjectElement(object childElement, ISaveContext context, List<object> collectedObjects)
	{
		Type type = childElement.GetType();
		bool isClass = type.IsClass;
		if (isClass && type != typeof(string))
		{
			collectedObjects.Add(childElement);
		}
		else
		{
			if (isClass)
			{
				return;
			}
			TypeDefinition structDefinition = context.DefinitionContext.GetStructDefinition(type);
			if (structDefinition == null)
			{
				return;
			}
			if (structDefinition.CollectObjectsMethod != null)
			{
				structDefinition.CollectObjectsMethod(childElement, collectedObjects);
				return;
			}
			for (int i = 0; i < structDefinition.MemberDefinitions.Count; i++)
			{
				MemberDefinition memberDefinition = structDefinition.MemberDefinitions[i];
				ObjectSaveData.GetChildObjectFrom(context, childElement, memberDefinition, collectedObjects);
			}
		}
	}

	private IEnumerable<object> GetChildString()
	{
		foreach (object childElement in GetChildElements())
		{
			if (childElement.GetType() == typeof(string))
			{
				yield return childElement;
			}
		}
	}
}
