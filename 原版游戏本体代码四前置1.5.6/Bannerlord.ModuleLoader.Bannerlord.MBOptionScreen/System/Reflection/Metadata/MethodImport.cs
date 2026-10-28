namespace System.Reflection.Metadata;

internal readonly struct MethodImport
{
	private readonly MethodImportAttributes _attributes;

	private readonly StringHandle _name;

	private readonly ModuleReferenceHandle _module;

	public MethodImportAttributes Attributes => _attributes;

	public StringHandle Name => _name;

	public ModuleReferenceHandle Module => _module;

	internal MethodImport(MethodImportAttributes attributes, StringHandle name, ModuleReferenceHandle module)
	{
		_attributes = attributes;
		_name = name;
		_module = module;
	}
}
