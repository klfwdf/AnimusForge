using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Newtonsoft.Json.Schema
{
	// Token: 0x020000AE RID: 174
	[Obsolete("JSON Schema validation has been moved to its own package. See https://www.newtonsoft.com/jsonschema for more details.")]
	internal class JsonSchemaNode
	{
		// Token: 0x170001AF RID: 431
		// (get) Token: 0x06000958 RID: 2392 RVA: 0x000276EA File Offset: 0x000258EA
		public string Id { get; }

		// Token: 0x170001B0 RID: 432
		// (get) Token: 0x06000959 RID: 2393 RVA: 0x000276F2 File Offset: 0x000258F2
		public ReadOnlyCollection<JsonSchema> Schemas { get; }

		// Token: 0x170001B1 RID: 433
		// (get) Token: 0x0600095A RID: 2394 RVA: 0x000276FA File Offset: 0x000258FA
		public Dictionary<string, JsonSchemaNode> Properties { get; }

		// Token: 0x170001B2 RID: 434
		// (get) Token: 0x0600095B RID: 2395 RVA: 0x00027702 File Offset: 0x00025902
		public Dictionary<string, JsonSchemaNode> PatternProperties { get; }

		// Token: 0x170001B3 RID: 435
		// (get) Token: 0x0600095C RID: 2396 RVA: 0x0002770A File Offset: 0x0002590A
		public List<JsonSchemaNode> Items { get; }

		// Token: 0x170001B4 RID: 436
		// (get) Token: 0x0600095D RID: 2397 RVA: 0x00027712 File Offset: 0x00025912
		// (set) Token: 0x0600095E RID: 2398 RVA: 0x0002771A File Offset: 0x0002591A
		public JsonSchemaNode AdditionalProperties { get; set; }

		// Token: 0x170001B5 RID: 437
		// (get) Token: 0x0600095F RID: 2399 RVA: 0x00027723 File Offset: 0x00025923
		// (set) Token: 0x06000960 RID: 2400 RVA: 0x0002772B File Offset: 0x0002592B
		public JsonSchemaNode AdditionalItems { get; set; }

		// Token: 0x06000961 RID: 2401 RVA: 0x00027734 File Offset: 0x00025934
		public JsonSchemaNode(JsonSchema schema)
		{
			this.Schemas = new ReadOnlyCollection<JsonSchema>(new JsonSchema[] { schema });
			this.Properties = new Dictionary<string, JsonSchemaNode>();
			this.PatternProperties = new Dictionary<string, JsonSchemaNode>();
			this.Items = new List<JsonSchemaNode>();
			this.Id = JsonSchemaNode.GetId(this.Schemas);
		}

		// Token: 0x06000962 RID: 2402 RVA: 0x00027790 File Offset: 0x00025990
		private JsonSchemaNode(JsonSchemaNode source, JsonSchema schema)
		{
			this.Schemas = new ReadOnlyCollection<JsonSchema>(source.Schemas.Union(new JsonSchema[] { schema }).ToList<JsonSchema>());
			this.Properties = new Dictionary<string, JsonSchemaNode>(source.Properties);
			this.PatternProperties = new Dictionary<string, JsonSchemaNode>(source.PatternProperties);
			this.Items = new List<JsonSchemaNode>(source.Items);
			this.AdditionalProperties = source.AdditionalProperties;
			this.AdditionalItems = source.AdditionalItems;
			this.Id = JsonSchemaNode.GetId(this.Schemas);
		}

		// Token: 0x06000963 RID: 2403 RVA: 0x00027824 File Offset: 0x00025A24
		public JsonSchemaNode Combine(JsonSchema schema)
		{
			return new JsonSchemaNode(this, schema);
		}

		// Token: 0x06000964 RID: 2404 RVA: 0x00027830 File Offset: 0x00025A30
		public static string GetId(IEnumerable<JsonSchema> schemata)
		{
			return string.Join("-", (from s in schemata
				select s.InternalId).OrderBy((string id) => id, StringComparer.Ordinal));
		}
	}
}
