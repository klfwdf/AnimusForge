using System;
using System.Collections.Generic;
using System.Linq;

namespace Newtonsoft.Json.Schema
{
	/// <summary>
	/// <para>
	/// Resolves <see cref="T:Newtonsoft.Json.Schema.JsonSchema" /> from an id.
	/// </para>
	/// <note type="caution">
	/// JSON Schema validation has been moved to its own package. See <see href="https://www.newtonsoft.com/jsonschema">https://www.newtonsoft.com/jsonschema</see> for more details.
	/// </note>
	/// </summary>
	// Token: 0x020000B0 RID: 176
	[Obsolete("JSON Schema validation has been moved to its own package. See https://www.newtonsoft.com/jsonschema for more details.")]
	public class JsonSchemaResolver
	{
		/// <summary>
		/// Gets or sets the loaded schemas.
		/// </summary>
		/// <value>The loaded schemas.</value>
		// Token: 0x170001B6 RID: 438
		// (get) Token: 0x06000967 RID: 2407 RVA: 0x000278A5 File Offset: 0x00025AA5
		// (set) Token: 0x06000968 RID: 2408 RVA: 0x000278AD File Offset: 0x00025AAD
		public IList<JsonSchema> LoadedSchemas { get; protected set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Schema.JsonSchemaResolver" /> class.
		/// </summary>
		// Token: 0x06000969 RID: 2409 RVA: 0x000278B6 File Offset: 0x00025AB6
		public JsonSchemaResolver()
		{
			this.LoadedSchemas = new List<JsonSchema>();
		}

		/// <summary>
		/// Gets a <see cref="T:Newtonsoft.Json.Schema.JsonSchema" /> for the specified reference.
		/// </summary>
		/// <param name="reference">The id.</param>
		/// <returns>A <see cref="T:Newtonsoft.Json.Schema.JsonSchema" /> for the specified reference.</returns>
		// Token: 0x0600096A RID: 2410 RVA: 0x000278CC File Offset: 0x00025ACC
		public virtual JsonSchema GetSchema(string reference)
		{
			JsonSchema jsonSchema = this.LoadedSchemas.SingleOrDefault((JsonSchema s) => string.Equals(s.Id, reference, StringComparison.Ordinal));
			if (jsonSchema == null)
			{
				jsonSchema = this.LoadedSchemas.SingleOrDefault((JsonSchema s) => string.Equals(s.Location, reference, StringComparison.Ordinal));
			}
			return jsonSchema;
		}
	}
}
