using System;

namespace Newtonsoft.Json
{
	/// <summary>
	/// Specifies type name handling options for the <see cref="T:Newtonsoft.Json.JsonSerializer" />.
	/// </summary>
	/// <remarks>
	/// <see cref="P:Newtonsoft.Json.JsonSerializer.TypeNameHandling" /> should be used with caution when your application deserializes JSON from an external source.
	/// Incoming types should be validated with a custom <see cref="P:Newtonsoft.Json.JsonSerializer.SerializationBinder" />
	/// when deserializing with a value other than <see cref="F:Newtonsoft.Json.TypeNameHandling.None" />.
	/// </remarks>
	// Token: 0x0200003E RID: 62
	[Flags]
	public enum TypeNameHandling
	{
		/// <summary>
		/// Do not include the .NET type name when serializing types.
		/// </summary>
		// Token: 0x0400013E RID: 318
		None = 0,
		/// <summary>
		/// Include the .NET type name when serializing into a JSON object structure.
		/// </summary>
		// Token: 0x0400013F RID: 319
		Objects = 1,
		/// <summary>
		/// Include the .NET type name when serializing into a JSON array structure.
		/// </summary>
		// Token: 0x04000140 RID: 320
		Arrays = 2,
		/// <summary>
		/// Always include the .NET type name when serializing.
		/// </summary>
		// Token: 0x04000141 RID: 321
		All = 3,
		/// <summary>
		/// Include the .NET type name when the type of the object being serialized is not the same as its declared type.
		/// Note that this doesn't include the root serialized object by default. To include the root object's type name in JSON
		/// you must specify a root type object with <see cref="M:Newtonsoft.Json.JsonConvert.SerializeObject(System.Object,System.Type,Newtonsoft.Json.JsonSerializerSettings)" />
		/// or <see cref="M:Newtonsoft.Json.JsonSerializer.Serialize(Newtonsoft.Json.JsonWriter,System.Object,System.Type)" />.
		/// </summary>
		// Token: 0x04000142 RID: 322
		Auto = 4
	}
}
