namespace System.Reflection.Metadata;

internal readonly struct ModuleDefinition
{
	private readonly System.Reflection.Metadata.MetadataReader _reader;

	public int Generation => _reader.ModuleTable.GetGeneration();

	public StringHandle Name => _reader.ModuleTable.GetName();

	public GuidHandle Mvid => _reader.ModuleTable.GetMvid();

	public GuidHandle GenerationId => _reader.ModuleTable.GetEncId();

	public GuidHandle BaseGenerationId => _reader.ModuleTable.GetEncBaseId();

	internal ModuleDefinition(System.Reflection.Metadata.MetadataReader reader)
	{
		_reader = reader;
	}

	public CustomAttributeHandleCollection GetCustomAttributes()
	{
		return new CustomAttributeHandleCollection(_reader, EntityHandle.ModuleDefinition);
	}
}
