using TaleWorlds.Library;

namespace TaleWorlds.SaveSystem;

public class SaveEntry
{
	private byte[] _data;

	public byte[] Data => _data;

	public EntryId Id { get; private set; }

	public int FolderId { get; private set; }

	public static SaveEntry CreateFrom(int entryFolderId, EntryId entryId, byte[] data)
	{
		return new SaveEntry
		{
			FolderId = entryFolderId,
			Id = entryId,
			_data = data
		};
	}

	public static SaveEntry CreateNew(SaveEntryFolder parentFolder, EntryId entryId)
	{
		return new SaveEntry
		{
			Id = entryId,
			FolderId = parentFolder.GlobalId
		};
	}

	public BinaryReader GetBinaryReader()
	{
		return new BinaryReader(_data);
	}

	public void FillFrom(BinaryWriter writer)
	{
		_data = writer.GetFinalData();
	}
}
