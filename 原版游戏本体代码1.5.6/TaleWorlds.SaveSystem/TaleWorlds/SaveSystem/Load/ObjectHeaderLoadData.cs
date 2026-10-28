using System;
using System.Runtime.Serialization;
using TaleWorlds.Library;
using TaleWorlds.SaveSystem.Definition;

namespace TaleWorlds.SaveSystem.Load;

public class ObjectHeaderLoadData
{
	public int Id { get; private set; }

	public object LoadedObject { get; private set; }

	public object Target { get; private set; }

	public short PropertyCount { get; private set; }

	public short ChildStructCount { get; private set; }

	public TypeDefinition TypeDefinition { get; private set; }

	public LoadContext Context { get; private set; }

	public SaveId SaveId { get; private set; }

	public ObjectHeaderLoadData(LoadContext context, int id)
	{
		Context = context;
		Id = id;
	}

	public void InitialieReaders(SaveEntryFolder saveEntryFolder)
	{
		BinaryReader binaryReader = saveEntryFolder.GetEntry(new EntryId(-1, SaveEntryExtension.Basics)).GetBinaryReader();
		SaveId = SaveId.ReadSaveIdFrom(binaryReader);
		PropertyCount = binaryReader.ReadShort();
		ChildStructCount = binaryReader.ReadShort();
	}

	public void CreateObject()
	{
		TypeDefinition = Context.DefinitionContext.TryGetTypeDefinition(SaveId) as TypeDefinition;
		if (TypeDefinition != null)
		{
			Type type = TypeDefinition.Type;
			LoadedObject = FormatterServices.GetUninitializedObject(type);
			Target = LoadedObject;
		}
	}

	public void AdvancedResolveObject(MetaData metaData, ObjectLoadData objectLoadData)
	{
		Target = TypeDefinition.AdvancedResolveObject(LoadedObject, metaData, objectLoadData);
	}

	public void ResolveObject()
	{
		Target = TypeDefinition.ResolveObject(LoadedObject);
	}
}
