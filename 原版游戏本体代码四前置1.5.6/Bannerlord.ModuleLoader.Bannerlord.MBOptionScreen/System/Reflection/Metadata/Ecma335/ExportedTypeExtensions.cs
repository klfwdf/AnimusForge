namespace System.Reflection.Metadata.Ecma335;

internal static class ExportedTypeExtensions
{
	public static int GetTypeDefinitionId(this ExportedType exportedType)
	{
		return exportedType.reader.ExportedTypeTable.GetTypeDefId(exportedType.rowId);
	}
}
