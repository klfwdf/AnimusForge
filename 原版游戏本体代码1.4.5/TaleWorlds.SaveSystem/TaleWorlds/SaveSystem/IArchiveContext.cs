namespace TaleWorlds.SaveSystem;

internal interface IArchiveContext
{
	SaveEntryFolder CreateFolder(SaveEntryFolder parentFolder, FolderId folderId, int entryCount);
}
