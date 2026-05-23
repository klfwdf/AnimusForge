using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Utilities;

namespace Newtonsoft.Json.Schema
{
	// Token: 0x020000AC RID: 172
	[Obsolete("JSON Schema validation has been moved to its own package. See https://www.newtonsoft.com/jsonschema for more details.")]
	internal class JsonSchemaModel
	{
		// Token: 0x17000198 RID: 408
		// (get) Token: 0x0600091E RID: 2334 RVA: 0x00026E84 File Offset: 0x00025084
		// (set) Token: 0x0600091F RID: 2335 RVA: 0x00026E8C File Offset: 0x0002508C
		public bool Required { get; set; }

		// Token: 0x17000199 RID: 409
		// (get) Token: 0x06000920 RID: 2336 RVA: 0x00026E95 File Offset: 0x00025095
		// (set) Token: 0x06000921 RID: 2337 RVA: 0x00026E9D File Offset: 0x0002509D
		public JsonSchemaType Type { get; set; }

		// Token: 0x1700019A RID: 410
		// (get) Token: 0x06000922 RID: 2338 RVA: 0x00026EA6 File Offset: 0x000250A6
		// (set) Token: 0x06000923 RID: 2339 RVA: 0x00026EAE File Offset: 0x000250AE
		public int? MinimumLength { get; set; }

		// Token: 0x1700019B RID: 411
		// (get) Token: 0x06000924 RID: 2340 RVA: 0x00026EB7 File Offset: 0x000250B7
		// (set) Token: 0x06000925 RID: 2341 RVA: 0x00026EBF File Offset: 0x000250BF
		public int? MaximumLength { get; set; }

		// Token: 0x1700019C RID: 412
		// (get) Token: 0x06000926 RID: 2342 RVA: 0x00026EC8 File Offset: 0x000250C8
		// (set) Token: 0x06000927 RID: 2343 RVA: 0x00026ED0 File Offset: 0x000250D0
		public double? DivisibleBy { get; set; }

		// Token: 0x1700019D RID: 413
		// (get) Token: 0x06000928 RID: 2344 RVA: 0x00026ED9 File Offset: 0x000250D9
		// (set) Token: 0x06000929 RID: 2345 RVA: 0x00026EE1 File Offset: 0x000250E1
		public double? Minimum { get; set; }

		// Token: 0x1700019E RID: 414
		// (get) Token: 0x0600092A RID: 2346 RVA: 0x00026EEA File Offset: 0x000250EA
		// (set) Token: 0x0600092B RID: 2347 RVA: 0x00026EF2 File Offset: 0x000250F2
		public double? Maximum { get; set; }

		// Token: 0x1700019F RID: 415
		// (get) Token: 0x0600092C RID: 2348 RVA: 0x00026EFB File Offset: 0x000250FB
		// (set) Token: 0x0600092D RID: 2349 RVA: 0x00026F03 File Offset: 0x00025103
		public bool ExclusiveMinimum { get; set; }

		// Token: 0x170001A0 RID: 416
		// (get) Token: 0x0600092E RID: 2350 RVA: 0x00026F0C File Offset: 0x0002510C
		// (set) Token: 0x0600092F RID: 2351 RVA: 0x00026F14 File Offset: 0x00025114
		public bool ExclusiveMaximum { get; set; }

		// Token: 0x170001A1 RID: 417
		// (get) Token: 0x06000930 RID: 2352 RVA: 0x00026F1D File Offset: 0x0002511D
		// (set) Token: 0x06000931 RID: 2353 RVA: 0x00026F25 File Offset: 0x00025125
		public int? MinimumItems { get; set; }

		// Token: 0x170001A2 RID: 418
		// (get) Token: 0x06000932 RID: 2354 RVA: 0x00026F2E File Offset: 0x0002512E
		// (set) Token: 0x06000933 RID: 2355 RVA: 0x00026F36 File Offset: 0x00025136
		public int? MaximumItems { get; set; }

		// Token: 0x170001A3 RID: 419
		// (get) Token: 0x06000934 RID: 2356 RVA: 0x00026F3F File Offset: 0x0002513F
		// (set) Token: 0x06000935 RID: 2357 RVA: 0x00026F47 File Offset: 0x00025147
		public IList<string> Patterns { get; set; }

		// Token: 0x170001A4 RID: 420
		// (get) Token: 0x06000936 RID: 2358 RVA: 0x00026F50 File Offset: 0x00025150
		// (set) Token: 0x06000937 RID: 2359 RVA: 0x00026F58 File Offset: 0x00025158
		public IList<JsonSchemaModel> Items { get; set; }

		// Token: 0x170001A5 RID: 421
		// (get) Token: 0x06000938 RID: 2360 RVA: 0x00026F61 File Offset: 0x00025161
		// (set) Token: 0x06000939 RID: 2361 RVA: 0x00026F69 File Offset: 0x00025169
		public IDictionary<string, JsonSchemaModel> Properties { get; set; }

		// Token: 0x170001A6 RID: 422
		// (get) Token: 0x0600093A RID: 2362 RVA: 0x00026F72 File Offset: 0x00025172
		// (set) Token: 0x0600093B RID: 2363 RVA: 0x00026F7A File Offset: 0x0002517A
		public IDictionary<string, JsonSchemaModel> PatternProperties { get; set; }

		// Token: 0x170001A7 RID: 423
		// (get) Token: 0x0600093C RID: 2364 RVA: 0x00026F83 File Offset: 0x00025183
		// (set) Token: 0x0600093D RID: 2365 RVA: 0x00026F8B File Offset: 0x0002518B
		public JsonSchemaModel AdditionalProperties { get; set; }

		// Token: 0x170001A8 RID: 424
		// (get) Token: 0x0600093E RID: 2366 RVA: 0x00026F94 File Offset: 0x00025194
		// (set) Token: 0x0600093F RID: 2367 RVA: 0x00026F9C File Offset: 0x0002519C
		public JsonSchemaModel AdditionalItems { get; set; }

		// Token: 0x170001A9 RID: 425
		// (get) Token: 0x06000940 RID: 2368 RVA: 0x00026FA5 File Offset: 0x000251A5
		// (set) Token: 0x06000941 RID: 2369 RVA: 0x00026FAD File Offset: 0x000251AD
		public bool PositionalItemsValidation { get; set; }

		// Token: 0x170001AA RID: 426
		// (get) Token: 0x06000942 RID: 2370 RVA: 0x00026FB6 File Offset: 0x000251B6
		// (set) Token: 0x06000943 RID: 2371 RVA: 0x00026FBE File Offset: 0x000251BE
		public bool AllowAdditionalProperties { get; set; }

		// Token: 0x170001AB RID: 427
		// (get) Token: 0x06000944 RID: 2372 RVA: 0x00026FC7 File Offset: 0x000251C7
		// (set) Token: 0x06000945 RID: 2373 RVA: 0x00026FCF File Offset: 0x000251CF
		public bool AllowAdditionalItems { get; set; }

		// Token: 0x170001AC RID: 428
		// (get) Token: 0x06000946 RID: 2374 RVA: 0x00026FD8 File Offset: 0x000251D8
		// (set) Token: 0x06000947 RID: 2375 RVA: 0x00026FE0 File Offset: 0x000251E0
		public bool UniqueItems { get; set; }

		// Token: 0x170001AD RID: 429
		// (get) Token: 0x06000948 RID: 2376 RVA: 0x00026FE9 File Offset: 0x000251E9
		// (set) Token: 0x06000949 RID: 2377 RVA: 0x00026FF1 File Offset: 0x000251F1
		public IList<JToken> Enum { get; set; }

		// Token: 0x170001AE RID: 430
		// (get) Token: 0x0600094A RID: 2378 RVA: 0x00026FFA File Offset: 0x000251FA
		// (set) Token: 0x0600094B RID: 2379 RVA: 0x00027002 File Offset: 0x00025202
		public JsonSchemaType Disallow { get; set; }

		// Token: 0x0600094C RID: 2380 RVA: 0x0002700B File Offset: 0x0002520B
		public JsonSchemaModel()
		{
			this.Type = JsonSchemaType.Any;
			this.AllowAdditionalProperties = true;
			this.AllowAdditionalItems = true;
			this.Required = false;
		}

		// Token: 0x0600094D RID: 2381 RVA: 0x00027030 File Offset: 0x00025230
		public static JsonSchemaModel Create(IList<JsonSchema> schemata)
		{
			JsonSchemaModel jsonSchemaModel = new JsonSchemaModel();
			foreach (JsonSchema schema in schemata)
			{
				JsonSchemaModel.Combine(jsonSchemaModel, schema);
			}
			return jsonSchemaModel;
		}

		// Token: 0x0600094E RID: 2382 RVA: 0x00027080 File Offset: 0x00025280
		private static void Combine(JsonSchemaModel model, JsonSchema schema)
		{
			model.Required = model.Required || schema.Required.GetValueOrDefault();
			model.Type &= schema.Type ?? JsonSchemaType.Any;
			model.MinimumLength = MathUtils.Max(model.MinimumLength, schema.MinimumLength);
			model.MaximumLength = MathUtils.Min(model.MaximumLength, schema.MaximumLength);
			model.DivisibleBy = MathUtils.Max(model.DivisibleBy, schema.DivisibleBy);
			model.Minimum = MathUtils.Max(model.Minimum, schema.Minimum);
			model.Maximum = MathUtils.Max(model.Maximum, schema.Maximum);
			model.ExclusiveMinimum = model.ExclusiveMinimum || schema.ExclusiveMinimum.GetValueOrDefault();
			model.ExclusiveMaximum = model.ExclusiveMaximum || schema.ExclusiveMaximum.GetValueOrDefault();
			model.MinimumItems = MathUtils.Max(model.MinimumItems, schema.MinimumItems);
			model.MaximumItems = MathUtils.Min(model.MaximumItems, schema.MaximumItems);
			model.PositionalItemsValidation = model.PositionalItemsValidation || schema.PositionalItemsValidation;
			model.AllowAdditionalProperties = model.AllowAdditionalProperties && schema.AllowAdditionalProperties;
			model.AllowAdditionalItems = model.AllowAdditionalItems && schema.AllowAdditionalItems;
			model.UniqueItems = model.UniqueItems || schema.UniqueItems;
			if (schema.Enum != null)
			{
				if (model.Enum == null)
				{
					model.Enum = new List<JToken>();
				}
				model.Enum.AddRangeDistinct(schema.Enum, JToken.EqualityComparer);
			}
			model.Disallow |= schema.Disallow.GetValueOrDefault();
			if (schema.Pattern != null)
			{
				if (model.Patterns == null)
				{
					model.Patterns = new List<string>();
				}
				model.Patterns.AddDistinct(schema.Pattern);
			}
		}
	}
}
