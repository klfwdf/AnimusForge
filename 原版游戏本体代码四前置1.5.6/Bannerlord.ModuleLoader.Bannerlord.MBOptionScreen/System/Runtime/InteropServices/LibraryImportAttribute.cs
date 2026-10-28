namespace System.Runtime.InteropServices;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
internal sealed class LibraryImportAttribute : Attribute
{
	public string LibraryName { get; }

	public string? EntryPoint { get; set; }

	public StringMarshalling StringMarshalling { get; set; }

	public Type? StringMarshallingCustomType { get; set; }

	public bool SetLastError { get; set; }

	public LibraryImportAttribute(string libraryName)
	{
		LibraryName = libraryName;
	}
}
