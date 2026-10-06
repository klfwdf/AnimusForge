using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using TaleWorlds.Library;
using TaleWorlds.SaveSystem.Definition;

namespace TaleWorlds.SaveSystem.Load;

public class ObjectLoadData
{
	private short _propertyCount;

	private List<PropertyLoadData> _propertyValues;

	private List<FieldLoadData> _fieldValues;

	private List<MemberLoadData> _memberValues;

	private SaveId _saveId;

	private List<ObjectLoadData> _childStructs;

	private short _childStructCount;

	public int Id { get; private set; }

	public object Target { get; private set; }

	public LoadContext Context { get; private set; }

	public TypeDefinition TypeDefinition { get; private set; }

	public object GetDataBySaveId(int localSaveId)
	{
		return _memberValues.SingleOrDefault((MemberLoadData value) => value.MemberSaveId.LocalSaveId == localSaveId)?.GetDataToUse();
	}

	public object GetMemberValueBySaveId(int localSaveId, int typeLevel)
	{
		return _memberValues.SingleOrDefault((MemberLoadData value) => value.MemberSaveId.LocalSaveId == localSaveId && value.MemberSaveId.TypeLevel == typeLevel)?.GetDataToUse();
	}

	public object GetMemberValueBySaveId(int localSaveId)
	{
		return _memberValues.SingleOrDefault((MemberLoadData value) => value.MemberSaveId.LocalSaveId == localSaveId)?.GetDataToUse();
	}

	public object GetFieldValueBySaveId(int localSaveId)
	{
		return _fieldValues.SingleOrDefault((FieldLoadData value) => value.MemberSaveId.LocalSaveId == localSaveId)?.GetDataToUse();
	}

	public object GetPropertyValueBySaveId(int localSaveId)
	{
		return _propertyValues.SingleOrDefault((PropertyLoadData value) => value.MemberSaveId.LocalSaveId == localSaveId)?.GetDataToUse();
	}

	public bool HasMember(int localSaveId)
	{
		return _memberValues.Any((MemberLoadData x) => x.MemberSaveId.LocalSaveId == localSaveId);
	}

	public bool HasMember(int localSaveId, int typeLevel)
	{
		return _memberValues.Any((MemberLoadData x) => x.MemberSaveId.LocalSaveId == localSaveId && x.MemberSaveId.TypeLevel == typeLevel);
	}

	public ObjectLoadData(LoadContext context, int id)
	{
		Context = context;
		Id = id;
		_propertyValues = new List<PropertyLoadData>();
		_fieldValues = new List<FieldLoadData>();
		_memberValues = new List<MemberLoadData>();
		_childStructs = new List<ObjectLoadData>();
	}

	public ObjectLoadData(ObjectHeaderLoadData headerLoadData)
	{
		Id = headerLoadData.Id;
		Target = headerLoadData.Target;
		Context = headerLoadData.Context;
		TypeDefinition = headerLoadData.TypeDefinition;
		_propertyValues = new List<PropertyLoadData>();
		_fieldValues = new List<FieldLoadData>();
		_memberValues = new List<MemberLoadData>();
		_childStructs = new List<ObjectLoadData>();
	}

	public void InitializeReaders(SaveEntryFolder saveEntryFolder)
	{
		BinaryReader binaryReader = saveEntryFolder.GetEntry(new EntryId(-1, SaveEntryExtension.Basics)).GetBinaryReader();
		_saveId = SaveId.ReadSaveIdFrom(binaryReader);
		_propertyCount = binaryReader.ReadShort();
		_childStructCount = binaryReader.ReadShort();
		for (int i = 0; i < _childStructCount; i++)
		{
			ObjectLoadData item = new ObjectLoadData(Context, i);
			_childStructs.Add(item);
		}
		foreach (SaveEntry childEntry in saveEntryFolder.ChildEntries)
		{
			if (childEntry.Id.Extension == SaveEntryExtension.Property)
			{
				BinaryReader binaryReader2 = childEntry.GetBinaryReader();
				PropertyLoadData item2 = new PropertyLoadData(this, binaryReader2);
				_propertyValues.Add(item2);
				_memberValues.Add(item2);
			}
			else if (childEntry.Id.Extension == SaveEntryExtension.Field)
			{
				BinaryReader binaryReader3 = childEntry.GetBinaryReader();
				FieldLoadData item3 = new FieldLoadData(this, binaryReader3);
				_fieldValues.Add(item3);
				_memberValues.Add(item3);
			}
		}
		for (int j = 0; j < _childStructCount; j++)
		{
			ObjectLoadData objectLoadData = _childStructs[j];
			SaveEntryFolder childFolder = saveEntryFolder.GetChildFolder(new FolderId(j, SaveFolderExtension.Struct));
			objectLoadData.InitializeReaders(childFolder);
		}
	}

	public void CreateStruct()
	{
		TypeDefinition = Context.DefinitionContext.TryGetTypeDefinition(_saveId) as TypeDefinition;
		if (TypeDefinition != null)
		{
			Type type = TypeDefinition.Type;
			Target = FormatterServices.GetUninitializedObject(type);
		}
		foreach (ObjectLoadData childStruct in _childStructs)
		{
			childStruct.CreateStruct();
		}
	}

	public void FillCreatedObject()
	{
		foreach (ObjectLoadData childStruct in _childStructs)
		{
			childStruct.CreateStruct();
		}
	}

	public void Read()
	{
		foreach (ObjectLoadData childStruct in _childStructs)
		{
			childStruct.Read();
		}
		foreach (MemberLoadData memberValue in _memberValues)
		{
			memberValue.Read();
			if (memberValue.SavedMemberType == SavedMemberType.CustomStruct)
			{
				int index = (int)memberValue.Data;
				object target = _childStructs[index].Target;
				memberValue.SetCustomStructData(target);
			}
		}
	}

	public void FillObject()
	{
		foreach (ObjectLoadData childStruct in _childStructs)
		{
			childStruct.FillObject();
		}
		foreach (FieldLoadData fieldValue in _fieldValues)
		{
			fieldValue.FillObject();
		}
		foreach (PropertyLoadData propertyValue in _propertyValues)
		{
			propertyValue.FillObject();
		}
	}
}
