using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Utilities;

namespace Newtonsoft.Json.Schema
{
	/// <summary>
	/// <para>
	/// An in-memory representation of a JSON Schema.
	/// </para>
	/// <note type="caution">
	/// JSON Schema validation has been moved to its own package. See <see href="https://www.newtonsoft.com/jsonschema">https://www.newtonsoft.com/jsonschema</see> for more details.
	/// </note>
	/// </summary>
	// Token: 0x020000A7 RID: 167
	[Obsolete("JSON Schema validation has been moved to its own package. See https://www.newtonsoft.com/jsonschema for more details.")]
	public class JsonSchema
	{
		/// <summary>
		/// Gets or sets the id.
		/// </summary>
		// Token: 0x1700016C RID: 364
		// (get) Token: 0x0600089C RID: 2204 RVA: 0x00024F84 File Offset: 0x00023184
		// (set) Token: 0x0600089D RID: 2205 RVA: 0x00024F8C File Offset: 0x0002318C
		public string Id { get; set; }

		/// <summary>
		/// Gets or sets the title.
		/// </summary>
		// Token: 0x1700016D RID: 365
		// (get) Token: 0x0600089E RID: 2206 RVA: 0x00024F95 File Offset: 0x00023195
		// (set) Token: 0x0600089F RID: 2207 RVA: 0x00024F9D File Offset: 0x0002319D
		public string Title { get; set; }

		/// <summary>
		/// Gets or sets whether the object is required.
		/// </summary>
		// Token: 0x1700016E RID: 366
		// (get) Token: 0x060008A0 RID: 2208 RVA: 0x00024FA6 File Offset: 0x000231A6
		// (set) Token: 0x060008A1 RID: 2209 RVA: 0x00024FAE File Offset: 0x000231AE
		public bool? Required { get; set; }

		/// <summary>
		/// Gets or sets whether the object is read-only.
		/// </summary>
		// Token: 0x1700016F RID: 367
		// (get) Token: 0x060008A2 RID: 2210 RVA: 0x00024FB7 File Offset: 0x000231B7
		// (set) Token: 0x060008A3 RID: 2211 RVA: 0x00024FBF File Offset: 0x000231BF
		public bool? ReadOnly { get; set; }

		/// <summary>
		/// Gets or sets whether the object is visible to users.
		/// </summary>
		// Token: 0x17000170 RID: 368
		// (get) Token: 0x060008A4 RID: 2212 RVA: 0x00024FC8 File Offset: 0x000231C8
		// (set) Token: 0x060008A5 RID: 2213 RVA: 0x00024FD0 File Offset: 0x000231D0
		public bool? Hidden { get; set; }

		/// <summary>
		/// Gets or sets whether the object is transient.
		/// </summary>
		// Token: 0x17000171 RID: 369
		// (get) Token: 0x060008A6 RID: 2214 RVA: 0x00024FD9 File Offset: 0x000231D9
		// (set) Token: 0x060008A7 RID: 2215 RVA: 0x00024FE1 File Offset: 0x000231E1
		public bool? Transient { get; set; }

		/// <summary>
		/// Gets or sets the description of the object.
		/// </summary>
		// Token: 0x17000172 RID: 370
		// (get) Token: 0x060008A8 RID: 2216 RVA: 0x00024FEA File Offset: 0x000231EA
		// (set) Token: 0x060008A9 RID: 2217 RVA: 0x00024FF2 File Offset: 0x000231F2
		public string Description { get; set; }

		/// <summary>
		/// Gets or sets the types of values allowed by the object.
		/// </summary>
		/// <value>The type.</value>
		// Token: 0x17000173 RID: 371
		// (get) Token: 0x060008AA RID: 2218 RVA: 0x00024FFB File Offset: 0x000231FB
		// (set) Token: 0x060008AB RID: 2219 RVA: 0x00025003 File Offset: 0x00023203
		public JsonSchemaType? Type { get; set; }

		/// <summary>
		/// Gets or sets the pattern.
		/// </summary>
		/// <value>The pattern.</value>
		// Token: 0x17000174 RID: 372
		// (get) Token: 0x060008AC RID: 2220 RVA: 0x0002500C File Offset: 0x0002320C
		// (set) Token: 0x060008AD RID: 2221 RVA: 0x00025014 File Offset: 0x00023214
		public string Pattern { get; set; }

		/// <summary>
		/// Gets or sets the minimum length.
		/// </summary>
		/// <value>The minimum length.</value>
		// Token: 0x17000175 RID: 373
		// (get) Token: 0x060008AE RID: 2222 RVA: 0x0002501D File Offset: 0x0002321D
		// (set) Token: 0x060008AF RID: 2223 RVA: 0x00025025 File Offset: 0x00023225
		public int? MinimumLength { get; set; }

		/// <summary>
		/// Gets or sets the maximum length.
		/// </summary>
		/// <value>The maximum length.</value>
		// Token: 0x17000176 RID: 374
		// (get) Token: 0x060008B0 RID: 2224 RVA: 0x0002502E File Offset: 0x0002322E
		// (set) Token: 0x060008B1 RID: 2225 RVA: 0x00025036 File Offset: 0x00023236
		public int? MaximumLength { get; set; }

		/// <summary>
		/// Gets or sets a number that the value should be divisible by.
		/// </summary>
		/// <value>A number that the value should be divisible by.</value>
		// Token: 0x17000177 RID: 375
		// (get) Token: 0x060008B2 RID: 2226 RVA: 0x0002503F File Offset: 0x0002323F
		// (set) Token: 0x060008B3 RID: 2227 RVA: 0x00025047 File Offset: 0x00023247
		public double? DivisibleBy { get; set; }

		/// <summary>
		/// Gets or sets the minimum.
		/// </summary>
		/// <value>The minimum.</value>
		// Token: 0x17000178 RID: 376
		// (get) Token: 0x060008B4 RID: 2228 RVA: 0x00025050 File Offset: 0x00023250
		// (set) Token: 0x060008B5 RID: 2229 RVA: 0x00025058 File Offset: 0x00023258
		public double? Minimum { get; set; }

		/// <summary>
		/// Gets or sets the maximum.
		/// </summary>
		/// <value>The maximum.</value>
		// Token: 0x17000179 RID: 377
		// (get) Token: 0x060008B6 RID: 2230 RVA: 0x00025061 File Offset: 0x00023261
		// (set) Token: 0x060008B7 RID: 2231 RVA: 0x00025069 File Offset: 0x00023269
		public double? Maximum { get; set; }

		/// <summary>
		/// Gets or sets a flag indicating whether the value can not equal the number defined by the <c>minimum</c> attribute (<see cref="P:Newtonsoft.Json.Schema.JsonSchema.Minimum" />).
		/// </summary>
		/// <value>A flag indicating whether the value can not equal the number defined by the <c>minimum</c> attribute (<see cref="P:Newtonsoft.Json.Schema.JsonSchema.Minimum" />).</value>
		// Token: 0x1700017A RID: 378
		// (get) Token: 0x060008B8 RID: 2232 RVA: 0x00025072 File Offset: 0x00023272
		// (set) Token: 0x060008B9 RID: 2233 RVA: 0x0002507A File Offset: 0x0002327A
		public bool? ExclusiveMinimum { get; set; }

		/// <summary>
		/// Gets or sets a flag indicating whether the value can not equal the number defined by the <c>maximum</c> attribute (<see cref="P:Newtonsoft.Json.Schema.JsonSchema.Maximum" />).
		/// </summary>
		/// <value>A flag indicating whether the value can not equal the number defined by the <c>maximum</c> attribute (<see cref="P:Newtonsoft.Json.Schema.JsonSchema.Maximum" />).</value>
		// Token: 0x1700017B RID: 379
		// (get) Token: 0x060008BA RID: 2234 RVA: 0x00025083 File Offset: 0x00023283
		// (set) Token: 0x060008BB RID: 2235 RVA: 0x0002508B File Offset: 0x0002328B
		public bool? ExclusiveMaximum { get; set; }

		/// <summary>
		/// Gets or sets the minimum number of items.
		/// </summary>
		/// <value>The minimum number of items.</value>
		// Token: 0x1700017C RID: 380
		// (get) Token: 0x060008BC RID: 2236 RVA: 0x00025094 File Offset: 0x00023294
		// (set) Token: 0x060008BD RID: 2237 RVA: 0x0002509C File Offset: 0x0002329C
		public int? MinimumItems { get; set; }

		/// <summary>
		/// Gets or sets the maximum number of items.
		/// </summary>
		/// <value>The maximum number of items.</value>
		// Token: 0x1700017D RID: 381
		// (get) Token: 0x060008BE RID: 2238 RVA: 0x000250A5 File Offset: 0x000232A5
		// (set) Token: 0x060008BF RID: 2239 RVA: 0x000250AD File Offset: 0x000232AD
		public int? MaximumItems { get; set; }

		/// <summary>
		/// Gets or sets the <see cref="T:Newtonsoft.Json.Schema.JsonSchema" /> of items.
		/// </summary>
		/// <value>The <see cref="T:Newtonsoft.Json.Schema.JsonSchema" /> of items.</value>
		// Token: 0x1700017E RID: 382
		// (get) Token: 0x060008C0 RID: 2240 RVA: 0x000250B6 File Offset: 0x000232B6
		// (set) Token: 0x060008C1 RID: 2241 RVA: 0x000250BE File Offset: 0x000232BE
		public IList<JsonSchema> Items { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether items in an array are validated using the <see cref="T:Newtonsoft.Json.Schema.JsonSchema" /> instance at their array position from <see cref="P:Newtonsoft.Json.Schema.JsonSchema.Items" />.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if items are validated using their array position; otherwise, <c>false</c>.
		/// </value>
		// Token: 0x1700017F RID: 383
		// (get) Token: 0x060008C2 RID: 2242 RVA: 0x000250C7 File Offset: 0x000232C7
		// (set) Token: 0x060008C3 RID: 2243 RVA: 0x000250CF File Offset: 0x000232CF
		public bool PositionalItemsValidation { get; set; }

		/// <summary>
		/// Gets or sets the <see cref="T:Newtonsoft.Json.Schema.JsonSchema" /> of additional items.
		/// </summary>
		/// <value>The <see cref="T:Newtonsoft.Json.Schema.JsonSchema" /> of additional items.</value>
		// Token: 0x17000180 RID: 384
		// (get) Token: 0x060008C4 RID: 2244 RVA: 0x000250D8 File Offset: 0x000232D8
		// (set) Token: 0x060008C5 RID: 2245 RVA: 0x000250E0 File Offset: 0x000232E0
		public JsonSchema AdditionalItems { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether additional items are allowed.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if additional items are allowed; otherwise, <c>false</c>.
		/// </value>
		// Token: 0x17000181 RID: 385
		// (get) Token: 0x060008C6 RID: 2246 RVA: 0x000250E9 File Offset: 0x000232E9
		// (set) Token: 0x060008C7 RID: 2247 RVA: 0x000250F1 File Offset: 0x000232F1
		public bool AllowAdditionalItems { get; set; }

		/// <summary>
		/// Gets or sets whether the array items must be unique.
		/// </summary>
		// Token: 0x17000182 RID: 386
		// (get) Token: 0x060008C8 RID: 2248 RVA: 0x000250FA File Offset: 0x000232FA
		// (set) Token: 0x060008C9 RID: 2249 RVA: 0x00025102 File Offset: 0x00023302
		public bool UniqueItems { get; set; }

		/// <summary>
		/// Gets or sets the <see cref="T:Newtonsoft.Json.Schema.JsonSchema" /> of properties.
		/// </summary>
		/// <value>The <see cref="T:Newtonsoft.Json.Schema.JsonSchema" /> of properties.</value>
		// Token: 0x17000183 RID: 387
		// (get) Token: 0x060008CA RID: 2250 RVA: 0x0002510B File Offset: 0x0002330B
		// (set) Token: 0x060008CB RID: 2251 RVA: 0x00025113 File Offset: 0x00023313
		public IDictionary<string, JsonSchema> Properties { get; set; }

		/// <summary>
		/// Gets or sets the <see cref="T:Newtonsoft.Json.Schema.JsonSchema" /> of additional properties.
		/// </summary>
		/// <value>The <see cref="T:Newtonsoft.Json.Schema.JsonSchema" /> of additional properties.</value>
		// Token: 0x17000184 RID: 388
		// (get) Token: 0x060008CC RID: 2252 RVA: 0x0002511C File Offset: 0x0002331C
		// (set) Token: 0x060008CD RID: 2253 RVA: 0x00025124 File Offset: 0x00023324
		public JsonSchema AdditionalProperties { get; set; }

		/// <summary>
		/// Gets or sets the pattern properties.
		/// </summary>
		/// <value>The pattern properties.</value>
		// Token: 0x17000185 RID: 389
		// (get) Token: 0x060008CE RID: 2254 RVA: 0x0002512D File Offset: 0x0002332D
		// (set) Token: 0x060008CF RID: 2255 RVA: 0x00025135 File Offset: 0x00023335
		public IDictionary<string, JsonSchema> PatternProperties { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether additional properties are allowed.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if additional properties are allowed; otherwise, <c>false</c>.
		/// </value>
		// Token: 0x17000186 RID: 390
		// (get) Token: 0x060008D0 RID: 2256 RVA: 0x0002513E File Offset: 0x0002333E
		// (set) Token: 0x060008D1 RID: 2257 RVA: 0x00025146 File Offset: 0x00023346
		public bool AllowAdditionalProperties { get; set; }

		/// <summary>
		/// Gets or sets the required property if this property is present.
		/// </summary>
		/// <value>The required property if this property is present.</value>
		// Token: 0x17000187 RID: 391
		// (get) Token: 0x060008D2 RID: 2258 RVA: 0x0002514F File Offset: 0x0002334F
		// (set) Token: 0x060008D3 RID: 2259 RVA: 0x00025157 File Offset: 0x00023357
		public string Requires { get; set; }

		/// <summary>
		/// Gets or sets the a collection of valid enum values allowed.
		/// </summary>
		/// <value>A collection of valid enum values allowed.</value>
		// Token: 0x17000188 RID: 392
		// (get) Token: 0x060008D4 RID: 2260 RVA: 0x00025160 File Offset: 0x00023360
		// (set) Token: 0x060008D5 RID: 2261 RVA: 0x00025168 File Offset: 0x00023368
		public IList<JToken> Enum { get; set; }

		/// <summary>
		/// Gets or sets disallowed types.
		/// </summary>
		/// <value>The disallowed types.</value>
		// Token: 0x17000189 RID: 393
		// (get) Token: 0x060008D6 RID: 2262 RVA: 0x00025171 File Offset: 0x00023371
		// (set) Token: 0x060008D7 RID: 2263 RVA: 0x00025179 File Offset: 0x00023379
		public JsonSchemaType? Disallow { get; set; }

		/// <summary>
		/// Gets or sets the default value.
		/// </summary>
		/// <value>The default value.</value>
		// Token: 0x1700018A RID: 394
		// (get) Token: 0x060008D8 RID: 2264 RVA: 0x00025182 File Offset: 0x00023382
		// (set) Token: 0x060008D9 RID: 2265 RVA: 0x0002518A File Offset: 0x0002338A
		public JToken Default { get; set; }

		/// <summary>
		/// Gets or sets the collection of <see cref="T:Newtonsoft.Json.Schema.JsonSchema" /> that this schema extends.
		/// </summary>
		/// <value>The collection of <see cref="T:Newtonsoft.Json.Schema.JsonSchema" /> that this schema extends.</value>
		// Token: 0x1700018B RID: 395
		// (get) Token: 0x060008DA RID: 2266 RVA: 0x00025193 File Offset: 0x00023393
		// (set) Token: 0x060008DB RID: 2267 RVA: 0x0002519B File Offset: 0x0002339B
		public IList<JsonSchema> Extends { get; set; }

		/// <summary>
		/// Gets or sets the format.
		/// </summary>
		/// <value>The format.</value>
		// Token: 0x1700018C RID: 396
		// (get) Token: 0x060008DC RID: 2268 RVA: 0x000251A4 File Offset: 0x000233A4
		// (set) Token: 0x060008DD RID: 2269 RVA: 0x000251AC File Offset: 0x000233AC
		public string Format { get; set; }

		// Token: 0x1700018D RID: 397
		// (get) Token: 0x060008DE RID: 2270 RVA: 0x000251B5 File Offset: 0x000233B5
		// (set) Token: 0x060008DF RID: 2271 RVA: 0x000251BD File Offset: 0x000233BD
		internal string Location { get; set; }

		// Token: 0x1700018E RID: 398
		// (get) Token: 0x060008E0 RID: 2272 RVA: 0x000251C6 File Offset: 0x000233C6
		internal string InternalId
		{
			get
			{
				return this._internalId;
			}
		}

		// Token: 0x1700018F RID: 399
		// (get) Token: 0x060008E1 RID: 2273 RVA: 0x000251CE File Offset: 0x000233CE
		// (set) Token: 0x060008E2 RID: 2274 RVA: 0x000251D6 File Offset: 0x000233D6
		internal string DeferredReference { get; set; }

		// Token: 0x17000190 RID: 400
		// (get) Token: 0x060008E3 RID: 2275 RVA: 0x000251DF File Offset: 0x000233DF
		// (set) Token: 0x060008E4 RID: 2276 RVA: 0x000251E7 File Offset: 0x000233E7
		internal bool ReferencesResolved { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Schema.JsonSchema" /> class.
		/// </summary>
		// Token: 0x060008E5 RID: 2277 RVA: 0x000251F0 File Offset: 0x000233F0
		public JsonSchema()
		{
			this.AllowAdditionalProperties = true;
			this.AllowAdditionalItems = true;
		}

		/// <summary>
		/// Reads a <see cref="T:Newtonsoft.Json.Schema.JsonSchema" /> from the specified <see cref="T:Newtonsoft.Json.JsonReader" />.
		/// </summary>
		/// <param name="reader">The <see cref="T:Newtonsoft.Json.JsonReader" /> containing the JSON Schema to read.</param>
		/// <returns>The <see cref="T:Newtonsoft.Json.Schema.JsonSchema" /> object representing the JSON Schema.</returns>
		// Token: 0x060008E6 RID: 2278 RVA: 0x00025229 File Offset: 0x00023429
		public static JsonSchema Read(JsonReader reader)
		{
			return JsonSchema.Read(reader, new JsonSchemaResolver());
		}

		/// <summary>
		/// Reads a <see cref="T:Newtonsoft.Json.Schema.JsonSchema" /> from the specified <see cref="T:Newtonsoft.Json.JsonReader" />.
		/// </summary>
		/// <param name="reader">The <see cref="T:Newtonsoft.Json.JsonReader" /> containing the JSON Schema to read.</param>
		/// <param name="resolver">The <see cref="T:Newtonsoft.Json.Schema.JsonSchemaResolver" /> to use when resolving schema references.</param>
		/// <returns>The <see cref="T:Newtonsoft.Json.Schema.JsonSchema" /> object representing the JSON Schema.</returns>
		// Token: 0x060008E7 RID: 2279 RVA: 0x00025236 File Offset: 0x00023436
		public static JsonSchema Read(JsonReader reader, JsonSchemaResolver resolver)
		{
			ValidationUtils.ArgumentNotNull(reader, "reader");
			ValidationUtils.ArgumentNotNull(resolver, "resolver");
			return new JsonSchemaBuilder(resolver).Read(reader);
		}

		/// <summary>
		/// Load a <see cref="T:Newtonsoft.Json.Schema.JsonSchema" /> from a string that contains JSON Schema.
		/// </summary>
		/// <param name="json">A <see cref="T:System.String" /> that contains JSON Schema.</param>
		/// <returns>A <see cref="T:Newtonsoft.Json.Schema.JsonSchema" /> populated from the string that contains JSON Schema.</returns>
		// Token: 0x060008E8 RID: 2280 RVA: 0x0002525A File Offset: 0x0002345A
		public static JsonSchema Parse(string json)
		{
			return JsonSchema.Parse(json, new JsonSchemaResolver());
		}

		/// <summary>
		/// Load a <see cref="T:Newtonsoft.Json.Schema.JsonSchema" /> from a string that contains JSON Schema using the specified <see cref="T:Newtonsoft.Json.Schema.JsonSchemaResolver" />.
		/// </summary>
		/// <param name="json">A <see cref="T:System.String" /> that contains JSON Schema.</param>
		/// <param name="resolver">The resolver.</param>
		/// <returns>A <see cref="T:Newtonsoft.Json.Schema.JsonSchema" /> populated from the string that contains JSON Schema.</returns>
		// Token: 0x060008E9 RID: 2281 RVA: 0x00025268 File Offset: 0x00023468
		public static JsonSchema Parse(string json, JsonSchemaResolver resolver)
		{
			ValidationUtils.ArgumentNotNull(json, "json");
			JsonSchema result;
			using (JsonReader jsonReader = new JsonTextReader(new StringReader(json)))
			{
				result = JsonSchema.Read(jsonReader, resolver);
			}
			return result;
		}

		/// <summary>
		/// Writes this schema to a <see cref="T:Newtonsoft.Json.JsonWriter" />.
		/// </summary>
		/// <param name="writer">A <see cref="T:Newtonsoft.Json.JsonWriter" /> into which this method will write.</param>
		// Token: 0x060008EA RID: 2282 RVA: 0x000252B4 File Offset: 0x000234B4
		public void WriteTo(JsonWriter writer)
		{
			this.WriteTo(writer, new JsonSchemaResolver());
		}

		/// <summary>
		/// Writes this schema to a <see cref="T:Newtonsoft.Json.JsonWriter" /> using the specified <see cref="T:Newtonsoft.Json.Schema.JsonSchemaResolver" />.
		/// </summary>
		/// <param name="writer">A <see cref="T:Newtonsoft.Json.JsonWriter" /> into which this method will write.</param>
		/// <param name="resolver">The resolver used.</param>
		// Token: 0x060008EB RID: 2283 RVA: 0x000252C2 File Offset: 0x000234C2
		public void WriteTo(JsonWriter writer, JsonSchemaResolver resolver)
		{
			ValidationUtils.ArgumentNotNull(writer, "writer");
			ValidationUtils.ArgumentNotNull(resolver, "resolver");
			new JsonSchemaWriter(writer, resolver).WriteSchema(this);
		}

		/// <summary>
		/// Returns a <see cref="T:System.String" /> that represents the current <see cref="T:System.Object" />.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.String" /> that represents the current <see cref="T:System.Object" />.
		/// </returns>
		// Token: 0x060008EC RID: 2284 RVA: 0x000252E8 File Offset: 0x000234E8
		public override string ToString()
		{
			StringWriter stringWriter = new StringWriter(CultureInfo.InvariantCulture);
			this.WriteTo(new JsonTextWriter(stringWriter)
			{
				Formatting = Formatting.Indented
			});
			return stringWriter.ToString();
		}

		// Token: 0x0400030E RID: 782
		private readonly string _internalId = Guid.NewGuid().ToString("N");
	}
}
