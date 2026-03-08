using System;

namespace Newtonsoft.Json.Schema
{
	/// <summary>
	/// <para>
	/// Specifies undefined schema Id handling options for the <see cref="T:Newtonsoft.Json.Schema.JsonSchemaGenerator" />.
	/// </para>
	/// <note type="caution">
	/// JSON Schema validation has been moved to its own package. See <see href="https://www.newtonsoft.com/jsonschema">https://www.newtonsoft.com/jsonschema</see> for more details.
	/// </note>
	/// </summary>
	// Token: 0x020000B3 RID: 179
	[Obsolete("JSON Schema validation has been moved to its own package. See https://www.newtonsoft.com/jsonschema for more details.")]
	public enum UndefinedSchemaIdHandling
	{
		/// <summary>
		/// Do not infer a schema Id.
		/// </summary>
		// Token: 0x0400036E RID: 878
		None,
		/// <summary>
		/// Use the .NET type name as the schema Id.
		/// </summary>
		// Token: 0x0400036F RID: 879
		UseTypeName,
		/// <summary>
		/// Use the assembly qualified .NET type name as the schema Id.
		/// </summary>
		// Token: 0x04000370 RID: 880
		UseAssemblyQualifiedName
	}
}
