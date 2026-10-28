namespace System.Reflection.PortableExecutable;

internal readonly struct CodeViewDebugDirectoryData
{
	public Guid Guid { get; }

	public int Age { get; }

	public string Path { get; }

	internal CodeViewDebugDirectoryData(Guid guid, int age, string path)
	{
		Path = path;
		Guid = guid;
		Age = age;
	}
}
