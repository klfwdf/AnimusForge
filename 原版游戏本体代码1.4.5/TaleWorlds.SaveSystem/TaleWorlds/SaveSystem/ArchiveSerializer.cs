using System.Collections.Generic;
using TaleWorlds.Library;

namespace TaleWorlds.SaveSystem;

internal class ArchiveSerializer : IArchiveContext
{
	private BinaryWriter _writer;

	private int _entryCount;

	private int _folderCount;

	private List<SaveEntryFolder> _folders;

	public ArchiveSerializer()
	{
		_writer = BinaryWriterFactory.GetBinaryWriter();
		_folders = new List<SaveEntryFolder>();
	}

	public void SerializeEntry(SaveEntry entry)
	{
		_writer.Write3ByteInt(entry.FolderId);
		_writer.Write3ByteInt(entry.Id.Id);
		_writer.WriteByte((byte)entry.Id.Extension);
		_writer.WriteShort((short)entry.Data.Length);
		_writer.WriteBytes(entry.Data);
		_entryCount++;
	}

	public void SerializeFolder(SaveEntryFolder folder)
	{
		foreach (SaveEntry allEntry in folder.GetAllEntries())
		{
			SerializeEntry(allEntry);
		}
	}

	public SaveEntryFolder CreateFolder(SaveEntryFolder parentFolder, FolderId folderId, int entryCount)
	{
		int folderCount = _folderCount;
		_folderCount++;
		SaveEntryFolder saveEntryFolder = new SaveEntryFolder(parentFolder, folderCount, folderId, entryCount);
		parentFolder.AddChildFolderEntry(saveEntryFolder);
		_folders.Add(saveEntryFolder);
		return saveEntryFolder;
	}

	public byte[] FinalizeAndGetBinaryData()
	{
		BinaryWriter binaryWriter = BinaryWriterFactory.GetBinaryWriter();
		binaryWriter.WriteInt(_folderCount);
		for (int i = 0; i < _folderCount; i++)
		{
			SaveEntryFolder saveEntryFolder = _folders[i];
			int parentGlobalId = saveEntryFolder.ParentGlobalId;
			int globalId = saveEntryFolder.GlobalId;
			int localId = saveEntryFolder.FolderId.LocalId;
			SaveFolderExtension extension = saveEntryFolder.FolderId.Extension;
			binaryWriter.Write3ByteInt(parentGlobalId);
			binaryWriter.Write3ByteInt(globalId);
			binaryWriter.Write3ByteInt(localId);
			binaryWriter.WriteByte((byte)extension);
		}
		binaryWriter.WriteInt(_entryCount);
		binaryWriter.AppendData(_writer);
		byte[] finalData = binaryWriter.GetFinalData();
		BinaryWriterFactory.ReleaseBinaryWriter(binaryWriter);
		BinaryWriterFactory.ReleaseBinaryWriter(_writer);
		_writer = null;
		return finalData;
	}

	public byte[] GetBinaryDataDebug()
	{
		BinaryWriter binaryWriter = BinaryWriterFactory.GetBinaryWriter();
		binaryWriter.WriteInt(_folderCount);
		for (int i = 0; i < _folderCount; i++)
		{
			SaveEntryFolder saveEntryFolder = _folders[i];
			int parentGlobalId = saveEntryFolder.ParentGlobalId;
			int globalId = saveEntryFolder.GlobalId;
			int localId = saveEntryFolder.FolderId.LocalId;
			SaveFolderExtension extension = saveEntryFolder.FolderId.Extension;
			binaryWriter.Write3ByteInt(parentGlobalId);
			binaryWriter.Write3ByteInt(globalId);
			binaryWriter.Write3ByteInt(localId);
			binaryWriter.WriteByte((byte)extension);
		}
		binaryWriter.WriteInt(_entryCount);
		binaryWriter.AppendData(_writer);
		byte[] finalData = binaryWriter.GetFinalData();
		BinaryWriterFactory.ReleaseBinaryWriter(binaryWriter);
		_writer = null;
		return finalData;
	}
}
