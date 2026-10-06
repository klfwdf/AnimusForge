using System.Collections.Generic;

namespace TaleWorlds.SaveSystem;

public class SaveEntryFolder
{
	private Dictionary<FolderId, SaveEntryFolder> _saveEntryFolders;

	private Dictionary<EntryId, SaveEntry> _entries;

	public int GlobalId { get; private set; }

	public int ParentGlobalId { get; private set; }

	public FolderId FolderId { get; private set; }

	public Dictionary<EntryId, SaveEntry>.ValueCollection ChildEntries => _entries.Values;

	public Dictionary<FolderId, SaveEntryFolder>.ValueCollection ChildFolders => _saveEntryFolders.Values;

	public List<SaveEntry> GetAllEntries()
	{
		List<SaveEntry> list = new List<SaveEntry>(_entries.Values);
		foreach (SaveEntryFolder value in _saveEntryFolders.Values)
		{
			list.AddRange(value.GetAllEntries());
		}
		return list;
	}

	public static SaveEntryFolder CreateRootFolder()
	{
		return new SaveEntryFolder(-1, -1, new FolderId(-1, SaveFolderExtension.Root), 3);
	}

	public SaveEntryFolder(SaveEntryFolder parent, int globalId, FolderId folderId, int entryCount)
		: this(parent.GlobalId, globalId, folderId, entryCount)
	{
	}

	public SaveEntryFolder(int parentGlobalId, int globalId, FolderId folderId, int entryCount)
	{
		ParentGlobalId = parentGlobalId;
		GlobalId = globalId;
		FolderId = folderId;
		_entries = new Dictionary<EntryId, SaveEntry>(entryCount);
		_saveEntryFolders = new Dictionary<FolderId, SaveEntryFolder>();
	}

	public void AddEntry(SaveEntry saveEntry)
	{
		_entries.Add(saveEntry.Id, saveEntry);
	}

	public SaveEntry GetEntry(EntryId entryId)
	{
		return _entries[entryId];
	}

	public void AddChildFolderEntry(SaveEntryFolder saveEntryFolder)
	{
		_saveEntryFolders.Add(saveEntryFolder.FolderId, saveEntryFolder);
	}

	internal SaveEntryFolder GetChildFolder(FolderId folderId)
	{
		return _saveEntryFolders[folderId];
	}

	public SaveEntry CreateEntry(EntryId entryId)
	{
		SaveEntry saveEntry = SaveEntry.CreateNew(this, entryId);
		AddEntry(saveEntry);
		return saveEntry;
	}
}
