using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.Library;
using TaleWorlds.SaveSystem.Definition;

namespace TaleWorlds.SaveSystem.Load;

internal class ContainerLoadData
{
	private SaveId _saveId;

	private int _elementCount;

	private ContainerType _containerType;

	private ElementLoadData[] _keys;

	private ElementLoadData[] _values;

	private Dictionary<int, ObjectLoadData> _childStructs;

	public int Id => ContainerHeaderLoadData.Id;

	public object Target => ContainerHeaderLoadData.Target;

	public LoadContext Context => ContainerHeaderLoadData.Context;

	public ContainerDefinition TypeDefinition => ContainerHeaderLoadData.TypeDefinition;

	public ContainerHeaderLoadData ContainerHeaderLoadData { get; private set; }

	public ContainerLoadData(ContainerHeaderLoadData headerLoadData)
	{
		ContainerHeaderLoadData = headerLoadData;
		_childStructs = new Dictionary<int, ObjectLoadData>();
		_saveId = headerLoadData.SaveId;
		_containerType = headerLoadData.ContainerType;
		_elementCount = headerLoadData.ElementCount;
		_keys = new ElementLoadData[_elementCount];
		_values = new ElementLoadData[_elementCount];
	}

	private FolderId[] GetChildStructNames(SaveEntryFolder saveEntryFolder)
	{
		List<FolderId> list = new List<FolderId>();
		foreach (SaveEntryFolder childFolder in saveEntryFolder.ChildFolders)
		{
			if (childFolder.FolderId.Extension == SaveFolderExtension.Struct && !list.Contains(childFolder.FolderId))
			{
				list.Add(childFolder.FolderId);
			}
		}
		return list.ToArray();
	}

	public void InitializeReaders(SaveEntryFolder saveEntryFolder)
	{
		FolderId[] childStructNames = GetChildStructNames(saveEntryFolder);
		foreach (FolderId folderId in childStructNames)
		{
			int localId = folderId.LocalId;
			ObjectLoadData value = new ObjectLoadData(Context, localId);
			_childStructs.Add(localId, value);
		}
		for (int j = 0; j < _elementCount; j++)
		{
			BinaryReader binaryReader = saveEntryFolder.GetEntry(new EntryId(j, SaveEntryExtension.Value)).GetBinaryReader();
			ElementLoadData elementLoadData = new ElementLoadData(this, binaryReader);
			_values[j] = elementLoadData;
			if (_containerType == ContainerType.Dictionary)
			{
				BinaryReader binaryReader2 = saveEntryFolder.GetEntry(new EntryId(j, SaveEntryExtension.Key)).GetBinaryReader();
				ElementLoadData elementLoadData2 = new ElementLoadData(this, binaryReader2);
				_keys[j] = elementLoadData2;
			}
		}
		foreach (KeyValuePair<int, ObjectLoadData> childStruct in _childStructs)
		{
			int key = childStruct.Key;
			ObjectLoadData value2 = childStruct.Value;
			SaveEntryFolder childFolder = saveEntryFolder.GetChildFolder(new FolderId(key, SaveFolderExtension.Struct));
			value2.InitializeReaders(childFolder);
		}
	}

	public void FillCreatedObject()
	{
		foreach (ObjectLoadData value in _childStructs.Values)
		{
			value.CreateStruct();
		}
	}

	public void Read()
	{
		for (int i = 0; i < _elementCount; i++)
		{
			_values[i].Read();
			if (_containerType == ContainerType.Dictionary)
			{
				_keys[i].Read();
			}
		}
		foreach (ObjectLoadData value in _childStructs.Values)
		{
			value.Read();
		}
	}

	private static Assembly GetAssemblyByName(string name)
	{
		return AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault((Assembly assembly) => assembly.GetName().FullName == name);
	}

	public void FillObject()
	{
		foreach (ObjectLoadData value6 in _childStructs.Values)
		{
			value6.FillObject();
		}
		for (int i = 0; i < _elementCount; i++)
		{
			if (_containerType == ContainerType.List || _containerType == ContainerType.CustomList || _containerType == ContainerType.CustomReadOnlyList)
			{
				IList obj = (IList)Target;
				ElementLoadData elementLoadData = _values[i];
				if (elementLoadData.SavedMemberType == SavedMemberType.CustomStruct)
				{
					int key = (int)elementLoadData.Data;
					object obj2 = null;
					obj2 = ((!_childStructs.TryGetValue(key, out var value)) ? GetDefaultObject(_saveId, Context) : value.Target);
					elementLoadData.SetCustomStructData(obj2);
				}
				object dataToUse = elementLoadData.GetDataToUse();
				obj?.Add(dataToUse);
			}
			else if (_containerType == ContainerType.Dictionary)
			{
				IDictionary dictionary = (IDictionary)Target;
				ElementLoadData elementLoadData2 = _keys[i];
				ElementLoadData elementLoadData3 = _values[i];
				if (elementLoadData2.SavedMemberType == SavedMemberType.CustomStruct)
				{
					int key2 = (int)elementLoadData2.Data;
					object obj3 = null;
					obj3 = ((!_childStructs.TryGetValue(key2, out var value2)) ? GetDefaultObject(_saveId, Context) : value2.Target);
					elementLoadData2.SetCustomStructData(obj3);
				}
				if (elementLoadData3.SavedMemberType == SavedMemberType.CustomStruct)
				{
					int key3 = (int)elementLoadData3.Data;
					object obj4 = null;
					obj4 = ((!_childStructs.TryGetValue(key3, out var value3)) ? GetDefaultObject(_saveId, Context, getValueId: true) : value3.Target);
					elementLoadData3.SetCustomStructData(obj4);
				}
				object dataToUse2 = elementLoadData2.GetDataToUse();
				object dataToUse3 = elementLoadData3.GetDataToUse();
				if (dictionary != null && dataToUse2 != null)
				{
					dictionary.Add(dataToUse2, dataToUse3);
				}
			}
			else if (_containerType == ContainerType.Array)
			{
				Array obj5 = (Array)Target;
				ElementLoadData elementLoadData4 = _values[i];
				if (elementLoadData4.SavedMemberType == SavedMemberType.CustomStruct)
				{
					int key4 = (int)elementLoadData4.Data;
					object obj6 = null;
					obj6 = ((!_childStructs.TryGetValue(key4, out var value4)) ? GetDefaultObject(_saveId, Context) : value4.Target);
					elementLoadData4.SetCustomStructData(obj6);
				}
				object dataToUse4 = elementLoadData4.GetDataToUse();
				obj5.SetValue(dataToUse4, i);
			}
			else if (_containerType == ContainerType.Queue)
			{
				ICollection collection = (ICollection)Target;
				ElementLoadData elementLoadData5 = _values[i];
				if (elementLoadData5.SavedMemberType == SavedMemberType.CustomStruct)
				{
					int key5 = (int)elementLoadData5.Data;
					object obj7 = null;
					obj7 = ((!_childStructs.TryGetValue(key5, out var value5)) ? GetDefaultObject(_saveId, Context) : value5.Target);
					elementLoadData5.SetCustomStructData(obj7);
				}
				object dataToUse5 = elementLoadData5.GetDataToUse();
				collection.GetType().GetMethod("Enqueue").Invoke(collection, new object[1] { dataToUse5 });
			}
		}
	}

	private static object GetDefaultObject(SaveId saveId, LoadContext context, bool getValueId = false)
	{
		ContainerSaveId containerSaveId = (ContainerSaveId)saveId;
		TypeDefinitionBase typeDefinitionBase = null;
		typeDefinitionBase = ((!getValueId) ? context.DefinitionContext.TryGetTypeDefinition(containerSaveId.KeyId) : context.DefinitionContext.TryGetTypeDefinition(containerSaveId.ValueId));
		return Activator.CreateInstance(((StructDefinition)typeDefinitionBase).Type);
	}
}
