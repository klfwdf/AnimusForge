using System;
using TaleWorlds.Library;
using TaleWorlds.SaveSystem.Definition;

namespace TaleWorlds.SaveSystem.Load;

public class ContainerHeaderLoadData
{
	public int Id { get; private set; }

	public object Target { get; private set; }

	public LoadContext Context { get; private set; }

	public ContainerDefinition TypeDefinition { get; private set; }

	public SaveId SaveId { get; private set; }

	public int ElementCount { get; private set; }

	public ContainerType ContainerType { get; private set; }

	public ContainerHeaderLoadData(LoadContext context, int id)
	{
		Context = context;
		Id = id;
	}

	public bool GetObjectTypeDefinition()
	{
		TypeDefinition = Context.DefinitionContext.TryGetTypeDefinition(SaveId) as ContainerDefinition;
		return TypeDefinition != null;
	}

	public void CreateObject()
	{
		Type type = TypeDefinition.Type;
		if (ContainerType == ContainerType.Array)
		{
			Target = Activator.CreateInstance(type, ElementCount);
		}
		else if (ContainerType == ContainerType.List)
		{
			Target = Activator.CreateInstance(typeof(MBList<>).MakeGenericType(type.GetGenericArguments()));
		}
		else
		{
			Target = Activator.CreateInstance(type);
		}
	}

	public void InitialieReaders(SaveEntryFolder saveEntryFolder)
	{
		BinaryReader binaryReader = saveEntryFolder.GetEntry(new EntryId(-1, SaveEntryExtension.Object)).GetBinaryReader();
		SaveId = SaveId.ReadSaveIdFrom(binaryReader);
		ContainerType = (ContainerType)binaryReader.ReadByte();
		ElementCount = binaryReader.ReadInt();
	}
}
