namespace System.Runtime.InteropServices;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
internal sealed class _003Cfea68302_002Dff1f_002D430a_002Da79d_002Df3b3dc787afe_003ELibraryImportAttribute : Attribute
{
	public string LibraryName { get; }

	public string? EntryPoint { get; set; }

	public _003Cfea68302_002Dff1f_002D430a_002Da79d_002Df3b3dc787afe_003EStringMarshalling StringMarshalling { get; set; }

	public Type? StringMarshallingCustomType { get; set; }

	public bool SetLastError { get; set; }

	public _003Cfea68302_002Dff1f_002D430a_002Da79d_002Df3b3dc787afe_003ELibraryImportAttribute(string libraryName)
	{
		LibraryName = libraryName;
	}
}
