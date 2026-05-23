using System;
using TaleWorlds.Library;
using TaleWorlds.SaveSystem.Definition;

namespace TaleWorlds.SaveSystem.Load;

internal abstract class VariableLoadData
{
	private IReader _reader;

	private TypeDefinitionBase _typeDefinition;

	private SaveId _saveId;

	private object _customStructObject;

	public LoadContext Context { get; private set; }

	public MemberTypeId MemberSaveId { get; private set; }

	public SavedMemberType SavedMemberType { get; private set; }

	public object Data { get; private set; }

	protected VariableLoadData(LoadContext context, IReader reader)
	{
		Context = context;
		_reader = reader;
	}

	public void Read()
	{
		SavedMemberType = (SavedMemberType)_reader.ReadByte();
		MemberSaveId = new MemberTypeId
		{
			TypeLevel = _reader.ReadByte(),
			LocalSaveId = _reader.ReadShort()
		};
		if (SavedMemberType == SavedMemberType.Object)
		{
			Data = _reader.ReadInt();
		}
		else if (SavedMemberType == SavedMemberType.Container)
		{
			Data = _reader.ReadInt();
		}
		else if (SavedMemberType == SavedMemberType.String)
		{
			Data = _reader.ReadInt();
		}
		else if (SavedMemberType == SavedMemberType.Enum)
		{
			_saveId = SaveId.ReadSaveIdFrom(_reader);
			_typeDefinition = Context.DefinitionContext.TryGetTypeDefinition(_saveId);
			string text = _reader.ReadString();
			EnumDefinition enumDefinition = (EnumDefinition)_typeDefinition;
			if (enumDefinition?.Resolver != null)
			{
				Data = enumDefinition.Resolver.ResolveObject(text);
			}
			else
			{
				Data = text;
			}
		}
		else if (SavedMemberType == SavedMemberType.BasicType)
		{
			_saveId = SaveId.ReadSaveIdFrom(_reader);
			_typeDefinition = Context.DefinitionContext.TryGetTypeDefinition(_saveId);
			BasicTypeDefinition basicTypeDefinition = (BasicTypeDefinition)_typeDefinition;
			Data = basicTypeDefinition.Serializer.Deserialize(_reader);
		}
		else if (SavedMemberType == SavedMemberType.CustomStruct)
		{
			Data = _reader.ReadInt();
		}
	}

	public void SetCustomStructData(object customStructObject)
	{
		_customStructObject = customStructObject;
	}

	public object GetDataToUse()
	{
		object result = null;
		if (SavedMemberType == SavedMemberType.Object)
		{
			ObjectHeaderLoadData objectWithId = Context.GetObjectWithId((int)Data);
			if (objectWithId != null)
			{
				result = objectWithId.Target;
			}
		}
		else if (SavedMemberType == SavedMemberType.Container)
		{
			ContainerHeaderLoadData containerWithId = Context.GetContainerWithId((int)Data);
			if (containerWithId != null)
			{
				result = containerWithId.Target;
			}
		}
		else if (SavedMemberType == SavedMemberType.String)
		{
			int id = (int)Data;
			result = Context.GetStringWithId(id);
		}
		else if (SavedMemberType == SavedMemberType.Enum)
		{
			if (_typeDefinition == null)
			{
				result = (string)Data;
			}
			else
			{
				EnumDefinition enumDefinition = (EnumDefinition)_typeDefinition;
				Type type = _typeDefinition.Type;
				if (Enum.IsDefined(type, Data) || enumDefinition.HasFlags)
				{
					result = Enum.Parse(type, (string)Data);
				}
			}
		}
		else if (SavedMemberType == SavedMemberType.BasicType)
		{
			result = Data;
		}
		else if (SavedMemberType == SavedMemberType.CustomStruct)
		{
			result = _customStructObject;
		}
		return result;
	}
}
