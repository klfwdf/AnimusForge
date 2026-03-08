using System;

namespace Newtonsoft.Json.Schema
{
	/// <summary>
	/// <para>
	/// The value types allowed by the <see cref="T:Newtonsoft.Json.Schema.JsonSchema" />.
	/// </para>
	/// <note type="caution">
	/// JSON Schema validation has been moved to its own package. See <see href="https://www.newtonsoft.com/jsonschema">https://www.newtonsoft.com/jsonschema</see> for more details.
	/// </note>
	/// </summary>
	// Token: 0x020000B1 RID: 177
	[Flags]
	[Obsolete("JSON Schema validation has been moved to its own package. See https://www.newtonsoft.com/jsonschema for more details.")]
	public enum JsonSchemaType
	{
		/// <summary>
		/// No type specified.
		/// </summary>
		// Token: 0x04000362 RID: 866
		None = 0,
		/// <summary>
		/// String type.
		/// </summary>
		// Token: 0x04000363 RID: 867
		String = 1,
		/// <summary>
		/// Float type.
		/// </summary>
		// Token: 0x04000364 RID: 868
		Float = 2,
		/// <summary>
		/// Integer type.
		/// </summary>
		// Token: 0x04000365 RID: 869
		Integer = 4,
		/// <summary>
		/// Boolean type.
		/// </summary>
		// Token: 0x04000366 RID: 870
		Boolean = 8,
		/// <summary>
		/// Object type.
		/// </summary>
		// Token: 0x04000367 RID: 871
		Object = 16,
		/// <summary>
		/// Array type.
		/// </summary>
		// Token: 0x04000368 RID: 872
		Array = 32,
		/// <summary>
		/// Null type.
		/// </summary>
		// Token: 0x04000369 RID: 873
		Null = 64,
		/// <summary>
		/// Any type.
		/// </summary>
		// Token: 0x0400036A RID: 874
		Any = 127
	}
}
