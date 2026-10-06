namespace TaleWorlds.Library;

public class ResourceDepotFile
{
	public ResourceDepotLocation ResourceDepotLocation { get; private set; }

	public string BasePath => ResourceDepotLocation.BasePath;

	public string Location => ResourceDepotLocation.Path;

	public string FileName { get; private set; }

	public string FullPath { get; private set; }

	public string FullPathLowerCase { get; private set; }

	public ResourceDepotFile(ResourceDepotLocation resourceDepotLocation, string fileName, string fullPath)
	{
		ResourceDepotLocation = resourceDepotLocation;
		FileName = fileName;
		FullPath = fullPath;
		FullPathLowerCase = fullPath.ToLower();
	}
}
