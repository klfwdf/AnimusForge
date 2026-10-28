namespace MCM.Abstractions.GameFeatures;

public interface IFileSystemProvider
{
	GameDirectory GetModSettingsDirectory();

	GameDirectory? GetDirectory(GameDirectory directory, string directoryName);

	GameDirectory GetOrCreateDirectory(GameDirectory rootFolder, string id);

	GameFile[] GetFiles(GameDirectory directory, string searchPattern);

	GameFile? GetFile(GameDirectory directory, string fileName);

	GameFile GetOrCreateFile(GameDirectory directory, string fileName);

	bool WriteData(GameFile file, byte[]? data);

	byte[]? ReadData(GameFile file);

	string? GetSystemPath(GameFile file);

	string? GetSystemPath(GameDirectory directory);
}
