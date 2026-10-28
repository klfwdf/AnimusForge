namespace System.Reflection.Metadata;

internal readonly struct AssemblyDefinition
{
	private readonly System.Reflection.Metadata.MetadataReader _reader;

	public AssemblyHashAlgorithm HashAlgorithm => _reader.AssemblyTable.GetHashAlgorithm();

	public Version Version => _reader.AssemblyTable.GetVersion();

	public AssemblyFlags Flags => _reader.AssemblyTable.GetFlags();

	public StringHandle Name => _reader.AssemblyTable.GetName();

	public StringHandle Culture => _reader.AssemblyTable.GetCulture();

	public System.Reflection.Metadata.BlobHandle PublicKey => _reader.AssemblyTable.GetPublicKey();

	public AssemblyName GetAssemblyName()
	{
		AssemblyFlags assemblyFlags = Flags;
		if (!PublicKey.IsNil)
		{
			assemblyFlags |= AssemblyFlags.PublicKey;
		}
		return _reader.GetAssemblyName(Name, Version, Culture, PublicKey, HashAlgorithm, assemblyFlags);
	}

	internal AssemblyDefinition(System.Reflection.Metadata.MetadataReader reader)
	{
		_reader = reader;
	}

	public CustomAttributeHandleCollection GetCustomAttributes()
	{
		return new CustomAttributeHandleCollection(_reader, EntityHandle.AssemblyDefinition);
	}

	public DeclarativeSecurityAttributeHandleCollection GetDeclarativeSecurityAttributes()
	{
		return new DeclarativeSecurityAttributeHandleCollection(_reader, EntityHandle.AssemblyDefinition);
	}
}
