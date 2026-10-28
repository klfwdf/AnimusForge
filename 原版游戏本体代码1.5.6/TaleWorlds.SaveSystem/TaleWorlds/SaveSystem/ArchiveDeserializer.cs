using System.Collections.Generic;
using TaleWorlds.Library;

namespace TaleWorlds.SaveSystem;

internal class ArchiveDeserializer
{
	public SaveEntryFolder RootFolder { get; private set; }

	public ArchiveDeserializer()
	{
		RootFolder = new SaveEntryFolder(-1, -1, new FolderId(-1, SaveFolderExtension.Root), 3);
	}

	public void LoadFrom(byte[] binaryArchive)
	{
		Dictionary<int, SaveEntryFolder> dictionary = new Dictionary<int, SaveEntryFolder>();
		List<SaveEntry> list = new List<SaveEntry>();
		BinaryReader binaryReader = new BinaryReader(binaryArchive);
		int num = binaryReader.ReadInt();
		for (int i = 0; i < num; i++)
		{
			int parentGlobalId = binaryReader.Read3ByteInt();
			int globalId = binaryReader.Read3ByteInt();
			int localId = binaryReader.Read3ByteInt();
			SaveFolderExtension extension = (SaveFolderExtension)binaryReader.ReadByte();
			FolderId folderId = new FolderId(localId, extension);
			SaveEntryFolder saveEntryFolder = new SaveEntryFolder(parentGlobalId, globalId, folderId, 3);
			dictionary.Add(saveEntryFolder.GlobalId, saveEntryFolder);
		}
		int num2 = binaryReader.ReadInt();
		for (int j = 0; j < num2; j++)
		{
			int entryFolderId = binaryReader.Read3ByteInt();
			int id = binaryReader.Read3ByteInt();
			SaveEntryExtension extension2 = (SaveEntryExtension)binaryReader.ReadByte();
			short length = binaryReader.ReadShort();
			byte[] data = binaryReader.ReadBytes(length);
			SaveEntry item = SaveEntry.CreateFrom(entryFolderId, new EntryId(id, extension2), data);
			list.Add(item);
		}
		foreach (SaveEntryFolder value in dictionary.Values)
		{
			if (value.ParentGlobalId != -1)
			{
				dictionary[value.ParentGlobalId].AddChildFolderEntry(value);
			}
			else
			{
				RootFolder.AddChildFolderEntry(value);
			}
		}
		foreach (SaveEntry item2 in list)
		{
			if (item2.FolderId != -1)
			{
				dictionary[item2.FolderId].AddEntry(item2);
			}
			else
			{
				RootFolder.AddEntry(item2);
			}
		}
	}
}
